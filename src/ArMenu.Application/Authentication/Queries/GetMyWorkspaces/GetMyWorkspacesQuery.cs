using ArMenu.Application.Team;
using ArMenu.Domain.Common;
using ArMenu.Domain.Users;
using Mediator;

namespace ArMenu.Application.Authentication.Queries.GetMyWorkspaces;

/// <summary>
/// The active businesses a person belongs to, for switching between them. Each still needs its own sign-in: sessions
/// stay scoped to one business.
/// </summary>
public sealed record GetMyWorkspacesQuery(UserId UserId) : IQuery<Result<IReadOnlyList<WorkspaceResponse>>>;

public sealed record WorkspaceResponse(string Slug, string Name, TeamRole Role);
