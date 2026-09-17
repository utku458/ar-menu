using ArMenu.Application.Abstractions.Authentication;
using ArMenu.Domain.Users;

namespace ArMenu.Infrastructure.Auditing;

/// <summary>Outside a web request (workers, tests) changes are made by the system.</summary>
internal sealed class NoCurrentUser : ICurrentUser
{
    public UserId? UserId => null;
}
