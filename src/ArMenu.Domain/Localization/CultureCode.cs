using System.Text.RegularExpressions;
using ArMenu.Domain.Common;

namespace ArMenu.Domain.Localization;

/// <summary>
/// A language tag in the <c>language[-script][-region]</c> subset of BCP 47, normalized to lower case
/// (e.g. <c>tr</c>, <c>en</c>, <c>pt-br</c>, <c>zh-hant</c>).
/// </summary>
public sealed partial record CultureCode
{
    public const int MaxLength = 12;

    private CultureCode(string value) => Value = value;

    public string Value { get; }

    /// <summary>
    /// The neutral language of a regional culture (<c>pt-br</c> → <c>pt</c>), or <see langword="null"/> for neutral cultures.
    /// </summary>
    public CultureCode? Parent
    {
        get
        {
            var separatorIndex = Value.IndexOf('-', StringComparison.Ordinal);
            return separatorIndex < 0 ? null : new CultureCode(Value[..separatorIndex]);
        }
    }

    public static Result<CultureCode> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return LocalizationErrors.CultureCodeRequired;
        }

        var normalized = value.Trim().ToLowerInvariant();

        return normalized.Length <= MaxLength && Pattern().IsMatch(normalized)
            ? new CultureCode(normalized)
            : LocalizationErrors.CultureCodeInvalid;
    }

    public override string ToString() => Value;

    [GeneratedRegex("^[a-z]{2,3}(-[a-z]{4})?(-([a-z]{2}|[0-9]{3}))?$", RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();
}
