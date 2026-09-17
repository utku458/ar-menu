using ArMenu.Domain.Common;
using Mediator;

namespace ArMenu.Application.Tenants.UpdateTenantLanguages;

/// <summary>
/// Sets the languages a menu is offered in. Translations in a removed language are kept, so adding it back restores them.
/// </summary>
public sealed record UpdateTenantLanguagesCommand(string DefaultCulture, IReadOnlyList<string> SupportedCultures) : ICommand<Result>;
