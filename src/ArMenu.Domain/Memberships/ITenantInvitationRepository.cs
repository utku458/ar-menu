using ArMenu.Domain.Users;

namespace ArMenu.Domain.Memberships;

/// <summary>Invitations of the tenant bound to the current scope.</summary>
public interface ITenantInvitationRepository
{
    Task<TenantInvitation?> GetByIdAsync(TenantInvitationId invitationId, CancellationToken cancellationToken = default);

    /// <summary>The pending invitation for an address, expired or not; there is at most one.</summary>
    Task<TenantInvitation?> GetPendingByEmailAsync(Email email, CancellationToken cancellationToken = default);

    void Add(TenantInvitation invitation);
}
