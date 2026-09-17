using ArMenu.Domain.Common;
using ArMenu.Domain.Tenants;
using ArMenu.Domain.Users;

namespace ArMenu.Domain.Memberships;

/// <summary>Grants a user a role inside one tenant.</summary>
public sealed class TenantMembership : AggregateRoot<TenantMembershipId>, ITenantScoped, IAuditable
{
    private TenantMembership(TenantMembershipId id, TenantId tenantId, UserId userId, TenantRole role)
        : base(id)
    {
        TenantId = tenantId;
        UserId = userId;
        Role = role;
    }

    private TenantMembership()
    {
    }

    public TenantId TenantId { get; private init; }

    public UserId UserId { get; private init; }

    public TenantRole Role { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public static TenantMembership Create(TenantId tenantId, UserId userId, TenantRole role)
    {
        Guard.NotDefault(tenantId);
        Guard.NotDefault(userId);

        return new TenantMembership(TenantMembershipId.New(), tenantId, userId, role);
    }

    public bool IsOwner => Role == TenantRole.Owner;

    /// <summary>
    /// Moves a member between manager and staff. Ownership is not handed over this way: the owner stays owner, and
    /// nobody becomes one. The change applies to the member's next access token, within minutes.
    /// </summary>
    public Result ChangeRole(TenantRole role)
    {
        if (IsOwner)
        {
            return MembershipErrors.OwnerUnchangeable;
        }

        if (role == TenantRole.Owner || !Enum.IsDefined(role))
        {
            return MembershipErrors.RoleNotAssignable;
        }

        Role = role;
        return Result.Success();
    }

    /// <summary>
    /// Hands the business over to another member of the team. The successor becomes the owner and this member a
    /// manager, in one step, so the business never has zero or two owners.
    /// </summary>
    public Result HandOverOwnershipTo(TenantMembership successor)
    {
        ArgumentNullException.ThrowIfNull(successor);

        if (successor.TenantId != TenantId)
        {
            throw new InvalidOperationException("Ownership can only be handed over within one business.");
        }

        if (!IsOwner)
        {
            return MembershipErrors.NotOwner;
        }

        if (successor.Id == Id || successor.IsOwner)
        {
            return MembershipErrors.AlreadyOwner;
        }

        successor.Role = TenantRole.Owner;
        Role = TenantRole.Manager;
        return Result.Success();
    }

    /// <summary>Checks that the member may be removed from the team: anyone but the owner.</summary>
    public Result EnsureRemovable() => IsOwner ? MembershipErrors.OwnerUnchangeable : Result.Success();
}
