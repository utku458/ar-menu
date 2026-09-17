using ArMenu.Domain.Memberships;
using ArMenu.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace ArMenu.Infrastructure.Persistence.Repositories;

internal sealed class TenantInvitationRepository(ArMenuDbContext dbContext) : ITenantInvitationRepository
{
    public Task<TenantInvitation?> GetByIdAsync(TenantInvitationId invitationId, CancellationToken cancellationToken = default) =>
        dbContext.TenantInvitations.SingleOrDefaultAsync(invitation => invitation.Id == invitationId, cancellationToken);

    public Task<TenantInvitation?> GetPendingByEmailAsync(Email email, CancellationToken cancellationToken = default) =>
        dbContext.TenantInvitations.SingleOrDefaultAsync(
            invitation => invitation.Email == email && invitation.Status == InvitationStatus.Pending,
            cancellationToken);

    public void Add(TenantInvitation invitation) => dbContext.TenantInvitations.Add(invitation);
}
