using ArMenu.Domain.Users;

namespace ArMenu.Domain.Memberships;

/// <summary>Memberships of the tenant bound to the current scope.</summary>
public interface ITenantMembershipRepository
{
    Task<TenantMembership?> GetByIdAsync(TenantMembershipId membershipId, CancellationToken cancellationToken = default);

    Task<TenantMembership?> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);

    void Add(TenantMembership membership);

    /// <summary>Removes the membership; the member's sessions in the tenant end with it.</summary>
    void Remove(TenantMembership membership);
}
