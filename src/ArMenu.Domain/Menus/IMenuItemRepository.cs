namespace ArMenu.Domain.Menus;

/// <summary>Menu items of the tenant bound to the current scope.</summary>
public interface IMenuItemRepository
{
    Task<MenuItem?> GetByIdAsync(MenuItemId itemId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MenuItem>> ListByCategoryAsync(MenuCategoryId categoryId, CancellationToken cancellationToken = default);

    Task<bool> AnyInCategoryAsync(MenuCategoryId categoryId, CancellationToken cancellationToken = default);

    /// <summary>The display order that places a new item after all existing items of the category.</summary>
    Task<int> GetNextDisplayOrderAsync(MenuCategoryId categoryId, CancellationToken cancellationToken = default);

    void Add(MenuItem item);

    void Remove(MenuItem item);
}
