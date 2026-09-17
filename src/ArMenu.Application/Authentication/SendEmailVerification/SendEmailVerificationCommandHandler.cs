using ArMenu.Application.Abstractions.Email;
using ArMenu.Application.Emails;
using ArMenu.Domain.Common;
using ArMenu.Domain.Users;
using Mediator;

namespace ArMenu.Application.Authentication.SendEmailVerification;

public sealed class SendEmailVerificationCommandHandler(
    IUserRepository users,
    UserLinks userLinks,
    IEmailOutbox outbox,
    IUnitOfWork unitOfWork)
    : ICommandHandler<SendEmailVerificationCommand, Result>
{
    public async ValueTask<Result> Handle(SendEmailVerificationCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var user = await users.GetByIdAsync(command.UserId, cancellationToken)
            ?? throw new InvalidOperationException("The signed-in user does not exist.");
        if (user.IsEmailVerified)
        {
            return Result.Success();
        }

        var link = await userLinks.IssueAsync(user, UserTokenPurpose.EmailVerification, cancellationToken);
        outbox.Add(
            AccountEmails.EmailVerification(user.Email.Value, user.FullName, link, EmailLanguage.Resolve(command.Language, fallbackCulture: null)),
            AccountEmails.EmailVerificationTemplate);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
