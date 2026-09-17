using ArMenu.Application.Abstractions.Assets;
using ArMenu.Application.Assets;
using ArMenu.Application.Common.Validation;
using ArMenu.Application.MultiTenancy;
using ArMenu.Domain.Common;
using ArMenu.Domain.Media;
using ArMenu.Domain.Menus;
using Mediator;

namespace ArMenu.Application.Menus.Items.AttachMenuItemArModel;

public sealed class AttachMenuItemArModelCommandHandler(
    ITenantContext tenantContext,
    IMenuItemRepository items,
    IAssetStorage storage,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AttachMenuItemArModelCommand, Result>
{
    public async ValueTask<Result> Handle(AttachMenuItemArModelCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var glbPath = AssetPath.Create(command.GlbPath);
        var sceneViewerGlbPath = CreateOptional(command.SceneViewerGlbPath);
        var usdzPath = CreateOptional(command.UsdzPath);
        var posterPath = CreateOptional(command.PosterPath);

        if (new Result[] { glbPath, sceneViewerGlbPath, usdzPath, posterPath }.FirstOrDefault(result => result.IsFailure) is { } invalidPath)
        {
            return invalidPath;
        }

        var arModel = ArModel.Create(glbPath.Value, sceneViewerGlbPath.Value, usdzPath.Value, posterPath.Value);
        if (arModel.IsFailure)
        {
            return arModel.Error;
        }

        var item = await items.GetByIdAsync(command.ItemId, cancellationToken);
        if (item is null)
        {
            return MenuItemErrors.NotFound;
        }

        var ownership = await CheckOwnershipAsync(item.ArModel, arModel.Value, cancellationToken);
        if (ownership.IsFailure)
        {
            return ownership.Error;
        }

        item.AttachArModel(arModel.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    /// <summary>
    /// A path is only accepted when this business published the file, and it still exists. Paths the item already uses
    /// are kept as they are, so editing one file of a model does not require uploading the others again.
    /// </summary>
    private async Task<Result> CheckOwnershipAsync(ArModel? current, ArModel requested, CancellationToken cancellationToken)
    {
        (string Field, AssetPath? Requested, AssetPath? Current)[] slots =
        [
            (nameof(AttachMenuItemArModelCommand.GlbPath), requested.GlbPath, current?.GlbPath),
            (nameof(AttachMenuItemArModelCommand.SceneViewerGlbPath), requested.SceneViewerGlbPath, current?.SceneViewerGlbPath),
            (nameof(AttachMenuItemArModelCommand.UsdzPath), requested.UsdzPath, current?.UsdzPath),
            (nameof(AttachMenuItemArModelCommand.PosterPath), requested.PosterPath, current?.PosterPath),
        ];

        var errors = new List<FieldError>();
        foreach (var (field, path, currentPath) in slots)
        {
            if (path is null || path == currentPath)
            {
                continue;
            }

            if (!TenantAssetKeys.IsPublishedBy(path, tenantContext.TenantId))
            {
                errors.Add(new FieldError(field, AssetErrors.NotOwned.Code, AssetErrors.NotOwned.Description));
            }
            else if (!await storage.ExistsAsync(path, cancellationToken))
            {
                errors.Add(new FieldError(field, AssetErrors.NotFound.Code, AssetErrors.NotFound.Description));
            }
        }

        return errors.Count == 0 ? Result.Success() : new ValidationError(errors);
    }

    private static Result<AssetPath?> CreateOptional(string? path)
    {
        if (path is null)
        {
            return Result.Success<AssetPath?>(null);
        }

        var created = AssetPath.Create(path);
        return created.IsSuccess ? created.Value : created.Error;
    }
}
