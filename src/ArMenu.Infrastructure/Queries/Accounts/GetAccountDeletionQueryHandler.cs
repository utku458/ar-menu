using ArMenu.Application.Abstractions.Accounts;
using ArMenu.Application.Accounts.Queries.GetAccountDeletion;
using ArMenu.Domain.Common;
using Mediator;

namespace ArMenu.Infrastructure.Queries.Accounts;

public sealed class GetAccountDeletionQueryHandler(IAccountErasure erasure) : IQueryHandler<GetAccountDeletionQuery, Result<AccountDeletionResponse>>
{
    public async ValueTask<Result<AccountDeletionResponse>> Handle(GetAccountDeletionQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var memberships = await erasure.ListMembershipsAsync(query.UserId, cancellationToken);

        List<AffectedWorkspace> Where(Func<AccountMembership, bool> predicate) =>
            [.. memberships.Where(predicate).Select(membership => new AffectedWorkspace(membership.Slug, membership.Name, membership.OtherMembers))];

        return new AccountDeletionResponse(
            Where(membership => membership.ClosesWithAccount),
            Where(membership => membership.BlocksDeletion),
            Where(membership => !membership.ClosesWithAccount && !membership.BlocksDeletion));
    }
}
