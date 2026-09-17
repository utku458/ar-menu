import type { ManagedMenuCategoryResponse } from '@armenu/api-client';
import { useState } from 'react';
import { Form } from 'react-aria-components';

import { type FormErrors, formErrorsFrom, noFormErrors } from '../../api/form-errors.ts';
import { useCurrentUser } from '../../app/workspace.ts';
import { useI18n } from '../../i18n/i18n-context.ts';
import { Button } from '../../ui/Button.tsx';
import { Switch } from '../../ui/fields.tsx';
import { FormAlert, Modal } from '../../ui/Modal.tsx';
import { useNotify } from '../../ui/toaster-context.ts';
import { useMenuMutations } from './menu-api.ts';
import { orderedCultures, readTranslations, withTranslationErrors } from './menu-editing.ts';
import { TranslationFields } from './TranslationFields.tsx';

interface CategoryDialogProps {
  /** The category to edit; undefined creates one. */
  readonly category: ManagedMenuCategoryResponse | undefined;
  readonly onClose: () => void;
}

export function CategoryDialog({ category, onClose }: CategoryDialogProps) {
  const { messages, describeError } = useI18n();
  const { tenant } = useCurrentUser();
  const { createCategory, updateCategory } = useMenuMutations();
  const notify = useNotify();
  const [errors, setErrors] = useState<FormErrors>(noFormErrors);
  const cultures = orderedCultures(tenant.defaultCulture, tenant.supportedCultures);

  const save = async (form: HTMLFormElement) => {
    const data = new FormData(form);
    const description = readTranslations(data, 'description', cultures);
    const body = {
      name: readTranslations(data, 'name', cultures),
      description: Object.keys(description).length === 0 ? null : description,
      isVisible: data.get('isVisible') === 'on',
    };

    try {
      await (category === undefined
        ? createCategory.mutateAsync(body)
        : updateCategory.mutateAsync({ id: category.id, body }));
      notify(messages.saved);
      onClose();
    } catch (error) {
      setErrors(formErrorsFrom(error, { describe: describeError, networkError: messages.networkError }));
    }
  };

  return (
    <Modal
      title={category === undefined ? messages.newCategory : messages.editCategory}
      isOpen
      onOpenChange={(isOpen) => {
        if (!isOpen) {
          onClose();
        }
      }}
    >
      <Form
        className="flex flex-col gap-5"
        validationErrors={withTranslationErrors(
          errors.fields,
          ['name', 'description'],
          tenant.defaultCulture,
        )}
        onSubmit={(event) => {
          event.preventDefault();
          void save(event.currentTarget);
        }}
      >
        <TranslationFields
          field="name"
          label={messages.name}
          cultures={cultures}
          defaultCulture={tenant.defaultCulture}
          values={category?.name}
          isRequired
          maxLength={100}
        />
        <TranslationFields
          field="description"
          label={messages.description}
          cultures={cultures}
          defaultCulture={tenant.defaultCulture}
          values={category?.description}
          multiline
          maxLength={500}
        />
        {cultures.length > 1 && <p className="text-xs text-ink-muted">{messages.translationsHint}</p>}
        <Switch name="isVisible" defaultSelected={category?.isVisible ?? true}>
          {messages.visibleToGuests}
        </Switch>
        {errors.form !== undefined && <FormAlert message={errors.form} />}
        <div className="flex justify-end gap-2 border-t border-line pt-4">
          <Button onPress={onClose}>{messages.cancel}</Button>
          <Button
            type="submit"
            variant="primary"
            isPending={createCategory.isPending || updateCategory.isPending}
          >
            {messages.save}
          </Button>
        </div>
      </Form>
    </Modal>
  );
}
