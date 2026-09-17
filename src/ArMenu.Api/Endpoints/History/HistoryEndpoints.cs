using ArMenu.Api.Authentication;
using ArMenu.Api.Http;
using ArMenu.Api.MultiTenancy;
using ArMenu.Application.History.Queries.GetHistory;
using Mediator;

namespace ArMenu.Api.Endpoints.History;

internal sealed class HistoryEndpoints : IEndpointModule
{
    private const int DefaultLimit = 50;

    public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/api/v1/manage/history", GetHistoryAsync)
            .RequireAuthorization(AuthorizationPolicies.MenuEditor)
            .RequireTenantFromClaims()
            .WithTags("History")
            .WithSummary("Who changed what in the business and when, newest first. Pass nextCursor as before for the next page.")
            .Produces<HistoryPage>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

    private static async Task<IResult> GetHistoryAsync(Guid? before, int? limit, IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetHistoryQuery(before, limit ?? DefaultLimit), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.Error.ToProblem();
    }
}
