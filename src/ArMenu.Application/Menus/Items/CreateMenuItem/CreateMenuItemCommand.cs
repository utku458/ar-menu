using ArMenu.Domain.Common;
using ArMenu.Domain.Menus;
using Mediator;

namespace ArMenu.Application.Menus.Items.CreateMenuItem;

/// <summary>
/// Adds an item to the end of a category. The price is in the tenant's currency. <c>Allergens</c> left
/// <see langword="null"/> means they were not declared; an empty list declares that the dish contains none.
/// </summary>
public sealed record CreateMenuItemCommand(
    MenuCategoryId CategoryId,
    IReadOnlyDictionary<string, string> Name,
    IReadOnlyDictionary<string, string>? Description,
    decimal Price,
    IReadOnlyList<string>? Allergens = null,
    IReadOnlyList<string>? DietaryLabels = null) : ICommand<Result<MenuItemId>>;
