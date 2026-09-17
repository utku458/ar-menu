using ArMenu.Domain.Common;

namespace ArMenu.Domain.Memberships;

public static class MembershipErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "membership.not_found", "The team member does not exist.");

    public static readonly Error AlreadyMember = Error.Conflict(
        "membership.already_member", "This person is already a member of the team.");

    public static readonly Error OwnerUnchangeable = Error.Conflict(
        "membership.owner_unchangeable", "The owner's role cannot be changed and the owner cannot be removed.");

    public static readonly Error RoleNotAssignable = Error.Validation(
        "membership.role_not_assignable", "Members can be managers or staff; a business has one owner.");

    public static readonly Error NotOwner = Error.Conflict(
        "membership.not_owner", "Only the owner can hand the business over.");

    public static readonly Error AlreadyOwner = Error.Conflict(
        "membership.already_owner", "This member already owns the business.");
}
