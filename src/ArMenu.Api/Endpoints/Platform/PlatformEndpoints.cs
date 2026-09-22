using System.Security.Claims;
using ArMenu.Api.Authentication;
using ArMenu.Api.Http;
using ArMenu.Application.Abstractions.Authentication;
using ArMenu.Application.Platform.EnterBusiness;
using ArMenu.Application.Platform.OpenBusiness;
using ArMenu.Application.Platform.Queries.ListBusinesses;
using ArMenu.Domain.Sessions;
using ArMenu.Domain.Tenants;
using ArMenu.Domain.Users;
using Mediator;
using static ArMenu.Api.Endpoints.Authentication.AuthenticationEndpoints;

namespace ArMenu.Api.Endpoints.Platform;

/// <summary>
/// What the platform administrator does above any single business: list the businesses, open new ones with their
/// owner's account, and enter one to run it.
/// </summary>
/// <remarks>
/// No tenant resolution, on purpose. These endpoints are not about one business: listing reads only the directory of
/// businesses, opening binds the scope to the business it creates, and entering binds it to the one entered. Nothing
/// here ever reads a business's own data while bound to another.
/// </remarks>
internal sealed class PlatformEndpoints : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var platform = endpoints.MapGroup("/api/v1/platform")
            .RequireAuthorization(AuthorizationPolicies.PlatformAdmin)
            .WithTags("Platform")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        platform.MapGet("/businesses", ListBusinessesAsync)
            .WithSummary("Every business on the platform, by name.")
            .Produces<IReadOnlyList<BusinessSummary>>();

        platform.MapPost("/businesses", OpenBusinessAsync)
            .WithSummary("Opens a business with its owner's account, who signs in with the given user name and password.")
            .Produces<OpenedBusinessResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        platform.MapPost("/businesses/{businessId:guid}/enter", EnterBusinessAsync)
            .WithSummary("An access token for the business with the owner's role. Short-lived and not refreshable: enter again when it expires.")
            .Produces<EnteredBusinessResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> ListBusinessesAsync(IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListBusinessesQuery(), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    private static async Task<IResult> OpenBusinessAsync(
        OpenBusinessRequest request,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new OpenBusinessCommand(
                UserIdOf(user),
                request.BusinessName,
                request.Slug,
                request.DefaultCulture,
                request.Currency,
                request.OwnerFullName,
                request.OwnerUserName,
                request.OwnerPassword,
                request.TimeZone),
            cancellationToken);

        return result.IsSuccess
            ? TypedResults.Created((string?)null, new OpenedBusinessResponse(result.Value.TenantId.Value, result.Value.Slug))
            : result.Error.ToProblem();
    }

    private static async Task<IResult> EnterBusinessAsync(
        Guid businessId,
        ClaimsPrincipal user,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new EnterBusinessCommand(UserIdOf(user), SessionIdOf(user), TenantId.From(businessId)),
            cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        // No cookie: there is no session in this business to refresh. The dashboard enters again when the token runs
        // out, which it can do as long as the administrator's own session is alive.
        httpContext.Response.Headers.CacheControl = "no-store";

        return TypedResults.Ok(new EnteredBusinessResponse(
            result.Value.AccessToken.Value,
            result.Value.AccessToken.ExpiresAt,
            SecondsUntil(httpContext, result.Value.AccessToken.ExpiresAt),
            result.Value.Slug));
    }

    private static UserId UserIdOf(ClaimsPrincipal user) => UserId.From(Guid.Parse(user.FindFirstValue(ArMenuClaimTypes.UserId)!));

    private static UserSessionId SessionIdOf(ClaimsPrincipal user) =>
        UserSessionId.From(Guid.Parse(user.FindFirstValue(ArMenuClaimTypes.SessionId)!));

    internal sealed record OpenBusinessRequest(
        string BusinessName,
        string Slug,
        string DefaultCulture,
        string Currency,
        string OwnerFullName,
        string OwnerUserName,
        string OwnerPassword,
        string? TimeZone)
    {
        public override string ToString() => $"OpenBusinessRequest {{ Slug = {Slug}, *** }}";
    }

    internal sealed record OpenedBusinessResponse(Guid Id, string Slug);

    /// <summary>
    /// A bearer token for a business the administrator entered, and the business's slug. There is no refresh token:
    /// when it expires, the dashboard asks to enter again.
    /// </summary>
    internal sealed record EnteredBusinessResponse(string AccessToken, DateTimeOffset ExpiresAt, int ExpiresIn, string Workspace)
    {
        public string TokenType { get; } = "Bearer";

        public override string ToString() => $"EnteredBusinessResponse {{ Workspace = {Workspace}, ExpiresAt = {ExpiresAt:O} }}";
    }
}
