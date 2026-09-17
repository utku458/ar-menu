using ArMenu.Domain.Common;
using Mediator;

namespace ArMenu.Application.Tenants.UpdateTenantBranding;

/// <summary>
/// Sets how the business presents itself: the name guests read, the logo above its menu and the colour the menu is
/// painted with. The whole appearance is sent at once; <see langword="null"/> means "no logo" and "no colour", not
/// "leave as it is", so removing either is an ordinary save.
/// </summary>
/// <remarks>
/// <c>LogoPath</c> is the storage key of a logo this business published; <c>AccentColor</c> is written <c>#rrggbb</c>.
/// </remarks>
public sealed record UpdateTenantBrandingCommand(string Name, string? LogoPath, string? AccentColor) : ICommand<Result>;
