using ArMenu.Domain.Common;

namespace ArMenu.Domain.Memberships;

public static class InvitationErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "invitation.not_found", "The invitation does not exist.");

    /// <summary>The same answer for a malformed link, an unknown invitation and a wrong secret.</summary>
    public static readonly Error InvalidLink = Error.NotFound(
        "invitation.invalid_link", "This invitation link is not valid. Ask for a new invitation.");

    public static readonly Error Expired = Error.Conflict(
        "invitation.expired", "This invitation has expired. Ask for a new invitation.");

    public static readonly Error NoLongerPending = Error.Conflict(
        "invitation.no_longer_pending", "This invitation was already used or withdrawn.");

    public static readonly Error AlreadyPending = Error.Conflict(
        "invitation.already_pending", "This e-mail address already has a pending invitation. Send it again instead.");

    public static readonly Error OwnerNotInvitable = Error.Validation(
        "invitation.owner_not_invitable", "Invitations are for managers and staff; a business has one owner.");

    public static readonly Error TokenHashInvalid = Error.Validation(
        "invitation.token_hash_invalid", "The invitation token hash must be 43 base64url characters.");
}
