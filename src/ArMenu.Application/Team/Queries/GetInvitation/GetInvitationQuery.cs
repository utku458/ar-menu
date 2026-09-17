using ArMenu.Domain.Common;
using Mediator;

namespace ArMenu.Application.Team.Queries.GetInvitation;

/// <summary>What an invitation link offers, for the person holding it: which business, which role, which address.</summary>
public sealed record GetInvitationQuery(string Token) : IQuery<Result<InvitationResponse>>
{
    public override string ToString() => "GetInvitationQuery { *** }";
}

/// <summary>
/// An invitation as its holder sees it, including whether the address already has an account, to be confirmed with
/// its password.
/// </summary>
public sealed record InvitationResponse(string BusinessName, string Email, TeamRole Role, DateTimeOffset ExpiresAt, bool HasAccount);
