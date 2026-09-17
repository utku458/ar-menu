using ArMenu.Domain.Memberships;
using ArMenu.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace ArMenu.Infrastructure.Persistence.Repositories;

internal sealed class TenantMembershipRepository(ArMenuDbContext dbContext) : ITenantMembershipRepository
{
    public Task<TenantMembership?> GetByIdAsync(TenantMembershipId membershipId, CancellationToken cancellationToken = default) =>
        dbContext.TenantMemberships.SingleOrDefaultAsync(membership => membership.Id == membershipId, cancellationToken);

    public Task<TenantMembership?> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default) =>
        dbContext.TenantMemberships.SingleOrDefaultAsync(membership => membership.UserId == userId, cancellationToken);

    public void Add(TenantMembership membership) => dbContext.TenantMemberships.Add(membership);

    // The database cascades the delete to the member's sessions in this tenant.
    public void Remove(TenantMembership membership) => dbContext.TenantMemberships.Remove(membership);
}
