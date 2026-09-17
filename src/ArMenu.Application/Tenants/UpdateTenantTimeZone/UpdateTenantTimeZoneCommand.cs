using ArMenu.Domain.Common;
using Mediator;

namespace ArMenu.Application.Tenants.UpdateTenantTimeZone;

/// <summary>Sets where the business's day begins. Days already counted keep the boundaries they were counted with.</summary>
public sealed record UpdateTenantTimeZoneCommand(string TimeZone) : ICommand<Result>;
