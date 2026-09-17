using ArMenu.Application.Abstractions.Authentication;
using ArMenu.Application.MultiTenancy;
using ArMenu.Application.Team;
using ArMenu.Application.Team.Queries.GetInvitation;
using ArMenu.Domain.Common;
using ArMenu.Domain.Memberships;
using ArMenu.Infrastructure.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ArMenu.Infrastructure.Queries.Team;

public sealed class GetInvitationQueryHandler(
    ArMenuDbContext dbContext,
    ITenantContext tenantContext,
    IInvitationTokenCodec tokenCodec,
    TimeProvider timeProvider)
    : IQueryHandler<GetInvitationQuery, Result<InvitationResponse>>
{
    public async ValueTask<Result<InvitationResponse>> Handle(GetInvitationQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!tokenCodec.TryDecode(query.Token, out var invitationId, out var presentedHash) ||
            await dbContext.TenantInvitations.AsNoTracking().SingleOrDefaultAsync(candidate => candidate.Id == invitationId, cancellationToken) is not { } invitation)
        {
            return InvitationErrors.InvalidLink;
        }

        // Nothing about the invitation is revealed without its secret.
        var verification = invitation.Verify(presentedHash, timeProvider.GetUtcNow());
        if (verification.IsFailure)
        {
            return verification.Error;
        }

        var hasAccount = await dbContext.Users.AnyAsync(user => user.Email == invitation.Email, cancellationToken);
        return new InvitationResponse(
            tenantContext.RequireTenant().Name,
            invitation.Email.Value,
            invitation.Role.ToTeamRole(),
            invitation.ExpiresAt,
            hasAccount);
    }
}
