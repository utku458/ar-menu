using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ArMenu.Application.Abstractions.Assets;
using ArMenu.Domain.ArModels;
using Microsoft.Extensions.Options;

namespace ArMenu.Infrastructure.ArModels;

/// <summary>
/// Client of the asset processor's job API (contracts/asset-processor). Any failure that is not a verdict on the file
/// (network, timeout, a busy or broken processor) becomes <see cref="ModelProcessingOutcome.Unavailable"/>, which the
/// job queue retries.
/// </summary>
internal sealed class HttpModelProcessor(HttpClient httpClient, IOptions<AssetProcessorOptions> options) : IModelProcessor
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ModelProcessingOutcome> ProcessAsync(ModelProcessingJob job, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(options.Value.Timeout);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "v1/jobs")
            {
                Content = JsonContent.Create(JobRequest.From(job), options: JsonOptions),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.Value.Token);

            using var response = await httpClient.SendAsync(request, timeout.Token);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<JobResult>(JsonOptions, timeout.Token);
                return result?.Report is { } report
                    ? new ModelProcessingOutcome.Processed(report)
                    : new ModelProcessingOutcome.Unavailable("The processor answered without a report.");
            }

            var problem = await ReadProblemAsync(response, timeout.Token);
            return response.StatusCode == HttpStatusCode.UnprocessableEntity && problem is { Code: not null }
                ? new ModelProcessingOutcome.Rejected(problem.Code, problem.Detail ?? string.Empty)
                : new ModelProcessingOutcome.Unavailable(
                    $"The processor answered HTTP {(int)response.StatusCode} ({problem?.Code ?? "no code"}).");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new ModelProcessingOutcome.Unavailable($"The processor did not finish within {options.Value.Timeout}.");
        }
        catch (HttpRequestException exception)
        {
            return new ModelProcessingOutcome.Unavailable($"The processor could not be reached: {exception.Message}");
        }
        catch (JsonException exception)
        {
            return new ModelProcessingOutcome.Unavailable($"The processor answered with invalid JSON: {exception.Message}");
        }
    }

    private static async Task<Problem?> ReadProblemAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<Problem>(JsonOptions, cancellationToken);
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            return null;
        }
    }

    // contracts/asset-processor/job-request.example.json
    internal sealed record JobRequest(Guid JobId, SourceDownload Source, Outputs Outputs)
    {
        public static JobRequest From(ModelProcessingJob job) => new(
            job.JobId,
            new SourceDownload(job.SourceUrl),
            new Outputs(
                Upload.From(job.Outputs.Model),
                Upload.From(job.Outputs.SceneViewerModel),
                Upload.From(job.Outputs.AppleModel),
                Upload.From(job.Outputs.Poster)));
    }

    internal sealed record SourceDownload(Uri Url);

    internal sealed record Outputs(Upload Model, Upload SceneViewerModel, Upload AppleModel, Upload Poster);

    internal sealed record Upload(Uri Url, IReadOnlyDictionary<string, string> Headers)
    {
        public static Upload From(PresignedUpload upload) => new(upload.Url, upload.Headers);
    }

    // contracts/asset-processor/job-result.example.json
    internal sealed record JobResult(ArModelReport? Report);

    // contracts/asset-processor/job-rejected.example.json
    internal sealed record Problem(string? Code, string? Detail);
}
