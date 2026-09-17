using ArMenu.Api.Endpoints.Authentication;
using ArMenu.Api.Http;
using ArMenu.Api.RateLimiting;
using ArMenu.Application.Tenants.SignUp;
using Mediator;

namespace ArMenu.Api.Endpoints.Tenants;

internal sealed class SignUpEndpoints : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/api/v1/tenants", SignUpAsync)
            .AllowAnonymous()
            .WithRateLimit(RateLimitingPolicies.SignUp)
            .WithTags("Onboarding")
            .WithSummary("Creates a business with its owner account and signs the owner in.")
            .Produces<SignUpResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<IResult> SignUpAsync(SignUpRequest request, IMediator mediator, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new SignUpCommand(
                request.BusinessName,
                request.Slug,
                request.DefaultCulture,
                request.Currency,
                request.OwnerFullName,
                request.OwnerEmail,
                request.Password,
                request.TimeZone),
            cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        var signUp = result.Value;
        AuthenticationEndpoints.IssueTokens(httpContext, signUp.Authentication);

        return TypedResults.Created(
            $"/api/v1/menus/{signUp.Slug}",
            new SignUpResponse(
                signUp.TenantId.Value,
                signUp.Slug,
                signUp.Authentication.AccessToken.Value,
                signUp.Authentication.AccessToken.ExpiresAt,
                AuthenticationEndpoints.SecondsUntil(httpContext, signUp.Authentication.AccessToken.ExpiresAt)));
    }

    internal sealed record SignUpRequest(
        string BusinessName,
        string Slug,
        string DefaultCulture,
        string Currency,
        string OwnerFullName,
        string OwnerEmail,
        string Password,
        string? TimeZone = null)
    {
        public override string ToString() => $"SignUpRequest {{ Slug = {Slug}, *** }}";
    }

    internal sealed record SignUpResponse(
        Guid TenantId,
        string Slug,
        string AccessToken,
        DateTimeOffset AccessTokenExpiresAt,
        int AccessTokenExpiresIn)
    {
        public override string ToString() => $"SignUpResponse {{ Slug = {Slug} }}";
    }
}
