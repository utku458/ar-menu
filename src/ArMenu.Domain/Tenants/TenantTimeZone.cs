using ArMenu.Domain.Common;

namespace ArMenu.Domain.Tenants;

/// <summary>
/// The IANA time zone a business operates in (e.g. <c>Europe/Istanbul</c>). It decides where a business's day begins,
/// for daily statistics and anything else counted per day.
/// </summary>
public sealed record TenantTimeZone
{
    public const int MaxLength = 64;

    // Lookups may ignore case (on macOS they read a case-insensitive file system), so the database's spelling is kept.
    private static readonly Lazy<Dictionary<string, string>> SystemZoneIds = new(() =>
        TimeZoneInfo.GetSystemTimeZones()
            .Select(zone => zone.Id)
            .Append(TimeZoneInfo.Utc.Id)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToDictionary(id => id, id => id, StringComparer.OrdinalIgnoreCase));

    private readonly TimeZoneInfo _zone;

    private TenantTimeZone(string id, TimeZoneInfo zone)
    {
        Id = id;
        _zone = zone;
    }

    public static TenantTimeZone Utc { get; } = new(TimeZoneInfo.Utc.Id, TimeZoneInfo.Utc);

    /// <summary>The IANA identifier.</summary>
    public string Id { get; }

    /// <summary>
    /// Accepts IANA identifiers only. Windows names would work on one operating system and not another, and browsers
    /// report IANA names (<c>Intl.DateTimeFormat().resolvedOptions().timeZone</c>).
    /// </summary>
    public static Result<TenantTimeZone> Create(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return TenantErrors.TimeZoneRequired;
        }

        var trimmed = id.Trim();
        if (trimmed.Length > MaxLength ||
            !TimeZoneInfo.TryFindSystemTimeZoneById(trimmed, out var zone) ||
            !zone.HasIanaId)
        {
            return TenantErrors.TimeZoneInvalid;
        }

        var spelled = SystemZoneIds.Value.GetValueOrDefault(trimmed, trimmed);
        return spelled == Utc.Id ? Utc : new TenantTimeZone(spelled, zone);
    }

    /// <summary>The calendar day it is in this time zone at <paramref name="instant"/>.</summary>
    public DateOnly DateAt(DateTimeOffset instant) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, _zone).DateTime);

    public bool Equals(TenantTimeZone? other) => other is not null && string.Equals(Id, other.Id, StringComparison.Ordinal);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Id);

    public override string ToString() => Id;
}
