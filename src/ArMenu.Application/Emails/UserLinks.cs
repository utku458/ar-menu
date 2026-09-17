using ArMenu.Application.Abstractions.Authentication;
using ArMenu.Domain.Common;
using ArMenu.Domain.Users;

namespace ArMenu.Application.Emails;

/// <summary>Issues the single-use links e-mailed to users. A new link retires the unused ones sent before it.</summary>
public sealed class UserLinks(
    IUserTokenRepository tokens,
    IUserTokenCodec tokenCodec,
    DashboardLinks dashboard,
    TimeProvider timeProvider)
{
    /// <returns>The address to e-mail. Save the unit of work before sending it.</returns>
    public async Task<Uri> IssueAsync(User user, UserTokenPurpose purpose, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        var now = timeProvider.GetUtcNow();

        foreach (var earlier in await tokens.ListUnusedAsync(user.Id, purpose, cancellationToken))
        {
            earlier.Supersede(now);
        }

        var secret = tokenCodec.GenerateSecret();
        var token = UserToken.Issue(user.Id, purpose, secret.Hash, now);
        tokens.Add(token);

        var encoded = tokenCodec.Encode(token.Id, secret);
        return purpose == UserTokenPurpose.PasswordReset ? dashboard.ResetPassword(encoded) : dashboard.VerifyEmail(encoded);
    }

    /// <summary>Uses a link for <paramref name="purpose"/>; the same failure for anything that is not a valid link.</summary>
    /// <returns>The user the link was for.</returns>
    public async Task<Result<UserId>> ConsumeAsync(string? token, UserTokenPurpose purpose, CancellationToken cancellationToken)
    {
        if (!tokenCodec.TryDecode(token, out var tokenId, out var presented) ||
            await tokens.GetByIdAsync(tokenId, cancellationToken) is not { } stored)
        {
            return UserTokenErrors.InvalidLink;
        }

        var consumption = stored.Consume(purpose, presented, timeProvider.GetUtcNow());
        return consumption.IsSuccess ? stored.UserId : consumption.Error;
    }
}
