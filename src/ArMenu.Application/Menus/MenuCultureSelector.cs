using ArMenu.Application.MultiTenancy;
using ArMenu.Domain.Localization;

namespace ArMenu.Application.Menus;

public static class MenuCultureSelector
{
    /// <summary>
    /// Picks the language a menu is served in: the first preferred culture the tenant supports, trying each exact tag and
    /// then its neutral parent (<c>de-AT</c> → <c>de</c>), falling back to the tenant's default culture.
    /// </summary>
    public static CultureCode Select(IEnumerable<string> preferredCultures, TenantInfo tenant)
    {
        ArgumentNullException.ThrowIfNull(preferredCultures);
        ArgumentNullException.ThrowIfNull(tenant);

        foreach (var preferred in preferredCultures)
        {
            if (CultureCode.Create(preferred) is not { IsSuccess: true } culture)
            {
                continue;
            }

            if (tenant.SupportedCultures.Contains(culture.Value.Value))
            {
                return culture.Value;
            }

            if (culture.Value.Parent is { } parent && tenant.SupportedCultures.Contains(parent.Value))
            {
                return parent;
            }
        }

        return CultureCode.Create(tenant.DefaultCulture).Value;
    }
}
