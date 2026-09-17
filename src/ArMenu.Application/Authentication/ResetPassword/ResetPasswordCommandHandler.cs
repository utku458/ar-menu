using ArMenu.Application.Abstractions.Authentication;
using ArMenu.Application.Emails;
using ArMenu.Domain.Common;
using ArMenu.Domain.Users;
using Mediator;

namespace ArMenu.Application.Authentication.ResetPassword;

public sealed class ResetPasswordCommandHandler(
    UserLinks userLinks,
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<ResetPasswordCommand, Result>
{
    public async ValueTask<Result> Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var use = await userLinks.ConsumeAsync(command.Token, UserTokenPurpose.PasswordReset, cancellationToken);
        if (use.IsFailure)
        {
            return use.Error;
        }

        var user = await users.GetByIdAsync(use.Value, cancellationToken);
        if (user is null)
        {
            return UserTokenErrors.InvalidLink;
        }

        user.ResetPassword(passwordHasher.Hash(command.Password), timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
