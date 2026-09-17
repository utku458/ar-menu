import { Link, useNavigate } from '@tanstack/react-router';
import { Form } from 'react-aria-components';

import { slugify } from '../auth/slug.ts';
import { useI18n } from '../i18n/i18n-context.ts';
import { formText } from '../ui/form-data.ts';
import { Button } from '../ui/Button.tsx';
import { TextField } from '../ui/fields.tsx';
import { AuthLayout } from '../ui/layout.tsx';

export function WorkspacePage() {
  const { messages } = useI18n();
  const navigate = useNavigate();

  return (
    <AuthLayout>
      <h1 className="text-2xl font-semibold tracking-tight">{messages.workspaceTitle}</h1>
      <p className="mt-2 text-sm text-ink-muted">{messages.workspaceIntro}</p>

      <Form
        className="mt-8 flex flex-col gap-5"
        onSubmit={(event) => {
          event.preventDefault();
          const workspace = slugify(formText(new FormData(event.currentTarget), 'workspace'));
          if (workspace !== '') {
            void navigate({ to: '/$workspace', params: { workspace } });
          }
        }}
      >
        <TextField
          name="workspace"
          label={messages.workspaceLabel}
          description={messages.workspaceDescription}
          autoComplete="organization"
          isRequired
        />
        <Button type="submit" variant="primary">
          {messages.continue}
        </Button>
      </Form>

      <p className="mt-8 text-sm text-ink-muted">
        {messages.newToArMenu}{' '}
        <Link to="/signup" className="font-medium text-accent underline-offset-4 hover:underline">
          {messages.createBusiness}
        </Link>
      </p>
    </AuthLayout>
  );
}
