using ArMenu.Application.Abstractions.Assets;
using ArMenu.Application.Menus.Queries.GetManagedMenu;
using ArMenu.Domain.Common;
using ArMenu.Domain.Menus;
using ArMenu.Infrastructure.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ArMenu.Infrastructure.Queries.Menus;

/// <summary>Read side of the management panel. Not cached: staff must always see their latest edits.</summary>
public sealed class GetManagedMenuQueryHandler(ArMenuDbContext dbContext, IAssetUrlResolver assetUrls)
    : IQueryHandler<GetManagedMenuQuery, Result<ManagedMenuResponse>>
{
    public async ValueTask<Result<ManagedMenuResponse>> Handle(GetManagedMenuQuery query, CancellationToken cancellationToken)
    {
        var categories = await dbContext.MenuCategories
            .AsNoTracking()
            .OrderBy(category => category.DisplayOrder)
            .Select(category => new { category.Id, category.Name, category.Description, category.DisplayOrder, category.IsVisible })
            .ToListAsync(cancellationToken);

        var items = await dbContext.MenuItems
            .AsNoTracking()
            .OrderBy(item => item.DisplayOrder)
            .Select(item => new
            {
                item.Id,
                item.CategoryId,
                item.Name,
                item.Description,
                item.Price.Amount,
                item.DisplayOrder,
                item.IsVisible,
                item.IsAvailable,
                item.ArModel,
                item.Allergens,
                item.DietaryLabels,
            })
            .ToListAsync(cancellationToken);

        var itemsByCategory = items.ToLookup(item => item.CategoryId);

        return new ManagedMenuResponse(
        [
            .. categories.Select(category => new ManagedMenuCategoryResponse(
                category.Id.Value,
                category.Name.Translations,
                category.Description?.Translations,
                category.DisplayOrder,
                category.IsVisible,
                [
                    .. itemsByCategory[category.Id].Select(item => new ManagedMenuItemResponse(
                        item.Id.Value,
                        item.Name.Translations,
                        item.Description?.Translations,
                        item.Amount,
                        item.DisplayOrder,
                        item.IsVisible,
                        item.IsAvailable,
                        ToResponse(item.ArModel),
                        item.Allergens?.Select(DietaryInformation.CodeOf).ToList(),
                        [.. item.DietaryLabels.Select(DietaryInformation.CodeOf)])),
                ])),
        ]);
    }

    private ManagedArModelResponse? ToResponse(ArModel? arModel) =>
        arModel is null
            ? null
            : new ManagedArModelResponse(
                arModel.GlbPath.Value,
                arModel.SceneViewerGlbPath?.Value,
                arModel.UsdzPath?.Value,
                arModel.PosterPath?.Value,
                assetUrls.Resolve(arModel.GlbPath),
                arModel.SceneViewerGlbPath is null ? null : assetUrls.Resolve(arModel.SceneViewerGlbPath),
                arModel.UsdzPath is null ? null : assetUrls.Resolve(arModel.UsdzPath),
                arModel.PosterPath is null ? null : assetUrls.Resolve(arModel.PosterPath));
}
