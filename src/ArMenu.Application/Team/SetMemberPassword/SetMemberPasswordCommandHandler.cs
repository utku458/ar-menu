using ArMenu.Application.Abstractions.Authentication;
using ArMenu.Domain.Common;
using ArMenu.Domain.Memberships;
using ArMenu.Domain.Users;
using Mediator;

namespace ArMenu.Application.Team.SetMemberPassword;

/// <remarks>
/// The membership is looked up under the business's row-level security, so an owner can only ever reach people on
/// their own team. And only user-name accounts: those exist for one business and have no other way back in, whereas
/// an e-mail account can belong to other businesses too, and one business's owner must not hold the key to the rest.
/// </remarks>
public sealed class SetMemberPasswordCommandHandler(
    ITenantMembershipRepository memberships,
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : ICommandHandler<SetMemberPasswordCommand, Result>
{
    public async ValueTask<Result> Handle(SetMemberPasswordCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var membership = await memberships.GetByIdAsync(command.MembershipId, cancellationToken);
        if (membership is null)
        {
            return MembershipErrors.NotFound;
        }

        var user = await users.GetByIdAsync(membership.UserId, cancellationToken);
        if (user is null || user.IsErased)
        {
            return MembershipErrors.NotFound;
        }

        if (user.HasMailbox)
        {
            return TeamErrors.PasswordManagedByMailbox;
        }

        user.SetPassword(passwordHasher.Hash(command.NewPassword), timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
