using ArMenu.Domain.Users;

namespace ArMenu.Application.Abstractions.Authentication;

/// <summary>The person the current request acts for, when there is one: background work and guests have none.</summary>
public interface ICurrentUser
{
    UserId? UserId { get; }
}
