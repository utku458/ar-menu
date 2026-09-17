using ArMenu.Domain.Common;

namespace ArMenu.Domain.Tenants;

public static class TenantErrors
{
    public static readonly Error NameRequired = Error.Validation(
        "tenant.name_required", "A tenant name is required.");

    public static readonly Error NameTooLong = Error.Validation(
        "tenant.name_too_long", FormattableString.Invariant($"Tenant names cannot exceed {Tenant.NameMaxLength} characters."));

    public static readonly Error SlugRequired = Error.Validation(
        "tenant.slug_required", "A slug is required.");

    public static readonly Error SlugInvalid = Error.Validation(
        "tenant.slug_invalid",
        FormattableString.Invariant(
            $"Slugs must be {TenantSlug.MinLength}-{TenantSlug.MaxLength} characters of lower-case letters, digits and single hyphens."));

    public static readonly Error SlugReserved = Error.Validation(
        "tenant.slug_reserved", "This slug is reserved by the platform.");

    public static readonly Error SlugTaken = Error.Conflict(
        "tenant.slug_taken", "This slug is already used by another business.");

    public static readonly Error CultureNotSupported = Error.Validation(
        "tenant.culture_not_supported", "The culture must be added to the tenant's supported cultures first.");

    public static readonly Error DefaultCultureCannotBeRemoved = Error.Validation(
        "tenant.default_culture_cannot_be_removed", "The default culture cannot be removed; change the default culture first.");

    public static readonly Error SupportedCultureLimitReached = Error.Validation(
        "tenant.supported_culture_limit_reached",
        FormattableString.Invariant($"A tenant can support at most {Tenant.MaxSupportedCultures} cultures."));

    public static readonly Error TimeZoneRequired = Error.Validation(
        "tenant.time_zone_required", "A time zone is required.");

    public static readonly Error TimeZoneInvalid = Error.Validation(
        "tenant.time_zone_invalid", "The time zone must be an IANA time zone name such as Europe/Istanbul.");

    public static readonly Error BrandColorRequired = Error.Validation(
        "tenant.brand_color_required", "A colour is required.");

    public static readonly Error BrandColorInvalid = Error.Validation(
        "tenant.brand_color_invalid", "The colour must be written as six hexadecimal digits, such as #b8442f.");

    public static readonly Error Closed = Error.Conflict(
        "tenant.closed", "The business was closed and cannot be reopened.");
}
