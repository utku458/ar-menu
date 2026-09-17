using ArMenu.Domain.Common;
using ArMenu.Domain.Memberships;
using Mediator;

namespace ArMenu.Application.Team.ChangeMemberRole;

public sealed class ChangeMemberRoleCommandHandler(ITenantMembershipRepository memberships, IUnitOfWork unitOfWork)
    : ICommandHandler<ChangeMemberRoleCommand, Result>
{
    public async ValueTask<Result> Handle(ChangeMemberRoleCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var membership = await memberships.GetByIdAsync(command.MembershipId, cancellationToken);
        if (membership is null)
        {
            return MembershipErrors.NotFound;
        }

        var change = membership.ChangeRole(command.Role.ToTenantRole());
        if (change.IsFailure)
        {
            return change;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
