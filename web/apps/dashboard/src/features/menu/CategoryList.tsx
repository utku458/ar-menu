import type { ManagedMenuCategoryResponse } from '@armenu/api-client';
import { GridList, GridListItem, useDragAndDrop } from 'react-aria-components';

import { useCurrentUser } from '../../app/workspace.ts';
import { useI18n } from '../../i18n/i18n-context.ts';
import { Button } from '../../ui/Button.tsx';
import { GripIcon } from '../../ui/icons.tsx';
import { useNotify } from '../../ui/toaster-context.ts';
import { useMenuMutations } from './menu-api.ts';
import { displayText, reorder } from './menu-editing.ts';
import { RowActions } from './RowActions.tsx';

interface CategoryListProps {
  readonly categories: readonly ManagedMenuCategoryResponse[];
  readonly selectedId: string | undefined;
  readonly canEdit: boolean;
  readonly onSelect: (categoryId: string) => void;
  readonly onEdit: (category: ManagedMenuCategoryResponse) => void;
  readonly onDelete: (category: ManagedMenuCategoryResponse) => void;
}

/** Categories in menu order. Dragging (or the keyboard: Enter on the handle, arrows, Enter) reorders them. */
export function CategoryList({
  categories,
  selectedId,
  canEdit,
  onSelect,
  onEdit,
  onDelete,
}: CategoryListProps) {
  const { messages, describeError } = useI18n();
  const { tenant } = useCurrentUser();
  const { reorderCategories } = useMenuMutations();
  const notify = useNotify();

  const { dragAndDropHooks } = useDragAndDrop({
    getItems: (keys) => [...keys].map((key) => ({ 'text/plain': String(key) })),
    onReorder: (event) => {
      const ids = reorder(
        categories.map((category) => category.id),
        new Set([...event.keys].map(String)),
        String(event.target.key),
        event.target.dropPosition === 'before' ? 'before' : 'after',
      );
      reorderCategories.mutate(ids, {
        onSuccess: () => {
          notify(messages.orderSaved);
        },
        onError: (error) => {
          notify(describeError((error as { code?: string }).code), 'error');
        },
      });
    },
  });

  return (
    <GridList
      aria-label={messages.categories}
      items={categories}
      selectionMode="single"
      selectionBehavior="replace"
      disallowEmptySelection
      selectedKeys={selectedId === undefined ? [] : [selectedId]}
      onSelectionChange={(keys) => {
        const [key] = keys === 'all' ? [] : [...keys];
        if (key !== undefined) {
          onSelect(String(key));
        }
      }}
      {...(canEdit ? { dragAndDropHooks } : {})}
      className="flex flex-col gap-1 self-start rounded-xl border border-line bg-surface p-2 outline-none"
    >
      {(category) => {
        const name = displayText(category.name, tenant.defaultCulture);
        return (
          <GridListItem
            id={category.id}
            textValue={name}
            className="flex cursor-default items-center gap-1 rounded-lg px-2 py-2 outline-none data-[dragging]:opacity-50 data-[focus-visible]:outline-2 data-[focus-visible]:outline-accent data-[hovered]:bg-sunken data-[selected]:bg-accent-soft"
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
            <div className="min-w-0 flex-1 px-1">
              <p className="truncate text-sm font-medium">{name}</p>
              <p className="text-xs text-ink-muted">
                {messages.itemCount(category.items.length)}
                {!category.isVisible && ` · ${messages.hidden}`}
              </p>
            </div>
            {canEdit && (
              <RowActions
                name={name}
                onEdit={() => {
                  onEdit(category);
                }}
                onDelete={() => {
                  onDelete(category);
                }}
              />
            )}
          </GridListItem>
        );
      }}
    </GridList>
  );
}
