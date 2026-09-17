using ArMenu.Domain.Menus;
using Microsoft.EntityFrameworkCore;

namespace ArMenu.Infrastructure.Persistence.Repositories;

internal sealed class MenuCategoryRepository(ArMenuDbContext dbContext) : IMenuCategoryRepository
{
    public Task<MenuCategory?> GetByIdAsync(MenuCategoryId categoryId, CancellationToken cancellationToken = default) =>
        dbContext.MenuCategories.SingleOrDefaultAsync(category => category.Id == categoryId, cancellationToken);

    public async Task<IReadOnlyList<MenuCategory>> ListAsync(CancellationToken cancellationToken = default) =>
        await dbContext.MenuCategories.OrderBy(category => category.DisplayOrder).ToListAsync(cancellationToken);

    public Task<bool> ExistsAsync(MenuCategoryId categoryId, CancellationToken cancellationToken = default) =>
        dbContext.MenuCategories.AnyAsync(category => category.Id == categoryId, cancellationToken);

    public async Task<int> GetNextDisplayOrderAsync(CancellationToken cancellationToken = default) =>
        (await dbContext.MenuCategories.MaxAsync(category => (int?)category.DisplayOrder, cancellationToken) ?? -1) + 1;

    public void Add(MenuCategory category) => dbContext.MenuCategories.Add(category);

    public void Remove(MenuCategory category) => dbContext.MenuCategories.Remove(category);
}
