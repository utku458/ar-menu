using System.Text.RegularExpressions;
using ArMenu.Domain.Common;

namespace ArMenu.Domain.Media;

/// <summary>
/// Location of a file in asset storage, relative to the storage/CDN root (e.g. <c>tenants/0198.../models/burger.glb</c>).
/// </summary>
/// <remarks>
/// Keys are stored instead of absolute URLs so the storage provider or CDN host can change without a data migration,
/// and so the delivery layer stays free to emit versioned, immutable (long-cacheable) URLs.
/// </remarks>
public sealed partial record AssetPath
{
    public const int MaxLength = 512;

    private AssetPath(string value) => Value = value;

    public string Value { get; }

    public static Result<AssetPath> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return MediaErrors.AssetPathRequired;
        }

        var trimmed = value.Trim();

        // Every segment must start with a letter or digit: rules out absolute paths, URLs, "..", hidden files and backslashes.
        return trimmed.Length <= MaxLength && Pattern().IsMatch(trimmed)
            ? new AssetPath(trimmed)
            : MediaErrors.AssetPathInvalid;
    }

    public bool HasExtension(string extension)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(extension);
        return Value.EndsWith(extension, StringComparison.OrdinalIgnoreCase);
    }

    public override string ToString() => Value;

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9._-]*(/[A-Za-z0-9][A-Za-z0-9._-]*)*$", RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();
}
