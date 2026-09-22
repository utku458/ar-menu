using System.ComponentModel;
using ArMenu.Api.Http;
using ArMenu.Api.MultiTenancy;
using ArMenu.Api.RateLimiting;
using ArMenu.Application.Menus.Queries.GetPublicMenu;
using Mediator;
using Microsoft.Net.Http.Headers;

namespace ArMenu.Api.Endpoints.Menus;

internal sealed class PublicMenuEndpoints : IEndpointModule
{
    private const int MaxPreferredCultures = 10;

    public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/api/v1/menus/{tenant}", GetMenuAsync)
            .RequireBusinessFromRoute()
            .AllowAnonymous()
            .WithRateLimit(RateLimitingPolicies.PublicMenu)
            .WithTags("Public menu")
            .WithSummary("The guest-facing menu opened from a QR code, in the best language available.")
            .Produces<PublicMenuResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<IResult> GetMenuAsync(
        [Description("Explicit language choice, e.g. from a language switcher. Wins over Accept-Language.")] string? lang,
        HttpContext httpContext,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetPublicMenuQuery(PreferredCultures(lang, httpContext.Request)), cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        // Short shared caching keeps a sold-out dish visible within seconds while absorbing QR-scan bursts at the edge.
        httpContext.Response.Headers.CacheControl = "public, max-age=30, stale-while-revalidate=300";
        httpContext.Response.Headers.Vary = HeaderNames.AcceptLanguage;
        httpContext.Response.Headers.ContentLanguage = result.Value.Culture;

        return TypedResults.Ok(result.Value);
    }

    private static List<string> PreferredCultures(string? lang, HttpRequest request)
    {
        var browserCultures = request.GetTypedHeaders().AcceptLanguage
            .OrderByDescending(language => language.Quality ?? 1)
            .Select(language => language.Value.Value)
            .OfType<string>()
            .Where(value => value != "*");

        return [.. (string.IsNullOrWhiteSpace(lang) ? browserCultures : browserCultures.Prepend(lang)).Take(MaxPreferredCultures)];
    }
}
