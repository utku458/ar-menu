using ArMenu.Application.Authentication;
using ArMenu.Application.MultiTenancy;
using ArMenu.Domain.Common;
using ArMenu.Domain.Memberships;
using ArMenu.Domain.Tenants;
using Mediator;

namespace ArMenu.Application.Platform.EnterBusiness;

/// <remarks>
/// No session is started, and that is the point. A session may only exist for a membership — <c>user_sessions</c> has
/// a foreign key to <c>tenant_memberships</c>, which is what makes removing someone end their sessions — and the
/// administrator is a member of no business. So entering one produces an access token alone: it lasts minutes rather
/// than weeks, cannot be refreshed, and leaves nothing behind in the business to revoke later.
///
/// Inside the business the token is an owner's, so every screen and every rule there is the one its owner meets, and
/// row-level security still holds each request to that one business. What the administrator changes is recorded in
/// the business's history under the administrator's own name.
/// </remarks>
public sealed class EnterBusinessCommandHandler(
    PlatformAdministrator administrator,
    ITenantLookup tenantLookup,
    ITenantContextSetter tenantContextSetter,
    SessionIssuer sessionIssuer)
    : ICommandHandler<EnterBusinessCommand, Result<EnteredBusiness>>
{
    public async ValueTask<Result<EnteredBusiness>> Handle(EnterBusinessCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var caller = await administrator.RequireAsync(command.AdministratorId, cancellationToken);
        if (caller.IsFailure)
        {
            return caller.Error;
        }

        var business = await tenantLookup.FindByIdAsync(command.TenantId, cancellationToken);

        // Suspended and closed businesses turn every request away anyway; the platform workspace is not a business.
        if (business is not { IsActive: true } || business.Slug == TenantSlug.PlatformValue)
        {
            return PlatformErrors.BusinessNotFound;
        }

        // Nothing is written here, but the scope is bound anyway: anything this request goes on to read belongs to
        // the business that was entered, and to no other.
        tenantContextSetter.SetTenant(business);

        var accessToken = sessionIssuer.IssueAccessToken(caller.Value, business.Id, TenantRole.Owner, command.SessionId);

        return new EnteredBusiness(accessToken, business.Slug);
    }
}
