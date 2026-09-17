using ArMenu.Domain.Common;
using ArMenu.Domain.Memberships;
using Mediator;

namespace ArMenu.Application.Team.RemoveMember;

public sealed class RemoveMemberCommandHandler(ITenantMembershipRepository memberships, IUnitOfWork unitOfWork)
    : ICommandHandler<RemoveMemberCommand, Result>
{
    public async ValueTask<Result> Handle(RemoveMemberCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var membership = await memberships.GetByIdAsync(command.MembershipId, cancellationToken);
        if (membership is null)
        {
            return MembershipErrors.NotFound;
        }

        var removable = membership.EnsureRemovable();
        if (removable.IsFailure)
        {
            return removable;
        }

        memberships.Remove(membership);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
