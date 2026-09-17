using ArMenu.Application.MultiTenancy;
using ArMenu.Domain.Common;
using ArMenu.Domain.Localization;

namespace ArMenu.Application.Menus;

/// <summary>Creates menu texts that are consistent with the tenant's language settings.</summary>
internal static class MenuTranslations
{
    public static Result<LocalizedText> Create(IReadOnlyDictionary<string, string> translations, TenantInfo tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        var text = LocalizedText.Create(translations);
        if (text.IsFailure)
        {
            return text.Error;
        }

        if (!text.Value.Translations.ContainsKey(tenant.DefaultCulture))
        {
            return MenuErrors.DefaultCultureTranslationMissing;
        }

        return text.Value.Translations.Keys.All(tenant.SupportedCultures.Contains)
            ? text
            : MenuErrors.CultureNotSupported;
    }

    /// <summary>Like <see cref="Create"/>, but a missing or empty set of translations means "no text".</summary>
    public static Result<LocalizedText?> CreateOptional(IReadOnlyDictionary<string, string>? translations, TenantInfo tenant)
    {
        if (translations is null || translations.Count == 0)
        {
            return Result.Success<LocalizedText?>(null);
        }

        var text = Create(translations, tenant);
        return text.IsSuccess ? text.Value : text.Error;
    }
}
