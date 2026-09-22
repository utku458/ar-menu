using ArMenu.Domain.Localization;
using ArMenu.Domain.Media;
using ArMenu.Domain.Memberships;
using ArMenu.Domain.Menus;
using ArMenu.Domain.Pricing;
using ArMenu.Domain.Sessions;
using ArMenu.Domain.Tenants;
using ArMenu.Domain.Users;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ArMenu.Infrastructure.Persistence.Converters;

// Materialization goes through the same validating factories as the application code. A row that violates an
// invariant fails loudly at read time instead of leaking an invalid value object into the domain.

internal sealed class TenantSlugConverter()
    : ValueConverter<TenantSlug, string>(slug => slug.Value, value => TenantSlug.Create(value).Value);

internal sealed class CultureCodeConverter()
    : ValueConverter<CultureCode, string>(culture => culture.Value, value => CultureCode.Create(value).Value);

internal sealed class TenantTimeZoneConverter()
    : ValueConverter<TenantTimeZone, string>(timeZone => timeZone.Id, value => TenantTimeZone.Create(value).Value);

internal sealed class CurrencyConverter()
    : ValueConverter<Currency, string>(currency => currency.Code, value => Currency.Create(value).Value);

internal sealed class AssetPathConverter()
    : ValueConverter<AssetPath, string>(path => path.Value, value => AssetPath.Create(value).Value);

internal sealed class AllergenCodeConverter()
    : ValueConverter<Allergen, string>(allergen => DietaryInformation.CodeOf(allergen), code => DietaryInformation.ParseAllergen(code).Value);

internal sealed class DietaryLabelCodeConverter()
    : ValueConverter<DietaryLabel, string>(label => DietaryInformation.CodeOf(label), code => DietaryInformation.ParseLabel(code).Value);

internal sealed class BrandColorConverter()
    : ValueConverter<BrandColor, string>(color => color.Value, value => BrandColor.Create(value).Value);

internal sealed class EmailConverter()
    : ValueConverter<Email, string>(email => email.Value, value => Email.Create(value).Value);

internal sealed class UserNameConverter()
    : ValueConverter<UserName, string>(userName => userName.Value, value => UserName.Create(value).Value);

internal sealed class RefreshTokenHashConverter()
    : ValueConverter<RefreshTokenHash, string>(hash => hash.Value, value => RefreshTokenHash.Create(value).Value);

internal sealed class InvitationTokenHashConverter()
    : ValueConverter<InvitationTokenHash, string>(hash => hash.Value, value => InvitationTokenHash.Create(value).Value);

internal sealed class UserTokenHashConverter()
    : ValueConverter<UserTokenHash, string>(hash => hash.Value, value => UserTokenHash.Create(value).Value);
