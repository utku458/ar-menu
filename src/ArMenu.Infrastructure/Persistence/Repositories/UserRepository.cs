using ArMenu.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace ArMenu.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository(ArMenuDbContext dbContext) : IUserRepository
{
    public Task<User?> GetByIdAsync(UserId userId, CancellationToken cancellationToken = default) =>
        dbContext.Users.SingleOrDefaultAsync(user => user.Id == userId, cancellationToken);

    public Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default) =>
        dbContext.Users.SingleOrDefaultAsync(user => user.Email == email, cancellationToken);

    public Task<bool> EmailExistsAsync(Email email, CancellationToken cancellationToken = default) =>
        dbContext.Users.AnyAsync(user => user.Email == email, cancellationToken);

    public Task<User?> GetByUserNameAsync(UserName userName, CancellationToken cancellationToken = default) =>
        dbContext.Users.SingleOrDefaultAsync(user => user.UserName == userName, cancellationToken);

    public Task<bool> UserNameExistsAsync(UserName userName, CancellationToken cancellationToken = default) =>
        dbContext.Users.AnyAsync(user => user.UserName == userName, cancellationToken);

    public Task<bool> PlatformAdminExistsAsync(CancellationToken cancellationToken = default) =>
        dbContext.Users.AnyAsync(user => user.IsPlatformAdmin, cancellationToken);

    public void Add(User user) => dbContext.Users.Add(user);
}
