import { useState } from 'react';
import { CheckboxButton, CheckboxField, CheckboxGroup, FieldError, Label, Text } from 'react-aria-components';

import { useI18n } from '../../i18n/i18n-context.ts';
import { Switch } from '../../ui/fields.tsx';
import { allergenCodes, dietaryLabelCodes } from './dietary.ts';

interface DietaryFieldsProps {
  /** The dish's allergens; null when they were never entered. */
  readonly allergens: readonly string[] | null;
  readonly dietaryLabels: readonly string[];
}

const box =
  'flex cursor-default items-center gap-2 rounded-lg border border-line px-2.5 py-1.5 text-sm outline-none ' +
  'data-[selected]:border-accent data-[selected]:bg-accent-soft data-[disabled]:opacity-50 ' +
  'data-[focus-visible]:outline-2 data-[focus-visible]:outline-offset-2 data-[focus-visible]:outline-accent';

function Tick({ isSelected }: { isSelected: boolean }) {
  return (
    <span
      aria-hidden="true"
      className={`grid size-4 shrink-0 place-items-center rounded border text-[10px] leading-none ${
        isSelected ? 'border-accent bg-accent text-accent-ink' : 'border-line'
      }`}
    >
      {isSelected ? '✓' : ''}
    </span>
  );
}

/**
 * The allergens a dish contains and the diets it suits. Whether allergens were entered at all is its own switch: a
 * dish nobody filled in must not reach guests as "contains none of the fourteen".
 */
export function DietaryFields({ allergens, dietaryLabels }: DietaryFieldsProps) {
  const { messages } = useI18n();
  const [declared, setDeclared] = useState(allergens !== null);
  const [selected, setSelected] = useState<string[]>([...(allergens ?? [])]);

  return (
    <fieldset className="flex flex-col gap-4 rounded-xl border border-line p-4">
      <legend className="px-1 text-sm font-semibold">{messages.dietarySection}</legend>

      <div className="flex flex-col gap-1">
        <Switch name="allergensDeclared" isSelected={declared} onChange={setDeclared}>
          {messages.allergensDeclared}
        </Switch>
        <p className="text-xs text-ink-muted">{messages.allergensDeclaredDescription}</p>
      </div>

      <CheckboxGroup
        name="allergens"
        value={selected}
        onChange={setSelected}
        isDisabled={!declared}
        className="flex flex-col gap-2"
      >
        <Label className="text-sm font-medium">{messages.allergensLabel}</Label>
        <div className="flex flex-wrap gap-2">
          {allergenCodes.map((code) => (
            <CheckboxField key={code} value={code} className="flex">
              <CheckboxButton className={box}>
                {({ isSelected }) => (
                  <>
                    <Tick isSelected={isSelected} />
                    {messages.allergenNames[code]}
                  </>
                )}
              </CheckboxButton>
            </CheckboxField>
          ))}
        </div>
        {declared && selected.length === 0 && (
          <Text slot="description" className="text-xs text-ink-muted">
            {messages.noAllergensChecked}
          </Text>
        )}
        <FieldError className="text-sm text-danger" />
      </CheckboxGroup>

      <CheckboxGroup name="dietaryLabels" defaultValue={[...dietaryLabels]} className="flex flex-col gap-2">
        <Label className="text-sm font-medium">{messages.dietaryLabelsLabel}</Label>
        <div className="flex flex-wrap gap-2">
          {dietaryLabelCodes.map((code) => (
            <CheckboxField key={code} value={code} className="flex">
              <CheckboxButton className={box}>
                {({ isSelected }) => (
                  <>
                    <Tick isSelected={isSelected} />
                    {messages.dietaryLabelNames[code]}
                  </>
                )}
              </CheckboxButton>
            </CheckboxField>
          ))}
        </div>
        <FieldError className="text-sm text-danger" />
      </CheckboxGroup>
    </fieldset>
  );
}
