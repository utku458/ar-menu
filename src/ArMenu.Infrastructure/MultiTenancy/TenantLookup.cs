using System.Linq.Expressions;
using ArMenu.Application.MultiTenancy;
using ArMenu.Domain.Tenants;
using ArMenu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

namespace ArMenu.Infrastructure.MultiTenancy;

/// <summary>
/// Cache-first tenant lookup. Scanning a QR code at peak hours means bursts of identical lookups;
/// HybridCache serves them from memory and collapses concurrent misses into a single database query.
/// </summary>
internal sealed class TenantLookup(HybridCache cache, IServiceScopeFactory scopeFactory) : ITenantLookup
{
    private static readonly HybridCacheEntryOptions EntryOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(5),
        LocalCacheExpiration = TimeSpan.FromMinutes(1),
    };

    public ValueTask<TenantInfo?> FindByIdAsync(TenantId tenantId, CancellationToken cancellationToken = default) =>
        cache.GetOrCreateAsync(
            CacheKeys.TenantById(tenantId),
            (scopeFactory, tenantId),
            static (state, token) => LoadAsync(state.scopeFactory, tenant => tenant.Id == state.tenantId, token),
            EntryOptions,
            cancellationToken: cancellationToken);

    public ValueTask<TenantInfo?> FindBySlugAsync(TenantSlug slug, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(slug);

        return cache.GetOrCreateAsync(
            CacheKeys.TenantBySlug(slug),
            (scopeFactory, slug),
            static (state, token) => LoadAsync(state.scopeFactory, tenant => tenant.Slug == state.slug, token),
            EntryOptions,
            cancellationToken: cancellationToken);
    }

    public async ValueTask InvalidateAsync(TenantId tenantId, TenantSlug slug, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(slug);

        await cache.RemoveAsync(CacheKeys.TenantById(tenantId), cancellationToken);
        await cache.RemoveAsync(CacheKeys.TenantBySlug(slug), cancellationToken);
    }

    private static async ValueTask<TenantInfo?> LoadAsync(
        IServiceScopeFactory scopeFactory,
        Expression<Func<Tenant, bool>> predicate,
        CancellationToken cancellationToken)
    {
        // HybridCache may run this factory on behalf of several concurrent requests (stampede protection),
        // so it must own its DbContext instead of borrowing one from whichever request happened to trigger it.
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ArMenuDbContext>();

        var tenant = await dbContext.Tenants
            .AsNoTracking()
            .SingleOrDefaultAsync(predicate, cancellationToken);

        return tenant is null ? null : TenantInfo.From(tenant);
    }
}
