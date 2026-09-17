using ArMenu.Domain.Common;
using ArMenu.Domain.Memberships;
using ArMenu.Domain.Users;
using Mediator;

namespace ArMenu.Application.Team.TransferOwnership;

/// <summary>
/// The owner hands the business over to another member, confirming with their password. The member becomes the owner
/// and the former owner a manager, from their next access tokens.
/// </summary>
public sealed record TransferOwnershipCommand(UserId CurrentOwner, TenantMembershipId SuccessorId, string Password) : ICommand<Result>
{
    public override string ToString() => $"TransferOwnershipCommand {{ SuccessorId = {SuccessorId}, *** }}";
}
