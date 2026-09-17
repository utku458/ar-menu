using System.Text.Json;
using ArMenu.Domain.Localization;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ArMenu.Infrastructure.Persistence.Converters;

/// <summary>Serializes <see cref="LocalizedText"/> to a JSON object keyed by culture code.</summary>
internal sealed class LocalizedTextConverter()
    : ValueConverter<LocalizedText, string>(text => Serialize(text), json => Deserialize(json))
{
    private static string Serialize(LocalizedText text) => JsonSerializer.Serialize(text.Translations);

    private static LocalizedText Deserialize(string json) =>
        LocalizedText.Create(JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? []).Value;
}

/// <summary>
/// Value semantics for change tracking. <see cref="LocalizedText"/> is immutable, so the snapshot can be the instance itself.
/// </summary>
internal sealed class LocalizedTextComparer()
    : ValueComparer<LocalizedText>(
        (left, right) => object.Equals(left, right),
        text => text.GetHashCode(),
        text => text);
