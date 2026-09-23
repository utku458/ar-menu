import { useMutation, useQueryClient, useSuspenseQuery } from '@tanstack/react-query';
import { useRouter } from '@tanstack/react-router';
import { useState } from 'react';
import { Form } from 'react-aria-components';

import { type FormErrors, formErrorsFrom, noFormErrors } from '../api/form-errors.ts';
import { unwrap } from '../api/result.ts';
import { administerBusiness, platformWorkspace } from '../auth/workspaces.ts';
import { slugify } from '../auth/slug.ts';
import { businessesQuery } from './platform-api.ts';
import { ChangePasswordCard } from '../features/account/ChangePasswordCard.tsx';
import { formatDate } from '../i18n/format.ts';
import { useI18n } from '../i18n/i18n-context.ts';
import { Button } from '../ui/Button.tsx';
import { SelectField, TextField } from '../ui/fields.tsx';
import { formText } from '../ui/form-data.ts';
import { Card, EmptyState, PageHeader } from '../ui/layout.tsx';
import { FormAlert, Modal } from '../ui/Modal.tsx';
import { useNotify } from '../ui/toaster-context.ts';

/** Every business on the platform: open a new one, or step into one to run it. */
export function PlatformPage() {
  const { messages, language } = useI18n();
  const platform = platformWorkspace();
  const businesses = useSuspenseQuery(businessesQuery(platform)).data;
  const [isOpening, setIsOpening] = useState(false);

  return (
    <main className="mx-auto max-w-4xl px-4 py-10">
      <PageHeader
        title={messages.platformTitle}
        description={messages.platformDescription}
        actions={
          <Button
            variant="primary"
            onPress={() => {
              setIsOpening(true);
            }}
          >
            {messages.openBusiness}
          </Button>
        }
      />

      {businesses.length === 0 ? (
        <EmptyState title={messages.noBusinesses} body={messages.noBusinessesBody} />
      ) : (
        <Card>
          <ul className="divide-y divide-line">
            {businesses.map((business) => (
              <li key={business.id} className="flex flex-wrap items-center justify-between gap-3 px-4 py-3">
                <div className="min-w-0">
                  <p className="font-medium">{business.name}</p>
                  <p className="text-sm text-ink-muted">
                    /m/{business.slug} · {messages.businessStatus(business.status)} ·{' '}
                    {formatDate(business.createdAt, language)}
                  </p>
                </div>
                <EnterBusinessButton id={business.id} name={business.name} />
              </li>
            ))}
          </ul>
        </Card>
      )}

      {/* The administrator has no business to hold an account page, so its own password is changed here. */}
      <div className="mt-8 max-w-2xl">
        <ChangePasswordCard workspace={platform} />
      </div>

      <OpenBusinessDialog
        isOpen={isOpening}
        onClose={() => {
          setIsOpening(false);
        }}
      />
    </main>
  );
}

function EnterBusinessButton({ id, name }: { id: string; name: string }) {
  const { messages, describeError } = useI18n();
  const router = useRouter();
  const notify = useNotify();

  const enter = useMutation({
    mutationFn: async () => {
      const platform = platformWorkspace();
      const entered = unwrap(
        await platform.api.POST('/api/v1/platform/businesses/{businessId}/enter', {
          params: { path: { businessId: id } },
        }),
      );

      const workspace = administerBusiness(id, entered.workspace, entered);
      await router.navigate({ to: '/$workspace/menu', params: { workspace: workspace.slug } });
    },
    onError: (error) => {
      notify(
        formErrorsFrom(error, { describe: describeError, networkError: messages.networkError }).form ??
          messages.networkError,
        'error',
      );
    },
  });

  return (
    <Button
      onPress={() => {
        enter.mutate();
      }}
      isPending={enter.isPending}
      aria-label={messages.manageBusiness(name)}
    >
      {messages.manage}
    </Button>
  );
}

function OpenBusinessDialog({ isOpen, onClose }: { isOpen: boolean; onClose: () => void }) {
  const { messages, describeError, language } = useI18n();
  const queryClient = useQueryClient();
  const notify = useNotify();
  const [errors, setErrors] = useState<FormErrors>(noFormErrors);
  const [slug, setSlug] = useState('');
  const [isSlugEdited, setIsSlugEdited] = useState(false);
  const [culture, setCulture] = useState<string>(language);

  const open = useMutation({
    mutationFn: async (form: HTMLFormElement) => {
      const data = new FormData(form);
      return unwrap(
        await platformWorkspace().api.POST('/api/v1/platform/businesses', {
          body: {
            businessName: formText(data, 'businessName'),
            slug: formText(data, 'slug'),
            defaultCulture: formText(data, 'defaultCulture'),
            currency: formText(data, 'currency'),
            ownerFullName: formText(data, 'ownerFullName'),
            ownerUserName: formText(data, 'ownerUserName'),
            ownerPassword: formText(data, 'ownerPassword'),
            timeZone: Intl.DateTimeFormat().resolvedOptions().timeZone,
          },
        }),
      );
    },
    onSuccess: async (business) => {
      notify(messages.businessOpened(business.slug));
      await queryClient.invalidateQueries({ queryKey: ['platform', 'businesses'] });
      close();
    },
    onError: (error) => {
      setErrors(formErrorsFrom(error, { describe: describeError, networkError: messages.networkError }));
    },
  });

  const close = () => {
    setErrors(noFormErrors);
    setSlug('');
    setIsSlugEdited(false);
    onClose();
  };

  return (
    <Modal
      title={messages.openBusiness}
      isOpen={isOpen}
      onOpenChange={(next) => {
        if (!next) {
          close();
        }
      }}
    >
      <Form
        className="flex flex-col gap-5"
        validationErrors={errors.fields}
        onSubmit={(event) => {
          event.preventDefault();
          open.mutate(event.currentTarget);
        }}
      >
        <TextField
          name="businessName"
          label={messages.businessName}
          isRequired
          // The address is suggested from the name, and left alone once it has been typed in by hand.
          onChange={(value) => {
            if (!isSlugEdited) {
              setSlug(slugify(value));
            }
          }}
        />
        <TextField
          name="slug"
          label={messages.menuAddress}
          description={messages.menuAddressHint}
          value={slug}
          onChange={(value) => {
            setIsSlugEdited(true);
            setSlug(value);
          }}
          isRequired
        />
        <SelectField
          name="defaultCulture"
          label={messages.defaultLanguage}
          options={[
            { id: 'tr', label: 'Türkçe' },
            { id: 'en', label: 'English' },
          ]}
          value={culture}
          onChange={(key) => {
            if (typeof key === 'string') {
              setCulture(key);
            }
          }}
          isRequired
        />
        <TextField name="currency" label={messages.currency} defaultValue="TRY" isRequired />

        <hr className="border-line" />
        <p className="text-sm text-ink-muted">{messages.ownerAccountHint}</p>

        <TextField name="ownerFullName" label={messages.ownerFullName} isRequired />
        <TextField
          name="ownerUserName"
          label={messages.userName}
          description={messages.userNameHint}
          autoComplete="off"
          isRequired
        />
        <TextField
          name="ownerPassword"
          type="password"
          label={messages.password}
          description={messages.passwordHint}
          autoComplete="new-password"
          isRequired
        />

        {errors.form !== undefined && <FormAlert message={errors.form} />}
        <div className="flex justify-end gap-2">
          <Button onPress={close}>{messages.cancel}</Button>
          <Button type="submit" variant="primary" isPending={open.isPending}>
            {messages.openBusiness}
          </Button>
        </div>
      </Form>
    </Modal>
  );
}
