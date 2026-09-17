using ArMenu.Domain.Common;
using ArMenu.Domain.Menus;
using Mediator;

namespace ArMenu.Application.Menus.Categories.ReorderMenuCategories;

/// <summary>Sets the order of all categories at once, e.g. after drag and drop.</summary>
public sealed record ReorderMenuCategoriesCommand(IReadOnlyList<MenuCategoryId> CategoryIds) : ICommand<Result>;
