using ArMenu.Domain.Common;
using ArMenu.Domain.Users;

namespace ArMenu.Application.Platform;

/// <summary>
/// Confirms, against the database, that the account a platform command acts for really is the administrator.
/// </summary>
/// <remarks>
/// The endpoints already require the <c>platform_admin</c> claim, and a signed token cannot be forged. Commands that
/// change the platform check again anyway, because the claim is a snapshot taken when the token was issued and the
/// account is the source of truth: two independent locks on the door that opens every business.
/// </remarks>
public sealed class PlatformAdministrator(IUserRepository users)
{
    public async Task<Result<User>> RequireAsync(UserId userId, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken);
        if (user is not { IsPlatformAdmin: true, IsErased: false })
        {
            return PlatformErrors.NotAdministrator;
        }

        return user;
    }
}
