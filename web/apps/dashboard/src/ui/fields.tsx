import type { ReactNode } from 'react';
import {
  Button as AriaButton,
  ComboBox,
  type ComboBoxProps,
  FieldError,
  Group,
  Input,
  Label,
  ListBox,
  ListBoxItem,
  NumberField as AriaNumberField,
  type NumberFieldProps as AriaNumberFieldProps,
  Popover,
  Select,
  type SelectProps,
  SelectValue,
  SwitchButton,
  SwitchField,
  type SwitchFieldProps,
  Text,
  TextArea,
  TextField as AriaTextField,
  type TextFieldProps as AriaTextFieldProps,
} from 'react-aria-components';

import { cx } from './cx.ts';
import { ChevronDownIcon } from './icons.tsx';

const control =
  'w-full rounded-lg border border-line bg-surface px-3 py-2 text-sm text-ink outline-none placeholder:text-ink-muted ' +
  'data-[focused]:border-accent data-[focused]:ring-2 data-[focused]:ring-accent/25 data-[invalid]:border-danger';

interface FieldText {
  readonly label: string;
  readonly description?: ReactNode;
}

function FieldChrome({ label, description, children }: FieldText & { children: ReactNode }) {
  return (
    <>
      <Label className="text-sm font-medium text-ink">{label}</Label>
      {children}
      {description !== undefined && (
        <Text slot="description" className="text-xs text-ink-muted">
          {description}
        </Text>
      )}
      <FieldError className="text-sm text-danger" />
    </>
  );
}

export function TextField({
  label,
  description,
  multiline,
  className,
  ...props
}: AriaTextFieldProps & FieldText & { multiline?: boolean; className?: string }) {
  return (
    <AriaTextField {...props} className={cx('flex flex-col gap-1.5', className)}>
      <FieldChrome label={label} description={description}>
        {multiline === true ? (
          <TextArea rows={3} className={cx(control, 'resize-y')} />
        ) : (
          <Input className={control} />
        )}
      </FieldChrome>
    </AriaTextField>
  );
}

export function NumberField({
  label,
  description,
  className,
  ...props
}: AriaNumberFieldProps & FieldText & { className?: string }) {
  return (
    <AriaNumberField {...props} className={cx('flex flex-col gap-1.5', className)}>
      <FieldChrome label={label} description={description}>
        <Group className="flex">
          <Input className={cx(control, 'tabular-nums')} />
        </Group>
      </FieldChrome>
    </AriaNumberField>
  );
}

export interface Option {
  readonly id: string;
  readonly label: string;
}

export function SelectField({
  label,
  description,
  options,
  className,
  ...props
}: Omit<SelectProps<Option>, 'children'> & FieldText & { options: readonly Option[]; className?: string }) {
  return (
    <Select {...props} className={cx('flex flex-col gap-1.5', className)}>
      <FieldChrome label={label} description={description}>
        <AriaButton
          className={cx(
            control,
            'flex items-center justify-between gap-2 text-start data-[focus-visible]:border-accent',
          )}
        >
          <SelectValue className="truncate data-[placeholder]:text-ink-muted" />
          <ChevronDownIcon className="size-4 text-ink-muted" />
        </AriaButton>
      </FieldChrome>
      <Popover className="min-w-(--trigger-width) overflow-auto rounded-lg border border-line bg-surface p-1 shadow-lg">
        <ListBox items={options} className="outline-none">
          {(option) => (
            <ListBoxItem
              id={option.id}
              textValue={option.label}
              className="cursor-default rounded-md px-3 py-2 text-sm outline-none data-[focused]:bg-sunken data-[selected]:font-semibold"
            >
              {option.label}
            </ListBoxItem>
          )}
        </ListBox>
      </Popover>
    </Select>
  );
}

/** A select that filters its options as you type: for long lists such as time zones. */
export function ComboBoxField({
  label,
  description,
  options,
  className,
  ...props
}: Omit<ComboBoxProps<Option>, 'children' | 'items' | 'defaultItems'> &
  FieldText & { options: readonly Option[]; className?: string }) {
  return (
    <ComboBox
      {...props}
      defaultItems={options}
      menuTrigger="focus"
      className={cx('flex flex-col gap-1.5', className)}
    >
      <FieldChrome label={label} description={description}>
        <Group className="relative flex">
          <Input className={cx(control, 'pe-9')} />
          <AriaButton className="absolute inset-y-0 end-0 flex w-9 items-center justify-center text-ink-muted outline-none">
            <ChevronDownIcon className="size-4" />
          </AriaButton>
        </Group>
      </FieldChrome>
      <Popover className="max-h-72 min-w-(--trigger-width) overflow-auto rounded-lg border border-line bg-surface p-1 shadow-lg">
        <ListBox className="outline-none">
          {(option: Option) => (
            <ListBoxItem
              id={option.id}
              textValue={option.label}
              className="cursor-default rounded-md px-3 py-2 text-sm outline-none data-[focused]:bg-sunken data-[selected]:font-semibold"
            >
              {option.label}
            </ListBoxItem>
          )}
        </ListBox>
      </Popover>
    </ComboBox>
  );
}

export function Switch({
  children,
  className,
  ...props
}: SwitchFieldProps & { children: ReactNode; className?: string }) {
  return (
    <SwitchField {...props} className={cx('flex', className)}>
      <SwitchButton className="group flex cursor-default items-center gap-3 text-sm outline-none">
        <span
          aria-hidden="true"
          className="flex h-6 w-10 shrink-0 items-center rounded-full bg-line p-0.5 transition-colors group-data-[disabled]:opacity-50 group-data-[focus-visible]:outline-2 group-data-[focus-visible]:outline-offset-2 group-data-[focus-visible]:outline-accent group-data-[selected]:bg-success"
        >
          <span className="size-5 rounded-full bg-white shadow transition-transform group-data-[selected]:translate-x-4 rtl:group-data-[selected]:-translate-x-4" />
        </span>
        {children}
      </SwitchButton>
    </SwitchField>
  );
}
