using ArMenu.Domain.Common;
using ArMenu.Domain.Menus;
using Mediator;

namespace ArMenu.Application.Menus.Categories.UpdateMenuCategory;

public sealed record UpdateMenuCategoryCommand(
    MenuCategoryId CategoryId,
    IReadOnlyDictionary<string, string> Name,
    IReadOnlyDictionary<string, string>? Description,
    bool IsVisible) : ICommand<Result>;
