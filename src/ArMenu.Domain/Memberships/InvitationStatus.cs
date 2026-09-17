namespace ArMenu.Domain.Memberships;

public enum InvitationStatus
{
    /// <summary>Sent and waiting; usable until it expires.</summary>
    Pending = 0,

    /// <summary>Turned into a membership.</summary>
    Accepted = 1,

    /// <summary>Withdrawn by the business, or replaced by a new invitation to the same address.</summary>
    Revoked = 2,
}
