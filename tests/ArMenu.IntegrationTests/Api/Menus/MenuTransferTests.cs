using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ArMenu.Domain.Memberships;
using ArMenu.IntegrationTests.TestSupport;
using static ArMenu.IntegrationTests.Api.Assets.AssetFlows;

namespace ArMenu.IntegrationTests.Api.Menus;

public sealed class MenuTransferTests(PostgresDatabaseFixture database) : IAsyncLifetime
{
    private const string ExportPath = "/api/v1/manage/menu/export";
    private const string ImportPath = "/api/v1/manage/menu/import";

    private readonly ArMenuApiFactory _factory = new(database);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task The_export_is_a_spreadsheet_that_imports_back_without_changes()
    {
        var restaurant = await database.Services.SeedTenantAsync(Ct);
        using var manager = await _factory.SignedInClientAsync(database, restaurant, TenantRole.Manager);

        using var export = await manager.GetAsync(ExportPath, Ct);
        export.StatusCode.ShouldBe(HttpStatusCode.OK);
        export.Content.Headers.ContentType!.MediaType.ShouldBe("text/csv");
        export.Content.Headers.ContentDisposition!.FileNameStar.ShouldStartWith($"{restaurant.Tenant.Slug}-menu-");
        var bytes = await export.Content.ReadAsByteArrayAsync(Ct);
        bytes.Take(3).ShouldBe(new byte[] { 0xEF, 0xBB, 0xBF }, "a byte order mark, for spreadsheets");

        var csv = Encoding.UTF8.GetString(bytes);
        csv.ShouldBe($"﻿id,category,name:tr,description:tr,price,visible,available,allergens,dietary\r\n{restaurant.ItemId.Value},Mezeler,Humus,,180.00,yes,yes,,\r\n");

        var result = await ImportAsync(manager, csv, dryRun: false);
        (result.GetProperty("applied").GetBoolean(), result.GetProperty("unchanged").GetInt32(), result.GetProperty("updated").GetInt32())
            .ShouldBe((true, 1, 0));
    }

    [Fact]
    public async Task A_preview_shows_what_would_change_and_applying_changes_exactly_that()
    {
        var restaurant = await database.Services.SeedTenantAsync(Ct, tenant => tenant.AddSupportedCulture(ArMenu.Domain.Localization.CultureCode.Create("en").Value));
        using var owner = await _factory.SignedInClientAsync(database, restaurant, TenantRole.Owner);
        var item = restaurant.ItemId.Value;

        // Saved by a Turkish spreadsheet: semicolons, decimal commas, evet/hayır, columns in another order.
        var csv = $"""
            id;name:tr;name:en;price;available;category
            {item};Humus;Hummus;195,50;hayır;
            ;Sigara böreği;;120;;Mezeler
            """;

        var preview = await ImportAsync(owner, csv, dryRun: true);
        preview.GetProperty("applied").GetBoolean().ShouldBeFalse();
        preview.GetProperty("errors").GetArrayLength().ShouldBe(0, preview.ToString());
        var changes = preview.GetProperty("changes").EnumerateArray().ToList();
        changes.Select(change => (change.GetProperty("line").GetInt32(), change.GetProperty("kind").GetString(), string.Join('+', change.GetProperty("fields").EnumerateArray().Select(field => field.GetString()))))
            .ShouldBe([(2, "updated", "name+price+available"), (3, "created", "")]);
        (await MenuAsync(owner)).ShouldBe([("Humus", 180m, true)], "a preview changes nothing");

        var applied = await ImportAsync(owner, csv, dryRun: false);
        (applied.GetProperty("applied").GetBoolean(), applied.GetProperty("created").GetInt32(), applied.GetProperty("updated").GetInt32()).ShouldBe((true, 1, 1));
        (await MenuAsync(owner)).ShouldBe([("Humus", 195.50m, false), ("Sigara böreği", 120m, true)]);

        var history = await owner.GetFromJsonAsync<JsonElement>("/api/v1/manage/history", Ct);
        history.GetProperty("entries").EnumerateArray().Take(2).Select(entry => entry.GetProperty("action").GetString())
            .ShouldBe(["updated", "created"], ignoreOrder: true);
    }

