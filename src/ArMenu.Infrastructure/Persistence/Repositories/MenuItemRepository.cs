using ArMenu.Domain.Menus;
using Microsoft.EntityFrameworkCore;

namespace ArMenu.Infrastructure.Persistence.Repositories;

internal sealed class MenuItemRepository(ArMenuDbContext dbContext) : IMenuItemRepository
{
    public Task<MenuItem?> GetByIdAsync(MenuItemId itemId, CancellationToken cancellationToken = default) =>
        dbContext.MenuItems.SingleOrDefaultAsync(item => item.Id == itemId, cancellationToken);

    public async Task<IReadOnlyList<MenuItem>> ListByCategoryAsync(MenuCategoryId categoryId, CancellationToken cancellationToken = default) =>
        await dbContext.MenuItems
            .Where(item => item.CategoryId == categoryId)
            .OrderBy(item => item.DisplayOrder)
            .ToListAsync(cancellationToken);

    public Task<bool> AnyInCategoryAsync(MenuCategoryId categoryId, CancellationToken cancellationToken = default) =>
        dbContext.MenuItems.AnyAsync(item => item.CategoryId == categoryId, cancellationToken);

    public async Task<int> GetNextDisplayOrderAsync(MenuCategoryId categoryId, CancellationToken cancellationToken = default) =>
        (await dbContext.MenuItems
            .Where(item => item.CategoryId == categoryId)
            .MaxAsync(item => (int?)item.DisplayOrder, cancellationToken) ?? -1) + 1;

    public void Add(MenuItem item) => dbContext.MenuItems.Add(item);

    public void Remove(MenuItem item) => dbContext.MenuItems.Remove(item);
}
