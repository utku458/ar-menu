using System.Net;
using System.Net.Http.Json;
using ArMenu.Application.Menus.Queries.GetPublicMenu;
using ArMenu.Domain.Memberships;
using ArMenu.IntegrationTests.TestSupport;

namespace ArMenu.IntegrationTests.Api.Settings;

public sealed class LanguageSettingsTests(PostgresDatabaseFixture database) : IAsyncLifetime
{
    private const string LanguagesPath = "/api/v1/manage/settings/languages";

    private readonly ArMenuApiFactory _factory = new(database);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Guests_and_staff_see_changed_languages_immediately_despite_caches()
    {
        var restaurant = await database.Services.SeedTenantAsync(Ct);
        using var owner = await SignedInClientAsync(restaurant);
        using var guest = _factory.CreateBrowserClient();
        (await GetMenuAsync(guest, restaurant)).Tenant.SupportedCultures.ShouldBe(["tr"]);

        using var response = await owner.PutAsJsonAsync(LanguagesPath, new LanguagesBody("en", ["tr", "en", "de"]), Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync(Ct));
        var tenant = (await GetMenuAsync(guest, restaurant)).Tenant;
        tenant.DefaultCulture.ShouldBe("en");
        tenant.SupportedCultures.ShouldBe(["tr", "en", "de"]);

        var me = await owner.GetFromJsonAsync<CurrentUserBody>("/api/v1/me", Ct);
        me!.Tenant.SupportedCultures.ShouldBe(["tr", "en", "de"]);
    }

    [Fact]
    public async Task The_default_language_must_be_one_of_the_offered_languages()
    {
        var restaurant = await database.Services.SeedTenantAsync(Ct);
        using var owner = await SignedInClientAsync(restaurant);

        using var response = await owner.PutAsJsonAsync(LanguagesPath, new LanguagesBody("de", ["tr", "en"]), Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadJsonAsync()).GetProperty("errorCodes").GetProperty("defaultCulture")[0].GetString()
            .ShouldBe("tenant.culture_not_supported");
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _factory.DisposeAsync();

    private async Task<HttpClient> SignedInClientAsync(SeededTenant restaurant)
    {
        var owner = await database.Services.SeedMemberAsync(restaurant.Tenant, TenantRole.Owner, Ct);
        var client = _factory.CreateBrowserClient();
        return client.Authenticate(await client.SignInAsync(restaurant.Tenant.Slug, owner.Email));
    }

    private static async Task<PublicMenuResponse> GetMenuAsync(HttpClient client, SeededTenant restaurant) =>
        (await client.GetFromJsonAsync<PublicMenuResponse>($"/api/v1/menus/{restaurant.Tenant.Slug}", Ct))!;

    private sealed record LanguagesBody(string DefaultCulture, List<string> SupportedCultures);

    private sealed record CurrentUserBody(CurrentTenantBody Tenant);

    private sealed record CurrentTenantBody(string DefaultCulture, List<string> SupportedCultures);
}
