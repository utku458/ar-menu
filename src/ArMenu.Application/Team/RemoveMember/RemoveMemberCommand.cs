using ArMenu.Domain.Common;
using ArMenu.Domain.Memberships;
using Mediator;

namespace ArMenu.Application.Team.RemoveMember;

/// <summary>
/// Removes a member from the team. Their sessions end at once, so they cannot refresh; an access token already issued
/// stays valid until it expires, at most the access token lifetime.
/// </summary>
public sealed record RemoveMemberCommand(TenantMembershipId MembershipId) : ICommand<Result>;
