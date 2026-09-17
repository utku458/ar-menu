using System.ComponentModel;
using ArMenu.Domain.Tenants;

namespace ArMenu.Application.MultiTenancy;

/// <summary>
/// Immutable snapshot of the tenant a scope is bound to. It carries exactly what cross-cutting concerns need
/// (isolation, localization, pricing) without loading the <see cref="Tenant"/> aggregate, and is safe to cache.
/// </summary>
/// <remarks>
/// <see cref="ImmutableObjectAttribute"/> tells HybridCache it may hand the same instance to every caller
/// instead of deserializing a defensive copy on each cache hit.
/// </remarks>
[ImmutableObject(true)]
public sealed record TenantInfo(
    TenantId Id,
    string Slug,
    string Name,
    TenantStatus Status,
    string DefaultCulture,
    IReadOnlyList<string> SupportedCultures,
    string Currency,
    string TimeZone,
    string? LogoPath = null,
    string? AccentColor = null)
{
    public bool IsActive => Status == TenantStatus.Active;

    /// <summary>The calendar day it is for the business at <paramref name="instant"/>.</summary>
    public DateOnly DateAt(DateTimeOffset instant) => TenantTimeZone.Create(TimeZone).Value.DateAt(instant);

    public static TenantInfo From(Tenant tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        return new TenantInfo(
            tenant.Id,
            tenant.Slug.Value,
            tenant.Name,
            tenant.Status,
            tenant.DefaultCulture.Value,
            [.. tenant.SupportedCultures.Select(culture => culture.Value)],
            tenant.Currency.Code,
            tenant.TimeZone.Id,
            tenant.Branding.LogoPath?.Value,
            tenant.Branding.AccentColor?.Value);
    }
}
