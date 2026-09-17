using ArMenu.Domain.Menus;
using ArMenu.Domain.Tenants;
using ArMenu.Infrastructure.Queries.Menus;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Hybrid;

namespace ArMenu.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Evicts a tenant's cached public menus after any successful change to its menu or tenant record. Doing it at the
/// persistence boundary means no command handler can forget it, so the menu can be cached aggressively.
/// </summary>
/// <remarks>
/// HybridCache without a distributed backplane evicts on this instance only; other instances converge within the
/// short local cache lifetime of the menu entries.
/// </remarks>
internal sealed class MenuCacheInvalidationInterceptor(HybridCache cache) : SaveChangesInterceptor
{
    private readonly HashSet<TenantId> _pendingTenants = [];

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        CollectChangedMenus(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        CollectChangedMenus(eventData.Context);
        return ValueTask.FromResult(result);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        EvictPendingAsync(CancellationToken.None).AsTask().GetAwaiter().GetResult();
        return result;
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        await EvictPendingAsync(cancellationToken);
        return result;
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData) => _pendingTenants.Clear();

    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        _pendingTenants.Clear();
        return Task.CompletedTask;
    }

    private void CollectChangedMenus(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                continue;
            }

            TenantId? tenantId = entry.Entity switch
            {
                MenuCategory category => category.TenantId,
                MenuItem item => item.TenantId,
                Tenant tenant => tenant.Id,
                _ => null,
            };

            if (tenantId is { } id)
            {
                _pendingTenants.Add(id);
            }
        }
    }

    private async ValueTask EvictPendingAsync(CancellationToken cancellationToken)
    {
        foreach (var tenantId in _pendingTenants)
        {
            await cache.RemoveByTagAsync(MenuCacheKeys.TenantMenusTag(tenantId), cancellationToken);
        }

        _pendingTenants.Clear();
    }
}
