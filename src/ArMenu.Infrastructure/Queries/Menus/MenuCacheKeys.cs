using ArMenu.Domain.Localization;
using ArMenu.Domain.Tenants;

namespace ArMenu.Infrastructure.Queries.Menus;

internal static class MenuCacheKeys
{
    public static string PublicMenu(TenantId tenantId, CultureCode culture) =>
        $"menus:public:{tenantId.Value:N}:{culture.Value}";

    /// <summary>Tag on every cached menu entry of a tenant, so one change invalidates all of its languages at once.</summary>
    public static string TenantMenusTag(TenantId tenantId) => $"menus:tenant:{tenantId.Value:N}";
}
