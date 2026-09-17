using ArMenu.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace ArMenu.Infrastructure.Persistence.Repositories;

internal sealed class UserTokenRepository(ArMenuDbContext dbContext) : IUserTokenRepository
{
    public Task<UserToken?> GetByIdAsync(UserTokenId tokenId, CancellationToken cancellationToken = default) =>
        dbContext.UserTokens.SingleOrDefaultAsync(token => token.Id == tokenId, cancellationToken);

    public async Task<IReadOnlyList<UserToken>> ListUnusedAsync(UserId userId, UserTokenPurpose purpose, CancellationToken cancellationToken = default) =>
        await dbContext.UserTokens
            .Where(token => token.UserId == userId && token.Purpose == purpose && token.ConsumedAt == null)
            .ToListAsync(cancellationToken);

    public void Add(UserToken token) => dbContext.UserTokens.Add(token);
}
