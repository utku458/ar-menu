using ArMenu.Domain.Common;
using ArMenu.Domain.Menus;
using Mediator;

namespace ArMenu.Application.Menus.Items.DetachMenuItemArModel;

public sealed record DetachMenuItemArModelCommand(MenuItemId ItemId) : ICommand<Result>;
