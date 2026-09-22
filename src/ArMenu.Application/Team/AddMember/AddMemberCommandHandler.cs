using ArMenu.Application.Abstractions.Authentication;
using ArMenu.Application.MultiTenancy;
using ArMenu.Domain.Common;
using ArMenu.Domain.Memberships;
using ArMenu.Domain.Users;
using Mediator;

namespace ArMenu.Application.Team.AddMember;

public sealed class AddMemberCommandHandler(
    ITenantContext tenantContext,
    IUserRepository users,
    ITenantMembershipRepository memberships,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork)
    : ICommandHandler<AddMemberCommand, Result<TenantMembershipId>>
{
    public async ValueTask<Result<TenantMembershipId>> Handle(AddMemberCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var tenant = tenantContext.RequireTenant();

        // The domain decides what a role may be, as for a role change; the validator only turns obvious cases away early.
        var role = command.Role.ToTenantRole();
        if (role == TenantRole.Owner || !Enum.IsDefined(role))
        {
            return MembershipErrors.RoleNotAssignable;
        }

        var userName = UserName.Create(command.UserName);
        if (userName.IsFailure)
        {
            return userName.Error;
        }

        // Names are unique across the platform, not per business: signing in asks for the name alone.
        if (await users.UserNameExistsAsync(userName.Value, cancellationToken))
        {
            return UserErrors.UserNameTaken;
        }

        var user = User.OpenWithUserName(userName.Value, command.FullName, passwordHasher.Hash(command.Password));
        if (user.IsFailure)
        {
            return user.Error;
        }

        var membership = TenantMembership.Create(tenant.Id, user.Value.Id, role);
        users.Add(user.Value);
        memberships.Add(membership);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return membership.Id;
    }
}
