using ArMenu.Application.Abstractions.Accounts;
using ArMenu.Application.Emails;
using ArMenu.Application.MultiTenancy;
using ArMenu.Domain.Common;
using ArMenu.Domain.Users;
using Mediator;

namespace ArMenu.Application.Accounts.DeleteAccount;

public sealed class DeleteAccountCommandHandler(
    IUserRepository users,
    IAccountErasure erasure,
    PasswordConfirmation passwordConfirmation,
    ITenantLookup tenantLookup,
    TimeProvider timeProvider)
    : ICommandHandler<DeleteAccountCommand, Result>
{
    public async ValueTask<Result> Handle(DeleteAccountCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var user = await users.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null || user.IsErased)
        {
            return AccountErrors.PasswordIncorrect;
        }

        var memberships = await erasure.ListMembershipsAsync(user.Id, cancellationToken);
        if (memberships.Any(membership => membership.BlocksDeletion))
        {
            return AccountErrors.OwnershipTransferRequired;
        }

        var confirmed = await passwordConfirmation.ConfirmAsync(user, command.Password, cancellationToken);
        if (confirmed.IsFailure)
        {
            return confirmed;
        }

        // Read before erasure: afterwards nothing about the person is left to write to.
        var address = user.Email.Value;
        var closing = memberships.Where(membership => membership.ClosesWithAccount).ToList();

        // Tells the address what happened, so a deletion by someone else who got into the account does not go unnoticed.
        // It is queued in the erasure's transaction: no deletion without its notice, no notice without the deletion.
        var notice = AccountEmails.AccountDeleted(address, [.. closing.Select(membership => membership.Name)], timeProvider.GetUtcNow(), EmailLanguage.Resolve(command.Language, null));
        await erasure.EraseAsync(user.Id, [.. closing.Select(membership => membership.TenantId)], notice, AccountEmails.AccountDeletedTemplate, cancellationToken);

        foreach (var membership in memberships)
        {
            await tenantLookup.InvalidateAsync(membership.TenantId, Domain.Tenants.TenantSlug.Create(membership.Slug).Value, cancellationToken);
        }

        return Result.Success();
    }
}
