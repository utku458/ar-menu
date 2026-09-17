using ArMenu.Domain.Common;

namespace ArMenu.Application.Menus;

/// <summary>Rules that span aggregates (menu content versus the tenant's settings).</summary>
public static class MenuErrors
{
    public static readonly Error DefaultCultureTranslationMissing = Error.Validation(
        "menu.default_culture_translation_missing",
        "A translation in the business's default language is required: it is what every guest falls back to.");

    public static readonly Error CultureNotSupported = Error.Validation(
        "menu.culture_not_supported", "Translations are only accepted in the languages the business supports.");

    public static readonly Error ReorderMismatch = Error.Validation(
        "menu.reorder_mismatch", "The new order must list every existing entry exactly once.");
}
