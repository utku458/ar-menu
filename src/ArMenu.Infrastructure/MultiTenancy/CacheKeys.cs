using ArMenu.Domain.Tenants;

namespace ArMenu.Infrastructure.MultiTenancy;

internal static class CacheKeys
{
    public static string TenantById(TenantId tenantId) => $"tenants:id:{tenantId.Value:N}";

    public static string TenantBySlug(TenantSlug slug) => $"tenants:slug:{slug.Value}";
}
