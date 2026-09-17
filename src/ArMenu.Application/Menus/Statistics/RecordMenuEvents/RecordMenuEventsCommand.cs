using ArMenu.Domain.Common;
using Mediator;

namespace ArMenu.Application.Menus.Statistics.RecordMenuEvents;

/// <summary>
/// Counts what guests do on the menu of the tenant bound to the current scope. Nothing identifies a guest: no cookie, no
/// id, no address is stored, only how often each thing happened each day.
/// </summary>
public sealed record RecordMenuEventsCommand(IReadOnlyList<MenuEventInput> Events) : ICommand<Result>;

public sealed record MenuEventInput(MenuEventType Type, Guid? ItemId);
