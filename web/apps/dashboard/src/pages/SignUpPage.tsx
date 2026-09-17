import { ApiError, createApiClient } from '@armenu/api-client';
import { languageName } from '@armenu/locale';
import { Link, useNavigate } from '@tanstack/react-router';
import { useState } from 'react';
import { Form } from 'react-aria-components';

import { type FormErrors, formErrorsFrom, noFormErrors } from '../api/form-errors.ts';
import { unwrap } from '../api/result.ts';
import { saveLastWorkspace } from '../auth/last-workspace.ts';
import { slugify } from '../auth/slug.ts';
import { workspaceFor } from '../auth/workspaces.ts';
import { env } from '../env.ts';
import { deviceTimeZone } from '../features/settings/time-zones.ts';
import { useI18n } from '../i18n/i18n-context.ts';
import { formText } from '../ui/form-data.ts';
import { Button } from '../ui/Button.tsx';
import { SelectField, TextField } from '../ui/fields.tsx';
import { AuthLayout } from '../ui/layout.tsx';
import { FormAlert } from '../ui/Modal.tsx';

const minimumPasswordLength = 12;
const cultures = ['tr', 'en', 'de', 'ru', 'ar', 'fr', 'es', 'it'];
const currencies = ['TRY', 'EUR', 'USD', 'GBP'];

// Conflicts are found by the database, not by field validation; they still belong next to a field.
const conflictFields: Readonly<Record<string, string>> = {
  'tenant.slug_taken': 'slug',
  'user.email_taken': 'ownerEmail',
};

export function SignUpPage() {
  const { messages, language, describeError } = useI18n();
  const navigate = useNavigate();
  const [slug, setSlug] = useState('');
  const [isSlugEdited, setIsSlugEdited] = useState(false);
  const [errors, setErrors] = useState<FormErrors>(noFormErrors);
  const [isPending, setIsPending] = useState(false);

  const signUp = async (form: HTMLFormElement) => {
    const data = new FormData(form);
    const body = {
      businessName: formText(data, 'businessName'),
      slug,
      defaultCulture: formText(data, 'defaultCulture'),
      currency: formText(data, 'currency'),
      ownerFullName: formText(data, 'ownerFullName'),
      ownerEmail: formText(data, 'ownerEmail'),
      password: formText(data, 'password'),
      // Where the business's days begin; the owner can change it in settings.
      timeZone: deviceTimeZone(),
    };

    setIsPending(true);
    try {
      // The response sets the owner's refresh cookie and carries the first access token.
      const created = unwrap(
        await createApiClient(env.apiBaseUrl).POST('/api/v1/tenants', { body, credentials: 'include' }),
      );
      workspaceFor(created.slug).session.accept({
        accessToken: created.accessToken,
        expiresIn: created.accessTokenExpiresIn,
      });
      saveLastWorkspace(created.slug);
      await navigate({ to: '/$workspace/menu', params: { workspace: created.slug } });
    } catch (error) {
      const field = error instanceof ApiError ? conflictFields[error.code ?? ''] : undefined;
      setErrors(
        field === undefined
          ? formErrorsFrom(error, { describe: describeError, networkError: messages.networkError })
          : { fields: { [field]: describeError((error as ApiError).code) }, form: undefined },
      );
    } finally {
      setIsPending(false);
    }
  };

  return (
    <AuthLayout>
      <h1 className="text-2xl font-semibold tracking-tight">{messages.signUpTitle}</h1>

      <Form
        className="mt-8 flex flex-col gap-5"
        validationErrors={errors.fields}
        onSubmit={(event) => {
          event.preventDefault();
          void signUp(event.currentTarget);
        }}
      >
        <TextField
          name="businessName"
          label={messages.businessName}
          autoComplete="organization"
          isRequired
          onChange={(name) => {
            if (!isSlugEdited) {
              setSlug(slugify(name));
            }
          }}
        />
        <TextField
          name="slug"
          label={messages.menuAddress}
          description={messages.menuAddressDescription(`${env.guestMenuBaseUrl}${slug === '' ? '…' : slug}`)}
          value={slug}
          onChange={(value) => {
            setIsSlugEdited(true);
            setSlug(value.toLowerCase());
          }}
          isRequired
          minLength={3}
          maxLength={63}
        />
        <div className="grid grid-cols-2 gap-4">
          <SelectField
            name="defaultCulture"
            label={messages.menuLanguage}
            options={cultures.map((culture) => ({ id: culture, label: languageName(culture) }))}
            defaultValue={language}
            isRequired
          />
          <SelectField
            name="currency"
            label={messages.currency}
            options={currencies.map((currency) => ({ id: currency, label: currency }))}
            defaultValue="TRY"
            isRequired
          />
        </div>
        <TextField name="ownerFullName" label={messages.fullName} autoComplete="name" isRequired />
        <TextField name="ownerEmail" type="email" label={messages.email} autoComplete="email" isRequired />
        <TextField
          name="password"
          type="password"
          label={messages.password}
          description={messages.passwordDescription(minimumPasswordLength)}
          autoComplete="new-password"
          minLength={minimumPasswordLength}
          isRequired
        />
        {errors.form !== undefined && <FormAlert message={errors.form} />}
        <Button type="submit" variant="primary" isPending={isPending}>
          {messages.createBusinessAction}
        </Button>
      </Form>

      <p className="mt-8 text-sm text-ink-muted">
        {messages.haveAccount}{' '}
        <Link
          to="/"
          search={{ choose: true }}
          className="font-medium text-accent underline-offset-4 hover:underline"
        >
          {messages.signIn}
        </Link>
      </p>
    </AuthLayout>
  );
}
