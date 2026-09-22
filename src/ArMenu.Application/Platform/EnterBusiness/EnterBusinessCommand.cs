using ArMenu.Application.Abstractions.Authentication;
using ArMenu.Domain.Common;
using ArMenu.Domain.Sessions;
using ArMenu.Domain.Tenants;
using ArMenu.Domain.Users;
using Mediator;

namespace ArMenu.Application.Platform.EnterBusiness;

/// <summary>
/// The platform administrator starts working inside a business: an access token for it with the owner's role, so its
/// menu, staff and settings open with the same screens and the same rules its owner has. Who is asking, and the
/// session they are asking from, come from their access token.
/// </summary>
public sealed record EnterBusinessCommand(UserId AdministratorId, UserSessionId SessionId, TenantId TenantId)
    : ICommand<Result<EnteredBusiness>>;

/// <param name="AccessToken">Short-lived and on its own: there is no refresh token for a business one is not part of.</param>
/// <param name="Slug">The business entered, which the dashboard needs to address its screens.</param>
public sealed record EnteredBusiness(AccessToken AccessToken, string Slug);
