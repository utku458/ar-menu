using ArMenu.Application.Abstractions.Authentication;
using ArMenu.Domain.Common;
using ArMenu.Domain.Sessions;
using Mediator;

namespace ArMenu.Application.Authentication.SignOut;

public sealed class SignOutCommandHandler(
    IUserSessionRepository sessions,
    IRefreshTokenCodec refreshTokenCodec,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<SignOutCommand, Result>
{
    public async ValueTask<Result> Handle(SignOutCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!refreshTokenCodec.TryDecode(command.RefreshToken, out var sessionId, out var presentedHash))
        {
            return Result.Success();
        }

        var session = await sessions.GetByIdAsync(sessionId, cancellationToken);

        // Only the holder of the current token may end the session; a guessed session id changes nothing.
        if (session is not null && session.IsCurrentRefreshToken(presentedHash))
        {
            session.Revoke(timeProvider.GetUtcNow(), SessionRevocationReason.SignedOut);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
