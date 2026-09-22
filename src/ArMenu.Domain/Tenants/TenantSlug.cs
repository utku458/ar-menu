using System.Collections.Frozen;
using System.Text.RegularExpressions;
using ArMenu.Domain.Common;

namespace ArMenu.Domain.Tenants;

/// <summary>
/// Unique, URL-safe handle of a tenant used in public menu links and QR codes (<c>https://armenu.app/m/{slug}</c>).
/// </summary>
/// <remarks>
/// The syntax follows DNS label rules (lower-case letters, digits, single hyphens, at most 63 characters),
/// so a slug can later be promoted to a subdomain (<c>{slug}.armenu.app</c>) without renaming anyone.
/// </remarks>
public sealed partial record TenantSlug
{
    public const int MinLength = 3;
    public const int MaxLength = 63;

    // Names that would collide with our own routes or subdomains. Enforced when a tenant claims a slug.
    private static readonly FrozenSet<string> ReservedValues = FrozenSet.Create(
        StringComparer.Ordinal,
        "admin", "api", "app", "assets", "auth", "billing", "blog", "cdn", "dashboard", "demo", "docs", "help", "login",
        "logout", "mail", "menu", "panel", "register", "root", "settings", "signup", "static", "status", "support",
        "system", "www");

    /// <summary>
    /// The platform workspace's handle. It is reserved, so no business can ever claim it: the platform administrator's
    /// sessions live here, and nothing else does (see <see cref="Tenant.CreatePlatform"/>).
    /// </summary>
    public const string PlatformValue = "system";

    private TenantSlug(string value) => Value = value;

    public static TenantSlug Platform { get; } = new(PlatformValue);

    public string Value { get; }

    public bool IsReserved => ReservedValues.Contains(Value);

    public bool IsPlatform => Value == PlatformValue;

    public static Result<TenantSlug> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return TenantErrors.SlugRequired;
        }

        var normalized = value.Trim().ToLowerInvariant();

        return normalized.Length is >= MinLength and <= MaxLength && Pattern().IsMatch(normalized)
            ? new TenantSlug(normalized)
            : TenantErrors.SlugInvalid;
    }

    public override string ToString() => Value;

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();
}
