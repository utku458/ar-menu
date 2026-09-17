using ArMenu.Domain.Common;

namespace ArMenu.Domain.Menus;

public static class MenuItemErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "menu_item.not_found", "The menu item does not exist.");

    public static readonly Error NameTooLong = Error.Validation(
        "menu_item.name_too_long",
        FormattableString.Invariant($"Item names cannot exceed {MenuItem.NameMaxLength} characters in any language."));

    public static readonly Error DescriptionTooLong = Error.Validation(
        "menu_item.description_too_long",
        FormattableString.Invariant($"Item descriptions cannot exceed {MenuItem.DescriptionMaxLength} characters in any language."));

    public static readonly Error DisplayOrderNegative = Error.Validation(
        "menu_item.display_order_negative", "Display order cannot be negative.");

    public static readonly Error ArModelGlbFormatInvalid = Error.Validation(
        "menu_item.ar_model_glb_format_invalid", "The 3D model must be a binary glTF (.glb) file.");

    public static readonly Error ArModelUsdzFormatInvalid = Error.Validation(
        "menu_item.ar_model_usdz_format_invalid", "The iOS AR model must be a .usdz file.");

    public static readonly Error ArModelPosterFormatInvalid = Error.Validation(
        "menu_item.ar_model_poster_format_invalid", "The model poster must be a .webp, .avif, .jpg or .png image.");

    public static readonly Error AllergenUnknown = Error.Validation(
        "menu_item.allergen_unknown",
        "Allergens are gluten, crustaceans, eggs, fish, peanuts, soybeans, milk, nuts, celery, mustard, sesame, sulphites, lupin or molluscs.");

    public static readonly Error DietaryLabelUnknown = Error.Validation(
        "menu_item.dietary_label_unknown", "Dietary labels are vegetarian, vegan or glutenFree.");

    /// <summary>A label promises a guest something the declared allergens break, e.g. a vegan dish with milk.</summary>
    public static Error LabelContradictsAllergen(string label, string allergen) => Error.Validation(
        "menu_item.dietary_label_contradicts_allergen",
        FormattableString.Invariant($"A dish labelled {label} cannot contain {allergen}."));
}
