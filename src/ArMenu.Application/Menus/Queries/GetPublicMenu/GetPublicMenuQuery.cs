using ArMenu.Domain.Common;
using Mediator;

namespace ArMenu.Application.Menus.Queries.GetPublicMenu;

/// <summary>
/// The guest-facing menu of the current tenant: visible categories and items only, in one language.
/// </summary>
/// <param name="PreferredCultures">The guest's languages in order of preference (explicit choice first, then the browser's).</param>
/// <remarks>Read side: handled in Infrastructure with a cached projection, without loading aggregates.</remarks>
public sealed record GetPublicMenuQuery(IReadOnlyList<string> PreferredCultures) : IQuery<Result<PublicMenuResponse>>;
