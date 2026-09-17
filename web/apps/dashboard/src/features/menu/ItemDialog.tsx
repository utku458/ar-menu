import type { ManagedMenuCategoryResponse, ManagedMenuItemResponse } from '@armenu/api-client';
import { useState } from 'react';
import { Form, Tab, TabList, TabPanel, Tabs } from 'react-aria-components';

import { type FormErrors, formErrorsFrom, noFormErrors } from '../../api/form-errors.ts';
import { useCurrentUser } from '../../app/workspace.ts';
import { useI18n } from '../../i18n/i18n-context.ts';
import { Button } from '../../ui/Button.tsx';
import { NumberField, SelectField, Switch } from '../../ui/fields.tsx';
import { FormAlert, Modal } from '../../ui/Modal.tsx';
import { formText } from '../../ui/form-data.ts';
import { useNotify } from '../../ui/toaster-context.ts';
import { ArModelPanel } from '../ar/ArModelPanel.tsx';
import { readDietary, withDietaryErrors } from './dietary.ts';
import { DietaryFields } from './DietaryFields.tsx';
import { useMenuMutations } from './menu-api.ts';
import { displayText, orderedCultures, readTranslations, withTranslationErrors } from './menu-editing.ts';
import { TranslationFields } from './TranslationFields.tsx';

interface ItemDialogProps {
  /** The item to edit; undefined creates one in `categoryId`. */
  readonly item: ManagedMenuItemResponse | undefined;
  readonly categoryId: string;
  readonly categories: readonly ManagedMenuCategoryResponse[];
  readonly onClose: () => void;
}

const tab =
  'cursor-default border-b-2 border-transparent px-3 py-2 text-sm font-medium text-ink-muted outline-none ' +
  'data-[hovered]:text-ink data-[selected]:border-accent data-[selected]:text-ink data-[focus-visible]:outline-2 data-[focus-visible]:outline-accent';

export function ItemDialog({ item, categoryId, categories, onClose }: ItemDialogProps) {
  const { messages, describeError } = useI18n();
  const { tenant } = useCurrentUser();
  const { createItem, updateItem } = useMenuMutations();
  const notify = useNotify();
  const [errors, setErrors] = useState<FormErrors>(noFormErrors);
  const cultures = orderedCultures(tenant.defaultCulture, tenant.supportedCultures);
  const itemName = item === undefined ? messages.newItem : displayText(item.name, tenant.defaultCulture);

  const save = async (form: HTMLFormElement) => {
    const data = new FormData(form);
    const description = readTranslations(data, 'description', cultures);
    const details = {
      categoryId: formText(data, 'categoryId') || categoryId,
      name: readTranslations(data, 'name', cultures),
      description: Object.keys(description).length === 0 ? null : description,
      price: Number(formText(data, 'price')),
      ...readDietary(data),
    };

    try {
      await (item === undefined
        ? createItem.mutateAsync(details)
        : updateItem.mutateAsync({
            id: item.id,
            body: { ...details, isVisible: data.get('isVisible') === 'on' },
          }));
      notify(messages.saved);
      onClose();
    } catch (error) {
      setErrors(formErrorsFrom(error, { describe: describeError, networkError: messages.networkError }));
    }
  };

  return (
    <Modal
      title={item === undefined ? messages.newItem : `${messages.editItem}: ${itemName}`}
      size="lg"
      isOpen
      onOpenChange={(isOpen) => {
        if (!isOpen) {
          onClose();
        }
      }}
    >
      <Tabs>
        <TabList aria-label={itemName} className="-mt-2 mb-5 flex gap-2 border-b border-line">
          <Tab id="details" className={tab}>
            {messages.detailsTab}
          </Tab>
          <Tab id="model" className={tab}>
            {messages.modelTab}
          </Tab>
        </TabList>

        <TabPanel id="details" className="outline-none">
          <Form
            className="flex flex-col gap-5"
            validationErrors={withDietaryErrors(
              withTranslationErrors(errors.fields, ['name', 'description'], tenant.defaultCulture),
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
              values={item?.name}
              isRequired
              maxLength={100}
            />
            <TranslationFields
              field="description"
              label={messages.description}
              cultures={cultures}
              defaultCulture={tenant.defaultCulture}
              values={item?.description}
              multiline
              maxLength={500}
            />
            {cultures.length > 1 && <p className="text-xs text-ink-muted">{messages.translationsHint}</p>}

            <div className="grid gap-4 sm:grid-cols-2">
              <NumberField
                name="price"
                label={messages.price}
                {...(item === undefined ? {} : { defaultValue: item.price })}
                minValue={0}
                step={0.01}
                formatOptions={{
                  style: 'currency',
                  currency: tenant.currency,
                  currencyDisplay: 'narrowSymbol',
                }}
                isRequired
              />
              <SelectField
                name="categoryId"
                label={messages.category}
                options={categories.map((category) => ({
                  id: category.id,
                  label: displayText(category.name, tenant.defaultCulture),
                }))}
                defaultValue={categoryId}
                isRequired
              />
            </div>

            <DietaryFields allergens={item?.allergens ?? null} dietaryLabels={item?.dietaryLabels ?? []} />

            {item !== undefined && (
              <Switch name="isVisible" defaultSelected={item.isVisible}>
                {messages.visibleToGuests}
              </Switch>
            )}

            {errors.form !== undefined && <FormAlert message={errors.form} />}
            <div className="flex justify-end gap-2 border-t border-line pt-4">
              <Button onPress={onClose}>{messages.cancel}</Button>
              <Button
                type="submit"
                variant="primary"
                isPending={createItem.isPending || updateItem.isPending}
              >
                {messages.save}
              </Button>
            </div>
          </Form>
        </TabPanel>

        <TabPanel id="model" className="outline-none">
          {item === undefined ? (
            <p className="rounded-lg bg-sunken px-4 py-6 text-center text-sm text-ink-muted">
              {messages.saveItemFirst}
            </p>
          ) : (
            <ArModelPanel item={item} itemName={itemName} />
          )}
        </TabPanel>
      </Tabs>
    </Modal>
  );
}
