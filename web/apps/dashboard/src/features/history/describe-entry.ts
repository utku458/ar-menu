import type { HistoryChange, HistoryEntryResponse, HistoryPerson } from '@armenu/api-client';
import { languageName } from '@armenu/locale';

import type { Messages } from '../../i18n/messages.ts';
import { displayText } from '../menu/menu-editing.ts';

export interface HistoryContext {
  readonly messages: Messages;
  readonly defaultCulture: string;
  readonly currency: string;
  readonly locale: string;
  /** The current name of a category, for changes that record its id. */
  readonly categoryName: (id: string) => string | undefined;
}

export interface DescribedChange {
  readonly field: string;
  readonly label: string;
  readonly before: string;
  readonly after: string;
}

export interface DescribedEntry {
  readonly actor: string;
  readonly summary: string;
  readonly changes: readonly DescribedChange[];
}

const empty = '—';

/** A history entry as a sentence ("Deniz Yılmaz" + "edited “Limonata”") and its changes as before and after. */
export function describeEntry(entry: HistoryEntryResponse, context: HistoryContext): DescribedEntry {
  const { messages } = context;
  const text = messages.history;
  const name = displayText(entry.subject.name, context.defaultCulture) || empty;
  const person = personName(entry.subject.person, messages);
  const only = entry.changes.length === 1 ? entry.changes[0] : undefined;

  let summary: string;
  switch (`${entry.subject.type} ${entry.action}`) {
    case 'menu_item created':
      summary = text.itemCreated(name);
      break;
    case 'menu_item deleted':
      summary = text.itemDeleted(name);
      break;
    case 'menu_item updated':
      summary =
        only?.field === 'available'
          ? only.after === false
            ? text.itemSoldOut(name)
            : text.itemBackOnSale(name)
          : text.itemUpdated(name);
      break;
    case 'menu_category created':
      summary = text.categoryCreated(name);
      break;
    case 'menu_category deleted':
      summary = text.categoryDeleted(name);
      break;
    case 'menu_category updated':
      summary = text.categoryUpdated(name);
      break;
    case 'menu_category reordered':
      summary = text.itemsReordered(
        entry.subject.name === null ? (context.categoryName(entry.subject.id) ?? empty) : name,
      );
      break;
    case 'menu reordered':
      summary = text.categoriesReordered;
      break;
    case 'member created':
      summary = text.memberJoined;
      break;
    case 'member updated':
      summary = text.memberRoleChanged(person);
      break;
    case 'member deleted':
      summary = entry.actor?.id === entry.subject.id ? text.memberLeft : text.memberRemoved(person);
      break;
    default:
      summary = text.businessUpdated;
  }

  // Availability is the whole story of its sentence; repeating it below would say it twice.
  const changes =
    only?.field === 'available' ? [] : entry.changes.map((change) => describeChange(change, context));

  return {
    actor: entry.actor === null ? messages.system : personName(entry.actor, messages),
    summary,
    changes,
  };
}

export function personName(person: HistoryPerson | null, messages: Messages): string {
  return person?.fullName ?? messages.deletedAccount;
}

function describeChange(change: HistoryChange, context: HistoryContext): DescribedChange {
  const { messages } = context;
  const text = messages.history;
  const format = (value: unknown): string => formatValue(change.field, value, context);

  // A file replaced by another is present before and after: say what happened instead.
  const replaced =
    (change.field === 'photo' || change.field === 'model' || change.field === 'logo') &&
    change.before === true &&
    change.after === true;

  return {
    field: change.field,
    label: text.fields[change.field] ?? change.field,
    before: format(change.before),
    after: replaced ? text.replaced : format(change.after),
  };
}

function formatValue(field: string, value: unknown, context: HistoryContext): string {
  const { messages, locale } = context;
  const text = messages.history;

  if (value === null || value === undefined) {
    // Allergens nobody entered are not "none": the difference is the whole point of recording them.
    return field === 'allergens' ? text.notDeclared : empty;
  }

  switch (field) {
    case 'name':
    case 'description':
      return isTexts(value) ? displayText(value, context.defaultCulture) || empty : asText(value);
    case 'price':
      return typeof value === 'number'
        ? new Intl.NumberFormat(locale, { style: 'currency', currency: context.currency }).format(value)
        : asText(value);
    case 'category':
      return context.categoryName(asText(value)) ?? empty;
    case 'photo':
    case 'model':
    case 'logo':
      return value === true ? text.present : text.absent;
    case 'visible':
      return value === true ? text.visible : text.hidden;
    case 'available':
      return value === true ? text.onSale : text.soldOut;
    case 'defaultLanguage':
      return languageName(asText(value));
    case 'languages':
      return Array.isArray(value)
        ? value.map((code) => languageName(asText(code))).join(', ')
        : asText(value);
    case 'allergens':
      return Array.isArray(value)
        ? value.length === 0
          ? text.noAllergens
          : value.map((code) => messages.allergenNames[asText(code)] ?? asText(code)).join(', ')
        : asText(value);
    case 'dietaryLabels':
      return Array.isArray(value)
        ? value.map((code) => messages.dietaryLabelNames[asText(code)] ?? asText(code)).join(', ') || empty
        : asText(value);
    case 'timeZone':
      return asText(value).replaceAll('_', ' ');
    case 'role':
      return messages.roles[asText(value)] ?? asText(value);
    default:
      return asText(value);
  }
}

function isTexts(value: unknown): value is Readonly<Record<string, string>> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function asText(value: unknown): string {
  return typeof value === 'string' || typeof value === 'number' || typeof value === 'boolean'
    ? String(value)
    : JSON.stringify(value);
}
