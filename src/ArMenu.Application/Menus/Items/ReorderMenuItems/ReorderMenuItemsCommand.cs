using ArMenu.Domain.Common;
using ArMenu.Domain.Menus;
using Mediator;

namespace ArMenu.Application.Menus.Items.ReorderMenuItems;

/// <summary>Sets the order of all items in a category at once.</summary>
public sealed record ReorderMenuItemsCommand(MenuCategoryId CategoryId, IReadOnlyList<MenuItemId> ItemIds) : ICommand<Result>;
