import { readFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';

import type {
  AccountDeletionResponse,
  ArModelProcessingResponse,
  CurrentUserResponse,
  HistoryEntryResponse,
  ManagedMenuResponse,
  TeamResponse,
  TeamRole,
} from '@armenu/api-client';
import type { Page, Request, Route } from '@playwright/test';

/** Origins from .env.e2e and the fake storage. The browser never reaches a real server. */
export const apiOrigin = 'http://localhost:5999';
export const storageOrigin = 'http://localhost:5998';
export const dashboardOrigin = 'http://localhost:4175';

export const slug = 'kadikoy-burger-lab';
export const password = 'correct-horse-battery';
export const ids = {
  burgers: '0198a1f2-0000-7000-8000-0000000000c1',
  drinks: '0198a1f2-0000-7000-8000-0000000000c2',
  smashBurger: '0198a1f2-0000-7000-8000-000000000001',
  owner: '0198a1f2-0000-7000-8000-0000000000bb',
  ownerMembership: '0198a1f2-0000-7000-8000-0000000000d1',
  staffMembership: '0198a1f2-0000-7000-8000-0000000000d2',
  staff: '0198a1f2-0000-7000-8000-0000000000bc',
  truffleBurger: '0198a1f2-0000-7000-8000-000000000002',
  lemonade: '0198a1f2-0000-7000-8000-000000000003',
} as const;

const demoModel = fileURLToPath(new URL('../../../../../assets/demo/models/sea-bass.glb', import.meta.url));
const refreshCookie = 'armenu_refresh';

/** The secret of the one invitation link that works, as it would arrive in an e-mail. */
export const invitationToken = '0198a1f2000070008000000000000e01.c2VjcmV0LXNlY3JldC1zZWNyZXQtc2VjcmV0LXNlY3I';

type Role = 'Owner' | 'Manager' | 'Staff';

/** The two people who can sign in; their roles can change (handing ownership over). */
type Person = 'owner' | 'staff';

interface RecordedRequest {
  readonly method: string;
  readonly path: string;
  readonly body: unknown;
  readonly headers: Readonly<Record<string, string>>;
}

/**
 * An in-memory ArMenu API and object storage, answering in the browser. Authentication behaves like the real API: an
 * access token in the body, a rotating HttpOnly refresh cookie scoped to the business's auth endpoints.
 */
export class FakeApi {
  readonly requests: RecordedRequest[] = [];
  readonly uploads: { readonly contentType: string | undefined; readonly size: number }[] = [];
  readonly menu: ManagedMenuResponse = initialMenu();
  readonly processings = new Map<string, ArModelProcessingResponse>();
  /** How the next processing ends, after one poll that still finds it running. */
  processingOutcome: { readonly failureCode: string } | 'succeeded' = 'succeeded';
  /** Whether the signed-in owner's address is confirmed. */
  emailVerified = true;
  /** The one reset or verification link secret that works, as it would arrive in an e-mail. */
  readonly accountLinkToken = '0198a1f2000070008000000000000f01.YWNjb3VudC1saW5rLXNlY3JldC1zZWNyZXQtc2VjcmV0';
  /** Guest activity the statistics endpoint reports: menu views per day, newest last. */
  dailyMenuViews: number[] = [];
  /** Whether the mail server accepts invitation e-mails. */
  emailDelivers = true;
  readonly team: TeamResponse = {
    members: [
      {
        id: ids.ownerMembership,
        userId: ids.owner,
        fullName: 'Deniz Yılmaz',
        email: `owner@${slug}.test`,
        role: 'Owner',
        joinedAt: '2026-09-01T09:00:00Z',
        lastSignedInAt: null,
      },
      {
        id: ids.staffMembership,
        userId: ids.staff,
        fullName: 'Ece Kaya',
        email: `staff@${slug}.test`,
        // A user-name account: no mailbox, so the owner is the one who resets its password.
        userName: 'staff',
        role: 'Staff',
        joinedAt: '2026-09-03T09:00:00Z',
        lastSignedInAt: null,
      },
    ],
    invitations: [],
  };
  tenant = {
    id: '0198a1f2-0000-7000-8000-0000000000aa',
    slug,
    name: 'Kadıköy Burger Lab',
    defaultCulture: 'tr',
    supportedCultures: ['tr', 'en'],
    currency: 'TRY',
    timeZone: 'Europe/Istanbul',
    logoPath: null as string | null,
    logoUrl: null as string | null,
    accentColor: null as string | null,
  };
  readonly roles: Record<Person, Role> = { owner: 'Owner', staff: 'Staff' };
  /** Newest first, as the API pages them. */
  readonly history: HistoryEntryResponse[] = initialHistory();
  /** People whose accounts were deleted: nothing of theirs works anymore. */
  readonly deleted = new Set<Person>();

  private readonly refreshTokens = new Map<string, Person>();
  private readonly accessTokens = new Map<string, Person>();
  private sequence = 0;
  private readonly page: Page;

  constructor(page: Page) {
    this.page = page;
  }

  requestsTo(method: string, path: string): RecordedRequest[] {
    return this.requests.filter((request) => request.method === method && request.path === path);
  }

  async install(): Promise<void> {
    await this.page.route(`${apiOrigin}/**`, (route) => this.answer(route));
    await this.page.route(`${storageOrigin}/**`, async (route) => {
      const request = route.request();
      const cors = {
        'Access-Control-Allow-Origin': dashboardOrigin,
        'Access-Control-Allow-Methods': 'GET, PUT',
        'Access-Control-Allow-Headers': '*',
      };
      if (request.method() === 'OPTIONS') {
        await route.fulfill({ status: 200, headers: cors });
      } else if (request.method() === 'PUT') {
        this.uploads.push({
          contentType: request.headers()['content-type'],
          size: request.postDataBuffer()?.byteLength ?? 0,
        });
        await route.fulfill({ status: 200, headers: cors });
      } else {
        await route.fulfill({
          status: 200,
          headers: { ...cors, 'Content-Type': 'model/gltf-binary' },
          body: await readFile(demoModel),
        });
      }
    });
  }

  private async answer(route: Route): Promise<void> {
    const request = route.request();
    const url = new URL(request.url());
    const cors = {
      'Access-Control-Allow-Origin': dashboardOrigin,
      'Access-Control-Allow-Credentials': 'true',
      'Access-Control-Allow-Headers': 'authorization, content-type',
      'Access-Control-Allow-Methods': 'GET, POST, PUT, DELETE',
    };

    if (request.method() === 'OPTIONS') {
      await route.fulfill({ status: 204, headers: cors });
      return;
    }

    const body = parseBody(request);
    this.requests.push({ method: request.method(), path: url.pathname, body, headers: request.headers() });

    const reply = (status: number, json?: unknown, headers: Record<string, string> = {}) =>
      route.fulfill({
        status,
        headers: {
          ...cors,
          ...headers,
          ...(json === undefined ? {} : { 'Content-Type': 'application/json' }),
        },
        ...(json === undefined ? {} : { body: JSON.stringify(json) }),
      });
    const problem = (status: number, code: string, extra: object = {}) =>
      reply(status, { status, code, detail: code, ...extra });

    const auth = `/api/v1/tenants/${slug}/auth`;
    switch (`${request.method()} ${url.pathname}`) {
      // The user-name sign-in the dashboard uses: no business in the URL, the answer says which one it found.
      case 'POST /api/v1/auth/sign-in': {
        const { userName, password: given } = body as { userName: string; password: string };
        const person = userName === 'owner' ? 'owner' : userName === 'staff' ? 'staff' : undefined;
        if (person === undefined || given !== password || this.deleted.has(person)) {
          await problem(401, 'auth.invalid_credentials');
          return;
        }
        await reply(
          200,
          { ...this.grant(person), workspace: slug, isPlatformAdmin: false },
          { 'Set-Cookie': this.cookie(person) },
        );
        return;
      }
      case `POST ${auth}/sign-in`: {
        const { email, password: given } = body as { email: string; password: string };
        const person =
          email === `owner@${slug}.test` ? 'owner' : email === `staff@${slug}.test` ? 'staff' : undefined;
        if (person === undefined || given !== password || this.deleted.has(person)) {
          await problem(401, 'auth.invalid_credentials');
          return;
        }
        await reply(200, this.grant(person), { 'Set-Cookie': this.cookie(person) });
        return;
      }
      case `POST ${auth}/refresh`: {
        const token = readCookie(request, refreshCookie);
        const person = token === undefined ? undefined : this.refreshTokens.get(token);
        if (token === undefined || person === undefined || this.deleted.has(person)) {
          await problem(401, 'auth.invalid_refresh_token');
          return;
        }
        this.refreshTokens.delete(token);
        await reply(200, this.grant(person), { 'Set-Cookie': this.cookie(person) });
        return;
      }
      case `POST /api/v1/tenants/${slug}/invitations/lookup`:
        if ((body as { token: string }).token !== invitationToken) {
          await problem(404, 'invitation.invalid_link');
          return;
        }
        await reply(200, {
          businessName: this.tenant.name,
          email: `yeni@${slug}.test`,
          role: 'Staff',
          expiresAt: '2030-01-01T00:00:00Z',
          hasAccount: false,
        });
        return;
      case `POST /api/v1/tenants/${slug}/invitations/accept`: {
        const { token, password: chosen } = body as { token: string; password: string };
        if (token !== invitationToken) {
          await problem(404, 'invitation.invalid_link');
        } else if (chosen.length < 12) {
          await problem(400, 'validation.failed', {
            errors: { password: ['Too short.'] },
            errorCodes: { password: ['auth.password_too_short'] },
          });
        } else {
          await reply(200, this.grant('staff'), { 'Set-Cookie': this.cookie('staff') });
        }
        return;
      }
      case 'POST /api/v1/auth/password-reset':
        await reply(202);
        return;
      case 'POST /api/v1/auth/password-reset/confirm':
      case 'POST /api/v1/auth/email-verification/confirm':
        if ((body as { token: string }).token === this.accountLinkToken) {
          await reply(204);
        } else {
          await problem(409, 'user_token.expired');
        }
        return;
      case `POST ${auth}/sign-out`:
        await reply(204, undefined, {
          'Set-Cookie': `${refreshCookie}=; Path=${auth}; Max-Age=0; HttpOnly; SameSite=Strict; Secure`,
        });
        return;
    }

    const person = this.accessTokens.get((request.headers().authorization ?? '').replace('Bearer ', ''));
    if (person === undefined || this.deleted.has(person)) {
      await problem(401, 'unauthorized');
      return;
    }

    // Like the real API, a token carries the role it was issued with, until it is refreshed.
    const role = this.roles[person];
    const isOwnAccount = url.pathname.startsWith('/api/v1/me');
    const isEditing = request.method() !== 'GET' && !url.pathname.endsWith('/availability') && !isOwnAccount;
    if (
      (role === 'Staff' && (isEditing || url.pathname.startsWith('/api/v1/manage/history'))) ||
      (role !== 'Owner' && url.pathname.startsWith('/api/v1/manage/team'))
    ) {
      await problem(403, 'forbidden');
      return;
    }

    if (request.method() === 'GET' && url.pathname === '/api/v1/manage/menu/export') {
      await route.fulfill({
        status: 200,
        headers: {
          ...cors,
          'Access-Control-Expose-Headers': 'Content-Disposition',
          'Content-Type': 'text/csv; charset=utf-8',
          'Content-Disposition': `attachment; filename=${slug}-menu-2026-09-15.csv; filename*=UTF-8''${slug}-menu-2026-09-15.csv`,
        },
        body: `\uFEFFid,category,name:tr,name:en,price\r\n${ids.lemonade},İçecekler,Limonata,,120.00\r\n`,
      });
      return;
    }

    if (isOwnAccount && (await this.answerAccount(person, request.method(), url, body, reply, problem))) {
      return;
    }

    await this.answerMenu(person, role, request.method(), url, body, reply, problem);
  }

  private async answerAccount(
    person: Person,
    method: string,
    url: URL,
    body: unknown,
    reply: (status: number, json?: unknown) => Promise<void>,
    problem: (status: number, code: string, extra?: object) => Promise<void>,
  ): Promise<boolean> {
    if (method === 'GET' && url.pathname === '/api/v1/me/deletion') {
      await reply(200, this.deletionFor(person));
    } else if (method === 'POST' && url.pathname === '/api/v1/me/deletion') {
      if ((body as { password: string }).password !== password) {
        await problem(400, 'validation.failed', wrongPassword);
      } else if (this.deletionFor(person).workspacesToHandOver.length > 0) {
        await problem(409, 'account.ownership_transfer_required');
      } else {
        this.deleted.add(person);
        await reply(204);
      }
    } else {
      return false;
    }

    return true;
  }

  /** An owner with a team must hand it over; staff leave; everyone also works at the fish restaurant. */
  private deletionFor(person: Person): AccountDeletionResponse {
    const own = { slug, name: this.tenant.name, otherMembers: this.team.members.length - 1 };
    const elsewhere = { slug: 'bogazici-balikcisi', name: 'Boğaziçi Balıkçısı', otherMembers: 3 };
    const isOwner = this.roles[person] === 'Owner';
    return {
      closingWorkspaces: isOwner && own.otherMembers === 0 ? [own] : [],
      workspacesToHandOver: isOwner && own.otherMembers > 0 ? [own] : [],
      workspacesToLeave: isOwner ? [elsewhere] : [own, elsewhere],
    };
  }

  private async answerMenu(
    person: Person,
    role: Role,
    method: string,
    url: URL,
    body: unknown,
    reply: (status: number, json?: unknown) => Promise<void>,
    problem: (status: number, code: string, extra?: object) => Promise<void>,
  ): Promise<void> {
    const path = url.pathname;
    const item = (id: string | undefined) =>
      this.menu.categories.flatMap((category) => category.items).find((candidate) => candidate.id === id);
    const itemId = /\/items\/([^/]+)/.exec(path)?.[1];

    if (method === 'GET' && path === '/api/v1/me') {
      const me: CurrentUserResponse = {
        id: ids.owner,
        email: `owner@${slug}.test`,
        emailVerified: this.emailVerified,
        fullName: 'Deniz Yılmaz',
        role,
        userName: null,
        isPlatformAdmin: false,
        tenant: this.tenant,
      };
      await reply(
        200,
        person === 'staff'
          ? { ...me, id: ids.staff, email: `staff@${slug}.test`, fullName: 'Ece Kaya', emailVerified: true }
          : me,
      );
    } else if (method === 'GET' && path === '/api/v1/manage/menu') {
      await reply(200, this.menu);
    } else if (method === 'PUT' && path.endsWith('/availability')) {
      const target = item(itemId);
      if (target !== undefined) {
        target.isAvailable = (body as { isAvailable: boolean }).isAvailable;
      }
      await reply(204);
    } else if (method === 'PUT' && /^\/api\/v1\/manage\/menu\/items\/[^/]+$/.test(path)) {
      const update = body as { price: number; allergens?: string[] | null; dietaryLabels?: string[] };
      if (update.price > 100_000) {
        await problem(400, 'validation.failed', {
          errors: { price: ['Too large.'] },
          errorCodes: { price: ['money.amount_too_large'] },
        });
        return;
      }
      const target = item(itemId);
      if (update.allergens?.includes('milk') === true && update.dietaryLabels?.includes('vegan') === true) {
        await problem(400, 'validation.failed', {
          errors: { dietaryLabels: ['Contradiction.'] },
          errorCodes: { dietaryLabels: ['menu_item.dietary_label_contradicts_allergen'] },
        });
        return;
      }
      if (target !== undefined) {
        target.price = update.price;
        target.allergens = update.allergens ?? null;
        target.dietaryLabels = update.dietaryLabels ?? [];
      }
      await reply(204);
    } else if (method === 'PUT' && path.endsWith('/items/order')) {
      const category = this.menu.categories.find((candidate) => path.includes(candidate.id));
      if (category !== undefined) {
        const order = (body as { ids: string[] }).ids;
        category.items = order.flatMap((id) => category.items.filter((candidate) => candidate.id === id));
      }
      await reply(204);
    } else if (method === 'POST' && path === '/api/v1/manage/assets/uploads') {
      const uploadId = `0198a1f2-0000-7000-8000-${String(++this.sequence).padStart(12, '0')}`;
      const { contentType } = body as { contentType: string };
      await reply(200, {
        uploadId,
        url: `${storageOrigin}/uploads/${uploadId}`,
        method: 'PUT',
        headers: { 'Content-Type': contentType },
        expiresAt: '2030-01-01T00:00:00Z',
      });
    } else if (method === 'POST' && path.endsWith('/publish')) {
      const uploadId = path.split('/').at(-2);
      await reply(200, {
        path: `tenants/aa/assets/${uploadId}.webp`,
        url: `${storageOrigin}/assets/${uploadId}.webp`,
        contentType: 'image/webp',
        size: 1,
      });
    } else if (method === 'PUT' && path.endsWith('/ar-model')) {
      await reply(204);
    } else if (method === 'POST' && path.endsWith('/ar-model/processing') && itemId !== undefined) {
      const processing: ArModelProcessingResponse = {
        id: `0198a1f2-0000-7000-8000-${String(++this.sequence).padStart(12, '0')}`,
        status: 'queued',
        attempts: 0,
        failureCode: null,
        report: null,
        createdAt: '2026-09-15T10:00:00Z',
        completedAt: null,
      };
      this.processings.set(itemId, processing);
      await reply(202, processing);
    } else if (method === 'GET' && path.endsWith('/ar-model/processing') && itemId !== undefined) {
      const processing = this.processings.get(itemId);
      if (processing === undefined) {
        await problem(404, 'ar_model_processing.not_found');
        return;
      }
      await reply(200, processing);
      // The worker picks the job up after the first look, and finishes before the next one.
      this.processings.set(itemId, this.advance(itemId, processing));
    } else if (method === 'GET' && path === '/api/v1/me/workspaces') {
      await reply(200, [
        { slug, name: this.tenant.name, role },
        { slug: 'bogazici-balikcisi', name: 'Boğaziçi Balıkçısı', role: 'Staff' },
      ]);
    } else if (method === 'POST' && path === '/api/v1/me/email-verification') {
      await reply(202);
    } else if (method === 'POST' && path === '/api/v1/manage/menu/import') {
      const dryRun = url.searchParams.get('dryRun') === 'true';
      const lines = String(body).trim().split(/\r?\n/).slice(1);
      const errors = lines.flatMap((line, index) =>
        line.includes('belki')
          ? [
              {
                line: index + 2,
                column: 'available',
                code: 'menu_import.flag_invalid',
                message: 'Write yes or no.',
              },
            ]
          : [],
      );
      const changes =
        errors.length > 0
          ? []
          : [{ line: 2, itemId: ids.lemonade, name: 'Limonata', kind: 'updated', fields: ['price'] }];
      if (!dryRun && errors.length === 0) {
        const lemonade = item(ids.lemonade);
        if (lemonade !== undefined) {
          lemonade.price = 135;
        }
      }
      await reply(200, {
        applied: !dryRun && errors.length === 0,
        created: 0,
        updated: changes.length,
        unchanged: Math.max(0, lines.length - changes.length),
        changes,
        errors,
      });
    } else if (method === 'GET' && path === '/api/v1/manage/history') {
      const limit = Number(url.searchParams.get('limit') ?? 50);
      const before = url.searchParams.get('before');
      const start = before === null ? 0 : this.history.findIndex((entry) => entry.id === before) + 1;
      const entries = this.history.slice(start, start + limit);
      const hasMore = start + limit < this.history.length;
      await reply(200, { entries, nextCursor: hasMore ? (entries.at(-1)?.id ?? null) : null });
    } else if (method === 'PUT' && path === '/api/v1/manage/settings/time-zone') {
      const { timeZone } = body as { timeZone: string };
      if (!Intl.supportedValuesOf('timeZone').includes(timeZone) && timeZone !== 'UTC') {
        await problem(400, 'validation.failed', {
          errors: { timeZone: ['Unknown.'] },
          errorCodes: { timeZone: ['tenant.time_zone_invalid'] },
        });
        return;
      }
      this.tenant.timeZone = timeZone;
      await reply(204);
    } else if (method === 'GET' && path === '/api/v1/manage/statistics') {
      await reply(200, this.statistics());
    } else if (path.startsWith('/api/v1/manage/team')) {
      await this.answerTeam(method, path, body, reply, problem);
    } else if (method === 'PUT' && path === '/api/v1/manage/settings/languages') {
      const { defaultCulture, supportedCultures } = body as {
        defaultCulture: string;
        supportedCultures: string[];
      };
      this.tenant.defaultCulture = defaultCulture;
      this.tenant.supportedCultures = supportedCultures;
      await reply(204);
    } else if (method === 'PUT' && path === '/api/v1/manage/settings/branding') {
      const { name, logoPath, accentColor } = body as {
        name: string;
        logoPath: string | null;
        accentColor: string | null;
      };
      if (name.trim() === '') {
        await problem(400, 'validation.failed', {
          errors: { name: ['Required.'] },
          errorCodes: { name: ['tenant.name_required'] },
        });
        return;
      }
      this.tenant = {
        ...this.tenant,
        name,
        logoPath,
        logoUrl: logoPath === null ? null : `https://assets.test/${logoPath}`,
        accentColor,
      };
      await reply(204);
    } else {
      await problem(404, 'not_found');
    }
  }

  private statistics() {
    const days = this.dailyMenuViews.map((menuViews, index) => ({
      day: `2026-09-${String(9 + index).padStart(2, '0')}`,
      menuViews,
      dishOpens: Math.round(menuViews / 2),
      arStarts: Math.round(menuViews / 5),
    }));
    const total = (key: 'menuViews' | 'dishOpens' | 'arStarts') =>
      days.reduce((sum, day) => sum + day[key], 0);
    return {
      from: days[0]?.day ?? '2026-09-09',
      to: days.at(-1)?.day ?? '2026-09-15',
      totals: {
        menuViews: total('menuViews'),
        dishOpens: total('dishOpens'),
        modelViews: total('arStarts') * 2,
        arStarts: total('arStarts'),
      },
      days,
      dishes:
        days.length === 0
          ? []
          : [
              {
                itemId: ids.smashBurger,
                name: 'Klasik Smash Burger',
                opens: 40,
                modelViews: 30,
                arStarts: 12,
              },
              { itemId: ids.lemonade, name: 'Limonata', opens: 9, modelViews: 0, arStarts: 0 },
            ],
    };
  }

  private async answerTeam(
    method: string,
    path: string,
    body: unknown,
    reply: (status: number, json?: unknown) => Promise<void>,
    problem: (status: number, code: string, extra?: object) => Promise<void>,
  ): Promise<void> {
    const id = /\/(?:invitations|members)\/([^/]+)/.exec(path)?.[1];

    if (method === 'GET' && path === '/api/v1/manage/team') {
      await reply(200, this.team);
    } else if (method === 'POST' && path === '/api/v1/manage/team/invitations') {
      const { email, role } = body as { email: string; role: TeamRole };
      if (this.team.invitations.some((invitation) => invitation.email === email)) {
        await problem(409, 'invitation.already_pending');
        return;
      }
      const invitationId = `0198a1f2-0000-7000-8000-${String(++this.sequence).padStart(12, '0')}`;
      this.team.invitations.unshift({
        id: invitationId,
        email,
        role,
        invitedBy: 'Deniz Yılmaz',
        sentAt: '2026-09-15T10:00:00Z',
        expiresAt: '2026-09-22T10:00:00Z',
        isExpired: false,
      });
      await reply(201, { invitationId, emailSent: this.emailDelivers });
    } else if (method === 'POST' && path === '/api/v1/manage/team/members') {
      const { fullName, userName, role } = body as { fullName: string; userName: string; role: TeamRole };
      if (this.team.members.some((member) => member.userName === userName)) {
        await problem(409, 'user.user_name_taken');
        return;
      }
      const membershipId = `0198a1f2-0000-7000-8000-${String(++this.sequence).padStart(12, '0')}`;
      this.team.members.push({
        id: membershipId,
        userId: `0198a1f2-0000-7000-8000-${String(++this.sequence).padStart(12, '0')}`,
        fullName,
        email: `${userName}@users.armenu.invalid`,
        userName,
        role,
        joinedAt: '2026-09-20T09:00:00Z',
        lastSignedInAt: null,
      });
      await reply(201, { membershipId });
    } else if (method === 'PUT' && path.endsWith('/password') && path.includes('/members/')) {
      await reply(this.team.members.some((member) => member.id === id) ? 204 : 404);
    } else if (method === 'POST' && path.endsWith('/resend') && id !== undefined) {
      await reply(200, { invitationId: id, emailSent: this.emailDelivers });
    } else if (method === 'DELETE' && path.includes('/invitations/')) {
      this.team.invitations = this.team.invitations.filter((invitation) => invitation.id !== id);
      await reply(204);
    } else if (method === 'PUT' && path.endsWith('/role')) {
      const member = this.team.members.find((candidate) => candidate.id === id);
      if (member !== undefined) {
        member.role = (body as { role: TeamRole }).role;
      }
      await reply(204);
    } else if (method === 'POST' && path.endsWith('/ownership')) {
      if ((body as { password: string }).password !== password) {
        await problem(400, 'validation.failed', wrongPassword);
        return;
      }
      const successor = this.team.members.find((candidate) => candidate.id === id);
      const owner = this.team.members.find((candidate) => candidate.role === 'Owner');
      if (successor === undefined || owner === undefined) {
        await problem(404, 'membership.not_found');
        return;
      }
      successor.role = 'Owner';
      owner.role = 'Manager';
      this.roles.owner = 'Manager';
      this.roles.staff = 'Owner';
      await reply(204);
    } else if (method === 'DELETE' && path.includes('/members/')) {
      this.team.members = this.team.members.filter((member) => member.id !== id);
      await reply(204);
    } else {
      await problem(404, 'not_found');
    }
  }

  private advance(itemId: string, processing: ArModelProcessingResponse): ArModelProcessingResponse {
    if (processing.status === 'queued') {
      return { ...processing, status: 'processing', attempts: 1 };
    }
    if (processing.status !== 'processing') {
      return processing;
    }

    const completedAt = '2026-09-15T10:00:40Z';
    if (this.processingOutcome !== 'succeeded') {
      return {
        ...processing,
        status: 'failed',
        failureCode: this.processingOutcome.failureCode,
        completedAt,
      };
    }

    const stem = `tenants/aa/assets/${processing.id}`;
    const target = this.menu.categories
      .flatMap((category) => category.items)
      .find((candidate) => candidate.id === itemId);
    if (target !== undefined) {
      target.arModel = {
        glbPath: `${stem}.glb`,
        sceneViewerGlbPath: `${stem}.scene-viewer.glb`,
        usdzPath: `${stem}.usdz`,
        posterPath: `${stem}.webp`,
        glbUrl: `${storageOrigin}/${stem}.glb`,
        sceneViewerGlbUrl: `${storageOrigin}/${stem}.scene-viewer.glb`,
        usdzUrl: `${storageOrigin}/${stem}.usdz`,
        posterUrl: `${storageOrigin}/${stem}.webp`,
      };
    }

    return {
      ...processing,
      status: 'succeeded',
      completedAt,
      report: {
        source: { triangles: 412_000, vertices: 208_400, materials: 2, textures: 3, maxTextureSize: 4096 },
        optimized: { triangles: 99_620, vertices: 51_250, materials: 2, textures: 3, maxTextureSize: 2048 },
        files: {
          source: 18_350_211,
          model: 1_342_877,
          sceneViewerModel: 3_921_064,
          appleModel: 4_102_345,
          poster: 38_112,
        },
        dimensions: { width: 0.182, height: 0.114, depth: 0.179 },
        warnings: ['model.simplified', 'model.textures_downscaled'],
      },
    };
  }

  private grant(person: Person) {
    const accessToken = `access-${person}-${++this.sequence}`;
    this.accessTokens.set(accessToken, person);
    return { accessToken, expiresAt: '2030-01-01T00:00:00Z', expiresIn: 900, tokenType: 'Bearer' };
  }

  private cookie(person: Person): string {
    const token = `refresh-${person}-${++this.sequence}`;
    this.refreshTokens.set(token, person);
    return `${refreshCookie}=${token}; Path=/api/v1/tenants/${slug}/auth; HttpOnly; SameSite=Strict; Secure`;
  }
}

const wrongPassword = {
  errors: { password: ['The password is incorrect.'] },
  errorCodes: { password: ['account.password_incorrect'] },
};

/** Three recent changes, then enough older ones to need a second page. */
function initialHistory(): HistoryEntryResponse[] {
  const deniz = { id: ids.owner, fullName: 'Deniz Yılmaz' };
  const ece = { id: ids.staff, fullName: 'Ece Kaya' };
  const lemonade = { type: 'menu_item', id: ids.lemonade, name: { tr: 'Limonata' }, person: null };
  const smash = {
    type: 'menu_item',
    id: ids.smashBurger,
    name: { tr: 'Klasik Smash Burger', en: 'Classic Smash Burger' },
    person: null,
  };
  const id = (index: number) => `0198a1f2-0000-7000-8000-${String(900_000 + index).padStart(12, '0')}`;

  return [
    {
      id: id(0),
      occurredAt: '2026-09-15T11:42:00Z',
      actor: deniz,
      action: 'updated',
      subject: smash,
      changes: [
        { field: 'price', before: 365, after: 385 },
        { field: 'description', before: null, after: { tr: 'Çift smash köfte, cheddar' } },
      ],
    },
    {
      id: id(1),
      occurredAt: '2026-09-15T09:05:00Z',
      actor: ece,
      action: 'updated',
      subject: lemonade,
      changes: [{ field: 'available', before: true, after: false }],
    },
    {
      id: id(2),
      occurredAt: '2026-09-14T20:30:00Z',
      actor: null,
      action: 'updated',
      subject: smash,
      changes: [{ field: 'model', before: false, after: true }],
    },
    ...Array.from({ length: 49 }, (_, index) => ({
      id: id(3 + index),
      occurredAt: new Date(Date.UTC(2026, 8, 13, 18, 0) - index * 3_600_000).toISOString(),
      actor: ece,
      action: 'updated',
      subject: lemonade,
      changes: [{ field: 'available', before: index % 2 === 0, after: index % 2 !== 0 }],
    })),
  ];
}

function parseBody(request: Request): unknown {
  const text = request.postData();
  if (text === null) {
    return undefined;
  }

  try {
    return JSON.parse(text) as unknown;
  } catch {
    return text;
  }
}

function readCookie(request: Request, name: string): string | undefined {
  return (request.headers().cookie ?? '')
    .split(';')
    .map((part) => part.trim().split('='))
    .find(([key]) => key === name)?.[1];
}

function initialMenu(): ManagedMenuResponse {
  return {
    categories: [
      {
        id: ids.burgers,
        name: { tr: 'Burgerler', en: 'Burgers' },
        description: null,
        displayOrder: 0,
        isVisible: true,
        items: [
          {
            id: ids.smashBurger,
            name: { tr: 'Klasik Smash Burger', en: 'Classic Smash Burger' },
            description: { tr: 'Çift smash köfte, cheddar' },
            price: 385,
            displayOrder: 0,
            isVisible: true,
            isAvailable: true,
            arModel: null,
            allergens: null,
            dietaryLabels: [],
          },
          {
            id: ids.truffleBurger,
            name: { tr: 'Trüflü Mantar Burger', en: 'Truffle Mushroom Burger' },
            description: null,
            price: 445,
            displayOrder: 1,
            isVisible: true,
            isAvailable: true,
            arModel: null,
            allergens: null,
            dietaryLabels: [],
          },
        ],
      },
      {
        id: ids.drinks,
        name: { tr: 'İçecekler', en: 'Drinks' },
        description: null,
        displayOrder: 1,
        isVisible: true,
        items: [
          {
            id: ids.lemonade,
            name: { tr: 'Limonata' },
            description: null,
            price: 120,
            displayOrder: 0,
            isVisible: true,
            isAvailable: true,
            arModel: null,
            allergens: null,
            dietaryLabels: [],
          },
        ],
      },
    ],
  };
}
