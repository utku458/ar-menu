using ArMenu.Domain.Common;
using Mediator;

namespace ArMenu.Application.Team.Queries.GetTeam;

/// <summary>The members of the tenant bound to the current scope and the invitations still waiting for an answer.</summary>
public sealed record GetTeamQuery : IQuery<Result<TeamResponse>>;
