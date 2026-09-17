export interface TokenGrant {
  readonly accessToken: string;
  /** Seconds until the token expires, measured by the server: immune to a wrong clock on this device. */
  readonly expiresIn: number;
}

/** How a session talks to the authentication endpoints of one business. */
export interface SessionTransport {
  signIn(email: string, password: string): Promise<TokenGrant>;
  /** A new token from the refresh cookie, or undefined when there is no valid session to refresh. */
  refresh(): Promise<TokenGrant | undefined>;
  signOut(): Promise<void>;
}

export interface SessionOptions {
  readonly now?: () => number;
  /** Serializes refreshes across tabs; defaults to the Web Locks API. */
  readonly lock?: <T>(name: string, task: () => Promise<T>) => Promise<T>;
}

/** Tokens are renewed this long before they expire, so a request never leaves with a token about to lapse. */
export const refreshMarginMs = 60_000;

/**
 * The staff session of one business in this tab. The access token is kept in memory only, out of reach of storage
 * reads by injected scripts; after a reload the HttpOnly refresh cookie restores it.
 */
export class Session {
  readonly #transport: SessionTransport;
  readonly #now: () => number;
  readonly #lock: <T>(name: string, task: () => Promise<T>) => Promise<T>;
  readonly #lockName: string;
  readonly #listeners = new Set<() => void>();
  #token: { readonly value: string; readonly expiresAt: number } | undefined;
  #refreshing: Promise<string | undefined> | undefined;

  constructor(workspace: string, transport: SessionTransport, options: SessionOptions = {}) {
    this.#transport = transport;
    this.#now = options.now ?? Date.now;
    this.#lock = options.lock ?? withWebLock;
    // Refreshes rotate the shared cookie: two tabs refreshing at once would present the same token twice.
    this.#lockName = `armenu:refresh:${workspace}`;
  }

  get isSignedIn(): boolean {
    return this.#token !== undefined;
  }

  async signIn(email: string, password: string): Promise<void> {
    this.accept(await this.#transport.signIn(email, password));
  }

  /** Starts the session from a token obtained elsewhere (sign-up). */
  accept(grant: TokenGrant): void {
    this.#token = { value: grant.accessToken, expiresAt: this.#now() + grant.expiresIn * 1000 };
    this.#notify();
  }

  /**
   * A token that stays valid for the next request, renewed when close to expiry. Resolves to undefined when the
   * session is over; rejects when the network fails, which does not end the session.
   */
  accessToken(): Promise<string | undefined> {
    if (this.#token !== undefined && this.#token.expiresAt - this.#now() > refreshMarginMs) {
      return Promise.resolve(this.#token.value);
    }

    this.#refreshing ??= this.#lock(this.#lockName, () => this.#transport.refresh())
      .then((grant) => {
        if (grant === undefined) {
          this.end();
          return undefined;
        }

        this.accept(grant);
        return grant.accessToken;
      })
      .finally(() => {
        this.#refreshing = undefined;
      });

    return this.#refreshing;
  }

  /** A token issued now, carrying what changed since the current one (a verified address, a new role). */
  renew(): Promise<string | undefined> {
    this.#token = undefined;
    return this.accessToken();
  }

  async signOut(): Promise<void> {
    this.end();
    await this.#transport.signOut();
  }

  /** Forgets the token, for example after the API rejected it (revoked session, removed membership). */
  end(): void {
    if (this.#token !== undefined) {
      this.#token = undefined;
      this.#notify();
    }
  }

  subscribe(listener: () => void): () => void {
    this.#listeners.add(listener);
    return () => this.#listeners.delete(listener);
  }

  #notify(): void {
    for (const listener of this.#listeners) {
      listener();
    }
  }
}

function withWebLock<T>(name: string, task: () => Promise<T>): Promise<T> {
  return 'locks' in navigator ? navigator.locks.request(name, task) : task();
}
