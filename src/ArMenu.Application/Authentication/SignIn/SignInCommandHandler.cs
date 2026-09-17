using ArMenu.Application.Abstractions.Authentication;
using ArMenu.Domain.Common;
using ArMenu.Domain.Memberships;
using ArMenu.Domain.Users;
using Mediator;

namespace ArMenu.Application.Authentication.SignIn;

public sealed class SignInCommandHandler(
    IUserRepository users,
    ITenantMembershipRepository memberships,
    IPasswordHasher passwordHasher,
    SessionIssuer sessionIssuer,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<SignInCommand, Result<AuthenticationResult>>
{
    public async ValueTask<Result<AuthenticationResult>> Handle(SignInCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var now = timeProvider.GetUtcNow();
        var email = Email.Create(command.Email);
        var user = email.IsSuccess ? await users.GetByEmailAsync(email.Value, cancellationToken) : null;

        if (user is null || user.IsLockedOut(now))
        {
            // Same work, same answer as a wrong password: timing and responses reveal neither accounts nor lockouts.
            // A locked account is not even checked, so guessing cannot continue during the lockout.
            passwordHasher.VerifyDecoy(command.Password);
            return AuthenticationErrors.InvalidCredentials;
        }

        var verification = passwordHasher.Verify(user.PasswordHash, command.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            user.RecordFailedSignIn(now);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return AuthenticationErrors.InvalidCredentials;
        }

        // The workspace in the URL decides the tenant: a valid account is not enough, it must be a member of that tenant.
        var membership = await memberships.GetByUserIdAsync(user.Id, cancellationToken);
        if (membership is null)
        {
            return AuthenticationErrors.InvalidCredentials;
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.ChangePasswordHash(passwordHasher.Hash(command.Password));
        }

        user.RecordSuccessfulSignIn(now);
        var authentication = sessionIssuer.StartSession(user, membership);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return authentication;
    }
}
