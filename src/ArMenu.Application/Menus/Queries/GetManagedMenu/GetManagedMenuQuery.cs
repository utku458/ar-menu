using ArMenu.Domain.Common;
using Mediator;

namespace ArMenu.Application.Menus.Queries.GetManagedMenu;

/// <summary>The complete menu of the current tenant for staff: hidden entries and every translation included.</summary>
public sealed record GetManagedMenuQuery : IQuery<Result<ManagedMenuResponse>>;
