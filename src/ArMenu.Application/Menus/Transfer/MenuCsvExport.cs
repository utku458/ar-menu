using ArMenu.Application.MultiTenancy;
using ArMenu.Domain.Localization;
using ArMenu.Domain.Menus;

namespace ArMenu.Application.Menus.Transfer;

/// <summary>Writes a menu spreadsheet; shared by the export and by tests that round-trip it.</summary>
public static class MenuCsvExport
{
    public static string Write(TenantInfo tenant, IReadOnlyList<MenuCategory> categories, IReadOnlyList<MenuItem> items)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        ArgumentNullException.ThrowIfNull(categories);
        ArgumentNullException.ThrowIfNull(items);

        var cultures = MenuCsvColumns.Cultures(tenant);
        var defaultCulture = CultureCode.Create(tenant.DefaultCulture).Value;
        var rows = new List<IReadOnlyList<string>> { MenuCsvColumns.For(tenant) };

        foreach (var category in categories.OrderBy(category => category.DisplayOrder))
        {
            var categoryName = category.Name.Resolve(defaultCulture, defaultCulture);
            foreach (var item in items.Where(item => item.CategoryId == category.Id).OrderBy(item => item.DisplayOrder))
            {
                rows.Add(
                [
                    item.Id.Value.ToString(),
                    categoryName,
                    .. cultures.Select(culture => item.Name.Translations.GetValueOrDefault(culture, "")),
                    .. cultures.Select(culture => item.Description?.Translations.GetValueOrDefault(culture, "") ?? ""),
                    MenuCsvColumns.FormatPrice(item.Price.Amount),
                    MenuCsvColumns.FormatFlag(item.IsVisible),
                    MenuCsvColumns.FormatFlag(item.IsAvailable),
                    item.Allergens switch
                    {
                        null => "",
                        [] => MenuCsvColumns.NoAllergens,
                        var allergens => MenuCsvColumns.FormatCodes(allergens.Select(DietaryInformation.CodeOf)),
                    },
                    MenuCsvColumns.FormatCodes(item.DietaryLabels.Select(DietaryInformation.CodeOf)),
                ]);
            }
        }

        // A byte order mark tells spreadsheets the file is UTF-8, so "Künefe" does not open as "KÃ¼nefe".
        return "﻿" + CsvTable.Write(rows);
    }
}
