using Microsoft.Extensions.Options;

namespace ArMenu.Infrastructure.Assets;

/// <summary>S3-compatible object storage (AWS S3 in production, SeaweedFS locally).</summary>
public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>Service key of the S3 client that signs URLs for services inside the network, such as the processor.</summary>
    public const string InternalClientKey = "internal";

    /// <summary>Service key of the S3 client that signs upload URLs for browsers.</summary>
    public const string PublicClientKey = "public";

    /// <summary>Endpoint of an S3-compatible service the API calls. Leave empty for AWS S3.</summary>
    public Uri? ServiceUrl { get; set; }

    /// <summary>
    /// Endpoint browsers upload to, when it differs from the one the API calls (for example a private network endpoint
    /// for the API and a public one for browsers). Defaults to <see cref="ServiceUrl"/>.
    /// </summary>
    public Uri? PublicServiceUrl { get; set; }

    /// <summary>
    /// Endpoint the asset processor reaches storage at, when it differs from the one browsers use (for example a
    /// container network name). Presigned URLs are only valid for the host they were signed for.
    /// </summary>
    public Uri? InternalServiceUrl { get; set; }

    public string Region { get; set; } = "us-east-1";

    /// <summary>Static credentials for local emulators. Leave empty to use the default AWS credential chain.</summary>
    public string? AccessKey { get; set; }

    public string? SecretKey { get; set; }

    /// <summary>Path-style addressing (<c>host/bucket/key</c>), required by most S3-compatible services.</summary>
    public bool ForcePathStyle { get; set; }

    /// <summary>Private bucket receiving browser uploads; objects in it should expire after a day.</summary>
    public string UploadsBucket { get; set; } = string.Empty;

    /// <summary>Publicly readable bucket behind the asset CDN (<c>Assets:PublicBaseUrl</c>).</summary>
    public string AssetsBucket { get; set; } = string.Empty;

    public TimeSpan UploadUrlLifetime { get; set; } = TimeSpan.FromMinutes(15);
}

internal sealed class StorageOptionsValidator : IValidateOptions<StorageOptions>
{
    public ValidateOptionsResult Validate(string? name, StorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.UploadsBucket) || string.IsNullOrWhiteSpace(options.AssetsBucket))
        {
            failures.Add("Storage:UploadsBucket and Storage:AssetsBucket are required.");
        }

        if (string.Equals(options.UploadsBucket, options.AssetsBucket, StringComparison.Ordinal))
        {
            failures.Add("Uploads and published assets must live in different buckets: uploads are private until inspected.");
        }

        if (string.IsNullOrEmpty(options.AccessKey) != string.IsNullOrEmpty(options.SecretKey))
        {
            failures.Add("Storage:AccessKey and Storage:SecretKey must be set together.");
        }

        if (options.UploadUrlLifetime <= TimeSpan.Zero || options.UploadUrlLifetime > TimeSpan.FromHours(1))
        {
            failures.Add("Storage:UploadUrlLifetime must be positive and at most one hour.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
