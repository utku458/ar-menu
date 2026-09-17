import type { ManagedMenuCategoryResponse, ManagedMenuItemResponse } from '@armenu/api-client';
import { formatPrice } from '@armenu/locale';
import { GridList, GridListItem, useDragAndDrop } from 'react-aria-components';

import { useCurrentUser } from '../../app/workspace.ts';
import { useI18n } from '../../i18n/i18n-context.ts';
import { Button } from '../../ui/Button.tsx';
import { Switch } from '../../ui/fields.tsx';
import { CubeIcon, GripIcon } from '../../ui/icons.tsx';
import { useNotify } from '../../ui/toaster-context.ts';
import { useMenuMutations } from './menu-api.ts';
import { displayText, reorder } from './menu-editing.ts';
import { RowActions } from './RowActions.tsx';

interface ItemListProps {
  readonly category: ManagedMenuCategoryResponse;
  readonly canEdit: boolean;
  readonly onEdit: (item: ManagedMenuItemResponse) => void;
  readonly onDelete: (item: ManagedMenuItemResponse) => void;
}

/** Dishes of one category. Everyone can mark them sold out; editors also open, reorder and delete them. */
export function ItemList({ category, canEdit, onEdit, onDelete }: ItemListProps) {
  const { messages, language, describeError } = useI18n();
  const { tenant } = useCurrentUser();
  const { reorderItems, setAvailability } = useMenuMutations();
  const notify = useNotify();

  const { dragAndDropHooks } = useDragAndDrop({
    getItems: (keys) => [...keys].map((key) => ({ 'text/plain': String(key) })),
    onReorder: (event) => {
      const ids = reorder(
        category.items.map((item) => item.id),
        new Set([...event.keys].map(String)),
        String(event.target.key),
        event.target.dropPosition === 'before' ? 'before' : 'after',
      );
      reorderItems.mutate(
        { categoryId: category.id, ids },
        {
          onSuccess: () => {
            notify(messages.orderSaved);
          },
          onError: (error) => {
            notify(describeError((error as { code?: string }).code), 'error');
          },
        },
      );
    },
  });

  const categoryName = displayText(category.name, tenant.defaultCulture);

  return (
    <GridList
      aria-label={`${messages.items}: ${categoryName}`}
      items={category.items}
      {...(canEdit
        ? {
            dragAndDropHooks,
            onAction: (key) => {
              const item = category.items.find((candidate) => candidate.id === key);
              if (item !== undefined) {
                onEdit(item);
              }
            },
          }
        : {})}
      renderEmptyState={() => (
        <p className="px-4 py-10 text-center text-sm text-ink-muted">{messages.emptyCategory}</p>
      )}
      className="divide-y divide-line rounded-xl border border-line bg-surface outline-none"
    >
      {(item) => {
        const name = displayText(item.name, tenant.defaultCulture);
        return (
          <GridListItem
            id={item.id}
            textValue={name}
            className="flex cursor-default items-center gap-3 px-3 py-3 outline-none first:rounded-t-xl last:rounded-b-xl data-[dragging]:opacity-50 data-[focus-visible]:outline-2 data-[focus-visible]:-outline-offset-2 data-[focus-visible]:outline-accent data-[hovered]:bg-sunken/60"
          >
            {canEdit && (
              <Button
                slot="drag"
                variant="ghost"
                size="icon"
                aria-label={messages.reorder(name)}
                className="text-ink-muted"
              >
                <GripIcon />
              </Button>
            )}
            <div className="grid size-12 shrink-0 place-items-center overflow-hidden rounded-lg bg-sunken">
              {item.arModel?.posterUrl ? (
                <img
                  src={item.arModel.posterUrl}
                  alt=""
                  className="size-full object-contain"
                  loading="lazy"
                />
              ) : item.arModel !== null ? (
                <CubeIcon className="size-5 text-ink-muted" />
              ) : null}
            </div>
            <div className="min-w-0 flex-1">
              <p className="truncate text-sm font-medium">{name}</p>
              <p className="flex flex-wrap items-center gap-x-2 text-xs text-ink-muted">
                <span className="tabular-nums">{formatPrice(item.price, tenant.currency, language)}</span>
                {item.arModel !== null && (
                  <span className="rounded bg-accent-soft px-1.5 font-medium text-accent">3D</span>
                )}
                {!item.isVisible && <span>{messages.hidden}</span>}
                {!item.isAvailable && <span className="font-medium text-danger">{messages.soldOut}</span>}
              </p>
            </div>
            <Switch
              isSelected={item.isAvailable}
              onChange={(isAvailable) => {
                setAvailability.mutate(
                  { id: item.id, isAvailable },
                  {
                    onError: (error) => {
                      notify(describeError((error as { code?: string }).code), 'error');
                    },
                  },
                );
              }}
            >
              <span className="sr-only">{messages.availableLabel(name)}</span>
            </Switch>
            {canEdit && (
              <RowActions
                name={name}
                onEdit={() => {
                  onEdit(item);
                }}
                onDelete={() => {
                  onDelete(item);
                }}
              />
            )}
          </GridListItem>
        );
      }}
    </GridList>
  );
}
