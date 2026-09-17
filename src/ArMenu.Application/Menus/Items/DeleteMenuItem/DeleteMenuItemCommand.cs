using ArMenu.Domain.Common;
using ArMenu.Domain.Menus;
using Mediator;

namespace ArMenu.Application.Menus.Items.DeleteMenuItem;

/// <summary>Deletes an item (soft delete: it disappears from menus but stays restorable).</summary>
public sealed record DeleteMenuItemCommand(MenuItemId ItemId) : ICommand<Result>;
