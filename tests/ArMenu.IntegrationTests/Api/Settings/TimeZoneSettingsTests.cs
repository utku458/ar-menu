using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ArMenu.Domain.Memberships;
using ArMenu.IntegrationTests.TestSupport;
using static ArMenu.IntegrationTests.Api.Assets.AssetFlows;

namespace ArMenu.IntegrationTests.Api.Settings;

public sealed class TimeZoneSettingsTests(PostgresDatabaseFixture database) : IAsyncLifetime
{
    private const string TimeZonePath = "/api/v1/manage/settings/time-zone";

    // UTC+14 all year: its date differs from UTC's for ten hours of every day, so the assertion below is never vacuous.
    private const string FarEast = "Pacific/Kiritimati";

    private readonly ArMenuApiFactory _factory = new(database);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Guest_events_are_counted_on_the_business_day()
    {
        var restaurant = await database.Services.SeedTenantAsync(Ct);
        using var owner = await _factory.SignedInClientAsync(database, restaurant, TenantRole.Owner);

        using (var response = await owner.PutAsJsonAsync(TimeZonePath, new { timeZone = FarEast }, Ct))
        {
            response.StatusCode.ShouldBe(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync(Ct));
        }

        (await owner.GetFromJsonAsync<JsonElement>("/api/v1/me", Ct)).GetProperty("tenant").GetProperty("timeZone").GetString().ShouldBe(FarEast);

        using (var events = await _factory.CreateClient().PostAsJsonAsync($"/api/v1/menus/{restaurant.Tenant.Slug}/events", new { events = new[] { new { type = "menu_viewed" } } }, Ct))
        {
            events.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }

        var statistics = await owner.GetFromJsonAsync<JsonElement>("/api/v1/manage/statistics", Ct);
        var expectedDay = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(FarEast)).DateTime);
        statistics.GetProperty("to").GetString().ShouldBe(expectedDay.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
        statistics.GetProperty("days")[6].GetProperty("menuViews").GetInt64().ShouldBe(1);
    }

    [Theory]
    [InlineData("Turkey Standard Time")]
    [InlineData("")]
    public async Task Only_IANA_time_zones_are_accepted(string timeZone)
    {
        var restaurant = await database.Services.SeedTenantAsync(Ct);
        using var owner = await _factory.SignedInClientAsync(database, restaurant, TenantRole.Owner);

        using var response = await owner.PutAsJsonAsync(TimeZonePath, new { timeZone }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadJsonAsync()).GetProperty("errorCodes").GetProperty("timeZone")[0].GetString().ShouldStartWith("tenant.time_zone_");
    }

    [Theory]
    [InlineData("Europe/Istanbul", "Europe/Istanbul")]
    [InlineData("Somewhere/Unknown", "UTC")]
    public async Task A_business_signs_up_in_the_time_zone_its_browser_reports_if_it_is_known(string reported, string expected)
    {
        using var client = _factory.CreateBrowserClient();
        using var response = await client.PostAsJsonAsync("/api/v1/tenants", new
        {
            businessName = "Moda Kahve",
            slug = $"test-{Guid.NewGuid():N}",
            defaultCulture = "tr",
            currency = "TRY",
            ownerFullName = "Ayşe Tan",
            ownerEmail = $"owner-{Guid.NewGuid():N}@armenu.test",
            password = TestConfiguration.Password,
            timeZone = reported,
        }, Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        client.Authenticate((await response.ReadJsonAsync()).GetProperty("accessToken").GetString()!);
        (await client.GetFromJsonAsync<JsonElement>("/api/v1/me", Ct)).GetProperty("tenant").GetProperty("timeZone").GetString().ShouldBe(expected);
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _factory.DisposeAsync();
}
