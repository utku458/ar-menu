namespace ArMenu.Domain.Menus;

/// <summary>
/// The fourteen allergens food businesses must declare under EU Regulation 1169/2011 (Annex II), which the Turkish Food
/// Codex labelling regulation follows. Values are stable: they are stored.
/// </summary>
public enum Allergen
{
    Gluten = 1,
    Crustaceans = 2,
    Eggs = 3,
    Fish = 4,
    Peanuts = 5,
    Soybeans = 6,
    Milk = 7,
    Nuts = 8,
    Celery = 9,
    Mustard = 10,
    Sesame = 11,
    Sulphites = 12,
    Lupin = 13,
    Molluscs = 14,
}

/// <summary>What a dish is suitable for. A label is a promise to a guest, so it may not contradict the declared allergens.</summary>
public enum DietaryLabel
{
    Vegetarian = 1,
    Vegan = 2,
    GlutenFree = 3,
}
