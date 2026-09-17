using ArMenu.Application.Accounts;
using ArMenu.Domain.Common;
using ArMenu.Domain.Memberships;
using ArMenu.Domain.Users;
using Mediator;

namespace ArMenu.Application.Team.TransferOwnership;

public sealed class TransferOwnershipCommandHandler(
    IUserRepository users,
    ITenantMembershipRepository memberships,
    PasswordConfirmation passwordConfirmation,
    IUnitOfWork unitOfWork)
    : ICommandHandler<TransferOwnershipCommand, Result>
{
    public async ValueTask<Result> Handle(TransferOwnershipCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // The role in the database decides, not the one in the access token: it may be minutes old.
        var owner = await memberships.GetByUserIdAsync(command.CurrentOwner, cancellationToken);
        var user = await users.GetByIdAsync(command.CurrentOwner, cancellationToken);
        if (owner is null || user is null)
        {
            return MembershipErrors.NotOwner;
        }

        var successor = await memberships.GetByIdAsync(command.SuccessorId, cancellationToken);
        if (successor is null)
        {
            return MembershipErrors.NotFound;
        }

        // Rules first, so a request that could never succeed does not use up password attempts.
        if (!owner.IsOwner)
        {
            return MembershipErrors.NotOwner;
        }

        if (successor.IsOwner || successor.Id == owner.Id)
        {
            return MembershipErrors.AlreadyOwner;
        }

        var confirmed = await passwordConfirmation.ConfirmAsync(user, command.Password, cancellationToken);
        if (confirmed.IsFailure)
        {
            return confirmed;
        }

        var handedOver = owner.HandOverOwnershipTo(successor);
        if (handedOver.IsFailure)
        {
            return handedOver;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
