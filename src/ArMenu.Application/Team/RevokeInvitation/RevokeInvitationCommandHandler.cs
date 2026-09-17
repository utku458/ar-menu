using ArMenu.Domain.Common;
using ArMenu.Domain.Memberships;
using Mediator;

namespace ArMenu.Application.Team.RevokeInvitation;

public sealed class RevokeInvitationCommandHandler(ITenantInvitationRepository invitations, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    : ICommandHandler<RevokeInvitationCommand, Result>
{
    public async ValueTask<Result> Handle(RevokeInvitationCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var invitation = await invitations.GetByIdAsync(command.InvitationId, cancellationToken);
        if (invitation is null)
        {
            return InvitationErrors.NotFound;
        }

        var revocation = invitation.Revoke(timeProvider.GetUtcNow());
        if (revocation.IsFailure)
        {
            return revocation;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
