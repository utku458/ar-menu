using ArMenu.Domain.Common;
using ArMenu.Domain.Tenants;
using ArMenu.Domain.Users;

namespace ArMenu.Domain.Memberships;

/// <summary>
/// An invitation for someone to join a business's team with a role. It is accepted through a link with a secret sent
/// to the invited address, which is what proves the person controls that address.
/// </summary>
public sealed class TenantInvitation : AggregateRoot<TenantInvitationId>, ITenantScoped, IAuditable
{
    /// <summary>Long enough to catch someone back from a weekend, short enough that a forgotten link dies.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);

    private TenantInvitation(
        TenantInvitationId id,
        TenantId tenantId,
        Email email,
        TenantRole role,
        UserId invitedBy,
        InvitationTokenHash tokenHash,
        DateTimeOffset now)
        : base(id)
    {
        TenantId = tenantId;
        Email = email;
        Role = role;
        InvitedBy = invitedBy;
        TokenHash = tokenHash;
        Status = InvitationStatus.Pending;
        SentAt = now;
        ExpiresAt = now + Lifetime;
    }

#pragma warning disable CS8618 // Used by EF Core; every property is populated during materialization.
    private TenantInvitation()
    {
    }
#pragma warning restore CS8618

    public TenantId TenantId { get; private init; }

    public Email Email { get; private init; }

    public TenantRole Role { get; private init; }

    public UserId InvitedBy { get; private init; }

    public InvitationTokenHash TokenHash { get; private set; }

    public InvitationStatus Status { get; private set; }

    /// <summary>When the current link was sent; sending again replaces the link.</summary>
    public DateTimeOffset SentAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RespondedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public static Result<TenantInvitation> Create(
        TenantId tenantId,
        Email email,
        TenantRole role,
        UserId invitedBy,
        InvitationTokenHash tokenHash,
        DateTimeOffset now)
    {
        Guard.NotDefault(tenantId);
        Guard.NotDefault(invitedBy);
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(tokenHash);

        if (role == TenantRole.Owner || !Enum.IsDefined(role))
        {
            return InvitationErrors.OwnerNotInvitable;
        }

        return new TenantInvitation(TenantInvitationId.New(), tenantId, email, role, invitedBy, tokenHash, now);
    }

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;

    /// <summary>Whether the invitation still blocks inviting the same address again.</summary>
    public bool IsOpen(DateTimeOffset now) => Status == InvitationStatus.Pending && !IsExpired(now);

    /// <summary>
    /// Sends the invitation again with a new link and a new deadline. The previous link stops working, so a resend is
    /// also the remedy for a link that went to the wrong inbox.
    /// </summary>
    public Result Renew(InvitationTokenHash tokenHash, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(tokenHash);

        if (Status != InvitationStatus.Pending)
        {
            return InvitationErrors.NoLongerPending;
        }

        TokenHash = tokenHash;
        SentAt = now;
        ExpiresAt = now + Lifetime;
        return Result.Success();
    }

    /// <summary>Checks that <paramref name="presentedHash"/> is this invitation's current link and that it can still be used.</summary>
    public Result Verify(InvitationTokenHash presentedHash, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(presentedHash);

        if (!TokenHash.Matches(presentedHash))
        {
            return InvitationErrors.InvalidLink;
        }

        if (Status != InvitationStatus.Pending)
        {
            return InvitationErrors.NoLongerPending;
        }

        return IsExpired(now) ? InvitationErrors.Expired : Result.Success();
    }

    public Result Accept(InvitationTokenHash presentedHash, DateTimeOffset now)
    {
        var verification = Verify(presentedHash, now);
        if (verification.IsFailure)
        {
            return verification;
        }

        Status = InvitationStatus.Accepted;
        RespondedAt = now;
        return Result.Success();
    }

    public Result Revoke(DateTimeOffset now)
    {
        if (Status != InvitationStatus.Pending)
        {
            return InvitationErrors.NoLongerPending;
        }

        Status = InvitationStatus.Revoked;
        RespondedAt = now;
        return Result.Success();
    }
}
