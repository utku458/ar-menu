using ArMenu.Application.MultiTenancy;
using ArMenu.Domain.Common;
using ArMenu.Domain.Localization;
using ArMenu.Domain.Menus;
using ArMenu.Domain.Pricing;
using Mediator;

namespace ArMenu.Application.Menus.Transfer.ImportMenu;

public sealed class ImportMenuCommandHandler(
    ITenantContext tenantContext,
    IMenuCategoryRepository categories,
    IMenuItemRepository items,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ImportMenuCommand, Result<MenuImportResponse>>
{
    public async ValueTask<Result<MenuImportResponse>> Handle(ImportMenuCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var tenant = tenantContext.RequireTenant();
        var table = CsvTable.Read(command.Csv);
        if (table is null)
        {
            return Failed(new MenuImportError(1, null, MenuImportErrors.Malformed, "A quoted field is never closed."));
        }

        if (table.Count < 2)
        {
            return Failed(new MenuImportError(1, null, MenuImportErrors.Empty, "The file has no dishes."));
        }

        if (table.Count - 1 > ImportMenuCommand.MaxRows)
        {
            return Failed(new MenuImportError(1, null, MenuImportErrors.TooManyRows, $"A file can hold at most {ImportMenuCommand.MaxRows} dishes."));
        }

        var header = ReadHeader(table[0].Cells, tenant, out var headerErrors);
        if (headerErrors.Count > 0)
        {
            return Failed([.. headerErrors]);
        }

        var session = new ImportSession(tenant, header, await categories.ListAsync(cancellationToken), items, cancellationToken);
        await session.LoadItemsAsync();

        foreach (var (line, cells) in table.Skip(1))
        {
            await session.ApplyRowAsync(line, cells);
        }

        if (session.Errors.Count > 0 || command.DryRun)
        {
            return session.Result(applied: false);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return session.Result(applied: true);
    }

    private static MenuImportResponse Failed(params MenuImportError[] errors) => new(false, 0, 0, 0, [], errors);

    private static Dictionary<string, int> ReadHeader(string[] cells, TenantInfo tenant, out List<MenuImportError> errors)
    {
        errors = [];
        var known = MenuCsvColumns.For(tenant).ToHashSet(StringComparer.Ordinal);
        var columns = new Dictionary<string, int>(StringComparer.Ordinal);

        for (var index = 0; index < cells.Length; index++)
        {
            var name = cells[index].Trim().ToLowerInvariant();
            if (name.Length == 0)
            {
                continue;
            }

            if (!known.Contains(name) || !columns.TryAdd(name, index))
            {
                errors.Add(new MenuImportError(1, name, MenuImportErrors.UnknownColumn,
                    "The column is not one of the menu's, or appears twice. Languages must be offered by the business first."));
            }
        }

        foreach (var required in new[] { MenuCsvColumns.Id, MenuCsvColumns.NamePrefix + tenant.DefaultCulture })
        {
            if (!columns.ContainsKey(required))
            {
                errors.Add(new MenuImportError(1, required, MenuImportErrors.MissingColumn, "The file needs this column."));
            }
        }

        return columns;
    }

    /// <summary>One file being applied to the tracked menu. Nothing is saved here; the handler decides.</summary>
    private sealed class ImportSession(
        TenantInfo tenant,
        Dictionary<string, int> columns,
        IReadOnlyList<MenuCategory> categories,
        IMenuItemRepository items,
        CancellationToken cancellationToken)
    {
        private readonly Currency _currency = Currency.Create(tenant.Currency).Value;
        private readonly CultureCode _defaultCulture = CultureCode.Create(tenant.DefaultCulture).Value;
        private readonly Dictionary<MenuItemId, MenuItem> _items = [];
        private readonly HashSet<MenuItemId> _seen = [];
        private readonly Dictionary<MenuCategoryId, int> _nextOrder = [];
        private readonly List<MenuImportChange> _changes = [];
        private int _unchanged;

        public List<MenuImportError> Errors { get; } = [];

        public async Task LoadItemsAsync()
        {
            foreach (var category in categories)
            {
                foreach (var item in await items.ListByCategoryAsync(category.Id, cancellationToken))
                {
                    _items[item.Id] = item;
                }
            }
        }

        public MenuImportResponse Result(bool applied) => new(
            applied,
            _changes.Count(change => change.Kind == "created"),
            _changes.Count(change => change.Kind == "updated"),
            _unchanged,
            _changes,
            Errors);

        public async Task ApplyRowAsync(int line, string[] cells)
        {
            var errorsBefore = Errors.Count;
            string? Cell(string column) =>
                columns.TryGetValue(column, out var index) ? CsvTable.Unescape(index < cells.Length ? cells[index].Trim() : "") : null;
            void Fail(string? column, string code, string message) => Errors.Add(new MenuImportError(line, column, code, message));

            // Resolve everything first; the dish is touched only when the whole row is valid.
            var idText = Cell(MenuCsvColumns.Id) ?? "";
            MenuItem? existing = null;
            if (idText.Length > 0)
            {
                if (!Guid.TryParse(idText, out var id) || !_items.TryGetValue(MenuItemId.From(id), out existing))
                {
                    // Checking the row as a new dish would only add noise: it was meant to change one.
                    Fail(MenuCsvColumns.Id, MenuImportErrors.ItemNotFound, "No dish of this menu has this id. Leave it empty to add a dish.");
                    CheckFormats(Cell, Fail);
                    return;
                }
                else if (!_seen.Add(existing.Id))
                {
                    Fail(MenuCsvColumns.Id, MenuImportErrors.DuplicateItem, "The dish appears more than once in the file.");
                }
            }

            var category = ResolveCategory(Cell(MenuCsvColumns.Category), existing, Fail);
            var name = Texts(existing?.Name, MenuCsvColumns.NamePrefix, Cell, required: true, MenuItem.NameMaxLength, MenuItemErrors.NameTooLong, Fail);
            var description = Texts(existing?.Description, MenuCsvColumns.DescriptionPrefix, Cell, required: false, MenuItem.DescriptionMaxLength, MenuItemErrors.DescriptionTooLong, Fail);
            var price = ResolvePrice(Cell(MenuCsvColumns.Price), existing, Fail);
            var visible = Flag(Cell(MenuCsvColumns.Visible), MenuCsvColumns.Visible, Fail);
            var available = Flag(Cell(MenuCsvColumns.Available), MenuCsvColumns.Available, Fail);
            var dietary = ResolveDietary(Cell(MenuCsvColumns.Allergens), Cell(MenuCsvColumns.Dietary), existing, Fail);

            if (Errors.Count > errorsBefore || category is null || name.Text is null || price is null || dietary is null)
            {
                return;
            }

            if (existing is null)
            {
                var created = MenuItem.Create(tenant.Id, category.Id, name.Text, price, await NextOrderAsync(category.Id), description.Text).Value;
                ApplyFlags(created, visible, available);
                created.ChangeDietaryInformation(dietary);
                items.Add(created);
                _changes.Add(new MenuImportChange(line, created.Id.Value, name.Text.Resolve(_defaultCulture, _defaultCulture), "created", []));
                return;
            }

            List<string> fields = [];
            if (existing.CategoryId != category.Id)
            {
                existing.MoveToCategory(category.Id);
                existing.ChangeDisplayOrder(await NextOrderAsync(category.Id));
                fields.Add(MenuCsvColumns.Category);
            }

            if (!Equals(existing.Name, name.Text))
            {
                existing.Rename(name.Text);
                fields.Add("name");
            }

            if (description.Present && !Equals(existing.Description, description.Text))
            {
                existing.ChangeDescription(description.Text);
                fields.Add("description");
            }

            if (existing.Price != price)
            {
                existing.ChangePrice(price);
                fields.Add(MenuCsvColumns.Price);
            }

            if (visible is { } isVisible && existing.IsVisible != isVisible)
            {
                fields.Add(MenuCsvColumns.Visible);
            }

            if (available is { } isAvailable && existing.IsAvailable != isAvailable)
            {
                fields.Add(MenuCsvColumns.Available);
            }

            ApplyFlags(existing, visible, available);

            var currentAllergens = existing.Allergens;
            if (currentAllergens is null ? dietary.Allergens is not null : dietary.Allergens is null || !currentAllergens.SequenceEqual(dietary.Allergens))
            {
                fields.Add(MenuCsvColumns.Allergens);
            }

            if (!existing.DietaryLabels.SequenceEqual(dietary.Labels))
            {
                fields.Add(MenuCsvColumns.Dietary);
            }

            existing.ChangeDietaryInformation(dietary);

            if (fields.Count == 0)
            {
                _unchanged++;
            }
            else
            {
                _changes.Add(new MenuImportChange(line, existing.Id.Value, existing.Name.Resolve(_defaultCulture, _defaultCulture), "updated", fields));
            }
        }

        private static void CheckFormats(Func<string, string?> cell, Action<string?, string, string> fail)
        {
            if (cell(MenuCsvColumns.Price) is { Length: > 0 } price && MenuCsvColumns.ParsePrice(price) is null)
            {
                fail(MenuCsvColumns.Price, MenuImportErrors.PriceInvalid, "Write the price as a number such as 185 or 185.50, without thousands separators.");
            }

            Flag(cell(MenuCsvColumns.Visible), MenuCsvColumns.Visible, fail);
            Flag(cell(MenuCsvColumns.Available), MenuCsvColumns.Available, fail);
            ResolveDietary(cell(MenuCsvColumns.Allergens), cell(MenuCsvColumns.Dietary), null, fail);
        }

        /// <summary>
        /// The allergens and labels a row asks for. A column missing from the file keeps what the dish has; in the
        /// allergens column an empty cell means "not declared" and <c>none</c> means "contains none", as exported.
        /// </summary>
        private static DietaryInformation? ResolveDietary(
            string? allergensCell,
            string? labelsCell,
            MenuItem? existing,
            Action<string?, string, string> fail)
        {
            IReadOnlyList<string>? allergens = allergensCell switch
            {
                null => existing?.Allergens?.Select(DietaryInformation.CodeOf).ToList(),
                "" => null,
                _ when string.Equals(allergensCell, MenuCsvColumns.NoAllergens, StringComparison.OrdinalIgnoreCase) => [],
                _ => MenuCsvColumns.ParseCodes(allergensCell),
            };
            IReadOnlyList<string> labels = labelsCell is null
                ? [.. existing?.DietaryLabels.Select(DietaryInformation.CodeOf) ?? []]
                : MenuCsvColumns.ParseCodes(labelsCell);

            var known = true;
            foreach (var code in allergens ?? [])
            {
                if (DietaryInformation.ParseAllergen(code) is { IsFailure: true } unknown)
                {
                    fail(MenuCsvColumns.Allergens, unknown.Error.Code, $"\"{code}\" is not an allergen. {unknown.Error.Description} Write none for a dish without any.");
                    known = false;
                }
            }

            foreach (var code in labels)
            {
                if (DietaryInformation.ParseLabel(code) is { IsFailure: true } unknown)
                {
                    fail(MenuCsvColumns.Dietary, unknown.Error.Code, $"\"{code}\" is not a dietary label. {unknown.Error.Description}");
                    known = false;
                }
            }

            if (!known)
            {
                return null;
            }

            var information = DietaryInformation.FromCodes(allergens, labels);
            if (information.IsFailure)
            {
                fail(MenuCsvColumns.Dietary, information.Error.Code, information.Error.Description);
                return null;
            }

            return information.Value;
        }

        private MenuCategory? ResolveCategory(string? cell, MenuItem? existing, Action<string?, string, string> fail)
        {
            if (string.IsNullOrEmpty(cell))
            {
                if (existing is not null)
                {
                    return categories.FirstOrDefault(category => category.Id == existing.CategoryId);
                }

                fail(MenuCsvColumns.Category, MenuImportErrors.CategoryRequired, "A new dish needs the name of its category.");
                return null;
            }

            var matches = categories
                .Where(category => string.Equals(category.Name.Resolve(_defaultCulture, _defaultCulture).Trim(), cell, StringComparison.OrdinalIgnoreCase))
                .ToList();

            switch (matches.Count)
            {
                case 1:
                    return matches[0];
                case 0:
                    fail(MenuCsvColumns.Category, MenuImportErrors.CategoryNotFound, "No category has this name in the default language. Create it in the menu first.");
                    return null;
                default:
                    fail(MenuCsvColumns.Category, MenuImportErrors.CategoryAmbiguous, "Several categories have this name; rename one of them first.");
                    return null;
            }
        }

        private (bool Present, LocalizedText? Text) Texts(
            LocalizedText? current,
            string prefix,
            Func<string, string?> cell,
            bool required,
            int maxLength,
            Error tooLong,
            Action<string?, string, string> fail)
        {
            var translations = new Dictionary<string, string>(current?.Translations ?? new Dictionary<string, string>());
            var present = false;
            foreach (var culture in tenant.SupportedCultures)
            {
                if (cell(prefix + culture) is not { } value)
                {
                    continue;
                }

                present = true;
                if (value.Length == 0)
                {
                    translations.Remove(culture);
                }
                else
                {
                    translations[culture] = value;
                }
            }

            var column = prefix + tenant.DefaultCulture;
            if (translations.Count == 0 && !required)
            {
                return (present, null);
            }

            var text = MenuTranslations.Create(translations, tenant);
            if (text.IsFailure)
            {
                fail(column, text.Error.Code, text.Error.Description);
                return (present, null);
            }

            if (text.Value.ExceedsLength(maxLength))
            {
                fail(column, tooLong.Code, tooLong.Description);
                return (present, null);
            }

            return (present, text.Value);
        }

        private Money? ResolvePrice(string? cell, MenuItem? existing, Action<string?, string, string> fail)
        {
            if (string.IsNullOrEmpty(cell))
            {
                if (existing is not null)
                {
                    return existing.Price;
                }

                fail(MenuCsvColumns.Price, MenuImportErrors.PriceRequired, "A new dish needs a price.");
                return null;
            }

            if (MenuCsvColumns.ParsePrice(cell) is not { } amount)
            {
                fail(MenuCsvColumns.Price, MenuImportErrors.PriceInvalid, "Write the price as a number such as 185 or 185.50, without thousands separators.");
                return null;
            }

            var money = Money.Create(amount, _currency);
            if (money.IsFailure)
            {
                fail(MenuCsvColumns.Price, money.Error.Code, money.Error.Description);
                return null;
            }

            return money.Value;
        }

        private static bool? Flag(string? cell, string column, Action<string?, string, string> fail)
        {
            if (string.IsNullOrEmpty(cell))
            {
                return null;
            }

            var flag = MenuCsvColumns.ParseFlag(cell);
            if (flag is null)
            {
                fail(column, MenuImportErrors.FlagInvalid, "Write yes or no.");
            }

            return flag;
        }

        private static void ApplyFlags(MenuItem item, bool? visible, bool? available)
        {
            if (visible == true)
            {
                item.Show();
            }
            else if (visible == false)
            {
                item.Hide();
            }

            if (available == true)
            {
                item.MarkAsAvailable();
            }
            else if (available == false)
            {
                item.MarkAsSoldOut();
            }
        }

        private async Task<int> NextOrderAsync(MenuCategoryId categoryId)
        {
            if (!_nextOrder.TryGetValue(categoryId, out var next))
            {
                next = await items.GetNextDisplayOrderAsync(categoryId, cancellationToken);
            }

            _nextOrder[categoryId] = next + 1;
            return next;
        }
    }
}