    [Fact]
    public async Task Allergens_are_filled_in_from_a_spreadsheet_and_round_trip_with_none_kept_apart_from_unknown()
    {
        var restaurant = await database.Services.SeedTenantAsync(Ct);
        using var owner = await _factory.SignedInClientAsync(database, restaurant, TenantRole.Owner);
        var item = restaurant.ItemId.Value;

        var csv = $"""
            id;category;name:tr;price;allergens;dietary
            {item};;Humus;;sesame, celery;vegan glutenFree
            ;Mezeler;Cacık;90;milk;vegan
            ;Mezeler;Çoban salata;85;none;vegan
            ;Mezeler;Günün mezesi;70;;
            """;

        var refused = await ImportAsync(owner, csv, dryRun: true);
        refused.GetProperty("errors").EnumerateArray()
            .Select(error => (error.GetProperty("line").GetInt32(), error.GetProperty("code").GetString()))
            .ShouldBe([(3, "menu_item.dietary_label_contradicts_allergen")]);

        var applied = await ImportAsync(owner, csv.Replace(";milk;vegan", ";milk;vegetarian", StringComparison.Ordinal), dryRun: false);
        applied.GetProperty("errors").GetArrayLength().ShouldBe(0, applied.ToString());

        using var export = await owner.GetAsync(ExportPath, Ct);
        var lines = (await export.Content.ReadAsStringAsync(Ct)).Split("\r\n");
        lines.ShouldContain(line => line.EndsWith(",Humus,,180.00,yes,yes,\"celery, sesame\",\"vegetarian, vegan, glutenFree\"", StringComparison.Ordinal));
        lines.ShouldContain(line => line.EndsWith(",Çoban salata,,85.00,yes,yes,none,\"vegetarian, vegan\"", StringComparison.Ordinal));
        lines.ShouldContain(line => line.EndsWith(",Günün mezesi,,70.00,yes,yes,,", StringComparison.Ordinal));
    }

    [Fact]
    public async Task One_problem_anywhere_changes_nothing_and_every_problem_is_reported_where_it_is()
    {
        var restaurant = await database.Services.SeedTenantAsync(Ct);
        using var owner = await _factory.SignedInClientAsync(database, restaurant, TenantRole.Owner);
        var item = restaurant.ItemId.Value;

        var csv = $"""
            id,category,name:tr,price,visible
            {item},,Humus,200,yes
            ,Tatlılar,Künefe,150,
            {Guid.NewGuid()},,Baklava,1.250,belki
            {item},,Humus,210,
            """;

        var result = await ImportAsync(owner, csv, dryRun: false);

        result.GetProperty("applied").GetBoolean().ShouldBeFalse();
        result.GetProperty("errors").EnumerateArray()
            .Select(error => (error.GetProperty("line").GetInt32(), error.GetProperty("column").GetString(), error.GetProperty("code").GetString()))
            .ShouldBe(
            [
                (3, "category", "menu_import.category_not_found"),
                (4, "id", "menu_import.item_not_found"),
                (4, "price", "menu_import.price_invalid"),
                (4, "visible", "menu_import.flag_invalid"),
                (5, "id", "menu_import.duplicate_item"),
            ]);
        (await MenuAsync(owner)).ShouldBe([("Humus", 180m, true)]);
    }

    [Fact]
    public async Task Unknown_languages_and_missing_columns_are_refused_before_any_row()
    {
        var restaurant = await database.Services.SeedTenantAsync(Ct);
        using var owner = await _factory.SignedInClientAsync(database, restaurant, TenantRole.Owner);

        var result = await ImportAsync(owner, "id,name:de,price\n,Hummus,10\n", dryRun: true);

        result.GetProperty("errors").EnumerateArray().Select(error => (error.GetProperty("column").GetString(), error.GetProperty("code").GetString()))
            .ShouldBe([("name:de", "menu_import.unknown_column"), ("name:tr", "menu_import.missing_column")]);
    }

    [Fact]
    public async Task Only_editors_transfer_menus_and_only_as_csv_of_a_reasonable_size()
    {
        var restaurant = await database.Services.SeedTenantAsync(Ct);
        using var staff = await _factory.SignedInClientAsync(database, restaurant, TenantRole.Staff);
        using var owner = await _factory.SignedInClientAsync(database, restaurant, TenantRole.Owner);

        (await staff.GetAsync(ExportPath, Ct)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);

        using var json = await owner.PostAsJsonAsync(ImportPath, new { csv = "id" }, Ct);
        json.StatusCode.ShouldBe(HttpStatusCode.UnsupportedMediaType);

        using var huge = await owner.PostAsync(ImportPath, new StringContent(new string('x', 1_048_577), Encoding.UTF8, "text/csv"), Ct);
        huge.StatusCode.ShouldBe(HttpStatusCode.RequestEntityTooLarge);
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _factory.DisposeAsync();

    private static async Task<JsonElement> ImportAsync(HttpClient client, string csv, bool dryRun)
    {
        using var response = await client.PostAsync($"{ImportPath}?dryRun={dryRun.ToString().ToLowerInvariant()}", new StringContent(csv, Encoding.UTF8, "text/csv"), Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(Ct));
        return await response.ReadJsonAsync();
    }

    private static async Task<List<(string Name, decimal Price, bool Available)>> MenuAsync(HttpClient client) =>
        [.. (await client.GetFromJsonAsync<JsonElement>("/api/v1/manage/menu", Ct)).GetProperty("categories").EnumerateArray()
            .SelectMany(category => category.GetProperty("items").EnumerateArray())
            .Select(item => (item.GetProperty("name").GetProperty("tr").GetString()!, item.GetProperty("price").GetDecimal(), item.GetProperty("isAvailable").GetBoolean()))];
}
