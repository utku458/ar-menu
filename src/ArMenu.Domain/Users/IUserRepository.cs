namespace ArMenu.Domain.Users;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(UserId userId, CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(Email email, CancellationToken cancellationToken = default);

    Task<User?> GetByUserNameAsync(UserName userName, CancellationToken cancellationToken = default);

    Task<bool> UserNameExistsAsync(UserName userName, CancellationToken cancellationToken = default);

    /// <summary>Whether the platform already has its administrator; there is only ever one to create.</summary>
    Task<bool> PlatformAdminExistsAsync(CancellationToken cancellationToken = default);

    void Add(User user);
}
