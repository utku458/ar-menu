using ArMenu.Domain.Memberships;
using ArMenu.Domain.Tenants;
using ArMenu.Domain.Users;

namespace ArMenu.Application.Abstractions.Accounts;

/// <summary>
/// The only writes that span businesses. Deleting an account touches every business the person belongs to, which no
/// tenant-bound unit of work can do; implementations still let row-level security check each business in turn.
/// </summary>
public interface IAccountErasure
{
    /// <summary>Every business the user belongs to that is not closed, with the number of other members in each.</summary>
    Task<IReadOnlyList<AccountMembership>> ListMembershipsAsync(UserId userId, CancellationToken cancellationToken);

    /// <summary>
    /// In one transaction: closes <paramref name="tenantsToClose"/>, removes the user from every business (ending their
    /// sessions), erases the account and queues <paramref name="notice"/> to the address it had.
    /// </summary>
    /// <exception cref="Persistence.ConcurrencyConflictException">
    /// Someone joined a business in <paramref name="tenantsToClose"/> in the meantime; nothing was changed.
    /// </exception>
    Task EraseAsync(UserId userId, IReadOnlyCollection<TenantId> tenantsToClose, Email.EmailMessage notice, string noticeTemplate, CancellationToken cancellationToken);
}

public sealed record AccountMembership(TenantId TenantId, string Slug, string Name, TenantRole Role, int OtherMembers)
{
    /// <summary>Nobody else could run the business: it closes with the account.</summary>
    public bool ClosesWithAccount => Role == TenantRole.Owner && OtherMembers == 0;

    /// <summary>Other people depend on the business: it must be handed over first.</summary>
    public bool BlocksDeletion => Role == TenantRole.Owner && OtherMembers > 0;
}
