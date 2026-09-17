using ArMenu.Domain.Common;
using ArMenu.Domain.Memberships;
using Mediator;

namespace ArMenu.Application.Team.ChangeMemberRole;

/// <summary>Moves a member between manager and staff. It applies from the member's next access token.</summary>
public sealed record ChangeMemberRoleCommand(TenantMembershipId MembershipId, TeamRole Role) : ICommand<Result>;
