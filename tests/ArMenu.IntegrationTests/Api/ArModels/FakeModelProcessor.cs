using System.Collections.Concurrent;
using System.Net.Http.Headers;
using ArMenu.Application.Abstractions.Assets;
using ArMenu.Domain.ArModels;
using ArMenu.IntegrationTests.TestSupport;

namespace ArMenu.IntegrationTests.Api.ArModels;

/// <summary>
/// Stands in for the asset processor. By default it behaves like the real one at its boundary: it downloads the source
/// from its presigned URL and uploads the pipeline's demo outputs to the presigned upload URLs, headers included.
/// </summary>
internal sealed class FakeModelProcessor : IModelProcessor
{
    public static readonly ArModelReport Report = new(
        source: new ModelStatistics(412_000, 208_400, 2, 3, 4096),
        optimized: new ModelStatistics(99_620, 51_250, 2, 3, 2048),
        files: new ModelFileSizes(18_350_211, 1_342_877, 3_921_064, 4_102_345, 38_112),
        dimensions: new ModelDimensions(0.182, 0.114, 0.179),
        warnings: ["model.simplified"]);

    private static readonly HttpClient Storage = new();

    private readonly ConcurrentQueue<Func<ModelProcessingJob, Task<ModelProcessingOutcome>>> _next = new();

    public ConcurrentQueue<ModelProcessingJob> Jobs { get; } = new();

    public ConcurrentQueue<byte[]> Sources { get; } = new();

    /// <summary>Answers the next job with <paramref name="behavior"/> instead of processing it.</summary>
    public FakeModelProcessor Then(Func<ModelProcessingJob, Task<ModelProcessingOutcome>> behavior)
    {
        _next.Enqueue(behavior);
        return this;
    }

    public FakeModelProcessor ThenAnswer(ModelProcessingOutcome outcome) => Then(_ => Task.FromResult(outcome));

    public async Task<ModelProcessingOutcome> ProcessAsync(ModelProcessingJob job, CancellationToken cancellationToken)
    {
        Jobs.Enqueue(job);
        return _next.TryDequeue(out var behavior) ? await behavior(job) : await ProcessDemoAsync(job, cancellationToken);
    }

    public async Task<ModelProcessingOutcome> ProcessDemoAsync(ModelProcessingJob job, CancellationToken cancellationToken)
    {
        Sources.Enqueue(await Storage.GetByteArrayAsync(job.SourceUrl, cancellationToken));

        await UploadAsync(job.Outputs.Model, "models/smash-burger.glb", cancellationToken);
        await UploadAsync(job.Outputs.SceneViewerModel, "models/smash-burger.scene-viewer.glb", cancellationToken);
        await UploadAsync(job.Outputs.AppleModel, "models/smash-burger.usdz", cancellationToken);
        await UploadAsync(job.Outputs.Poster, "posters/smash-burger.webp", cancellationToken);

        return new ModelProcessingOutcome.Processed(Report);
    }

    public static async Task UploadAsync(PresignedUpload upload, byte[] body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, upload.Url) { Content = new ByteArrayContent(body) };
        foreach (var (name, value) in upload.Headers)
        {
            if (name.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
            {
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(value);
            }
            else
            {
                request.Headers.TryAddWithoutValidation(name, value);
            }
        }

        using var response = await Storage.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private static async Task UploadAsync(PresignedUpload upload, string demoFile, CancellationToken cancellationToken) =>
        await UploadAsync(upload, await File.ReadAllBytesAsync(RepositoryPaths.DemoAsset(demoFile), cancellationToken), cancellationToken);
}
