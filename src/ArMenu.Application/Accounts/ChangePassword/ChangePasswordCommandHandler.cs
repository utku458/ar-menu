using ArMenu.Application.Abstractions.Authentication;
using ArMenu.Domain.Common;
using ArMenu.Domain.Users;
using Mediator;

namespace ArMenu.Application.Accounts.ChangePassword;

public sealed class ChangePasswordCommandHandler(
    IUserRepository users,
    PasswordConfirmation passwordConfirmation,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<ChangePasswordCommand, Result>
{
    public async ValueTask<Result> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var user = await users.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null || user.IsErased)
        {
            return AccountErrors.PasswordIncorrect;
        }

        // Wrong guesses count towards the same lockout as signing in: a stolen token is not a way to try passwords.
        var confirmation = await passwordConfirmation.ConfirmAsync(user, command.CurrentPassword, cancellationToken);
        if (confirmation.IsFailure)
        {
            return confirmation;
        }

        user.SetPassword(passwordHasher.Hash(command.NewPassword), timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
