using ArMenu.Domain.Localization;
using ArMenu.Domain.Media;
using ArMenu.Domain.Memberships;
using ArMenu.Domain.Menus;
using ArMenu.Domain.Pricing;
using ArMenu.Domain.Tenants;

namespace ArMenu.Infrastructure.Persistence.Seeding;

/// <summary>
/// Demo content for development databases. Asset paths point to the generated sample models in <c>assets/demo</c>
/// (<c>pnpm --dir web generate:demo-assets</c>), served by the local asset CDN.
/// </summary>
internal static class DemoMenus
{
    /// <summary>Password of every demo account. Development databases only.</summary>
    public const string DemoPassword = "ArMenu-Demo-2026";

    public static IReadOnlyList<Func<DemoMenu>> All { get; } = [KadikoyBurgerLab, BogaziciBalikcisi];

    private static DemoMenu KadikoyBurgerLab()
    {
        var menu = DemoMenu.ForTenant("Kadıköy Burger Lab", "kadikoy-burger-lab", "tr", "en");
        menu.AddMember("owner@kadikoy-burger-lab.test", "Deniz Yılmaz", TenantRole.Owner);
        menu.AddMember("staff@kadikoy-burger-lab.test", "Ece Kaya", TenantRole.Staff);

        var burgers = menu.AddCategory([("tr", "Burgerler"), ("en", "Burgers")]);
        menu.AddItem(burgers, 385m, [("tr", "Klasik Smash Burger"), ("en", "Classic Smash Burger")],
            [("tr", "Çift smash köfte, cheddar, karamelize soğan"), ("en", "Double smashed patty, cheddar, caramelized onion")],
            dish: "smash-burger");
        menu.AddItem(burgers, 445m, [("tr", "Trüflü Mantar Burger"), ("en", "Truffle Mushroom Burger")]);

        var drinks = menu.AddCategory([("tr", "İçecekler"), ("en", "Drinks")]);
        menu.AddItem(drinks, 120m, [("tr", "Ev Yapımı Limonata"), ("en", "Homemade Lemonade")]);

        return menu;
    }

    private static DemoMenu BogaziciBalikcisi()
    {
        var menu = DemoMenu.ForTenant("Boğaziçi Balıkçısı", "bogazici-balikcisi", "tr", "en", "de");
        menu.AddMember("owner@bogazici-balikcisi.test", "Murat Demir", TenantRole.Owner);

        var starters = menu.AddCategory([("tr", "Mezeler"), ("en", "Starters"), ("de", "Vorspeisen")]);
        menu.AddItem(starters, 180m, [("tr", "Humus"), ("en", "Hummus"), ("de", "Hummus")]);
        menu.AddItem(starters, 210m, [("tr", "Deniz Börülcesi"), ("en", "Samphire"), ("de", "Queller")]);

        var mains = menu.AddCategory([("tr", "Ana Yemekler"), ("en", "Main Courses"), ("de", "Hauptgerichte")]);
        menu.AddItem(mains, 750m, [("tr", "Izgara Levrek"), ("en", "Grilled Sea Bass"), ("de", "Gegrillter Wolfsbarsch")],
            dish: "sea-bass");

        return menu;
    }
}

internal sealed class DemoMenu
{
    private DemoMenu(Tenant tenant) => Tenant = tenant;

    public Tenant Tenant { get; }

    public List<MenuCategory> Categories { get; } = [];

    public List<MenuItem> Items { get; } = [];

    public List<DemoMember> Members { get; } = [];

    public static DemoMenu ForTenant(string name, string slug, string defaultCulture, params string[] otherCultures)
    {
        var tenant = Tenant.Create(
            name,
            TenantSlug.Create(slug).Value,
            CultureCode.Create(defaultCulture).Value,
            Currency.Create("TRY").Value).Value;

        foreach (var culture in otherCultures)
        {
            var added = tenant.AddSupportedCulture(CultureCode.Create(culture).Value);
            if (added.IsFailure)
            {
                throw new InvalidOperationException(added.Error.Description);
            }
        }

        return new DemoMenu(tenant);
    }

    public void AddMember(string email, string fullName, TenantRole role) => Members.Add(new DemoMember(email, fullName, role));

    public MenuCategory AddCategory((string Culture, string Text)[] name)
    {
        var category = MenuCategory.Create(Tenant.Id, Text(name), displayOrder: Categories.Count).Value;
        Categories.Add(category);
        return category;
    }

    public void AddItem(
        MenuCategory category,
        decimal price,
        (string Culture, string Text)[] name,
        (string Culture, string Text)[]? description = null,
        string? dish = null)
    {
        var displayOrder = Items.Count(item => item.CategoryId == category.Id);

        var item = MenuItem.Create(
            Tenant.Id,
            category.Id,
            Text(name),
            Money.Create(price, Tenant.Currency).Value,
            displayOrder,
            description is null ? null : Text(description)).Value;

        // Files produced by the asset pipeline from the generated demo dishes (web/tools/demo-assets).
        if (dish is not null)
        {
            item.AttachArModel(ArModel.Create(
                glbPath: Asset($"demo/models/{dish}.glb"),
                sceneViewerGlbPath: Asset($"demo/models/{dish}.scene-viewer.glb"),
                usdzPath: Asset($"demo/models/{dish}.usdz"),
                posterPath: Asset($"demo/posters/{dish}.webp")).Value);
        }

        Items.Add(item);
    }

    private static LocalizedText Text((string Culture, string Text)[] translations) =>
        LocalizedText.Create(translations.Select(pair => KeyValuePair.Create(pair.Culture, pair.Text))).Value;

    private static AssetPath Asset(string path) => AssetPath.Create(path).Value;

}

internal sealed record DemoMember(string Email, string FullName, TenantRole Role);
