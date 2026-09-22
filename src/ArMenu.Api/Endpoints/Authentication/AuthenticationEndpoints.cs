using System.Security.Claims;
using ArMenu.Api.Authentication;
using ArMenu.Api.Http;
using ArMenu.Api.MultiTenancy;
using ArMenu.Api.RateLimiting;
using ArMenu.Application.Abstractions.Assets;
using ArMenu.Application.Abstractions.Authentication;
using ArMenu.Application.Authentication;
using ArMenu.Application.Authentication.RefreshSession;
using ArMenu.Application.Authentication.SignIn;
using ArMenu.Application.Authentication.SignInWithUserName;
using ArMenu.Application.Authentication.SignOut;
using ArMenu.Application.MultiTenancy;
using ArMenu.Domain.Media;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ArMenu.Api.Endpoints.Authentication;

/// <summary>
/// Workspace-style sign-in: every authentication endpoint lives under the tenant slug, so credentials are only ever
/// checked against that tenant's memberships, and sessions, cookies and tokens are scoped to it.
/// </summary>
internal sealed class AuthenticationEndpoints : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        var auth = endpoints.MapGroup("/api/v1/tenants/{tenant}/auth")
            .RequireTenantFromRoute()
            .AllowAnonymous()
            .WithRateLimit(RateLimitingPolicies.Authentication)
            .WithTags("Authentication");

        auth.MapPost("/sign-in", SignInAsync)
            .WithSummary("Signs a member in and returns an access token; the refresh token is set as an HttpOnly cookie.")
            .Produces<AccessTokenResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        auth.MapPost("/refresh", RefreshAsync)
            .WithSummary("Rotates the refresh token cookie and returns a new access token.")
            .Produces<AccessTokenResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        auth.MapPost("/sign-out", SignOutAsync)
            .WithSummary("Ends the session and clears the refresh token cookie.")
            .Produces(StatusCodes.Status204NoContent);

        // The same credentials check and session as the workspace sign-in above; only which workspace comes from the
        // account instead of the URL. The refresh cookie still belongs to that workspace's refresh endpoint.
        endpoints.MapPost("/api/v1/auth/sign-in", SignInWithUserNameAsync)
            .AllowAnonymous()
            .WithRateLimit(RateLimitingPolicies.Authentication)
            .WithTags("Authentication")
            .WithSummary("Signs a user-name account in to its own business, or the administrator to the platform; says which.")
            .Produces<WorkspaceAccessTokenResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        endpoints.MapGet("/api/v1/me", GetCurrentUser)
            .RequireAuthorization()
            .RequireTenantFromClaims()
            .WithTags("Authentication")
            .WithSummary("The signed-in member and the tenant the access token is scoped to.")
            .Produces<CurrentUserResponse>();
    }

    internal static IResult IssueTokens(HttpContext httpContext, AuthenticationResult authentication)
    {
        RefreshTokenCookie.Write(httpContext, authentication.RefreshToken, authentication.RefreshTokenExpiresAt);

        // Token responses must never be stored by browsers or proxies (RFC 6749, section 5.1).
        httpContext.Response.Headers.CacheControl = "no-store";

        return TypedResults.Ok(new AccessTokenResponse(
            authentication.AccessToken.Value,
            authentication.AccessToken.ExpiresAt,
            SecondsUntil(httpContext, authentication.AccessToken.ExpiresAt)));
    }

    /// <summary>
    /// Relative lifetime (OAuth's <c>expires_in</c>): clients schedule refreshes with it, which keeps working when their
    /// clock disagrees with the server's.
    /// </summary>
    internal static int SecondsUntil(HttpContext httpContext, DateTimeOffset expiresAt)
    {
        var now = httpContext.RequestServices.GetRequiredService<TimeProvider>().GetUtcNow();
        return Math.Max(0, (int)Math.Floor((expiresAt - now).TotalSeconds));
    }

    private static async Task<IResult> SignInAsync(SignInRequest request, IMediator mediator, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SignInCommand(request.Email, request.Password), cancellationToken);
        return result.IsSuccess ? IssueTokens(httpContext, result.Value) : result.Error.ToProblem();
    }

    private static async Task<IResult> SignInWithUserNameAsync(
        UserNameSignInRequest request,
        IMediator mediator,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SignInWithUserNameCommand(request.UserName, request.Password), cancellationToken);
        return result.IsSuccess ? IssueWorkspaceTokens(httpContext, result.Value) : result.Error.ToProblem();
    }

    /// <summary>
    /// <see cref="IssueTokens"/> plus where the session lives. The scope is already bound to that workspace by the
    /// command, which is what puts the refresh cookie on its path.
    /// </summary>
    internal static IResult IssueWorkspaceTokens(HttpContext httpContext, WorkspaceSignInResult signIn)
    {
        var authentication = signIn.Authentication;
        RefreshTokenCookie.Write(httpContext, authentication.RefreshToken, authentication.RefreshTokenExpiresAt);
        httpContext.Response.Headers.CacheControl = "no-store";

        return TypedResults.Ok(new WorkspaceAccessTokenResponse(
            authentication.AccessToken.Value,
            authentication.AccessToken.ExpiresAt,
            SecondsUntil(httpContext, authentication.AccessToken.ExpiresAt),
            signIn.WorkspaceSlug,
            signIn.IsPlatformAdmin));
    }

    private static async Task<IResult> RefreshAsync(IMediator mediator, HttpContext httpContext, CancellationToken cancellationToken)
    {
        if (RefreshTokenCookie.Read(httpContext) is not { } refreshToken)
        {
            return AuthenticationErrors.InvalidRefreshToken.ToProblem();
        }

        // On failure the cookie is left alone: a parallel refresh in another tab may just have replaced it.
        var result = await mediator.Send(new RefreshSessionCommand(refreshToken), cancellationToken);
        return result.IsSuccess ? IssueTokens(httpContext, result.Value) : result.Error.ToProblem();
    }

    private static async Task<IResult> SignOutAsync(IMediator mediator, HttpContext httpContext, CancellationToken cancellationToken)
    {
        await mediator.Send(new SignOutCommand(RefreshTokenCookie.Read(httpContext)), cancellationToken);
        RefreshTokenCookie.Delete(httpContext);
        return TypedResults.NoContent();
    }

    private static Ok<CurrentUserResponse> GetCurrentUser(ClaimsPrincipal user, ITenantContext tenantContext, IAssetUrlResolver assetUrls)
    {
        var tenant = tenantContext.RequireTenant();

        return TypedResults.Ok(new CurrentUserResponse(
            Guid.Parse(user.FindFirstValue(ArMenuClaimTypes.UserId)!),
            user.FindFirstValue(ArMenuClaimTypes.Email)!,
            // Verification after sign-in shows up with the next access token, within minutes.
            string.Equals(user.FindFirstValue(ArMenuClaimTypes.EmailVerified), "true", StringComparison.OrdinalIgnoreCase),
            user.FindFirstValue(ArMenuClaimTypes.Name)!,
            user.FindFirstValue(ArMenuClaimTypes.Role)!,
            user.FindFirstValue(ArMenuClaimTypes.UserName),
            string.Equals(user.FindFirstValue(ArMenuClaimTypes.PlatformAdmin), "true", StringComparison.OrdinalIgnoreCase),
            new CurrentTenantResponse(
                tenant.Id.Value,
                tenant.Slug,
                tenant.Name,
                tenant.DefaultCulture,
                tenant.SupportedCultures,
                tenant.Currency,
                tenant.TimeZone,
                tenant.LogoPath,
                tenant.LogoPath is null ? null : assetUrls.Resolve(AssetPath.Create(tenant.LogoPath).Value),
                tenant.AccentColor)));
    }

    internal sealed record SignInRequest(string Email, string Password)
    {
        public override string ToString() => "SignInRequest { *** }";
    }

    internal sealed record UserNameSignInRequest(string UserName, string Password)
    {
        public override string ToString() => "UserNameSignInRequest { *** }";
    }

    /// <summary>
    /// A token response that also says where the session lives: <c>Workspace</c> is the slug whose auth endpoints
    /// refresh and end it, and <c>IsPlatformAdmin</c> is true for the administrator, whose workspace is the platform.
    /// </summary>
    internal sealed record WorkspaceAccessTokenResponse(
        string AccessToken,
        DateTimeOffset ExpiresAt,
        int ExpiresIn,
        string Workspace,
        bool IsPlatformAdmin)
    {
        public string TokenType { get; } = "Bearer";

        public override string ToString() => $"WorkspaceAccessTokenResponse {{ Workspace = {Workspace}, ExpiresAt = {ExpiresAt:O} }}";
    }

    internal sealed record AccessTokenResponse(string AccessToken, DateTimeOffset ExpiresAt, int ExpiresIn)
    {
        public string TokenType { get; } = "Bearer";

        public override string ToString() => $"AccessTokenResponse {{ ExpiresAt = {ExpiresAt:O} }}";
    }

    /// <summary>
    /// The signed-in person. <c>UserName</c> is how a user-name account signs in, and null for e-mail accounts, whose
    /// <c>Email</c> is then a real address; <c>IsPlatformAdmin</c> says whether the platform pages are open to them.
    /// </summary>
    internal sealed record CurrentUserResponse(
        Guid Id,
        string Email,
        bool EmailVerified,
        string FullName,
        string Role,
        string? UserName,
        bool IsPlatformAdmin,
        CurrentTenantResponse Tenant);

    internal sealed record CurrentTenantResponse(
        Guid Id,
        string Slug,
        string Name,
        string DefaultCulture,
        IReadOnlyList<string> SupportedCultures,
        string Currency,
        string TimeZone,
        string? LogoPath,
        Uri? LogoUrl,
        string? AccentColor);
}
