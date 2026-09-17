namespace ArMenu.Domain.Users;

public interface IUserTokenRepository
{
    Task<UserToken?> GetByIdAsync(UserTokenId tokenId, CancellationToken cancellationToken = default);

    /// <summary>Unused links of a user for one purpose, so a newer link can replace them.</summary>
    Task<IReadOnlyList<UserToken>> ListUnusedAsync(UserId userId, UserTokenPurpose purpose, CancellationToken cancellationToken = default);

    void Add(UserToken token);
}
