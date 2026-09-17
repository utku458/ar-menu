using Amazon.S3.Model;
using ArMenu.Application.Assets;
using ArMenu.Domain.Tenants;
using ArMenu.Infrastructure.Retention;
using ArMenu.IntegrationTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace ArMenu.IntegrationTests.Retention;

public sealed class TenantPurgeTests(PostgresDatabaseFixture database, StorageFixture storage) : IAsyncDisposable
{
    private static readonly DateTimeOffset ClosedOn = new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _time = new(ClosedOn);
    private TestServices? _services;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_closed_business_keeps_its_data_for_the_retention_period_then_loses_everything_but_its_slug()
    {
        var closed = await SeedClosedAsync();
        var neighbour = await Services.SeedTenantAsync(Ct);
        var files = new[]
        {
            (StorageFixture.AssetsBucket, $"{TenantAssetKeys.PublishedPrefix(closed.Tenant.Id)}photo.webp"),
            (StorageFixture.UploadsBucket, $"{TenantAssetKeys.SourcesPrefix(closed.Tenant.Id)}model.glb"),
            (StorageFixture.UploadsBucket, $"{TenantAssetKeys.StagingPrefix(closed.Tenant.Id)}upload"),
        };
        var neighbourFile = $"{TenantAssetKeys.PublishedPrefix(neighbour.Tenant.Id)}photo.webp";
        foreach (var (bucket, key) in files.Append((StorageFixture.AssetsBucket, neighbourFile)))
        {
            await storage.Client.PutObjectAsync(new PutObjectRequest { BucketName = bucket, Key = key, ContentBody = "file" }, Ct);
        }

        _time.Advance(TimeSpan.FromDays(29));
        (await Purge.RunAsync(Ct)).ShouldBe(0, "still within the 30 days");

        _time.Advance(TimeSpan.FromDays(1));
        (await Purge.RunAsync(Ct)).ShouldBeGreaterThanOrEqualTo(1);

        foreach (var (bucket, key) in files)
        {
            (await ExistsAsync(bucket, key)).ShouldBeFalse(key);
        }

        (await ExistsAsync(StorageFixture.AssetsBucket, neighbourFile)).ShouldBeTrue("another business's files");

        var id = closed.Tenant.Id.Value;
        foreach (var table in new[] { "menu_items", "menu_categories", "audit_log_entries", "menu_daily_statistics", "tenant_memberships" })
        {
            (await database.ScalarAsOwnerAsync<long>($"SELECT count(*) FROM {table} WHERE tenant_id = '{id}'", Ct)).ShouldBe(0, table);
        }

        (await database.ScalarAsOwnerAsync<long>($"SELECT count(*) FROM menu_items WHERE tenant_id = '{neighbour.Tenant.Id.Value}'", Ct)).ShouldBe(1);

        await using var scope = Services.BeginScope();
        var tenant = await scope.Db.Tenants.SingleAsync(row => row.Id == closed.Tenant.Id, Ct);
        tenant.Status.ShouldBe(TenantStatus.Closed);
        tenant.PurgedAt.ShouldBe(_time.GetUtcNow());

        (await Purge.RunAsync(Ct)).ShouldBe(0, "a purged business is not purged again");
    }

    public async ValueTask DisposeAsync()
    {
        if (_services is not null)
        {
            await _services.DisposeAsync();
        }
    }

    private TestServices Services => _services ??= new TestServices(database.RuntimeConnectionString, _time, storageFixture: storage);

    private TenantPurge Purge => Services.Get<TenantPurge>();

    private async Task<SeededTenant> SeedClosedAsync()
    {
        var seeded = await Services.SeedTenantAsync(Ct);
        await using var scope = Services.BeginScope(seeded.Tenant);
        var item = await scope.Db.MenuItems.SingleAsync(Ct);
        item.MarkAsSoldOut();
        await scope.Db.SaveChangesAsync(Ct);

        // Closed the way an erasure closes it, without a tenant-bound write path.
        await database.ExecuteAsOwnerAsync(
            $"UPDATE tenants SET status = 'Closed', closed_at = '{ClosedOn:O}' WHERE id = '{seeded.Tenant.Id.Value}'", Ct);
        return seeded;
    }

    private async Task<bool> ExistsAsync(string bucket, string key)
    {
        var listing = await storage.Client.ListObjectsV2Async(new ListObjectsV2Request { BucketName = bucket, Prefix = key }, Ct);
        return (listing.S3Objects ?? []).Any(entry => entry.Key == key);
    }
}
