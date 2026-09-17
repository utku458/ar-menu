using System.Net;
using System.Net.Http.Json;
using ArMenu.Api.Endpoints.Menus;
using ArMenu.Application.Menus.Queries.GetManagedMenu;
using ArMenu.Application.Menus.Queries.GetPublicMenu;
using ArMenu.Domain.Localization;
using ArMenu.Domain.Memberships;
using ArMenu.IntegrationTests.TestSupport;

namespace ArMenu.IntegrationTests.Api.Menus;

public sealed class ManageMenuTests(PostgresDatabaseFixture database) : IAsyncLifetime
{
    private const string MenuPath = "/api/v1/manage/menu";

    private readonly ArMenuApiFactory _factory = new(database);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Anonymous_callers_are_rejected()
    {
        using var client = _factory.CreateBrowserClient();

        using var response = await client.GetAsync(MenuPath, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task Staff_can_mark_items_sold_out_but_cannot_change_the_menu()
    {
        var restaurant = await database.Services.SeedTenantAsync(Ct);
        using var staff = await SignedInClientAsync(restaurant, TenantRole.Staff);

        using var createCategory = await staff.PostAsJsonAsync($"{MenuPath}/categories", new { name = new { tr = "Tatlılar" } }, Ct);
        using var soldOut = await staff.PutAsJsonAsync($"{MenuPath}/items/{restaurant.ItemId}/availability", new { isAvailable = false }, Ct);

        createCategory.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        soldOut.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Editors_build_a_menu_that_guests_see_immediately()
    {
        var restaurant = await database.Services.SeedTenantAsync(Ct, tenant => tenant.AddSupportedCulture(CultureCode.Create("en").Value));
        using var owner = await SignedInClientAsync(restaurant, TenantRole.Owner);

        var categoryId = await CreateAsync(owner, $"{MenuPath}/categories", new { name = new { tr = "Tatlılar", en = "Desserts" } });
        var itemId = await CreateAsync(owner, $"{MenuPath}/items", new
        {
            categoryId,
            name = new { tr = "San Sebastian", en = "Basque Cheesecake" },
            description = new { tr = "Yanık cheesecake" },
            price = 220.50m,
        });

        var managed = await owner.GetFromJsonAsync<ManagedMenuResponse>(MenuPath, Ct);
        var managedItem = managed!.Categories.Single(category => category.Id == categoryId).Items.ShouldHaveSingleItem();
        managedItem.Name.ShouldBe(new Dictionary<string, string> { ["en"] = "Basque Cheesecake", ["tr"] = "San Sebastian" });

        var publicMenu = await owner.GetFromJsonAsync<PublicMenuResponse>($"/api/v1/menus/{restaurant.Tenant.Slug}?lang=en", Ct);
        var guestItem = publicMenu!.Categories.Single(category => category.Id == categoryId).Items.ShouldHaveSingleItem();
        guestItem.Name.ShouldBe("Basque Cheesecake");
        guestItem.Description.ShouldBe("Yanık cheesecake");
        guestItem.Price.ShouldBe(220.50m);
    }

    [Fact]
    public async Task Validation_reports_tenant_language_rules_and_field_rules_together()
    {
        var restaurant = await database.Services.SeedTenantAsync(Ct);
        using var owner = await SignedInClientAsync(restaurant, TenantRole.Owner);

        using var response = await owner.PostAsJsonAsync(
            $"{MenuPath}/items",
            new { categoryId = restaurant.CategoryId.Value, name = new { en = "Only English" }, price = -5.555m },
            Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var errorCodes = (await response.ReadJsonAsync()).GetProperty("errorCodes");
        errorCodes.GetProperty("name")[0].GetString().ShouldBe("menu.default_culture_translation_missing");
        errorCodes.GetProperty("price")[0].GetString().ShouldBe("money.amount_negative");
    }

    [Fact]
    public async Task Numbers_must_be_sent_as_json_numbers()
    {
        var restaurant = await database.Services.SeedTenantAsync(Ct);
        using var owner = await SignedInClientAsync(restaurant, TenantRole.Owner);

        // The published contract types prices as numbers only; "number or string" would leak into generated clients.
        using var response = await owner.PostAsJsonAsync(
            $"{MenuPath}/items",
            new { categoryId = restaurant.CategoryId.Value, name = new { tr = "Ayran" }, price = "45" },
            Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task Another_tenants_menu_is_neither_visible_nor_editable()
    {
        var burgerLab = await database.Services.SeedTenantAsync(Ct);
        var fishRestaurant = await database.Services.SeedTenantAsync(Ct);
        using var burgerLabOwner = await SignedInClientAsync(burgerLab, TenantRole.Owner);

        using var soldOut = await burgerLabOwner.PutAsJsonAsync($"{MenuPath}/items/{fishRestaurant.ItemId}/availability", new { isAvailable = false }, Ct);
        using var delete = await burgerLabOwner.DeleteAsync($"{MenuPath}/items/{fishRestaurant.ItemId}", Ct);
        var managed = await burgerLabOwner.GetFromJsonAsync<ManagedMenuResponse>(MenuPath, Ct);

        soldOut.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        delete.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        managed!.Categories.Select(category => category.Id).ShouldBe([burgerLab.CategoryId.Value]);
    }

    [Fact]
    public async Task Categories_must_be_emptied_before_they_can_be_deleted()
    {
        var restaurant = await database.Services.SeedTenantAsync(Ct);
        using var owner = await SignedInClientAsync(restaurant, TenantRole.Owner);

        using var whileNotEmpty = await owner.DeleteAsync($"{MenuPath}/categories/{restaurant.CategoryId}", Ct);
        using var deleteItem = await owner.DeleteAsync($"{MenuPath}/items/{restaurant.ItemId}", Ct);
        using var onceEmpty = await owner.DeleteAsync($"{MenuPath}/categories/{restaurant.CategoryId}", Ct);

        whileNotEmpty.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await whileNotEmpty.ReadJsonAsync()).GetProperty("code").GetString().ShouldBe("menu_category.not_empty");
        deleteItem.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        onceEmpty.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Reordering_requires_the_complete_list_and_then_applies_it()
    {
        var restaurant = await database.Services.SeedTenantAsync(Ct);
        using var owner = await SignedInClientAsync(restaurant, TenantRole.Owner);
        var second = await CreateAsync(owner, $"{MenuPath}/categories", new { name = new { tr = "İçecekler" } });
        var first = restaurant.CategoryId.Value;

        using var partial = await owner.PutAsJsonAsync($"{MenuPath}/categories/order", new { ids = new[] { second } }, Ct);
        using var complete = await owner.PutAsJsonAsync($"{MenuPath}/categories/order", new { ids = new[] { second, first } }, Ct);
        var managed = await owner.GetFromJsonAsync<ManagedMenuResponse>(MenuPath, Ct);

        partial.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await partial.ReadJsonAsync()).GetProperty("code").GetString().ShouldBe("menu.reorder_mismatch");
        complete.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        managed!.Categories.Select(category => category.Id).ShouldBe([second, first]);
    }

    [Fact]
    public async Task Ar_models_can_only_use_files_the_business_published()
    {
        var restaurant = await database.Services.SeedTenantAsync(Ct);
        using var owner = await SignedInClientAsync(restaurant, TenantRole.Owner);

        // A well-formed key that this business never uploaded (another tenant's file, or anything else in storage).
        using var response = await owner.PutAsJsonAsync(
            $"{MenuPath}/items/{restaurant.ItemId}/ar-model",
            new { glbPath = "tenants/0198a1f2000070008000000000000001/assets/burger.glb" },
            Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadJsonAsync()).GetProperty("errorCodes").GetProperty("glbPath")[0].GetString().ShouldBe("asset.not_owned");
    }

    [Fact]
    public async Task Ar_models_must_use_the_formats_model_viewer_expects()
    {
        var restaurant = await database.Services.SeedTenantAsync(Ct);
        using var owner = await SignedInClientAsync(restaurant, TenantRole.Owner);

        using var response = await owner.PutAsJsonAsync(
            $"{MenuPath}/items/{restaurant.ItemId}/ar-model",
            new { glbPath = "models/burger.obj" },
            Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.ReadJsonAsync()).GetProperty("code").GetString().ShouldBe("menu_item.ar_model_glb_format_invalid");
    }

    [Fact]
    public async Task Guests_see_allergens_and_diets_and_can_tell_undeclared_from_none()
    {
        var restaurant = await database.Services.SeedTenantAsync(Ct);
        using var owner = await SignedInClientAsync(restaurant, TenantRole.Owner);
        var categoryId = restaurant.CategoryId.Value;

        var baklava = await CreateAsync(owner, $"{MenuPath}/items", new
        {
            categoryId,
            name = new { tr = "Baklava" },
            price = 180m,
            allergens = new List<string> { "nuts", "gluten", "milk" },
            dietaryLabels = new List<string> { "vegetarian" },
        });
        var salad = await CreateAsync(owner, $"{MenuPath}/items", new
        {
            categoryId,
            name = new { tr = "Çoban salata" },
            price = 90m,
            allergens = Array.Empty<string>(),
            dietaryLabels = new List<string> { "vegan", "glutenFree" },
        });
        var undeclared = await CreateAsync(owner, $"{MenuPath}/items", new { categoryId, name = new { tr = "Günün çorbası" }, price = 70m });

        var menu = await owner.GetFromJsonAsync<PublicMenuResponse>($"/api/v1/menus/{restaurant.Tenant.Slug}", Ct);
        var items = menu!.Categories.SelectMany(category => category.Items).ToDictionary(item => item.Id);
        items[baklava].Allergens.ShouldBe(["gluten", "milk", "nuts"]);
        items[baklava].DietaryLabels.ShouldBe(["vegetarian"]);
        items[salad].Allergens.ShouldBeEmpty();
        items[salad].DietaryLabels.ShouldBe(["vegetarian", "vegan", "glutenFree"]);
        items[undeclared].Allergens.ShouldBeNull();
        items[undeclared].DietaryLabels.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_dish_cannot_be_labelled_against_its_own_allergens()
    {
        var restaurant = await database.Services.SeedTenantAsync(Ct);
        using var owner = await SignedInClientAsync(restaurant, TenantRole.Owner);

        using var contradiction = await owner.PostAsJsonAsync($"{MenuPath}/items", new
        {
            categoryId = restaurant.CategoryId.Value,
            name = new { tr = "Sütlaç" },
            price = 95m,
            allergens = new List<string> { "milk" },
            dietaryLabels = new List<string> { "vegan" },
        }, Ct);
        using var unknown = await owner.PostAsJsonAsync($"{MenuPath}/items", new
        {
            categoryId = restaurant.CategoryId.Value,
            name = new { tr = "Sütlaç" },
            price = 95m,
            allergens = new List<string> { "milk", "lactose" },
        }, Ct);

        contradiction.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await contradiction.ReadJsonAsync()).GetProperty("errorCodes").GetProperty("dietaryLabels")[0].GetString()
            .ShouldBe("menu_item.dietary_label_contradicts_allergen");
        (await unknown.ReadJsonAsync()).GetProperty("errorCodes").GetProperty("allergens[1]")[0].GetString()
            .ShouldBe("menu_item.allergen_unknown");
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _factory.DisposeAsync();

    private async Task<HttpClient> SignedInClientAsync(SeededTenant restaurant, TenantRole role)
    {
        var member = await database.Services.SeedMemberAsync(restaurant.Tenant, role, Ct);
        var client = _factory.CreateBrowserClient();
        return client.Authenticate(await client.SignInAsync(restaurant.Tenant.Slug, member.Email));
    }

    private static async Task<Guid> CreateAsync(HttpClient client, string path, object body)
    {
        using var response = await client.PostAsJsonAsync(path, body, Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync(Ct));
        return (await response.Content.ReadFromJsonAsync<ManageMenuEndpoints.CreatedResponse>(Ct))!.Id;
    }
}
