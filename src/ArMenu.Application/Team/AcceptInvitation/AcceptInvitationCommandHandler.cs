using ArMenu.Application.Abstractions.Authentication;
using ArMenu.Application.Authentication;
using ArMenu.Application.Common.Validation;
using ArMenu.Domain.Common;
using ArMenu.Domain.Memberships;
using ArMenu.Domain.Users;
using Mediator;

namespace ArMenu.Application.Team.AcceptInvitation;

public sealed class AcceptInvitationCommandHandler(
    ITenantInvitationRepository invitations,
    ITenantMembershipRepository memberships,
    IUserRepository users,
    IInvitationTokenCodec tokenCodec,
    IPasswordHasher passwordHasher,
    SessionIssuer sessionIssuer,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<AcceptInvitationCommand, Result<AuthenticationResult>>
{
    public async ValueTask<Result<AuthenticationResult>> Handle(AcceptInvitationCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Scoped to the workspace in the URL: another business's invitation simply does not exist here.
        if (!tokenCodec.TryDecode(command.Token, out var invitationId, out var presentedHash) ||
            await invitations.GetByIdAsync(invitationId, cancellationToken) is not { } invitation)
        {
            return InvitationErrors.InvalidLink;
        }

        var now = timeProvider.GetUtcNow();
        var verification = invitation.Verify(presentedHash, now);
        if (verification.IsFailure)
        {
            return verification.Error;
        }

        var existing = await users.GetByEmailAsync(invitation.Email, cancellationToken);
        var user = existing is null
            ? Register(invitation.Email, command)
            : await ConfirmAccountAsync(existing, command.Password, now, cancellationToken);
        if (user.IsFailure)
        {
            return user.Error;
        }

        var membership = await memberships.GetByUserIdAsync(user.Value.Id, cancellationToken);
        if (membership is null)
        {
            membership = TenantMembership.Create(invitation.TenantId, user.Value.Id, invitation.Role);
            memberships.Add(membership);
        }

        // The link was e-mailed to this address: whoever used it receives mail there.
        user.Value.MarkEmailVerified(now);
        invitation.Accept(presentedHash, now);
        var authentication = sessionIssuer.StartSession(user.Value, membership);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return authentication;
    }

    private Result<User> Register(Email email, AcceptInvitationCommand command)
    {
        List<FieldError> errors = [];
        if (string.IsNullOrWhiteSpace(command.FullName))
        {
            errors.Add(new FieldError(nameof(command.FullName), UserErrors.FullNameRequired.Code, UserErrors.FullNameRequired.Description));
        }

        if (command.Password.Length < PasswordPolicy.MinLength)
        {
            errors.Add(new FieldError(nameof(command.Password), TeamErrors.PasswordTooShort.Code, TeamErrors.PasswordTooShort.Description));
        }

        if (errors.Count > 0)
        {
            return new ValidationError(errors);
        }

        var registered = User.Register(email, command.FullName!, passwordHasher.Hash(command.Password));
        if (registered.IsSuccess)
        {
            users.Add(registered.Value);
        }

        return registered;
    }

    /// <summary>The same checks and lockout as signing in: an invitation link must not become a way to guess passwords.</summary>
    private async Task<Result<User>> ConfirmAccountAsync(User user, string password, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (user.IsLockedOut(now))
        {
            passwordHasher.VerifyDecoy(password);
            return AuthenticationErrors.InvalidCredentials;
        }

        var verification = passwordHasher.Verify(user.PasswordHash, password);
        if (verification == PasswordVerificationResult.Failed)
        {
            user.RecordFailedSignIn(now);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return AuthenticationErrors.InvalidCredentials;
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.ChangePasswordHash(passwordHasher.Hash(password));
        }

        user.RecordSuccessfulSignIn(now);
        return user;
    }
}
