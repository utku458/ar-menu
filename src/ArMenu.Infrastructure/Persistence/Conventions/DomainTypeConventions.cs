using ArMenu.Domain.Common;
using ArMenu.Domain.Localization;
using ArMenu.Domain.Media;
using ArMenu.Domain.Memberships;
using ArMenu.Domain.Pricing;
using ArMenu.Domain.Sessions;
using ArMenu.Domain.Tenants;
using ArMenu.Domain.Users;
using ArMenu.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;

namespace ArMenu.Infrastructure.Persistence.Conventions;

/// <summary>
/// Model-wide mapping of domain value types, configured once instead of in every entity configuration.
/// </summary>
internal static class DomainTypeConventions
{
    private static readonly Type[] StronglyTypedIdTypes = typeof(IStronglyTypedId<>).Assembly
        .GetTypes()
        .Where(type => type is { IsValueType: true, IsAbstract: false } && type.GetInterfaces().Any(IsStronglyTypedId))
        .ToArray();

    public static void AddDomainTypeConventions(this ModelConfigurationBuilder builder)
    {
        // Discovered by reflection so a new aggregate's identifier can never be forgotten here.
        foreach (var idType in StronglyTypedIdTypes)
        {
            builder.Properties(idType).HaveConversion(typeof(StronglyTypedIdConverter<>).MakeGenericType(idType));
        }

        builder.Properties<TenantSlug>()
            .HaveConversion<TenantSlugConverter>()
            .HaveMaxLength(TenantSlug.MaxLength);

        builder.Properties<CultureCode>()
            .HaveConversion<CultureCodeConverter>()
            .HaveMaxLength(CultureCode.MaxLength);

        builder.Properties<TenantTimeZone>()
            .HaveConversion<TenantTimeZoneConverter>()
            .HaveMaxLength(TenantTimeZone.MaxLength);

        builder.Properties<Currency>()
            .HaveConversion<CurrencyConverter>()
            .HaveMaxLength(Currency.Length)
            .AreFixedLength();

        builder.Properties<BrandColor>()
            .HaveConversion<BrandColorConverter>()
            .HaveMaxLength(BrandColor.Length);

        builder.Properties<AssetPath>()
            .HaveConversion<AssetPathConverter>()
            .HaveMaxLength(AssetPath.MaxLength);

        // One jsonb document per text ({"tr": "...", "en": "..."}): a single row read serves every language.
        builder.Properties<LocalizedText>()
            .HaveConversion<LocalizedTextConverter, LocalizedTextComparer>()
            .HaveColumnType("jsonb");

        builder.Properties<Email>()
            .HaveConversion<EmailConverter>()
            .HaveMaxLength(Email.MaxLength);

        builder.Properties<RefreshTokenHash>()
            .HaveConversion<RefreshTokenHashConverter>()
            .HaveMaxLength(RefreshTokenHash.Length)
            .AreFixedLength();

        builder.Properties<InvitationTokenHash>()
            .HaveConversion<InvitationTokenHashConverter>()
            .HaveMaxLength(InvitationTokenHash.Length)
            .AreFixedLength();

        builder.Properties<UserTokenHash>()
            .HaveConversion<UserTokenHashConverter>()
            .HaveMaxLength(UserTokenHash.Length)
            .AreFixedLength();

        // Enums are stored by name: readable in SQL and immune to member reordering.
        builder.Properties<TenantStatus>().HaveConversion<string>().HaveMaxLength(20);
        builder.Properties<TenantRole>().HaveConversion<string>().HaveMaxLength(20);
        builder.Properties<UserTokenPurpose>().HaveConversion<string>().HaveMaxLength(32);
        builder.Properties<InvitationStatus>().HaveConversion<string>().HaveMaxLength(20);
        builder.Properties<SessionRevocationReason>().HaveConversion<string>().HaveMaxLength(32);
    }

    private static bool IsStronglyTypedId(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IStronglyTypedId<>);
}
