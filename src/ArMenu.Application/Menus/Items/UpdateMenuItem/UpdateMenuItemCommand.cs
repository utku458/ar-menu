using ArMenu.Domain.Common;
using ArMenu.Domain.Menus;
using Mediator;

namespace ArMenu.Application.Menus.Items.UpdateMenuItem;

/// <summary>
/// Replaces an item's details, its allergens and dietary labels included. Moving it to another category appends it to
/// that category. <c>Allergens</c> left <see langword="null"/> means they are not declared.
/// </summary>
public sealed record UpdateMenuItemCommand(
    MenuItemId ItemId,
    MenuCategoryId CategoryId,
    IReadOnlyDictionary<string, string> Name,
    IReadOnlyDictionary<string, string>? Description,
    decimal Price,
    bool IsVisible,
    IReadOnlyList<string>? Allergens = null,
    IReadOnlyList<string>? DietaryLabels = null) : ICommand<Result>;
