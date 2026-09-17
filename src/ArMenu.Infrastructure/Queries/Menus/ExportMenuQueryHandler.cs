using ArMenu.Application.Menus.Transfer;
using ArMenu.Application.Menus.Transfer.Queries.ExportMenu;
using ArMenu.Application.MultiTenancy;
using ArMenu.Domain.Common;
using ArMenu.Infrastructure.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ArMenu.Infrastructure.Queries.Menus;

public sealed class ExportMenuQueryHandler(ArMenuDbContext dbContext, ITenantContext tenantContext, TimeProvider timeProvider)
    : IQueryHandler<ExportMenuQuery, Result<MenuExport>>
{
    public async ValueTask<Result<MenuExport>> Handle(ExportMenuQuery query, CancellationToken cancellationToken)
    {
        var tenant = tenantContext.RequireTenant();
        var categories = await dbContext.MenuCategories.AsNoTracking().ToListAsync(cancellationToken);
        var items = await dbContext.MenuItems.AsNoTracking().ToListAsync(cancellationToken);

        var day = tenant.DateAt(timeProvider.GetUtcNow()).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        return new MenuExport($"{tenant.Slug}-menu-{day}.csv", MenuCsvExport.Write(tenant, categories, items));
    }
}
