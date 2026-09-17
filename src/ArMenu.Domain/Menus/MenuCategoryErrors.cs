using ArMenu.Domain.Common;

namespace ArMenu.Domain.Menus;

public static class MenuCategoryErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "menu_category.not_found", "The menu category does not exist.");

    public static readonly Error NotEmpty = Error.Conflict(
        "menu_category.not_empty", "Only empty categories can be deleted; move or delete their items first.");

    public static readonly Error NameTooLong = Error.Validation(
        "menu_category.name_too_long",
        FormattableString.Invariant($"Category names cannot exceed {MenuCategory.NameMaxLength} characters in any language."));

    public static readonly Error DescriptionTooLong = Error.Validation(
        "menu_category.description_too_long",
        FormattableString.Invariant($"Category descriptions cannot exceed {MenuCategory.DescriptionMaxLength} characters in any language."));

    public static readonly Error DisplayOrderNegative = Error.Validation(
        "menu_category.display_order_negative", "Display order cannot be negative.");
}
