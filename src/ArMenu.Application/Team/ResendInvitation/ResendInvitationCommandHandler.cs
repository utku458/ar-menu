using ArMenu.Application.Abstractions.Authentication;
using ArMenu.Application.MultiTenancy;
using ArMenu.Domain.Common;
using ArMenu.Domain.Memberships;
using ArMenu.Domain.Users;
using Mediator;

namespace ArMenu.Application.Team.ResendInvitation;

public sealed class ResendInvitationCommandHandler(
    ITenantContext tenantContext,
    ITenantInvitationRepository invitations,
    IUserRepository users,
    IInvitationTokenCodec tokenCodec,
    InvitationMailer mailer,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<ResendInvitationCommand, Result<InvitationSent>>
{
    public async ValueTask<Result<InvitationSent>> Handle(ResendInvitationCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var sender = await users.GetByIdAsync(command.SentBy, cancellationToken)
            ?? throw new InvalidOperationException("The signed-in user does not exist.");
        if (!sender.IsEmailVerified)
        {
            return TeamErrors.EmailNotVerified;
        }

        var invitation = await invitations.GetByIdAsync(command.InvitationId, cancellationToken);
        if (invitation is null)
        {
            return InvitationErrors.NotFound;
        }

        var secret = tokenCodec.GenerateSecret();
        var renewal = invitation.Renew(secret.Hash, timeProvider.GetUtcNow());
        if (renewal.IsFailure)
        {
            return renewal.Error;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var sent = await mailer.SendAsync(invitation, secret, tenantContext.RequireTenant(), sender.FullName, command.Language, cancellationToken);
        return new InvitationSent(invitation.Id.Value, sent);
    }
}
