using ArMenu.Application.Abstractions.Authentication;
using ArMenu.Domain.Common;
using ArMenu.Domain.Users;

namespace ArMenu.Application.Accounts;

/// <summary>
/// Asks a signed-in person for their password again before something that cannot be undone. A stolen access token or
/// an unlocked laptop is then not enough to give a business away or delete an account. Wrong guesses count towards the
/// same lockout as signing in.
/// </summary>
public sealed class PasswordConfirmation(IPasswordHasher passwordHasher, IUnitOfWork unitOfWork, TimeProvider timeProvider)
{
    public async Task<Result> ConfirmAsync(User user, string password, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        var now = timeProvider.GetUtcNow();
        if (user.IsLockedOut(now))
        {
            passwordHasher.VerifyDecoy(password);
            return AccountErrors.TooManyAttempts;
        }

        var verification = passwordHasher.Verify(user.PasswordHash, password);
        if (verification == PasswordVerificationResult.Failed)
        {
            user.RecordFailedSignIn(now);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return user.IsLockedOut(now) ? AccountErrors.TooManyAttempts : AccountErrors.PasswordIncorrect;
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.ChangePasswordHash(passwordHasher.Hash(password));
        }

        return Result.Success();
    }
}
