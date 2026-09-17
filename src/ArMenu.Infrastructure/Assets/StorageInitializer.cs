using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ArMenu.Infrastructure.Assets;

public static class StorageInitializer
{
    private const string DemoAssetsCacheControl = "public, max-age=3600";

    /// <summary>
    /// Prepares local S3-compatible storage the way infrastructure-as-code prepares it in the cloud: both buckets, their
    /// CORS rules (browser uploads, and model downloads by guests), and the demo dishes the development seed refers to.
    /// </summary>
    /// <remarks>
    /// Development convenience only. Public read access to the assets bucket is granted by the storage service itself
    /// (see <c>deploy/storage/s3.json</c>), like a bucket policy in production.
    /// </remarks>
    public static async Task InitializeDevelopmentStorageAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var s3 = services.GetRequiredService<IAmazonS3>();
        var options = services.GetRequiredService<IOptions<StorageOptions>>().Value;
        var configuration = services.GetRequiredService<IConfiguration>();

        foreach (var bucket in new[] { options.UploadsBucket, options.AssetsBucket })
        {
            if (!await AmazonS3Util.DoesS3BucketExistV2Async(s3, bucket))
            {
                await s3.PutBucketAsync(bucket, cancellationToken);
            }
        }

        var dashboardOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<List<string>>() ?? [];
        await PutCorsAsync(s3, options.UploadsBucket, new CORSRule
        {
            AllowedMethods = ["PUT"],
            AllowedOrigins = dashboardOrigins,
            AllowedHeaders = ["*"],
            MaxAgeSeconds = 600,
        }, cancellationToken);
        await PutCorsAsync(s3, options.AssetsBucket, new CORSRule
        {
            AllowedMethods = ["GET", "HEAD"],
            AllowedOrigins = ["*"],
            AllowedHeaders = ["*"],
            MaxAgeSeconds = 3600,
        }, cancellationToken);

        var demoAssets = configuration["Development:DemoAssetsDirectory"];
        if (!string.IsNullOrWhiteSpace(demoAssets))
        {
            var root = Path.GetFullPath(demoAssets, services.GetRequiredService<IHostEnvironment>().ContentRootPath);
            await UploadMissingAsync(s3, options.AssetsBucket, root, cancellationToken);
        }
    }

    private static Task<PutCORSConfigurationResponse> PutCorsAsync(IAmazonS3 s3, string bucket, CORSRule rule, CancellationToken cancellationToken) =>
        s3.PutCORSConfigurationAsync(
            new PutCORSConfigurationRequest { BucketName = bucket, Configuration = new CORSConfiguration { Rules = [rule] } },
            cancellationToken);

    private static async Task UploadMissingAsync(IAmazonS3 s3, string bucket, string root, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(root))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            var key = Path.GetRelativePath(root, file).Replace(Path.DirectorySeparatorChar, '/');
            if (key.StartsWith('.') || key.Contains("/.", StringComparison.Ordinal))
            {
                continue;
            }

            try
            {
                await s3.GetObjectMetadataAsync(bucket, key, cancellationToken);
            }
            catch (AmazonS3Exception exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                var put = new PutObjectRequest { BucketName = bucket, Key = key, FilePath = file, ContentType = ContentTypeOf(key) };
                put.Headers.CacheControl = DemoAssetsCacheControl;
                await s3.PutObjectAsync(put, cancellationToken);
            }
        }
    }

    private static string ContentTypeOf(string key) => Path.GetExtension(key).ToLowerInvariant() switch
    {
        ".glb" => "model/gltf-binary",
        ".usdz" => "model/vnd.usdz+zip",
        ".webp" => "image/webp",
        ".avif" => "image/avif",
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        _ => "application/octet-stream",
    };
}
