using ArMenu.Application.Emails;
using ArMenu.Domain.Common;
using ArMenu.Domain.Users;
using Mediator;

namespace ArMenu.Application.Authentication.VerifyEmail;

public sealed class VerifyEmailCommandHandler(UserLinks userLinks, IUserRepository users, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    : ICommandHandler<VerifyEmailCommand, Result>
{
    public async ValueTask<Result> Handle(VerifyEmailCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var use = await userLinks.ConsumeAsync(command.Token, UserTokenPurpose.EmailVerification, cancellationToken);
        if (use.IsFailure)
        {
            return use.Error;
        }

        var user = await users.GetByIdAsync(use.Value, cancellationToken);
        if (user is null)
        {
            return UserTokenErrors.InvalidLink;
        }

        user.MarkEmailVerified(timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
