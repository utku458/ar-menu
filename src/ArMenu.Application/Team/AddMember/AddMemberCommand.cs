using ArMenu.Domain.Common;
using ArMenu.Domain.Memberships;
using Mediator;

namespace ArMenu.Application.Team.AddMember;

/// <summary>
/// Opens an account for someone on the team and adds them in one step: they sign in with the user name and the
/// password the owner hands them. The alternative to an e-mail invitation for staff who have no address to receive
/// one — or a platform without e-mail. The role is manager or staff: a business has exactly one owner, and it is not
/// created this way.
/// </summary>
public sealed record AddMemberCommand(string FullName, string UserName, string Password, TeamRole Role)
    : ICommand<Result<TenantMembershipId>>
{
    public override string ToString() => $"AddMemberCommand {{ UserName = {UserName}, Role = {Role}, *** }}";
}
