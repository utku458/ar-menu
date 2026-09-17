using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ArMenu.Domain.Memberships;
using ArMenu.IntegrationTests.TestSupport;
using static ArMenu.IntegrationTests.Api.Assets.AssetFlows;

namespace ArMenu.IntegrationTests.Api.Menus;

public sealed class MenuStatisticsTests(PostgresDatabaseFixture database) : IAsyncLifetime
{
    private readonly ArMenuApiFactory _factory = new(database);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Guest_events_add_up_per_day_and_dish_for_the_business_only()
    {
        var restaurant = await database.Services.SeedTenantAsync(Ct);
        var neighbour = await database.Services.SeedTenantAsync(Ct);
        using var guest = _factory.CreateClient();

        (await SendAsync(guest, restaurant.Tenant.Slug,
            new { type = "menu_viewed" },
            new { type = "dish_opened", itemId = restaurant.ItemId.Value },
            new { type = "model_viewed", itemId = restaurant.ItemId.Value },
            new { type = "ar_started", itemId = restaurant.ItemId.Value })).ShouldBe(HttpStatusCode.NoContent);
        (await SendAsync(guest, restaurant.Tenant.Slug,
            new { type = "menu_viewed" },
            new { type = "dish_opened", itemId = restaurant.ItemId.Value },
            // Made-up and foreign dishes count nothing.
            new { type = "dish_opened", itemId = Guid.NewGuid() },
            new { type = "dish_opened", itemId = neighbour.ItemId.Value })).ShouldBe(HttpStatusCode.NoContent);
        (await SendAsync(guest, neighbour.Tenant.Slug, new { type = "menu_viewed" })).ShouldBe(HttpStatusCode.NoContent);

        using var manager = await _factory.SignedInClientAsync(database, restaurant, TenantRole.Manager);
        var statistics = await manager.GetFromJsonAsync<JsonElement>("/api/v1/manage/statistics?days=30", Ct);

        var totals = statistics.GetProperty("totals");
        (totals.GetProperty("menuViews").GetInt64(), totals.GetProperty("dishOpens").GetInt64(), totals.GetProperty("modelViews").GetInt64(), totals.GetProperty("arStarts").GetInt64())
            .ShouldBe((2L, 2L, 1L, 1L));

        var days = statistics.GetProperty("days");
        days.GetArrayLength().ShouldBe(30);
        days[29].GetProperty("menuViews").GetInt64().ShouldBe(2);
        days[29].GetProperty("day").GetString().ShouldBe(statistics.GetProperty("to").GetString());

        var dish = statistics.GetProperty("dishes").EnumerateArray().ShouldHaveSingleItem();
        dish.GetProperty("itemId").GetGuid().ShouldBe(restaurant.ItemId.Value);
        dish.GetProperty("name").GetString().ShouldBe("Humus");
        dish.GetProperty("opens").GetInt64().ShouldBe(2);
        dish.GetProperty("arStarts").GetInt64().ShouldBe(1);
    }

    [Fact]
    public async Task Guests_opening_the_same_dish_at_once_are_all_counted()
    {
        var restaurant = await database.Services.SeedTenantAsync(Ct);
        using var guest = _factory.CreateClient();

        await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => SendAsync(guest, restaurant.Tenant.Slug, new { type = "dish_opened", itemId = restaurant.ItemId.Value })));

        using var owner = await _factory.SignedInClientAsync(database, restaurant, TenantRole.Owner);
        var statistics = await owner.GetFromJsonAsync<JsonElement>("/api/v1/manage/statistics", Ct);
        statistics.GetProperty("totals").GetProperty("dishOpens").GetInt64().ShouldBe(20);
        statistics.GetProperty("days").GetArrayLength().ShouldBe(7);
    }

    [Fact]
    public async Task Malformed_batches_are_refused_and_staff_cannot_read_statistics()
    {
        var restaurant = await database.Services.SeedTenantAsync(Ct);
        using var guest = _factory.CreateClient();

        (await SendAsync(guest, restaurant.Tenant.Slug, new { type = "dish_opened" })).ShouldBe(HttpStatusCode.BadRequest);
        (await SendAsync(guest, restaurant.Tenant.Slug, new { type = "menu_viewed", itemId = restaurant.ItemId.Value })).ShouldBe(HttpStatusCode.BadRequest);
        (await SendAsync(guest, restaurant.Tenant.Slug, [.. Enumerable.Repeat<object>(new { type = "menu_viewed" }, 21)])).ShouldBe(HttpStatusCode.BadRequest);
        (await SendAsync(guest, "no-such-restaurant", new { type = "menu_viewed" })).ShouldBe(HttpStatusCode.NotFound);

        using var staff = await _factory.SignedInClientAsync(database, restaurant, TenantRole.Staff);
        (await staff.GetAsync("/api/v1/manage/statistics", Ct)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        using var owner = await _factory.SignedInClientAsync(database, restaurant, TenantRole.Owner);
        (await owner.GetAsync("/api/v1/manage/statistics?days=365", Ct)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private static async Task<HttpStatusCode> SendAsync(HttpClient client, string slug, params object[] events)
    {
        using var response = await client.PostAsJsonAsync($"/api/v1/menus/{slug}/events", new { events }, Ct);
        return response.StatusCode;
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _factory.DisposeAsync();
}
