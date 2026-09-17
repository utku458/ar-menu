using ArMenu.Application.Abstractions.Assets;
using ArMenu.Application.Menus;
using ArMenu.Application.Menus.Queries.GetPublicMenu;
using ArMenu.Application.MultiTenancy;
using ArMenu.Domain.Common;
using ArMenu.Domain.Localization;
using ArMenu.Domain.Media;
using ArMenu.Domain.Menus;
using ArMenu.Domain.Tenants;
using ArMenu.Infrastructure.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

namespace ArMenu.Infrastructure.Queries.Menus;

/// <summary>
/// Read side of the guest menu: a projection straight from the database into a cached, immutable response.
/// Aggregates are never loaded; any menu change evicts the tenant's entries (see MenuCacheInvalidationInterceptor).
/// </summary>
public sealed class GetPublicMenuQueryHandler(ITenantContext tenantContext, HybridCache cache, IServiceScopeFactory scopeFactory)
    : IQueryHandler<GetPublicMenuQuery, Result<PublicMenuResponse>>
{
    private static readonly HybridCacheEntryOptions EntryOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(10),
        LocalCacheExpiration = TimeSpan.FromMinutes(2),
    };

    public async ValueTask<Result<PublicMenuResponse>> Handle(GetPublicMenuQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var tenant = tenantContext.RequireTenant();
        var culture = MenuCultureSelector.Select(query.PreferredCultures, tenant);

        return await cache.GetOrCreateAsync(
            MenuCacheKeys.PublicMenu(tenant.Id, culture),
            (Tenant: tenant, Culture: culture, ScopeFactory: scopeFactory),
            static (state, token) => LoadAsync(state.ScopeFactory, state.Tenant, state.Culture, token),
            EntryOptions,
            [MenuCacheKeys.TenantMenusTag(tenant.Id)],
            cancellationToken);
    }

    private static async ValueTask<PublicMenuResponse> LoadAsync(
        IServiceScopeFactory scopeFactory,
        TenantInfo tenant,
        CultureCode culture,
        CancellationToken cancellationToken)
    {
        // HybridCache may run this factory on behalf of many concurrent guests, so it uses its own tenant-bound scope
        // instead of the DbContext of whichever request happened to trigger it.
        await using var scope = scopeFactory.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<ITenantContextSetter>().SetTenant(tenant);
        var dbContext = scope.ServiceProvider.GetRequiredService<ArMenuDbContext>();
        var assetUrls = scope.ServiceProvider.GetRequiredService<IAssetUrlResolver>();

        var categories = await dbContext.MenuCategories
            .AsNoTracking()
            .Where(category => category.IsVisible)
            .OrderBy(category => category.DisplayOrder)
            .Select(category => new { category.Id, category.Name, category.Description })
            .ToListAsync(cancellationToken);

        var items = await dbContext.MenuItems
            .AsNoTracking()
            .Where(item => item.IsVisible)
            .OrderBy(item => item.DisplayOrder)
            .Select(item => new
            {
                item.Id,
                item.CategoryId,
                item.Name,
                item.Description,
                item.Price.Amount,
                item.IsAvailable,
                item.ArModel,
                item.Allergens,
                item.DietaryLabels,
            })
            .ToListAsync(cancellationToken);

        var fallback = CultureCode.Create(tenant.DefaultCulture).Value;
        var itemsByCategory = items.ToLookup(item => item.CategoryId);

        var accentColor = tenant.AccentColor is null ? null : BrandColor.Create(tenant.AccentColor).Value;

        return new PublicMenuResponse(
            new PublicTenantResponse(
                tenant.Name,
                tenant.Slug,
                tenant.Currency,
                tenant.DefaultCulture,
                tenant.SupportedCultures,
                tenant.LogoPath is null ? null : assetUrls.Resolve(AssetPath.Create(tenant.LogoPath).Value),
                accentColor?.Value,
                accentColor?.Foreground),
            culture.Value,
            [
                .. categories
                    .Where(category => itemsByCategory[category.Id].Any())
                    .Select(category => new PublicMenuCategoryResponse(
                        category.Id.Value,
                        category.Name.Resolve(culture, fallback),
                        category.Description?.Resolve(culture, fallback),
                        [
                            .. itemsByCategory[category.Id].Select(item => new PublicMenuItemResponse(
                                item.Id.Value,
                                item.Name.Resolve(culture, fallback),
                                item.Description?.Resolve(culture, fallback),
                                item.Amount,
                                item.IsAvailable,
                                ToResponse(item.ArModel, assetUrls),
                                item.Allergens?.Select(DietaryInformation.CodeOf).ToList(),
                                [.. item.DietaryLabels.Select(DietaryInformation.CodeOf)])),
                        ])),
            ]);
    }

    private static ArModelResponse? ToResponse(ArModel? arModel, IAssetUrlResolver assetUrls) =>
        arModel is null
            ? null
            : new ArModelResponse(
                assetUrls.Resolve(arModel.GlbPath),
                arModel.SceneViewerGlbPath is null ? null : assetUrls.Resolve(arModel.SceneViewerGlbPath),
                arModel.UsdzPath is null ? null : assetUrls.Resolve(arModel.UsdzPath),
                arModel.PosterPath is null ? null : assetUrls.Resolve(arModel.PosterPath));
}
