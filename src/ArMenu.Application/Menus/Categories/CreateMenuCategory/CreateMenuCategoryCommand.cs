using ArMenu.Domain.Common;
using ArMenu.Domain.Menus;
using Mediator;

namespace ArMenu.Application.Menus.Categories.CreateMenuCategory;

/// <summary>Adds a category to the end of the current tenant's menu.</summary>
public sealed record CreateMenuCategoryCommand(
    IReadOnlyDictionary<string, string> Name,
    IReadOnlyDictionary<string, string>? Description) : ICommand<Result<MenuCategoryId>>;
