using System.Collections.Frozen;
using ArMenu.Domain.Common;

namespace ArMenu.Domain.Menus;

/// <summary>
/// What a guest with an allergy or a diet needs to know about a dish: the allergens it contains and the diets it suits.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Allergens"/> being <see langword="null"/> means the business has not said. That is not the same as an empty
/// list, which says the dish contains none of the fourteen. A menu that was never filled in must never tell a guest with
/// a peanut allergy that a dish is safe.
/// </para>
/// <para>
/// Codes (<c>gluten</c>, <c>glutenFree</c>…) are the spelling used by the API, the spreadsheet and the database, so the
/// enum's names can change without breaking any of them.
/// </para>
/// </remarks>
public sealed record DietaryInformation
{
    private static readonly FrozenDictionary<Allergen, string> AllergenCodes = new Dictionary<Allergen, string>
    {
        [Allergen.Gluten] = "gluten",
        [Allergen.Crustaceans] = "crustaceans",
        [Allergen.Eggs] = "eggs",
        [Allergen.Fish] = "fish",
        [Allergen.Peanuts] = "peanuts",
        [Allergen.Soybeans] = "soybeans",
        [Allergen.Milk] = "milk",
        [Allergen.Nuts] = "nuts",
        [Allergen.Celery] = "celery",
        [Allergen.Mustard] = "mustard",
        [Allergen.Sesame] = "sesame",
        [Allergen.Sulphites] = "sulphites",
        [Allergen.Lupin] = "lupin",
        [Allergen.Molluscs] = "molluscs",
    }.ToFrozenDictionary();

    private static readonly FrozenDictionary<DietaryLabel, string> LabelCodes = new Dictionary<DietaryLabel, string>
    {
        [DietaryLabel.Vegetarian] = "vegetarian",
        [DietaryLabel.Vegan] = "vegan",
        [DietaryLabel.GlutenFree] = "glutenFree",
    }.ToFrozenDictionary();

    private static readonly FrozenDictionary<string, Allergen> AllergensByCode =
        AllergenCodes.ToFrozenDictionary(pair => pair.Value, pair => pair.Key, StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenDictionary<string, DietaryLabel> LabelsByCode =
        LabelCodes.ToFrozenDictionary(pair => pair.Value, pair => pair.Key, StringComparer.OrdinalIgnoreCase);

    // What each label rules out. Honey, gelatine and the like are not allergens, so they cannot be checked here.
    private static readonly FrozenDictionary<DietaryLabel, Allergen[]> RuledOut = new Dictionary<DietaryLabel, Allergen[]>
    {
        [DietaryLabel.Vegetarian] = [Allergen.Fish, Allergen.Crustaceans, Allergen.Molluscs],
        [DietaryLabel.Vegan] = [Allergen.Fish, Allergen.Crustaceans, Allergen.Molluscs, Allergen.Milk, Allergen.Eggs],
        [DietaryLabel.GlutenFree] = [Allergen.Gluten],
    }.ToFrozenDictionary();

    private DietaryInformation(IReadOnlyList<Allergen>? allergens, IReadOnlyList<DietaryLabel> labels)
    {
        Allergens = allergens;
        Labels = labels;
    }

    /// <summary>Nothing said about the dish yet.</summary>
    public static DietaryInformation Unknown { get; } = new(null, []);

    /// <summary>The allergens the dish contains in a fixed order; <see langword="null"/> when the business has not said.</summary>
    public IReadOnlyList<Allergen>? Allergens { get; }

    /// <summary>The diets the dish suits, in a fixed order. A vegan dish is always vegetarian as well.</summary>
    public IReadOnlyList<DietaryLabel> Labels { get; }

    public static IReadOnlyCollection<string> AllAllergenCodes => AllergenCodes.Values;

    public static IReadOnlyCollection<string> AllLabelCodes => LabelCodes.Values;

    public static Result<DietaryInformation> Create(IEnumerable<Allergen>? allergens, IEnumerable<DietaryLabel> labels)
    {
        ArgumentNullException.ThrowIfNull(labels);

        var declared = allergens?.Distinct().Order().ToList();
        var suits = labels.Distinct().ToHashSet();

        // Someone who marks a dish vegan has said it is vegetarian too; a guest filtering for vegetarian should find it.
        if (suits.Contains(DietaryLabel.Vegan))
        {
            suits.Add(DietaryLabel.Vegetarian);
        }

        if (declared is not null)
        {
            foreach (var label in suits.Order())
            {
                if (RuledOut[label].FirstOrDefault(declared.Contains) is var allergen && allergen != default)
                {
                    return MenuItemErrors.LabelContradictsAllergen(CodeOf(label), CodeOf(allergen));
                }
            }
        }

        return new DietaryInformation(declared, [.. suits.Order()]);
    }

    /// <summary>Reads the codes the API and the spreadsheet use.</summary>
    public static Result<DietaryInformation> FromCodes(IEnumerable<string>? allergenCodes, IEnumerable<string>? labelCodes)
    {
        List<Allergen>? allergens = null;
        if (allergenCodes is not null)
        {
            allergens = [];
            foreach (var code in allergenCodes)
            {
                var allergen = ParseAllergen(code);
                if (allergen.IsFailure)
                {
                    return allergen.Error;
                }

                allergens.Add(allergen.Value);
            }
        }

        List<DietaryLabel> labels = [];
        foreach (var code in labelCodes ?? [])
        {
            var label = ParseLabel(code);
            if (label.IsFailure)
            {
                return label.Error;
            }

            labels.Add(label.Value);
        }

        return Create(allergens, labels);
    }

    public static Result<Allergen> ParseAllergen(string? code) =>
        code is not null && AllergensByCode.TryGetValue(code.Trim(), out var allergen)
            ? allergen
            : MenuItemErrors.AllergenUnknown;

    public static Result<DietaryLabel> ParseLabel(string? code) =>
        code is not null && LabelsByCode.TryGetValue(code.Trim(), out var label)
            ? label
            : MenuItemErrors.DietaryLabelUnknown;

    public static string CodeOf(Allergen allergen) => AllergenCodes[allergen];

    public static string CodeOf(DietaryLabel label) => LabelCodes[label];

    public IReadOnlyList<string>? AllergenCodesOf() => Allergens?.Select(CodeOf).ToList();

    public IReadOnlyList<string> LabelCodesOf() => [.. Labels.Select(CodeOf)];

    public bool Equals(DietaryInformation? other) =>
        other is not null &&
        (Allergens is null ? other.Allergens is null : other.Allergens is not null && Allergens.SequenceEqual(other.Allergens)) &&
        Labels.SequenceEqual(other.Labels);

    public override int GetHashCode() => HashCode.Combine(Allergens?.Count ?? -1, Labels.Count);
}
