namespace ArMenu.Domain.Sessions;

/// <summary>Sessions of the tenant bound to the current scope.</summary>
public interface IUserSessionRepository
{
    Task<UserSession?> GetByIdAsync(UserSessionId sessionId, CancellationToken cancellationToken = default);

    void Add(UserSession session);
}
