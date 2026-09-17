using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using ArMenu.Domain.Common;

namespace ArMenu.Domain.Localization;

/// <summary>
/// Text translated into one or more cultures, e.g. a dish name in Turkish, English and German.
/// Multi-language menus are a core requirement for tourist-heavy markets, so every customer-facing text is modeled
/// as <see cref="LocalizedText"/> from day one rather than retrofitted later.
/// </summary>
/// <remarks>Immutable value object: two instances are equal when they hold exactly the same translations.</remarks>
public sealed class LocalizedText : IEquatable<LocalizedText>
{
    private readonly ImmutableSortedDictionary<string, string> _translations;

    private LocalizedText(ImmutableSortedDictionary<string, string> translations) => _translations = translations;

    /// <summary>Translations keyed by normalized culture code (see <see cref="CultureCode"/>), ordered by key.</summary>
    public IReadOnlyDictionary<string, string> Translations => _translations;

    public static Result<LocalizedText> Create(IEnumerable<KeyValuePair<string, string>> translations)
    {
        ArgumentNullException.ThrowIfNull(translations);

        var builder = ImmutableSortedDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);

        foreach (var (culture, text) in translations)
        {
            var cultureCode = CultureCode.Create(culture);
            if (cultureCode.IsFailure)
            {
                return cultureCode.Error;
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                return LocalizationErrors.TranslationEmpty;
            }

            // "EN" and "en" normalize to the same key: reject ambiguous input instead of silently picking one.
            if (!builder.TryAdd(cultureCode.Value.Value, text.Trim()))
            {
                return LocalizationErrors.TranslationDuplicated;
            }
        }

        return builder.Count == 0
            ? LocalizationErrors.TextRequired
            : new LocalizedText(builder.ToImmutable());
    }

    public static Result<LocalizedText> Create(CultureCode culture, string text)
    {
        ArgumentNullException.ThrowIfNull(culture);
        return Create([new KeyValuePair<string, string>(culture.Value, text)]);
    }

    public bool TryGetTranslation(CultureCode culture, [NotNullWhen(true)] out string? text)
    {
        ArgumentNullException.ThrowIfNull(culture);
        return _translations.TryGetValue(culture.Value, out text);
    }

    /// <summary>
    /// Picks the best translation for a reader: the requested culture, then its neutral parent (<c>de-at</c> → <c>de</c>),
    /// then the tenant's fallback culture, then the first available translation. Never returns an empty string.
    /// </summary>
    public string Resolve(CultureCode requested, CultureCode fallback)
    {
        ArgumentNullException.ThrowIfNull(requested);
        ArgumentNullException.ThrowIfNull(fallback);

        if (TryGetTranslation(requested, out var text))
        {
            return text;
        }

        if (requested.Parent is { } parent && TryGetTranslation(parent, out text))
        {
            return text;
        }

        return TryGetTranslation(fallback, out text) ? text : _translations.First().Value;
    }

    /// <summary>Whether any translation is longer than <paramref name="maxLength"/> characters.</summary>
    public bool ExceedsLength(int maxLength) => _translations.Values.Any(text => text.Length > maxLength);

    public bool Equals(LocalizedText? other) =>
        other is not null &&
        (ReferenceEquals(this, other) ||
         (_translations.Count == other._translations.Count &&
          _translations.All(pair => other._translations.TryGetValue(pair.Key, out var text) &&
                                    string.Equals(pair.Value, text, StringComparison.Ordinal))));

    public override bool Equals(object? obj) => Equals(obj as LocalizedText);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var (culture, text) in _translations)
        {
            hash.Add(culture, StringComparer.Ordinal);
            hash.Add(text, StringComparer.Ordinal);
        }

        return hash.ToHashCode();
    }

    public override string ToString() => string.Join(" | ", _translations.Select(pair => $"{pair.Key}: {pair.Value}"));
}
