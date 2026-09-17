using System.Security.Claims;
using ArMenu.Api.Authentication;
using ArMenu.Api.Http;
using ArMenu.Api.MultiTenancy;
using ArMenu.Api.RateLimiting;
using ArMenu.Application.Abstractions.Authentication;
using ArMenu.Application.Team;
using ArMenu.Application.Team.AcceptInvitation;
using ArMenu.Application.Team.ChangeMemberRole;
using ArMenu.Application.Team.InviteMember;
using ArMenu.Application.Team.Queries.GetInvitation;
using ArMenu.Application.Team.Queries.GetTeam;
using ArMenu.Application.Team.RemoveMember;
using ArMenu.Application.Team.ResendInvitation;
using ArMenu.Application.Team.RevokeInvitation;
using ArMenu.Application.Team.TransferOwnership;
using ArMenu.Domain.Memberships;
using ArMenu.Domain.Users;
using Mediator;
using static ArMenu.Api.Endpoints.Authentication.AuthenticationEndpoints;

namespace ArMenu.Api.Endpoints.Team;

/// <summary>
/// The team of a business: the owner invites managers and staff by e-mail, changes their roles and removes them. The
/// invited person answers under the workspace in the URL, like signing in.
/// </summary>
internal sealed class TeamEndpoints : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var team = endpoints.MapGroup("/api/v1/manage/team")
            .RequireAuthorization(AuthorizationPolicies.TeamAdmin)
            .RequireTenantFromClaims()
            .WithTags("Team")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        team.MapGet("/", GetTeamAsync)
            .WithSummary("Members of the business and the invitations waiting for an answer.")
            .Produces<TeamResponse>();

        team.MapPost("/invitations", InviteAsync)
            .WithSummary("Invites someone by e-mail. The invitation is saved even when the e-mail cannot be delivered; emailSent tells.")
            .Produces<InvitationSent>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        team.MapPost("/invitations/{invitationId:guid}/resend", ResendAsync)
            .WithSummary("Sends a pending invitation again with a new link and deadline; the previous link stops working.")
            .Produces<InvitationSent>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        team.MapDelete("/invitations/{invitationId:guid}", RevokeAsync)
            .WithSummary("Withdraws a pending invitation.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        team.MapPut("/members/{membershipId:guid}/role", ChangeRoleAsync)
            .WithSummary("Moves a member between manager and staff, from their next access token on.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        team.MapDelete("/members/{membershipId:guid}", RemoveAsync)
            .WithSummary("Removes a member and ends their sessions. The owner cannot be removed.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        team.MapPost("/members/{membershipId:guid}/ownership", TransferOwnershipAsync)
            .WithRateLimit(RateLimitingPolicies.Authentication)
            .WithSummary("Hands the business over to a member, confirmed with the owner's password. The owner becomes a manager.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        var invitations = endpoints.MapGroup("/api/v1/tenants/{tenant}/invitations")
            .RequireTenantFromRoute()
            .AllowAnonymous()
            .WithRateLimit(RateLimitingPolicies.Authentication)
            .WithTags("Team");

        // POST, so the secret stays in the body: never in a URL, a proxy log or browser history.
        invitations.MapPost("/lookup", LookUpAsync)
            .WithSummary("What an invitation link offers: the business, the role, the address, and whether it has an account.")
            .Produces<InvitationResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        invitations.MapPost("/accept", AcceptAsync)
            .WithSummary("Joins the team with an invitation link and signs in; the refresh token is set as an HttpOnly cookie.")
            .Produces<AccessTokenResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> GetTeamAsync(IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTeamQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<IResult> InviteAsync(InvitationRequest request, ClaimsPrincipal user, IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new InviteMemberCommand(UserIdOf(user), request.Email, request.Role, request.Language), cancellationToken);
        return result.IsSuccess ? TypedResults.Created((string?)null, result.Value) : result.Error.ToProblem();
    }

    private static async Task<IResult> ResendAsync(Guid invitationId, ResendRequest request, ClaimsPrincipal user, IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ResendInvitationCommand(UserIdOf(user), TenantInvitationId.From(invitationId), request.Language), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<IResult> RevokeAsync(Guid invitationId, IMediator mediator, CancellationToken cancellationToken) =>
        (await mediator.Send(new RevokeInvitationCommand(TenantInvitationId.From(invitationId)), cancellationToken)).ToNoContent();

    private static async Task<IResult> ChangeRoleAsync(Guid membershipId, RoleRequest request, IMediator mediator, CancellationToken cancellationToken) =>
        (await mediator.Send(new ChangeMemberRoleCommand(TenantMembershipId.From(membershipId), request.Role), cancellationToken)).ToNoContent();

    private static async Task<IResult> RemoveAsync(Guid membershipId, IMediator mediator, CancellationToken cancellationToken) =>
        (await mediator.Send(new RemoveMemberCommand(TenantMembershipId.From(membershipId)), cancellationToken)).ToNoContent();

    private static async Task<IResult> TransferOwnershipAsync(Guid membershipId, PasswordConfirmationRequest request, ClaimsPrincipal user, IMediator mediator, CancellationToken cancellationToken) =>
        (await mediator.Send(new TransferOwnershipCommand(UserIdOf(user), TenantMembershipId.From(membershipId), request.Password), cancellationToken)).ToNoContent();

    private static async Task<IResult> LookUpAsync(InvitationTokenRequest request, IMediator mediator, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetInvitationQuery(request.Token), cancellationToken);
        httpContext.Response.Headers.CacheControl = "no-store";
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<IResult> AcceptAsync(AcceptInvitationRequest request, IMediator mediator, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new AcceptInvitationCommand(request.Token, request.FullName, request.Password), cancellationToken);
        return result.IsSuccess ? IssueTokens(httpContext, result.Value) : result.Error.ToProblem();
    }

    private static UserId UserIdOf(ClaimsPrincipal user) => UserId.From(Guid.Parse(user.FindFirstValue(ArMenuClaimTypes.UserId)!));

    internal sealed record InvitationRequest(string Email, TeamRole Role, string? Language);

    internal sealed record ResendRequest(string? Language);

    internal sealed record RoleRequest(TeamRole Role);

    internal sealed record PasswordConfirmationRequest(string Password)
    {
        public override string ToString() => "PasswordConfirmationRequest { *** }";
    }

    internal sealed record InvitationTokenRequest(string Token)
    {
        public override string ToString() => "InvitationTokenRequest { *** }";
    }

    internal sealed record AcceptInvitationRequest(string Token, string? FullName, string Password)
    {
        public override string ToString() => "AcceptInvitationRequest { *** }";
    }
}
