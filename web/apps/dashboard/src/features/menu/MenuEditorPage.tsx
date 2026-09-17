import { useSuspenseQuery } from '@tanstack/react-query';
import { getRouteApi } from '@tanstack/react-router';
import { useState } from 'react';

import { formErrorsFrom } from '../../api/form-errors.ts';
import { useCanEditMenu, useCurrentUser, useWorkspace } from '../../app/workspace.ts';
import { useI18n } from '../../i18n/i18n-context.ts';
import { Button } from '../../ui/Button.tsx';
import { DownloadIcon, PlusIcon, UploadIcon } from '../../ui/icons.tsx';
import { EmptyState, PageHeader } from '../../ui/layout.tsx';
import { ConfirmDialog } from '../../ui/Modal.tsx';
import { useNotify } from '../../ui/toaster-context.ts';
import { CategoryDialog } from './CategoryDialog.tsx';
import { CategoryList } from './CategoryList.tsx';
import { ItemDialog } from './ItemDialog.tsx';
import { ItemList } from './ItemList.tsx';
import { managedMenuQuery, useMenuMutations } from './menu-api.ts';
import { MenuImportDialog } from './MenuImportDialog.tsx';
import { downloadMenu } from './menu-transfer.ts';
import { displayText } from './menu-editing.ts';

const route = getRouteApi('/$workspace/menu');

// Ids only: dialogs always show the latest menu data, including the result of their own saves.
type Editing =
  | { readonly kind: 'none' }
  | { readonly kind: 'category'; readonly categoryId: string | undefined }
  | { readonly kind: 'item'; readonly itemId: string | undefined; readonly categoryId: string }
  | { readonly kind: 'delete-category'; readonly categoryId: string }
  | { readonly kind: 'delete-item'; readonly itemId: string };

export function MenuEditorPage() {
  const { messages, describeError } = useI18n();
  const workspace = useWorkspace();
  const { tenant } = useCurrentUser();
  const canEdit = useCanEditMenu();
  const notify = useNotify();
  const { deleteCategory, deleteItem } = useMenuMutations();
  const { data: menu } = useSuspenseQuery(managedMenuQuery(workspace));
  const { category: categoryParam } = route.useSearch();
  const navigate = route.useNavigate();
  const [editing, setEditing] = useState<Editing>({ kind: 'none' });
  const [deleteError, setDeleteError] = useState<string>();
  const [isImporting, setIsImporting] = useState(false);
  const [isExporting, setIsExporting] = useState(false);

  // The selection lives in the URL, so a reload or a shared link keeps the category.
  const selected = menu.categories.find((category) => category.id === categoryParam) ?? menu.categories[0];
  const findCategory = (id: string | undefined) => menu.categories.find((category) => category.id === id);
  const findItem = (id: string | undefined) =>
    menu.categories.flatMap((category) => category.items).find((item) => item.id === id);
  const close = () => {
    setEditing({ kind: 'none' });
    setDeleteError(undefined);
  };

  const confirmDelete = async (remove: () => Promise<unknown>) => {
    try {
      await remove();
      notify(messages.saved);
      close();
    } catch (error) {
      setDeleteError(
        formErrorsFrom(error, { describe: describeError, networkError: messages.networkError }).form,
      );
    }
  };

  return (
    <>
      <PageHeader
        title={messages.navMenu}
        actions={
          canEdit && (
            <>
              <Button
                variant="ghost"
                isPending={isExporting}
                onPress={() => {
                  setIsExporting(true);
                  downloadMenu(workspace)
                    .catch((error: unknown) => {
                      notify(
                        formErrorsFrom(error, {
                          describe: describeError,
                          networkError: messages.networkError,
                        }).form ?? messages.unexpectedError,
                        'error',
                      );
                    })
                    .finally(() => {
                      setIsExporting(false);
                    });
                }}
              >
                <DownloadIcon />
                {messages.exportMenu}
              </Button>
              <Button
                variant="ghost"
                onPress={() => {
                  setIsImporting(true);
                }}
              >
                <UploadIcon />
                {messages.importMenu}
              </Button>
              {selected !== undefined && (
                <Button
                  onPress={() => {
                    setEditing({ kind: 'item', itemId: undefined, categoryId: selected.id });
                  }}
                >
                  <PlusIcon />
                  {messages.addItem}
                </Button>
              )}
              <Button
                variant="primary"
                onPress={() => {
                  setEditing({ kind: 'category', categoryId: undefined });
                }}
              >
                <PlusIcon />
                {messages.addCategory}
              </Button>
            </>
          )
        }
      />

      {selected === undefined ? (
        <EmptyState title={messages.emptyMenuTitle} body={messages.emptyMenuBody} />
      ) : (
        <div className="grid gap-6 lg:grid-cols-[20rem_minmax(0,1fr)]">
          <CategoryList
            categories={menu.categories}
            selectedId={selected.id}
            canEdit={canEdit}
            onSelect={(categoryId) => {
              void navigate({ search: { category: categoryId }, replace: true });
            }}
            onEdit={(category) => {
              setEditing({ kind: 'category', categoryId: category.id });
            }}
            onDelete={(category) => {
              setEditing({ kind: 'delete-category', categoryId: category.id });
            }}
          />
          <section aria-labelledby="items-heading">
            <h2 id="items-heading" className="mb-3 text-sm font-semibold text-ink-muted">
              {displayText(selected.name, tenant.defaultCulture)}
            </h2>
            <ItemList
              category={selected}
              canEdit={canEdit}
              onEdit={(item) => {
                setEditing({ kind: 'item', itemId: item.id, categoryId: selected.id });
              }}
              onDelete={(item) => {
                setEditing({ kind: 'delete-item', itemId: item.id });
              }}
            />
          </section>
        </div>
      )}

      {editing.kind === 'category' && (
        <CategoryDialog category={findCategory(editing.categoryId)} onClose={close} />
      )}
      {editing.kind === 'item' && (
        <ItemDialog
          item={findItem(editing.itemId)}
          categoryId={editing.categoryId}
          categories={menu.categories}
          onClose={close}
        />
      )}
      {editing.kind === 'delete-category' && (
        <ConfirmDialog
          title={messages.deleteCategory}
          body={messages.deleteCategoryConfirm(
            displayText(findCategory(editing.categoryId)?.name, tenant.defaultCulture),
          )}
          confirmLabel={messages.delete}
          isOpen
          isPending={deleteCategory.isPending}
          error={deleteError}
          onConfirm={() => void confirmDelete(() => deleteCategory.mutateAsync(editing.categoryId))}
          onOpenChange={(isOpen) => {
            if (!isOpen) {
              close();
            }
          }}
        />
      )}
      {editing.kind === 'delete-item' && (
        <ConfirmDialog
          title={messages.deleteItem}
          body={`${messages.deleteItemConfirm(displayText(findItem(editing.itemId)?.name, tenant.defaultCulture))} ${messages.deleteCannotBeUndone}`}
          confirmLabel={messages.delete}
          isOpen
          isPending={deleteItem.isPending}
          error={deleteError}
          onConfirm={() => void confirmDelete(() => deleteItem.mutateAsync(editing.itemId))}
          onOpenChange={(isOpen) => {
            if (!isOpen) {
              close();
            }
          }}
        />
      )}
      {isImporting && (
        <MenuImportDialog
          onClose={() => {
            setIsImporting(false);
          }}
        />
      )}
    </>
  );
}
