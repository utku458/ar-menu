using ArMenu.Application.MultiTenancy;
using ArMenu.Domain.Common;
using ArMenu.Domain.Tenants;
using Mediator;

namespace ArMenu.Application.Tenants.UpdateTenantTimeZone;

public sealed class UpdateTenantTimeZoneCommandHandler(
    ITenantContext tenantContext,
    ITenantRepository tenants,
    ITenantLookup tenantLookup,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateTenantTimeZoneCommand, Result>
{
    public async ValueTask<Result> Handle(UpdateTenantTimeZoneCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var tenant = await tenants.GetByIdAsync(tenantContext.TenantId, cancellationToken)
            ?? throw new TenantNotResolvedException();

        tenant.ChangeTimeZone(TenantTimeZone.Create(command.TimeZone).Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Tenant resolution caches the time zone, which decides the day guest events are counted on.
        await tenantLookup.InvalidateAsync(tenant.Id, tenant.Slug, cancellationToken);

        return Result.Success();
    }
}
