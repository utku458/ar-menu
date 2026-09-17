using Amazon.S3;
using Amazon.S3.Model;
using ArMenu.Application.Assets;
using ArMenu.Application.Common.Diagnostics;
using ArMenu.Application.MultiTenancy;
using ArMenu.Domain.ArModels;
using ArMenu.Domain.Media;
using ArMenu.Domain.Menus;
using ArMenu.Domain.Tenants;
using ArMenu.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArMenu.Infrastructure.Assets;

/// <summary>
/// Deletes files nothing refers to anymore: abandoned uploads, models replaced or removed from a dish, sources of
/// failed or superseded processings. Keys are immutable, so a file that no menu item references and that is older than
/// the grace period can never be needed again.
/// </summary>
/// <remarks>
/// What is referenced is read per tenant, through row-level security. Soft-deleted items keep their files, like the
/// rest of their data. Demo files live outside tenant prefixes and are never touched.
/// </remarks>
internal sealed partial class AssetCleanup(
    IServiceScopeFactory scopeFactory,
    IAmazonS3 s3,
    IOptions<StorageOptions> storageOptions,
    IOptions<AssetCleanupOptions> cleanupOptions,
    TimeProvider timeProvider,
    ArMenuMetrics metrics,
    ILogger<AssetCleanup> logger)
{
    private readonly StorageOptions _storage = storageOptions.Value;

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        using var activity = ArMenuTelemetry.ActivitySource.StartActivity("AssetCleanup");
        var deleted = 0;
        foreach (var tenantId in await ListTenantIdsAsync(cancellationToken))
        {
            deleted += await CleanTenantAsync(tenantId, cancellationToken);
        }

        if (deleted > 0)
        {
            LogDeleted(logger, deleted);
        }

        return deleted;
    }

    /// <summary>Cleans one tenant's staging uploads, published files and sources.</summary>
    public async Task<int> CleanTenantAsync(TenantId tenantId, CancellationToken cancellationToken)
    {
        var cutoff = timeProvider.GetUtcNow() - cleanupOptions.Value.GracePeriod;

        await using var scope = scopeFactory.CreateAsyncScope();
        var tenant = await scope.ServiceProvider.GetRequiredService<ITenantLookup>().FindByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            return 0;
        }

        scope.ServiceProvider.GetRequiredService<ITenantContextSetter>().SetTenant(tenant);
        var dbContext = scope.ServiceProvider.GetRequiredService<ArMenuDbContext>();

        var staging = await ListOlderThanAsync(_storage.UploadsBucket, TenantAssetKeys.StagingPrefix(tenantId), cutoff, cancellationToken);

        var models = await dbContext.MenuItems
            .IgnoreQueryFilters([QueryFilters.SoftDelete])
            .AsNoTracking()
            .Select(item => item.ArModel)
            .ToListAsync(cancellationToken);
        var referenced = models
            .OfType<ArModel>()
            .SelectMany(model => new[] { model.GlbPath, model.SceneViewerGlbPath, model.UsdzPath, model.PosterPath })
            .OfType<AssetPath>()
            .Select(path => path.Value)
            .ToHashSet(StringComparer.Ordinal);

        // The business's logo lives under the same published prefix as the models, and nothing else points at it.
        if (tenant.LogoPath is { } logoPath)
        {
            referenced.Add(logoPath);
        }

        var unfinished = (await dbContext.ArModelProcessings
                .AsNoTracking()
                .Where(processing => processing.Status == ArModelProcessingStatus.Queued || processing.Status == ArModelProcessingStatus.Processing)
                .Select(processing => processing.Id)
                .ToListAsync(cancellationToken))
            .Select(id => id.Value.ToString("N"))
            .ToHashSet(StringComparer.Ordinal);

        // File names start with the id of the upload or processing that produced them.
        var referencedIds = referenced.Select(IdOf).ToHashSet(StringComparer.Ordinal);

        var published = (await ListOlderThanAsync(_storage.AssetsBucket, TenantAssetKeys.PublishedPrefix(tenantId), cutoff, cancellationToken))
            .Where(key => !referenced.Contains(key) && !unfinished.Contains(IdOf(key)))
            .ToList();

        // Sources are kept while their model is in use, so it can be processed again when the pipeline improves.
        var sources = (await ListOlderThanAsync(_storage.UploadsBucket, TenantAssetKeys.SourcesPrefix(tenantId), cutoff, cancellationToken))
            .Where(key => !referencedIds.Contains(IdOf(key)) && !unfinished.Contains(IdOf(key)))
            .ToList();

        return await DeleteAsync(_storage.UploadsBucket, "staging", staging, cancellationToken) +
               await DeleteAsync(_storage.AssetsBucket, "published", published, cancellationToken) +
               await DeleteAsync(_storage.UploadsBucket, "source", sources, cancellationToken);
    }

    private static string IdOf(string key)
    {
        var name = key[(key.LastIndexOf('/') + 1)..];
        var dot = name.IndexOf('.', StringComparison.Ordinal);
        return dot < 0 ? name : name[..dot];
    }

    private async Task<List<TenantId>> ListTenantIdsAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ArMenuDbContext>().Tenants
            .AsNoTracking()
            .Select(tenant => tenant.Id)
            .ToListAsync(cancellationToken);
    }

    private async Task<List<string>> ListOlderThanAsync(string bucket, string prefix, DateTimeOffset cutoff, CancellationToken cancellationToken)
    {
        var keys = new List<string>();
        var request = new ListObjectsV2Request { BucketName = bucket, Prefix = prefix };

        do
        {
            var response = await s3.ListObjectsV2Async(request, cancellationToken);
            keys.AddRange((response.S3Objects ?? [])
                .Where(entry => entry.LastModified is { } modified && new DateTimeOffset(modified.ToUniversalTime(), TimeSpan.Zero) < cutoff)
                .Select(entry => entry.Key));
            request.ContinuationToken = response.NextContinuationToken;
        }
        while (!string.IsNullOrEmpty(request.ContinuationToken));

        return keys;
    }

    private async Task<int> DeleteAsync(string bucket, string location, List<string> keys, CancellationToken cancellationToken)
    {
        foreach (var batch in keys.Chunk(1000))
        {
            await s3.DeleteObjectsAsync(
                new DeleteObjectsRequest { BucketName = bucket, Objects = [.. batch.Select(key => new KeyVersion { Key = key })] },
                cancellationToken);
        }

        metrics.FilesCleanedUp(location, keys.Count);
        return keys.Count;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Asset cleanup deleted {Count} unused files")]
    private static partial void LogDeleted(ILogger logger, int count);
}
