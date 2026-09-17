using ArMenu.Domain.ArModels;

namespace ArMenu.Application.Abstractions.Assets;

/// <summary>
/// The asset pipeline, which turns an uploaded GLB into every file guests' devices need (web/services/asset-processor).
/// It reaches storage only through the presigned URLs of the job.
/// </summary>
public interface IModelProcessor
{
    Task<ModelProcessingOutcome> ProcessAsync(ModelProcessingJob job, CancellationToken cancellationToken);
}

public sealed record ModelProcessingJob(Guid JobId, Uri SourceUrl, ModelProcessingUploads Outputs);

public sealed record ModelProcessingUploads(
    PresignedUpload Model,
    PresignedUpload SceneViewerModel,
    PresignedUpload AppleModel,
    PresignedUpload Poster);

public abstract record ModelProcessingOutcome
{
    private ModelProcessingOutcome()
    {
    }

    /// <summary>Every output was uploaded.</summary>
    public sealed record Processed(ArModelReport Report) : ModelProcessingOutcome;

    /// <summary>The file cannot become a model. Final: the same file would be rejected again.</summary>
    public sealed record Rejected(string Code, string Detail) : ModelProcessingOutcome;

    /// <summary>The processor could not do the job right now (down, busy, storage failed). Worth retrying.</summary>
    public sealed record Unavailable(string Reason) : ModelProcessingOutcome;
}
