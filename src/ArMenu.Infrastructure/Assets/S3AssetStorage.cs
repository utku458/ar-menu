using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using ArMenu.Application.Abstractions.Assets;
using ArMenu.Domain.Media;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ArMenu.Infrastructure.Assets;

internal sealed class S3AssetStorage(
    IAmazonS3 s3,
    [FromKeyedServices(StorageOptions.InternalClientKey)] IAmazonS3 internalS3,
    [FromKeyedServices(StorageOptions.PublicClientKey)] IAmazonS3 publicS3,
    IOptions<StorageOptions> options,
    TimeProvider timeProvider) : IAssetStorage
{
    /// <summary>Published keys are never overwritten (a new upload gets a new key), so they can be cached forever.</summary>
    public const string PublishedCacheControl = "public, max-age=31536000, immutable";

    private readonly StorageOptions _options = options.Value;

    public async Task<PresignedUpload> CreateUploadAsync(string stagingKey, string contentType, long size, CancellationToken cancellationToken)
    {
        var expiresAt = timeProvider.GetUtcNow() + _options.UploadUrlLifetime;
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.UploadsBucket,
            Key = stagingKey,
            Verb = HttpVerb.PUT,
            Expires = expiresAt.UtcDateTime,
            ContentType = contentType,
            Protocol = ProtocolOf(_options.PublicServiceUrl ?? _options.ServiceUrl),
        };

        // Signing the length and the type makes storage itself reject any other file, before a byte is stored.
        request.Headers.ContentLength = size;

        // Signing needs no connection: the public client may name a host the API itself cannot reach.
        var url = await publicS3.GetPreSignedURLAsync(request);
        return new PresignedUpload(new Uri(url), new Dictionary<string, string> { ["Content-Type"] = contentType }, expiresAt);
    }

    public Task<StagedObject?> FindStagedAsync(string stagingKey, CancellationToken cancellationToken) =>
        FindAsync(_options.UploadsBucket, stagingKey, cancellationToken);

    public Task<byte[]> ReadStagedAsync(string stagingKey, long offset, int count, CancellationToken cancellationToken) =>
        ReadAsync(_options.UploadsBucket, stagingKey, offset, count, cancellationToken);

    public async Task PublishAsync(string stagingKey, AssetPath path, string contentType, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(path);

        var copy = new CopyObjectRequest
        {
            SourceBucket = _options.UploadsBucket,
            SourceKey = stagingKey,
            DestinationBucket = _options.AssetsBucket,
            DestinationKey = path.Value,
            // The detected type replaces whatever the uploader declared.
            MetadataDirective = S3MetadataDirective.REPLACE,
            ContentType = contentType,
        };
        copy.Headers.CacheControl = PublishedCacheControl;

        await s3.CopyObjectAsync(copy, cancellationToken);
        await DeleteStagedAsync(stagingKey, cancellationToken);
    }

    public Task DeleteStagedAsync(string stagingKey, CancellationToken cancellationToken) =>
        s3.DeleteObjectAsync(_options.UploadsBucket, stagingKey, cancellationToken);

    public async Task<bool> ExistsAsync(AssetPath path, CancellationToken cancellationToken) =>
        await FindPublishedAsync(path, cancellationToken) is not null;

    public async Task MoveToSourcesAsync(string stagingKey, string sourceKey, CancellationToken cancellationToken)
    {
        await s3.CopyObjectAsync(_options.UploadsBucket, stagingKey, _options.UploadsBucket, sourceKey, cancellationToken);
        await DeleteStagedAsync(stagingKey, cancellationToken);
    }

    public async Task<Uri> CreateSourceDownloadAsync(string sourceKey, TimeSpan lifetime, CancellationToken cancellationToken)
    {
        var url = await internalS3.GetPreSignedURLAsync(new GetPreSignedUrlRequest
        {
            BucketName = _options.UploadsBucket,
            Key = sourceKey,
            Verb = HttpVerb.GET,
            Expires = (timeProvider.GetUtcNow() + lifetime).UtcDateTime,
            Protocol = ProtocolOf(_options.InternalServiceUrl ?? _options.ServiceUrl),
        });

        return new Uri(url);
    }

    public async Task<PresignedUpload> CreateProcessedUploadAsync(
        AssetPath path,
        string contentType,
        TimeSpan lifetime,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(path);

        var expiresAt = timeProvider.GetUtcNow() + lifetime;
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.AssetsBucket,
            Key = path.Value,
            Verb = HttpVerb.PUT,
            Expires = expiresAt.UtcDateTime,
            ContentType = contentType,
            Protocol = ProtocolOf(_options.InternalServiceUrl ?? _options.ServiceUrl),
        };
        request.Headers.CacheControl = PublishedCacheControl;

        var url = await internalS3.GetPreSignedURLAsync(request);
        return new PresignedUpload(
            new Uri(url),
            new Dictionary<string, string> { ["Content-Type"] = contentType, ["Cache-Control"] = PublishedCacheControl },
            expiresAt);
    }

    public async Task<StagedObject?> FindPublishedAsync(AssetPath path, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(path);
        return await FindAsync(_options.AssetsBucket, path.Value, cancellationToken);
    }

    public Task<byte[]> ReadPublishedAsync(AssetPath path, long offset, int count, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(path);
        return ReadAsync(_options.AssetsBucket, path.Value, offset, count, cancellationToken);
    }

    public Task DeletePublishedAsync(AssetPath path, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(path);
        return s3.DeleteObjectAsync(_options.AssetsBucket, path.Value, cancellationToken);
    }

    private static Protocol ProtocolOf(Uri? serviceUrl) => serviceUrl?.Scheme == Uri.UriSchemeHttp ? Protocol.HTTP : Protocol.HTTPS;

    private async Task<StagedObject?> FindAsync(string bucket, string key, CancellationToken cancellationToken)
    {
        try
        {
            var metadata = await s3.GetObjectMetadataAsync(bucket, key, cancellationToken);
            return new StagedObject(metadata.ContentLength, metadata.Headers.ContentType);
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    private async Task<byte[]> ReadAsync(string bucket, string key, long offset, int count, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        using var response = await s3.GetObjectAsync(
            new GetObjectRequest { BucketName = bucket, Key = key, ByteRange = new ByteRange(offset, offset + count - 1) },
            cancellationToken);

        var buffer = new byte[count];
        var read = await response.ResponseStream.ReadAtLeastAsync(buffer, count, throwOnEndOfStream: false, cancellationToken);
        return read == count ? buffer : buffer[..read];
    }
}
