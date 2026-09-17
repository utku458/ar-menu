namespace ArMenu.Domain.Menus;

/// <summary>Categories of the tenant bound to the current scope.</summary>
public interface IMenuCategoryRepository
{
    Task<MenuCategory?> GetByIdAsync(MenuCategoryId categoryId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MenuCategory>> ListAsync(CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(MenuCategoryId categoryId, CancellationToken cancellationToken = default);

    /// <summary>The display order that places a new category after all existing ones.</summary>
    Task<int> GetNextDisplayOrderAsync(CancellationToken cancellationToken = default);

    void Add(MenuCategory category);

    void Remove(MenuCategory category);
}
