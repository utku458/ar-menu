using System.Net;
using System.Net.Http.Json;
using ArMenu.Application.Menus.Queries.GetPublicMenu;
using ArMenu.Domain.Localization;
using ArMenu.Domain.Memberships;
using ArMenu.IntegrationTests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace ArMenu.IntegrationTests.Api.Menus;

public sealed class PublicMenuTests(PostgresDatabaseFixture database) : IAsyncLifetime
{
    private readonly ArMenuApiFactory _factory = new(database);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Guests_get_the_best_available_language_with_cache_friendly_headers()
    {
        var restaurant = await SeedMultilingualRestaurantAsync();
        using var guest = _factory.CreateBrowserClient();

        using var explicitChoice = await guest.GetAsync($"/api/v1/menus/{restaurant.Tenant.Slug}?lang=de", Ct);
        var german = await explicitChoice.Content.ReadFromJsonAsync<PublicMenuResponse>(Ct);

        german!.Culture.ShouldBe("de");
        german.Categories.ShouldHaveSingleItem().Items.ShouldHaveSingleItem().Name.ShouldBe("Hummus mit Tahini");
        explicitChoice.Content.Headers.ContentLanguage.ShouldBe(["de"]);
        explicitChoice.Headers.CacheControl!.Public.ShouldBeTrue();
        explicitChoice.Headers.Vary.ShouldContain("Accept-Language");

        (await GetMenuAsync(guest, restaurant, acceptLanguage: "de-AT,de;q=0.9")).Culture.ShouldBe("de");
        (await GetMenuAsync(guest, restaurant, acceptLanguage: "fr-FR,fr;q=0.9")).Culture.ShouldBe("tr");
    }

    [Fact]
    public async Task Hidden_entries_and_categories_without_visible_items_stay_private()
    {
        var restaurant = await database.Services.SeedTenantAsync(Ct);
        await using (var scope = database.Services.BeginScope(restaurant.Tenant))
        {
            var hiddenCategory = TestData.NewCategory(restaurant.Tenant.Id);
            hiddenCategory.Hide();
            var emptyCategory = TestData.NewCategory(restaurant.Tenant.Id);
            var hiddenItem = TestData.NewItem(restaurant.Tenant.Id, restaurant.CategoryId);
            hiddenItem.Hide();

            scope.Db.MenuCategories.AddRange(hiddenCategory, emptyCategory);
            scope.Db.MenuItems.AddRange(hiddenItem, TestData.NewItem(restaurant.Tenant.Id, hiddenCategory.Id));
            await scope.Db.SaveChangesAsync(Ct);
        }

        using var guest = _factory.CreateBrowserClient();
        var menu = await GetMenuAsync(guest, restaurant);

        menu.Categories.Select(category => category.Id).ShouldBe([restaurant.CategoryId.Value]);
        menu.Categories[0].Items.Select(item => item.Id).ShouldBe([restaurant.ItemId.Value]);
    }

    [Fact]
    public async Task Edits_reach_guests_immediately_despite_the_menu_cache()
    {
        var restaurant = await database.Services.SeedTenantAsync(Ct);
        var owner = await database.Services.SeedMemberAsync(restaurant.Tenant, TenantRole.Owner, Ct);
        using var guest = _factory.CreateBrowserClient();
        using var staffClient = _factory.CreateBrowserClient();
        staffClient.Authenticate(await staffClient.SignInAsync(restaurant.Tenant.Slug, owner.Email));

        (await GetMenuAsync(guest, restaurant)).Categories[0].Items[0].IsAvailable.ShouldBeTrue();

        using (var soldOut = await staffClient.PutAsJsonAsync($"/api/v1/manage/menu/items/{restaurant.ItemId}/availability", new { isAvailable = false }, Ct))
        {
            soldOut.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }

        (await GetMenuAsync(guest, restaurant)).Categories[0].Items[0].IsAvailable.ShouldBeFalse();
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _factory.DisposeAsync();

    private static async Task<PublicMenuResponse> GetMenuAsync(HttpClient client, SeededTenant restaurant, string? acceptLanguage = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/menus/{restaurant.Tenant.Slug}");
        if (acceptLanguage is not null)
        {
            request.Headers.Add("Accept-Language", acceptLanguage);
        }

        using var response = await client.SendAsync(request, Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<PublicMenuResponse>(Ct))!;
    }

    private async Task<SeededTenant> SeedMultilingualRestaurantAsync()
    {
        var restaurant = await database.Services.SeedTenantAsync(Ct, tenant => tenant.AddSupportedCulture(CultureCode.Create("de").Value));

        await using var scope = database.Services.BeginScope(restaurant.Tenant);
        var item = await scope.Db.MenuItems.SingleAsync(Ct);
        item.Rename(LocalizedText.Create([KeyValuePair.Create("tr", "Tahinli Humus"), KeyValuePair.Create("de", "Hummus mit Tahini")]).Value)
            .IsSuccess.ShouldBeTrue();
        await scope.Db.SaveChangesAsync(Ct);

        return restaurant;
    }
}
