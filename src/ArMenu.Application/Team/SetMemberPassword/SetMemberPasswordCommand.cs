using ArMenu.Domain.Common;
using ArMenu.Domain.Memberships;
using Mediator;

namespace ArMenu.Application.Team.SetMemberPassword;

/// <summary>
/// Gives a team member a new password, for the forgotten password of an account that has no mailbox to receive a
/// reset link. Every session the member had ends, so a password someone else learned stops working at once.
/// </summary>
public sealed record SetMemberPasswordCommand(TenantMembershipId MembershipId, string NewPassword) : ICommand<Result>
{
    public override string ToString() => $"SetMemberPasswordCommand {{ MembershipId = {MembershipId}, *** }}";
}
