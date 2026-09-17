using ArMenu.Domain.Localization;
using ArMenu.Domain.Media;
using ArMenu.Domain.Menus;
using ArMenu.Infrastructure.Persistence;
using ArMenu.IntegrationTests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace ArMenu.IntegrationTests.Persistence;

/// <summary>Soft deletion, auditing, optimistic concurrency and value object round-trips.</summary>
public sealed class EntityLifecycleTests(PostgresDatabaseFixture database)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Removing_a_menu_item_soft_deletes_it_and_stamps_the_time()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 9, 13, 12, 0, 0, TimeSpan.Zero));
        await using var services = new TestServices(database.RuntimeConnectionString, clock);
        var seeded = await services.SeedTenantAsync(Ct);

        clock.Advance(TimeSpan.FromHours(3));
        await using (var scope = services.BeginScope(seeded.Tenant))
        {
            var item = await scope.Db.MenuItems.SingleAsync(Ct);
            scope.Db.MenuItems.Remove(item);
            await scope.Db.SaveChangesAsync(Ct);
        }

        await using var verification = services.BeginScope(seeded.Tenant);
        (await verification.Db.MenuItems.AnyAsync(Ct)).ShouldBeFalse();

        var deleted = await verification.Db.MenuItems.IgnoreQueryFilters([QueryFilters.SoftDelete]).SingleAsync(Ct);
        deleted.IsDeleted.ShouldBeTrue();
        deleted.DeletedAt.ShouldBe(clock.GetUtcNow());
        deleted.CreatedAt.ShouldBe(new DateTimeOffset(2026, 9, 13, 12, 0, 0, TimeSpan.Zero));
        deleted.UpdatedAt.ShouldBe(clock.GetUtcNow());
    }

    [Fact]
    public async Task Concurrent_edits_of_the_same_item_are_detected()
    {
        var seeded = await database.Services.SeedTenantAsync(Ct);

        await using var firstEditor = database.Services.BeginScope(seeded.Tenant);
        await using var secondEditor = database.Services.BeginScope(seeded.Tenant);
        var firstCopy = await firstEditor.Db.MenuItems.SingleAsync(Ct);
        var secondCopy = await secondEditor.Db.MenuItems.SingleAsync(Ct);

        firstCopy.MarkAsSoldOut();
        await firstEditor.Db.SaveChangesAsync(Ct);

        secondCopy.Hide();
        await Should.ThrowAsync<DbUpdateConcurrencyException>(() => secondEditor.Db.SaveChangesAsync(Ct));
    }

    [Fact]
    public async Task Value_objects_survive_a_database_round_trip()
    {
        var seeded = await database.Services.SeedTenantAsync(Ct);
        var name = LocalizedText.Create([KeyValuePair.Create("tr", "Izgara Levrek"), KeyValuePair.Create("de", "Gegrillter Wolfsbarsch")]).Value;
        var arModel = ArModel.Create(
            AssetPath.Create("tenants/demo/models/sea-bass.glb").Value,
            AssetPath.Create("tenants/demo/models/sea-bass.scene-viewer.glb").Value,
            AssetPath.Create("tenants/demo/models/sea-bass.usdz").Value,
            AssetPath.Create("tenants/demo/posters/sea-bass.webp").Value).Value;

        await using (var scope = database.Services.BeginScope(seeded.Tenant))
        {
            var item = await scope.Db.MenuItems.SingleAsync(Ct);
            item.Rename(name).IsSuccess.ShouldBeTrue();
            item.AttachArModel(arModel);
            await scope.Db.SaveChangesAsync(Ct);
        }

        await using var verification = database.Services.BeginScope(seeded.Tenant);
        var reloaded = await verification.Db.MenuItems.AsNoTracking().SingleAsync(Ct);
        var tenant = await verification.Db.Tenants.AsNoTracking().SingleAsync(t => t.Id == seeded.Tenant.Id, Ct);

        reloaded.Name.ShouldBe(name);
        reloaded.ArModel.ShouldBe(arModel);
        reloaded.Price.Amount.ShouldBe(180m);
        tenant.SupportedCultures.ShouldBe([CultureCode.Create("tr").Value]);
    }

    [Fact]
    public async Task Items_without_ar_model_load_with_a_null_model()
    {
        var seeded = await database.Services.SeedTenantAsync(Ct);

        await using var scope = database.Services.BeginScope(seeded.Tenant);
        var item = await scope.Db.MenuItems.AsNoTracking().SingleAsync(menuItem => menuItem.Id == seeded.ItemId, Ct);

        item.ArModel.ShouldBeNull();
    }
}
