using System.Net;
using System.Net.Http.Json;
using ArMenu.Application.Menus.Queries.GetPublicMenu;
using ArMenu.Domain.Memberships;
using ArMenu.IntegrationTests.TestSupport;
using static ArMenu.IntegrationTests.Api.Assets.AssetFlows;

namespace ArMenu.IntegrationTests.Api.Settings;

public sealed class BrandingSettingsTests(PostgresDatabaseFixture database, StorageFixture storage) : IAsyncLifetime
{
    private const string BrandingPath = "/api/v1/manage/settings/branding";

    private readonly ArMenuApiFactory _factory = new(database, storage);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Guests_see_the_name_logo_and_colour_the_business_saved()
    {
        var restaurant = await database.Services.SeedTenantAsync(Ct);
        using var owner = await _factory.SignedInClientAsync(database, restaurant, TenantRole.Owner);
        using var guest = _factory.CreateBrowserClient();
        var logo = await UploadAndPublishAsync(owner, "logo", "image/webp", await DemoFileAsync("posters/sea-bass.webp"));

        using var response = await owner.PutAsJsonAsync(BrandingPath, new BrandingBody("Deniz Balık", logo.Path, "#1F6F5C"), Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync(Ct));
        var tenant = (await GetMenuAsync(guest, restaurant)).Tenant;
        tenant.Name.ShouldBe("Deniz Balık");
        tenant.LogoUrl!.ToString().ShouldEndWith(logo.Path);
        tenant.AccentColor.ShouldBe("#1f6f5c");
        // Dark green carries white text; the API decides the pairing so no client has to.
        tenant.OnAccentColor.ShouldBe("#ffffff");
    }

    [Fact]
    public async Task A_business_can_go_back_to_having_no_logo_and_no_colour()
    {
        var restaurant = await database.Services.SeedTenantAsync(Ct);
        using var owner = await _factory.SignedInClientAsync(database, restaurant, TenantRole.Owner);
        using var guest = _factory.CreateBrowserClient();
        var logo = await UploadAndPublishAsync(owner, "logo", "image/webp", await DemoFileAsync("posters/sea-bass.webp"));
        (await owner.PutAsJsonAsync(BrandingPath, new BrandingBody("Deniz Balık", logo.Path, "#1f6f5c"), Ct)).Dispose();

        using var response = await owner.PutAsJsonAsync(BrandingPath, new BrandingBody("Deniz Balık", null, null), Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var tenant = (await GetMenuAsync(guest, restaurant)).Tenant;
        tenant.LogoUrl.ShouldBeNull();
        tenant.AccentColor.ShouldBeNull();
        tenant.OnAccentColor.ShouldBeNull();
    }

    [Fact]
    public async Task Another_business_logo_cannot_be_put_on_this_menu()
    {
        var neighbour = await database.Services.SeedTenantAsync(Ct);
        using var neighbourOwner = await _factory.SignedInClientAsync(database, neighbour, TenantRole.Owner);
        var logo = await UploadAndPublishAsync(neighbourOwner, "logo", "image/webp", await DemoFileAsync("posters/sea-bass.webp"));

        var restaurant = await database.Services.SeedTenantAsync(Ct);
        using var owner = await _factory.SignedInClientAsync(database, restaurant, TenantRole.Owner);

        using var response = await owner.PutAsJsonAsync(BrandingPath, new BrandingBody("Deniz Balık", logo.Path, null), Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadJsonAsync()).GetProperty("errorCodes").GetProperty("logoPath")[0].GetString()
            .ShouldBe("asset.not_owned");
    }

    [Theory]
    [InlineData("1f6f5c")]
    [InlineData("#1f6f5c80")]
    [InlineData("rebeccapurple")]
    public async Task A_colour_that_is_not_six_hexadecimal_digits_is_refused(string color)
    {
        var restaurant = await database.Services.SeedTenantAsync(Ct);
        using var owner = await _factory.SignedInClientAsync(database, restaurant, TenantRole.Owner);

        using var response = await owner.PutAsJsonAsync(BrandingPath, new BrandingBody("Deniz Balık", null, color), Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadJsonAsync()).GetProperty("errorCodes").GetProperty("accentColor")[0].GetString()
            .ShouldBe("tenant.brand_color_invalid");
    }

    [Fact]
    public async Task An_svg_cannot_be_uploaded_as_a_logo()
    {
        var restaurant = await database.Services.SeedTenantAsync(Ct);
        using var owner = await _factory.SignedInClientAsync(database, restaurant, TenantRole.Owner);
        var svg = "<svg xmlns='http://www.w3.org/2000/svg'><script>alert(1)</script></svg>"u8.ToArray();

        // Declaring it as a PNG gets it into storage; publishing reads the bytes and refuses it.
        var upload = await UploadAsync(owner, "logo", "image/png", svg);
        using var publish = await PublishAsync(owner, upload.UploadId, "logo");

        publish.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await publish.ReadJsonAsync()).GetProperty("code").GetString().ShouldBe("asset.logo_invalid");
    }

    [Fact]
    public async Task Staff_cannot_change_how_the_business_looks()
    {
        var restaurant = await database.Services.SeedTenantAsync(Ct);
        using var staff = await _factory.SignedInClientAsync(database, restaurant, TenantRole.Staff);

        using var response = await staff.PutAsJsonAsync(BrandingPath, new BrandingBody("Başka Ad", null, null), Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _factory.DisposeAsync();

    private static async Task<PublicMenuResponse> GetMenuAsync(HttpClient client, SeededTenant restaurant) =>
        (await client.GetFromJsonAsync<PublicMenuResponse>($"/api/v1/menus/{restaurant.Tenant.Slug}", Ct))!;

    private sealed record BrandingBody(string Name, string? LogoPath, string? AccentColor);
}
