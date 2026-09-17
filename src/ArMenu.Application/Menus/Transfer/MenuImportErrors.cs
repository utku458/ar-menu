namespace ArMenu.Application.Menus.Transfer;

/// <summary>Codes of problems found in a menu spreadsheet, reported per row and column.</summary>
public static class MenuImportErrors
{
    public const string Empty = "menu_import.empty";
    public const string Malformed = "menu_import.malformed";
    public const string TooManyRows = "menu_import.too_many_rows";
    public const string MissingColumn = "menu_import.missing_column";
    public const string UnknownColumn = "menu_import.unknown_column";
    public const string ItemNotFound = "menu_import.item_not_found";
    public const string DuplicateItem = "menu_import.duplicate_item";
    public const string CategoryRequired = "menu_import.category_required";
    public const string CategoryNotFound = "menu_import.category_not_found";
    public const string CategoryAmbiguous = "menu_import.category_ambiguous";
    public const string PriceRequired = "menu_import.price_required";
    public const string PriceInvalid = "menu_import.price_invalid";
    public const string FlagInvalid = "menu_import.flag_invalid";
}
