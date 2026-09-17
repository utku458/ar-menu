using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ArMenu.Domain.Memberships;
using ArMenu.IntegrationTests.TestSupport;
using static ArMenu.IntegrationTests.Api.Assets.AssetFlows;

namespace ArMenu.IntegrationTests.Api.History;

public sealed class HistoryTests(PostgresDatabaseFixture database) : IAsyncLifetime
{
    private const string HistoryPath = "/api/v1/manage/history";

    private readonly ArMenuApiFactory _factory = new(database);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Edits_are_recorded_with_who_made_them_and_what_changed()
    {
        var restaurant = await database.Services.SeedTenantAsync(Ct);
        using var manager = await _factory.SignedInClientAsync(database, restaurant, TenantRole.Manager);
        using var staff = await _factory.SignedInClientAsync(database, restaurant, TenantRole.Staff);
        var itemPath = $"/api/v1/manage/menu/items/{restaurant.ItemId.Value}";

        using (var update = await manager.PutAsJsonAsync(itemPath, new
        {
            categoryId = restaurant.CategoryId.Value,
            name = new Dictionary<string, string> { ["tr"] = "Ev yapımı humus" },
            description = (object?)null,
            price = 210m,
            isVisible = true,
        }, Ct))
        {
            update.StatusCode.ShouldBe(HttpStatusCode.NoContent, await update.Content.ReadAsStringAsync(Ct));
        }

        using (var soldOut = await staff.PutAsJsonAsync($"{itemPath}/availability", new { isAvailable = false }, Ct))
        {
            soldOut.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }

        // Newest first; seeding the menu came before.
        var entries = (await manager.GetFromJsonAsync<JsonElement>(HistoryPath, Ct)).GetProperty("entries").EnumerateArray().ToList();

        var (availability, edit) = (entries[0], entries[1]);
        availability.GetProperty("actor").GetProperty("fullName").GetString().ShouldBe("Test Staff");
        Changes(availability).ShouldBe([("available", "true", "false")]);

        edit.GetProperty("action").GetString().ShouldBe("updated");
        edit.GetProperty("actor").GetProperty("fullName").GetString().ShouldBe("Test Manager");
        edit.GetProperty("subject").GetProperty("type").GetString().ShouldBe("menu_item");
        edit.GetProperty("subject").GetProperty("name").GetProperty("tr").GetString().ShouldBe("Ev yapımı humus");
        Changes(edit).ShouldBe(
        [
            ("name", """{"tr":"Humus"}""", """{"tr":"Ev yapımı humus"}"""),
            ("price", "180.00", "210"),
        ]);
    }

    [Fact]
    public async Task Reordering_is_one_entry_and_deleting_keeps_the_name()
    {
        var restaurant = await database.Services.SeedTenantAsync(Ct);
        using var owner = await _factory.SignedInClientAsync(database, restaurant, TenantRole.Owner);

        using var created = await owner.PostAsJsonAsync("/api/v1/manage/menu/items", new
        {
            categoryId = restaurant.CategoryId.Value,
            name = new Dictionary<string, string> { ["tr"] = "Babagannuş" },
            description = (object?)null,
            price = 150m,
        }, Ct);
        var newItem = (await created.ReadJsonAsync()).GetProperty("id").GetGuid();

        using (var reorder = await owner.PutAsJsonAsync($"/api/v1/manage/menu/categories/{restaurant.CategoryId.Value}/items/order", new { ids = new[] { newItem, restaurant.ItemId.Value } }, Ct))
        {
            reorder.StatusCode.ShouldBe(HttpStatusCode.NoContent, await reorder.Content.ReadAsStringAsync(Ct));
        }

        (await owner.DeleteAsync($"/api/v1/manage/menu/items/{newItem}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var entries = (await owner.GetFromJsonAsync<JsonElement>(HistoryPath, Ct)).GetProperty("entries").EnumerateArray()
            .Select(entry => (Action: entry.GetProperty("action").GetString(), Type: entry.GetProperty("subject").GetProperty("type").GetString(), Name: entry.GetProperty("subject").GetProperty("name")))
            .ToList();

        entries.Take(3).Select(entry => (entry.Action, entry.Type)).ShouldBe(
        [
            ("deleted", "menu_item"),
            ("reordered", "menu_category"),
            ("created", "menu_item"),
        ]);
        entries[0].Name.GetProperty("tr").GetString().ShouldBe("Babagannuş");
    }

    [Fact]
    public async Task History_comes_in_pages_and_belongs_to_the_business()
    {
        var restaurant = await database.Services.SeedTenantAsync(Ct);
        var neighbour = await database.Services.SeedTenantAsync(Ct);
        using var owner = await _factory.SignedInClientAsync(database, restaurant, TenantRole.Owner);
        using var neighbourOwner = await _factory.SignedInClientAsync(database, neighbour, TenantRole.Owner);

        foreach (var isAvailable in new[] { false, true, false })
        {
            using var response = await owner.PutAsJsonAsync($"/api/v1/manage/menu/items/{restaurant.ItemId.Value}/availability", new { isAvailable }, Ct);
            response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }

        // Three changes after the seeded category and item: pages of two are 2 + 2 + 1.
        List<JsonElement> seen = [];
        string? cursor = null;
        do
        {
            var page = await owner.GetFromJsonAsync<JsonElement>($"{HistoryPath}?limit=2{(cursor is null ? "" : $"&before={cursor}")}", Ct);
            page.GetProperty("entries").GetArrayLength().ShouldBeInRange(1, 2);
            seen.AddRange(page.GetProperty("entries").EnumerateArray());
            cursor = page.GetProperty("nextCursor").ValueKind == JsonValueKind.Null ? null : page.GetProperty("nextCursor").GetString();
        }
        while (cursor is not null);

        seen.Select(entry => entry.GetProperty("id").GetGuid()).ShouldBeUnique();
        seen.Select(entry => entry.GetProperty("action").GetString()).ShouldBe(["updated", "updated", "updated", "created", "created"]);
        Changes(seen[2]).ShouldBe([("available", "true", "false")]);

        (await neighbourOwner.GetFromJsonAsync<JsonElement>(HistoryPath, Ct)).GetProperty("entries").EnumerateArray()
            .Select(entry => entry.GetProperty("subject").GetProperty("id").GetGuid())
            .ShouldBe([neighbour.ItemId.Value, neighbour.CategoryId.Value], ignoreOrder: true);

        using var staff = await _factory.SignedInClientAsync(database, restaurant, TenantRole.Staff);
        (await staff.GetAsync(HistoryPath, Ct)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await owner.GetAsync($"{HistoryPath}?limit=500", Ct)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _factory.DisposeAsync();

    private static List<(string Field, string Before, string After)> Changes(JsonElement entry) =>
        [.. entry.GetProperty("changes").EnumerateArray().Select(change => (
            change.GetProperty("field").GetString()!,
            change.GetProperty("before").GetRawText(),
            change.GetProperty("after").GetRawText()))];
}
