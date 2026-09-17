import type {
  ArModelRequest,
  CategoryRequest,
  CreateItemRequest,
  ManagedMenuResponse,
  UpdateItemRequest,
} from '@armenu/api-client';
import { queryOptions, useMutation, useQueryClient } from '@tanstack/react-query';

import { ensureOk, unwrap } from '../../api/result.ts';
import { useWorkspace } from '../../app/workspace.ts';
import type { Workspace } from '../../auth/workspaces.ts';

export function managedMenuQuery(workspace: Workspace) {
  return queryOptions({
    // The API client is the business's singleton; the slug identifies it.
    queryKey: ['managed-menu', workspace.slug],
    queryFn: async ({ signal }) => unwrap(await workspace.api.GET('/api/v1/manage/menu', { signal })),
  });
}

type MenuUpdate = (menu: ManagedMenuResponse) => ManagedMenuResponse;

/** Every menu change, invalidating the menu afterwards. Toggles and reorders apply optimistically. */
export function useMenuMutations() {
  const workspace = useWorkspace();
  const { api } = workspace;
  const queryClient = useQueryClient();
  const { queryKey } = managedMenuQuery(workspace);

  const refresh = () => queryClient.invalidateQueries({ queryKey });

  // Shows the expected result at once, and rolls it back if the API disagrees.
  const optimistically = async (update: MenuUpdate) => {
    await queryClient.cancelQueries({ queryKey });
    const previous = queryClient.getQueryData(queryKey);
    if (previous !== undefined) {
      queryClient.setQueryData(queryKey, update(previous));
    }

    return { previous };
  };
  const rollback = (
    _error: unknown,
    _variables: unknown,
    context: { previous: ManagedMenuResponse | undefined } | undefined,
  ) => {
    if (context?.previous !== undefined) {
      queryClient.setQueryData(queryKey, context.previous);
    }
  };

  return {
    createCategory: useMutation({
      mutationFn: async (body: CategoryRequest) =>
        unwrap(await api.POST('/api/v1/manage/menu/categories', { body })),
      onSuccess: refresh,
    }),
    updateCategory: useMutation({
      mutationFn: async ({ id, body }: { id: string; body: CategoryRequest }) => {
        ensureOk(
          await api.PUT('/api/v1/manage/menu/categories/{categoryId}', {
            params: { path: { categoryId: id } },
            body,
          }),
        );
      },
      onSuccess: refresh,
    }),
    deleteCategory: useMutation({
      mutationFn: async (id: string) => {
        ensureOk(
          await api.DELETE('/api/v1/manage/menu/categories/{categoryId}', {
            params: { path: { categoryId: id } },
          }),
        );
      },
      onSuccess: refresh,
    }),
    reorderCategories: useMutation({
      mutationFn: async (ids: string[]) => {
        ensureOk(await api.PUT('/api/v1/manage/menu/categories/order', { body: { ids } }));
      },
      onMutate: (ids) =>
        optimistically((menu) => ({
          ...menu,
          categories: ids.flatMap((id) => menu.categories.filter((category) => category.id === id)),
        })),
      onError: rollback,
      onSettled: refresh,
    }),

    createItem: useMutation({
      mutationFn: async (body: CreateItemRequest) =>
        unwrap(await api.POST('/api/v1/manage/menu/items', { body })),
      onSuccess: refresh,
    }),
    updateItem: useMutation({
      mutationFn: async ({ id, body }: { id: string; body: UpdateItemRequest }) => {
        ensureOk(
          await api.PUT('/api/v1/manage/menu/items/{itemId}', { params: { path: { itemId: id } }, body }),
        );
      },
      onSuccess: refresh,
    }),
    deleteItem: useMutation({
      mutationFn: async (id: string) => {
        ensureOk(
          await api.DELETE('/api/v1/manage/menu/items/{itemId}', { params: { path: { itemId: id } } }),
        );
      },
      onSuccess: refresh,
    }),
    reorderItems: useMutation({
      mutationFn: async ({ categoryId, ids }: { categoryId: string; ids: string[] }) => {
        ensureOk(
          await api.PUT('/api/v1/manage/menu/categories/{categoryId}/items/order', {
            params: { path: { categoryId } },
            body: { ids },
          }),
        );
      },
      onMutate: ({ categoryId, ids }) =>
        optimistically((menu) => ({
          ...menu,
          categories: menu.categories.map((category) =>
            category.id === categoryId
              ? { ...category, items: ids.flatMap((id) => category.items.filter((item) => item.id === id)) }
              : category,
          ),
        })),
      onError: rollback,
      onSettled: refresh,
    }),
    setAvailability: useMutation({
      mutationFn: async ({ id, isAvailable }: { id: string; isAvailable: boolean }) => {
        ensureOk(
          await api.PUT('/api/v1/manage/menu/items/{itemId}/availability', {
            params: { path: { itemId: id } },
            body: { isAvailable },
          }),
        );
      },
      onMutate: ({ id, isAvailable }) =>
        optimistically((menu) => ({
          ...menu,
          categories: menu.categories.map((category) => ({
            ...category,
            items: category.items.map((item) => (item.id === id ? { ...item, isAvailable } : item)),
          })),
        })),
      onError: rollback,
      onSettled: refresh,
    }),

    attachArModel: useMutation({
      mutationFn: async ({ id, body }: { id: string; body: ArModelRequest }) => {
        ensureOk(
          await api.PUT('/api/v1/manage/menu/items/{itemId}/ar-model', {
            params: { path: { itemId: id } },
            body,
          }),
        );
      },
      onSuccess: refresh,
    }),
    detachArModel: useMutation({
      mutationFn: async (id: string) => {
        ensureOk(
          await api.DELETE('/api/v1/manage/menu/items/{itemId}/ar-model', {
            params: { path: { itemId: id } },
          }),
        );
      },
      onSuccess: refresh,
    }),
  };
}
