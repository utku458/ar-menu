using System.Net;
using Amazon.Runtime;
using Amazon.S3;
using ArMenu.IntegrationTests.TestSupport;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

[assembly: AssemblyFixture(typeof(StorageFixture))]

namespace ArMenu.IntegrationTests.TestSupport;

/// <summary>
/// One disposable S3-compatible storage server (SeaweedFS) for the test assembly, configured with the same identities
/// as docker-compose: the API's credentials, and anonymous read access to the published assets bucket only.
/// </summary>
public sealed class StorageFixture : IAsyncLifetime
{
    public const string AccessKey = "armenu";
    public const string SecretKey = "armenu-dev-secret";
    public const string UploadsBucket = "armenu-uploads";
    public const string AssetsBucket = "armenu-assets";

    private const int S3Port = 8333;

    private readonly IContainer _container = new ContainerBuilder("chrislusf/seaweedfs:4.47")
        .WithCommand("server", "-s3", "-s3.config=/etc/seaweedfs/s3.json", "-dir=/data", "-master.volumeSizeLimitMB=256", "-volume.max=100")
        .WithResourceMapping(new FileInfo(Path.Combine(AppContext.BaseDirectory, "Storage", "s3.json")), "/etc/seaweedfs/")
        .WithPortBinding(S3Port, assignRandomHostPort: true)
        // Anonymous requests to the service root are refused once the S3 gateway is serving.
        .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request => request.ForPort(S3Port).ForStatusCode(HttpStatusCode.Forbidden)))
        .Build();

    public Uri ServiceUrl => new($"http://{_container.Hostname}:{_container.GetMappedPublicPort(S3Port)}/");

    /// <summary>Public base URL of published assets, as a CDN would serve them.</summary>
    public Uri AssetsBaseUrl => new(ServiceUrl, $"{AssetsBucket}/");

    public IAmazonS3 Client { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        Client = new AmazonS3Client(
            new BasicAWSCredentials(AccessKey, SecretKey),
            new AmazonS3Config
            {
                ServiceURL = ServiceUrl.ToString(),
                ForcePathStyle = true,
                AuthenticationRegion = "us-east-1",
                RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
                ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
            });

        // The gateway answers before its metadata store accepts writes; bucket creation is retried until it does.
        foreach (var bucket in new[] { UploadsBucket, AssetsBucket })
        {
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    await Client.PutBucketAsync(bucket);
                    break;
                }
                catch (AmazonS3Exception) when (attempt < 30)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(500));
                }
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        Client?.Dispose();
        await _container.DisposeAsync();
    }
}
