using ArMenu.Application.MultiTenancy;
using ArMenu.Domain.Common;
using ArMenu.Domain.Localization;
using ArMenu.Domain.Tenants;
using Mediator;

namespace ArMenu.Application.Tenants.UpdateTenantLanguages;

public sealed class UpdateTenantLanguagesCommandHandler(
    ITenantContext tenantContext,
    ITenantRepository tenants,
    ITenantLookup tenantLookup,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateTenantLanguagesCommand, Result>
{
    public async ValueTask<Result> Handle(UpdateTenantLanguagesCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var tenant = await tenants.GetByIdAsync(tenantContext.TenantId, cancellationToken)
            ?? throw new TenantNotResolvedException();

        var updated = tenant.SetLanguages(
            CultureCode.Create(command.DefaultCulture).Value,
            command.SupportedCultures.Select(culture => CultureCode.Create(culture).Value));

        if (updated.IsFailure)
        {
            return updated;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Tenant resolution caches the languages; cached public menus are evicted by the save itself.
        await tenantLookup.InvalidateAsync(tenant.Id, tenant.Slug, cancellationToken);

        return Result.Success();
    }
}
