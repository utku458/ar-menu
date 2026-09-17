using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ArMenu.Application.Abstractions.Assets;
using ArMenu.Infrastructure.ArModels;
using ArMenu.IntegrationTests.TestSupport;
using Microsoft.Extensions.Options;

namespace ArMenu.IntegrationTests.Api.ArModels;

/// <summary>
/// The API's side of the processor contract (contracts/asset-processor). The processor's tests check the same example
/// documents, so a change on either side that breaks the other fails a build.
/// </summary>
public sealed class HttpModelProcessorTests
{
    private const string Token = "test-processor-token-long-enough-for-validation";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Jobs_are_sent_exactly_as_the_contract_documents()
    {
        var example = JsonNode.Parse(await File.ReadAllTextAsync(RepositoryPaths.ProcessorContract("job-request.example.json"), Ct))!;
        var handler = new RecordingHandler(HttpStatusCode.OK, await ContractAsync("job-result.example.json"));

        await Processor(handler).ProcessAsync(JobFrom(example), Ct);

        handler.Request!.RequestUri!.ToString().ShouldBe("http://processor.test/v1/jobs");
        handler.Request.Headers.Authorization!.ToString().ShouldBe($"Bearer {Token}");
        JsonNode.DeepEquals(JsonNode.Parse(handler.Body!), example).ShouldBeTrue(handler.Body);
    }

    [Fact]
    public async Task A_documented_result_becomes_the_report()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, await ContractAsync("job-result.example.json"));

        var outcome = await Processor(handler).ProcessAsync(await ExampleJobAsync(), Ct);

        var report = outcome.ShouldBeOfType<ModelProcessingOutcome.Processed>().Report;
        report.Source.Triangles.ShouldBe(412_000);
        report.Optimized.MaxTextureSize.ShouldBe(2048);
        report.Files.SceneViewerModel.ShouldBe(3_921_064);
        report.Dimensions.Width.ShouldBe(0.182);
        report.Warnings.ShouldBe(["model.simplified", "model.textures_downscaled"]);
    }

    [Fact]
    public async Task A_documented_rejection_is_final()
    {
        var handler = new RecordingHandler(HttpStatusCode.UnprocessableEntity, await ContractAsync("job-rejected.example.json"));

        var outcome = await Processor(handler).ProcessAsync(await ExampleJobAsync(), Ct);

        outcome.ShouldBe(new ModelProcessingOutcome.Rejected("model.texture_unsupported", "Texture 'crust' is image/ktx2."));
    }

    public static TheoryData<HttpStatusCode, string> TransientAnswers => new()
    {
        { HttpStatusCode.ServiceUnavailable, """{"code":"processor.busy"}""" },
        { HttpStatusCode.BadGateway, """{"code":"processor.storage_failed"}""" },
        { HttpStatusCode.Unauthorized, """{"code":"processor.unauthorized"}""" },
        { HttpStatusCode.UnprocessableEntity, "<html>proxy error</html>" },
        { HttpStatusCode.OK, "not json" },
    };

    [Theory]
    [MemberData(nameof(TransientAnswers))]
    public async Task Anything_that_is_not_a_verdict_on_the_file_is_retried(HttpStatusCode status, string body)
    {
        var outcome = await Processor(new RecordingHandler(status, body)).ProcessAsync(await ExampleJobAsync(), Ct);

        outcome.ShouldBeOfType<ModelProcessingOutcome.Unavailable>();
    }

    [Fact]
    public async Task An_unreachable_or_slow_processor_is_retried()
    {
        var unreachable = await Processor(new FailingHandler(new HttpRequestException("Connection refused"))).ProcessAsync(await ExampleJobAsync(), Ct);
        var slow = await Processor(new FailingHandler(new TaskCanceledException("timed out")), TimeSpan.FromMilliseconds(1))
            .ProcessAsync(await ExampleJobAsync(), Ct);

        unreachable.ShouldBeOfType<ModelProcessingOutcome.Unavailable>();
        slow.ShouldBeOfType<ModelProcessingOutcome.Unavailable>();
    }

    private static HttpModelProcessor Processor(HttpMessageHandler handler, TimeSpan? timeout = null) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("http://processor.test/") },
            Options.Create(new AssetProcessorOptions { Url = new Uri("http://processor.test/"), Token = Token, Timeout = timeout ?? TimeSpan.FromMinutes(1) }));

    private static Task<string> ContractAsync(string fileName) =>
        File.ReadAllTextAsync(RepositoryPaths.ProcessorContract(fileName), Ct);

    private static async Task<ModelProcessingJob> ExampleJobAsync() => JobFrom(JsonNode.Parse(await ContractAsync("job-request.example.json"))!);

    private static ModelProcessingJob JobFrom(JsonNode example)
    {
        PresignedUpload Upload(string name) => new(
            new Uri(example["outputs"]![name]!["url"]!.GetValue<string>()),
            example["outputs"]![name]!["headers"]!.Deserialize<Dictionary<string, string>>()!,
            DateTimeOffset.UnixEpoch);

        return new ModelProcessingJob(
            example["jobId"]!.GetValue<Guid>(),
            new Uri(example["source"]!["url"]!.GetValue<string>()),
            new ModelProcessingUploads(Upload("model"), Upload("sceneViewerModel"), Upload("appleModel"), Upload("poster")));
    }

    private sealed class RecordingHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }

    private sealed class FailingHandler(Exception exception) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(20), cancellationToken);
            throw exception;
        }
    }
}
