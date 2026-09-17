using ArMenu.Application.MultiTenancy;
using ArMenu.Domain.Common;
using ArMenu.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ArMenu.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Guards writes the way query filters guard reads: before anything reaches the database, every tenant-scoped change
/// must belong to the tenant bound to the current scope. This also covers entities that were attached manually
/// (e.g. built from request data) and therefore never passed through a filtered query.
/// </summary>
internal sealed class TenantIsolationSaveChangesInterceptor(ITenantContext tenantContext) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        EnforceTenantIsolation(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        EnforceTenantIsolation(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private void EnforceTenantIsolation(DbContext? context)
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

            switch (entry.Entity)
            {
                case ITenantScoped:
                    EnsureBelongsToCurrentTenant(entry, entry.Property(nameof(ITenantScoped.TenantId)));
                    break;

                // A tenant-bound scope may change its own tenant record, but never another tenant's.
                case Tenant when tenantContext.Tenant is not null:
                    EnsureBelongsToCurrentTenant(entry, entry.Property(nameof(Tenant.Id)));
                    break;
            }
        }
    }

    private void EnsureBelongsToCurrentTenant(EntityEntry entry, PropertyEntry tenantIdProperty)
    {
        // Throws TenantNotResolvedException when the scope is not bound to any tenant.
        var currentTenantId = tenantContext.TenantId;

        var owner = entry.State == EntityState.Added ? tenantIdProperty.CurrentValue : tenantIdProperty.OriginalValue;
        var ownerChanged = !Equals(tenantIdProperty.CurrentValue, tenantIdProperty.OriginalValue);

        if (owner is not TenantId ownerId || ownerId != currentTenantId || ownerChanged)
        {
            throw new TenantIsolationViolationException(
                $"Rejected {entry.State} of '{entry.Metadata.DisplayName()}': the entity does not belong to the current tenant.");
        }
    }
}
