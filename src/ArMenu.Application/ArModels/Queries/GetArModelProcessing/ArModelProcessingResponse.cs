using System.Text.Json.Serialization;

namespace ArMenu.Application.ArModels.Queries.GetArModelProcessing;

public sealed record ArModelProcessingResponse(
    Guid Id,
    ArModelProcessingState Status,
    int Attempts,
    string? FailureCode,
    ArModelReportResponse? Report,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);

[JsonConverter(typeof(JsonStringEnumConverter<ArModelProcessingState>))]
public enum ArModelProcessingState
{
    [JsonStringEnumMemberName("queued")]
    Queued = 1,

    [JsonStringEnumMemberName("processing")]
    Processing = 2,

    [JsonStringEnumMemberName("succeeded")]
    Succeeded = 3,

    [JsonStringEnumMemberName("failed")]
    Failed = 4,

    [JsonStringEnumMemberName("superseded")]
    Superseded = 5,
}

public sealed record ArModelReportResponse(
    ModelStatisticsResponse Source,
    ModelStatisticsResponse Optimized,
    ModelFileSizesResponse Files,
    ModelDimensionsResponse Dimensions,
    IReadOnlyList<string> Warnings);

public sealed record ModelStatisticsResponse(int Triangles, int Vertices, int Materials, int Textures, int MaxTextureSize);

public sealed record ModelFileSizesResponse(long Source, long Model, long SceneViewerModel, long AppleModel, long Poster);

public sealed record ModelDimensionsResponse(double Width, double Height, double Depth);
