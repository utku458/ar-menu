using ArMenu.Application.Team;
using ArMenu.Application.Team.Queries.GetTeam;
using ArMenu.Domain.Common;
using ArMenu.Domain.Memberships;
using ArMenu.Infrastructure.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ArMenu.Infrastructure.Queries.Team;

public sealed class GetTeamQueryHandler(ArMenuDbContext dbContext, TimeProvider timeProvider)
    : IQueryHandler<GetTeamQuery, Result<TeamResponse>>
{
    public async ValueTask<Result<TeamResponse>> Handle(GetTeamQuery query, CancellationToken cancellationToken)
    {
        // Memberships and invitations are read through the tenant filter and row-level security; users are global and
        // joined by id, so only the people of this team are ever read.
        var members = await dbContext.TenantMemberships
            .AsNoTracking()
            .Join(dbContext.Users, membership => membership.UserId, user => user.Id, (membership, user) => new { membership, user })
            .OrderBy(row => row.membership.Role)
            .ThenBy(row => row.membership.CreatedAt)
            .Select(row => new
            {
                row.membership.Id,
                row.user.FullName,
                row.user.Email,
                row.membership.Role,
                row.membership.CreatedAt,
                row.user.LastSignedInAt,
                UserId = row.user.Id,
            })
            .ToListAsync(cancellationToken);

        var invitations = await dbContext.TenantInvitations
            .AsNoTracking()
            .Where(invitation => invitation.Status == InvitationStatus.Pending)
            .Join(dbContext.Users, invitation => invitation.InvitedBy, user => user.Id, (invitation, inviter) => new { invitation, inviter.FullName })
            .OrderByDescending(row => row.invitation.SentAt)
            .ToListAsync(cancellationToken);

        var now = timeProvider.GetUtcNow();
        return new TeamResponse(
            [.. members.Select(member => new TeamMemberResponse(
                member.Id.Value,
                member.UserId.Value,
                member.FullName,
                member.Email.Value,
                member.Role.ToTeamRole(),
                member.CreatedAt,
                member.LastSignedInAt))],
            [.. invitations.Select(row => new PendingInvitationResponse(
                row.invitation.Id.Value,
                row.invitation.Email.Value,
                row.invitation.Role.ToTeamRole(),
                row.FullName,
                row.invitation.SentAt,
                row.invitation.ExpiresAt,
                row.invitation.IsExpired(now)))]);
    }
}
