using ArMenu.Application.Abstractions.Authentication;
using ArMenu.Application.MultiTenancy;
using ArMenu.Domain.Common;
using ArMenu.Domain.Memberships;
using ArMenu.Domain.Users;
using Mediator;

namespace ArMenu.Application.Team.InviteMember;

public sealed class InviteMemberCommandHandler(
    ITenantContext tenantContext,
    ITenantInvitationRepository invitations,
    ITenantMembershipRepository memberships,
    IUserRepository users,
    IInvitationTokenCodec tokenCodec,
    InvitationMailer mailer,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<InviteMemberCommand, Result<InvitationSent>>
{
    public async ValueTask<Result<InvitationSent>> Handle(InviteMemberCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var tenant = tenantContext.RequireTenant();
        var email = Email.Create(command.Email).Value;
        var now = timeProvider.GetUtcNow();

        var existingUser = await users.GetByEmailAsync(email, cancellationToken);
        if (existingUser is not null && await memberships.GetByUserIdAsync(existingUser.Id, cancellationToken) is not null)
        {
            return MembershipErrors.AlreadyMember;
        }

        // One pending invitation per address. An expired one is withdrawn and replaced, so a late "invite again" works.
        if (await invitations.GetPendingByEmailAsync(email, cancellationToken) is { } pending)
        {
            if (pending.IsOpen(now))
            {
                return InvitationErrors.AlreadyPending;
            }

            pending.Revoke(now);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var inviter = await users.GetByIdAsync(command.InvitedBy, cancellationToken)
            ?? throw new InvalidOperationException("The signed-in user does not exist.");

        // The platform sends the e-mail in the owner's name: only from an address shown to be theirs.
        if (!inviter.IsEmailVerified)
        {
            return TeamErrors.EmailNotVerified;
        }

        var secret = tokenCodec.GenerateSecret();
        var invitation = TenantInvitation.Create(tenant.Id, email, command.Role.ToTenantRole(), inviter.Id, secret.Hash, now);
        if (invitation.IsFailure)
        {
            return invitation.Error;
        }

        invitations.Add(invitation.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var sent = await mailer.SendAsync(invitation.Value, secret, tenant, inviter.FullName, command.Language, cancellationToken);
        return new InvitationSent(invitation.Value.Id.Value, sent);
    }
}
