using ArMenu.Domain.Sessions;
using Microsoft.EntityFrameworkCore;

namespace ArMenu.Infrastructure.Persistence.Repositories;

internal sealed class UserSessionRepository(ArMenuDbContext dbContext) : IUserSessionRepository
{
    public Task<UserSession?> GetByIdAsync(UserSessionId sessionId, CancellationToken cancellationToken = default) =>
        dbContext.UserSessions.SingleOrDefaultAsync(session => session.Id == sessionId, cancellationToken);

    public void Add(UserSession session) => dbContext.UserSessions.Add(session);
}
