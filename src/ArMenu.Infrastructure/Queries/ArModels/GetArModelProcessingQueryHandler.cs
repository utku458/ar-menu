using ArMenu.Application.ArModels.Queries.GetArModelProcessing;
using ArMenu.Domain.ArModels;
using ArMenu.Domain.Common;
using ArMenu.Infrastructure.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ArMenu.Infrastructure.Queries.ArModels;

public sealed class GetArModelProcessingQueryHandler(ArMenuDbContext dbContext)
    : IQueryHandler<GetArModelProcessingQuery, Result<ArModelProcessingResponse>>
{
    public async ValueTask<Result<ArModelProcessingResponse>> Handle(GetArModelProcessingQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var processing = await dbContext.ArModelProcessings
            .AsNoTracking()
            .Where(candidate => candidate.MenuItemId == query.ItemId)
            .OrderByDescending(candidate => candidate.CreatedAt)
            .ThenByDescending(candidate => candidate.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return processing is null
            ? ArModelProcessingErrors.NotFound
            : new ArModelProcessingResponse(
                processing.Id.Value,
                (ArModelProcessingState)processing.Status,
                processing.Attempts,
                processing.FailureCode,
                processing.Report is { } report
                    ? new ArModelReportResponse(
                        ToResponse(report.Source),
                        ToResponse(report.Optimized),
                        new ModelFileSizesResponse(report.Files.Source, report.Files.Model, report.Files.SceneViewerModel, report.Files.AppleModel, report.Files.Poster),
                        new ModelDimensionsResponse(report.Dimensions.Width, report.Dimensions.Height, report.Dimensions.Depth),
                        report.Warnings)
                    : null,
                processing.CreatedAt,
                processing.CompletedAt);
    }

    private static ModelStatisticsResponse ToResponse(ModelStatistics statistics) =>
        new(statistics.Triangles, statistics.Vertices, statistics.Materials, statistics.Textures, statistics.MaxTextureSize);
}
