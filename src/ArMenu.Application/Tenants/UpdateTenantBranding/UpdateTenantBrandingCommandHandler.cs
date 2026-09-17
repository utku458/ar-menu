using ArMenu.Application.Abstractions.Assets;
using ArMenu.Application.Assets;
using ArMenu.Application.Common.Validation;
using ArMenu.Application.MultiTenancy;
using ArMenu.Domain.Common;
using ArMenu.Domain.Media;
using ArMenu.Domain.Tenants;
using Mediator;

namespace ArMenu.Application.Tenants.UpdateTenantBranding;

public sealed class UpdateTenantBrandingCommandHandler(
    ITenantContext tenantContext,
    ITenantRepository tenants,
    ITenantLookup tenantLookup,
    IAssetStorage storage,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateTenantBrandingCommand, Result>
{
    public async ValueTask<Result> Handle(UpdateTenantBrandingCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var tenant = await tenants.GetByIdAsync(tenantContext.TenantId, cancellationToken)
            ?? throw new TenantNotResolvedException();

        var logoPath = command.LogoPath is null ? null : AssetPath.Create(command.LogoPath).Value;
        var accentColor = command.AccentColor is null ? null : BrandColor.Create(command.AccentColor).Value;

        var ownership = await CheckLogoAsync(logoPath, tenant.Branding.LogoPath, cancellationToken);
        if (ownership.IsFailure)
        {
            return ownership.Error;
        }

        var renamed = tenant.Rename(command.Name);
        if (renamed.IsFailure)
        {
            return renamed.Error;
        }

        tenant.ChangeBranding(new TenantBranding(logoPath, accentColor));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Tenant resolution caches the name and the branding, and the guest menu is served from that snapshot.
        await tenantLookup.InvalidateAsync(tenant.Id, tenant.Slug, cancellationToken);

        return Result.Success();
    }

    /// <summary>
    /// A logo is only accepted when this business published the file and it is still in storage. The logo already in
    /// use is taken as it is, so re-saving the name or the colour cannot fail because of a file cleanup elsewhere.
    /// </summary>
    private async Task<Result> CheckLogoAsync(AssetPath? requested, AssetPath? current, CancellationToken cancellationToken)
    {
        if (requested is null || requested == current)
        {
            return Result.Success();
        }

        var error = !TenantAssetKeys.IsPublishedBy(requested, tenantContext.TenantId) ? AssetErrors.NotOwned
            : !await storage.ExistsAsync(requested, cancellationToken) ? AssetErrors.NotFound
            : null;

        return error is null
            ? Result.Success()
            : new ValidationError([new FieldError(nameof(UpdateTenantBrandingCommand.LogoPath), error.Code, error.Description)]);
    }
}
