using ArMenu.Domain.Common;
using ArMenu.Domain.Users;
using Mediator;

namespace ArMenu.Application.Accounts.Queries.GetAccountDeletion;

/// <summary>What deleting the account would do: which businesses close with it and which must be handed over first.</summary>
public sealed record GetAccountDeletionQuery(UserId UserId) : IQuery<Result<AccountDeletionResponse>>;

public sealed record AccountDeletionResponse(
    IReadOnlyList<AffectedWorkspace> ClosingWorkspaces,
    IReadOnlyList<AffectedWorkspace> WorkspacesToHandOver,
    IReadOnlyList<AffectedWorkspace> WorkspacesToLeave);

public sealed record AffectedWorkspace(string Slug, string Name, int OtherMembers);
