using ArMenu.Domain.Common;
using ArMenu.Domain.Menus;
using Mediator;

namespace ArMenu.Application.Menus.Items.SetMenuItemAvailability;

/// <summary>Marks an item as sold out or available again. Allowed for front-of-house staff.</summary>
public sealed record SetMenuItemAvailabilityCommand(MenuItemId ItemId, bool IsAvailable) : ICommand<Result>;
