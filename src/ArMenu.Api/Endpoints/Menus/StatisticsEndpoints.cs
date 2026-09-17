using ArMenu.Api.Authentication;
using ArMenu.Api.Http;
using ArMenu.Api.MultiTenancy;
using ArMenu.Api.RateLimiting;
using ArMenu.Application.Menus.Statistics.Queries.GetMenuStatistics;
using ArMenu.Application.Menus.Statistics.RecordMenuEvents;
using Mediator;

namespace ArMenu.Api.Endpoints.Menus;

/// <summary>
/// Guest activity: the guest app reports what happens on a menu, and the dashboard shows the daily counts. Nothing about
/// a guest is received or kept beyond the events themselves.
/// </summary>
internal sealed class StatisticsEndpoints : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/menus/{tenant}/events", RecordAsync)
            .RequireTenantFromRoute()
            .AllowAnonymous()
            .WithRateLimit(RateLimitingPolicies.PublicMenu)
            .WithTags("Public menu")
            .WithSummary("Counts guest events on the menu: views, opened dishes, loaded models, AR starts. No guest identifier is accepted or stored.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        endpoints.MapGet("/api/v1/manage/statistics", GetAsync)
            .RequireAuthorization(AuthorizationPolicies.MenuEditor)
            .RequireTenantFromClaims()
            .WithTags("Statistics")
            .WithSummary("Daily guest activity and the dishes guests open most, over the last days (7 by default, at most 90).")
            .Produces<MenuStatisticsResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> RecordAsync(MenuEventsRequest request, IMediator mediator, CancellationToken cancellationToken) =>
        (await mediator.Send(new RecordMenuEventsCommand(request.Events), cancellationToken)).ToNoContent();

    private static async Task<IResult> GetAsync(IMediator mediator, CancellationToken cancellationToken, int days = 7)
    {
        var result = await mediator.Send(new GetMenuStatisticsQuery(days), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }

    internal sealed record MenuEventsRequest(IReadOnlyList<MenuEventInput> Events);
}
