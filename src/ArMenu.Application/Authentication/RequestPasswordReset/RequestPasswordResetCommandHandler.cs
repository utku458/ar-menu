using ArMenu.Application.Abstractions.Email;
using ArMenu.Application.Emails;
using ArMenu.Domain.Common;
using ArMenu.Domain.Users;
using Mediator;

namespace ArMenu.Application.Authentication.RequestPasswordReset;

public sealed class RequestPasswordResetCommandHandler(
    IUserRepository users,
    UserLinks userLinks,
    IEmailOutbox outbox,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RequestPasswordResetCommand, Result>
{
    public async ValueTask<Result> Handle(RequestPasswordResetCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var user = await users.GetByEmailAsync(Email.Create(command.Email).Value, cancellationToken);
        if (user is null)
        {
            return Result.Success();
        }

        var link = await userLinks.IssueAsync(user, UserTokenPurpose.PasswordReset, cancellationToken);
        outbox.Add(
            AccountEmails.PasswordReset(user.Email.Value, link, EmailLanguage.Resolve(command.Language, fallbackCulture: null)),
            AccountEmails.PasswordResetTemplate);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
