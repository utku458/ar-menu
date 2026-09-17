using System.Buffers.Text;
using System.Security.Cryptography;
using ArMenu.Domain.Localization;
using ArMenu.Domain.Media;
using ArMenu.Domain.Memberships;
using ArMenu.Domain.Menus;
using ArMenu.Domain.Pricing;
using ArMenu.Domain.Sessions;
using ArMenu.Domain.Tenants;
using ArMenu.Domain.Users;

namespace ArMenu.Domain.UnitTests.TestSupport;

/// <summary>Terse factories for valid domain values, so each test spells out only what it is about.</summary>
internal static class Make
{
    public static CultureCode Culture(string value) => CultureCode.Create(value).ShouldSucceed();

    public static LocalizedText Text(params (string Culture, string Text)[] translations) =>
        LocalizedText.Create(translations.Select(pair => KeyValuePair.Create(pair.Culture, pair.Text))).ShouldSucceed();

    public static Currency CurrencyCode(string code = "TRY") => Currency.Create(code).ShouldSucceed();

    public static Money Price(decimal amount, string currency = "TRY") =>
        Money.Create(amount, CurrencyCode(currency)).ShouldSucceed();

    public static TenantSlug Slug(string value = "kadikoy-burger-lab") => TenantSlug.Create(value).ShouldSucceed();

    public static AssetPath Asset(string path) => AssetPath.Create(path).ShouldSucceed();

    public static Tenant NewTenant(string name = "Kadıköy Burger Lab", string slug = "kadikoy-burger-lab", string defaultCulture = "tr") =>
        Tenant.Create(name, Slug(slug), Culture(defaultCulture), CurrencyCode()).ShouldSucceed();

    public static RefreshTokenHash Hash() =>
        RefreshTokenHash.Create(Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32))).ShouldSucceed();

    public static InvitationTokenHash InvitationHash() =>
        InvitationTokenHash.Create(Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32))).ShouldSucceed();

    public static UserTokenHash UserTokenHash() =>
        Domain.Users.UserTokenHash.Create(Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32))).ShouldSucceed();

    public static Email EmailAddress(string value = "owner@armenu.test") => Email.Create(value).ShouldSucceed();

    public static MenuItem NewMenuItem(TenantId? tenantId = null) =>
        MenuItem.Create(
            tenantId ?? TenantId.New(),
            MenuCategoryId.New(),
            Text(("tr", "Adana Kebap")),
            Price(450m),
            displayOrder: 0).ShouldSucceed();
}
