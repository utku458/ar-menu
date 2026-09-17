using System.Text.Json;
using ArMenu.Domain.Media;
using ArMenu.Domain.Menus;
using ArMenu.Infrastructure.Auditing;
using ArMenu.IntegrationTests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace ArMenu.IntegrationTests.Persistence;

/// <summary>The history written by the save pipeline itself, for changes no endpoint test reaches easily.</summary>
public sealed class AuditTrailTests(PostgresDatabaseFixture database)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Models_attached_by_the_system_are_recorded_without_an_actor()
    {
        var seeded = await database.Services.SeedTenantAsync(Ct);

        await using (var scope = database.Services.BeginScope(seeded.Tenant))
        {
            var item = await scope.Db.MenuItems.SingleAsync(Ct);
            item.AttachArModel(ArModel.Create(AssetPath.Create("tenants/x/assets/humus.glb").Value).Value);
            await scope.Db.SaveChangesAsync(Ct);

            item.DetachArModel();
            await scope.Db.SaveChangesAsync(Ct);
        }

        await using var verification = database.Services.BeginScope(seeded.Tenant);
        var entries = await verification.Db.Set<AuditLogEntry>()
            .Where(entry => entry.Action == AuditActions.Updated)
            .OrderBy(entry => entry.Id)
            .ToListAsync(Ct);

        entries.Select(entry => JsonSerializer.Deserialize<List<AuditChange>>(entry.Changes, AuditJson.Options)!.Single())
            .Select(change => (change.Field, change.Before!.GetValue<bool>(), change.After!.GetValue<bool>()))
            .ShouldBe([("model", false, true), ("model", true, false)]);
        entries.ShouldAllBe(entry => entry.ActorUserId == null);
    }

    [Fact]
    public async Task A_failed_save_leaves_no_history_behind_for_the_next_one()
    {
        var seeded = await database.Services.SeedTenantAsync(Ct);

        await using (var scope = database.Services.BeginScope(seeded.Tenant))
        {
            // A category that does not exist: the database refuses the item.
            var orphan = TestData.NewItem(seeded.Tenant.Id, MenuCategoryId.New());
            scope.Db.MenuItems.Add(orphan);
            await Should.ThrowAsync<DbUpdateException>(() => scope.Db.SaveChangesAsync(Ct));

            scope.Db.Entry(orphan).State = EntityState.Detached;
            var item = await scope.Db.MenuItems.SingleAsync(Ct);
            item.MarkAsSoldOut();
            await scope.Db.SaveChangesAsync(Ct);
        }

        await using var verification = database.Services.BeginScope(seeded.Tenant);
        var actions = await verification.Db.Set<AuditLogEntry>().OrderBy(entry => entry.Id).Select(entry => entry.Action).ToListAsync(Ct);
        actions.Where(action => action != AuditActions.Created).ShouldBe([AuditActions.Updated]);
        actions.Count(action => action == AuditActions.Created).ShouldBe(2, "the seeded category and item, not the refused item");
    }
}
