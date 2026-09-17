using ArMenu.Domain.Common;
using ArMenu.Domain.Localization;
using ArMenu.Domain.Media;
using ArMenu.Domain.Pricing;
using ArMenu.Domain.Tenants;

namespace ArMenu.Domain.Menus;

/// <summary>A dish or drink on a tenant's menu, optionally viewable in 3D and augmented reality.</summary>
/// <remarks>
/// <see cref="MenuItem"/> is its own aggregate (it references its category by id) so that staff editing different
/// items never contend for the same aggregate, and a large menu never has to be loaded to change a single price.
/// Cross-aggregate rules (the category exists in the same tenant, the price uses the tenant currency) are enforced
/// by the application layer and backed by a composite (tenant_id, category_id) foreign key in the database.
/// </remarks>
public sealed class MenuItem : AggregateRoot<MenuItemId>, ITenantScoped, ISoftDeletable, IAuditable
{
    public const int NameMaxLength = 120;
    public const int DescriptionMaxLength = 1000;

    // Null: the business has not said which allergens the dish contains (see DietaryInformation).
    private List<Allergen>? _allergens;
    private List<DietaryLabel> _dietaryLabels = [];

    private MenuItem(
        MenuItemId id,
        TenantId tenantId,
        MenuCategoryId categoryId,
        LocalizedText name,
        LocalizedText? description,
        Money price,
        int displayOrder)
        : base(id)
    {
        TenantId = tenantId;
        CategoryId = categoryId;
        Name = name;
        Description = description;
        Price = price;
        DisplayOrder = displayOrder;
        IsVisible = true;
        IsAvailable = true;
    }

#pragma warning disable CS8618 // Used by EF Core; every property is populated during materialization.
    private MenuItem()
    {
    }
#pragma warning restore CS8618

    public TenantId TenantId { get; private init; }

    public MenuCategoryId CategoryId { get; private set; }

    public LocalizedText Name { get; private set; }

    public LocalizedText? Description { get; private set; }

    public Money Price { get; private set; }

    /// <summary>Regular 2D photo of the item.</summary>
    public AssetPath? ImagePath { get; private set; }

    public ArModel? ArModel { get; private set; }

    /// <summary>The allergens the dish contains; <see langword="null"/> when the business has not said.</summary>
    public IReadOnlyList<Allergen>? Allergens => _allergens?.AsReadOnly();

    /// <summary>The diets the dish suits.</summary>
    public IReadOnlyList<DietaryLabel> DietaryLabels => _dietaryLabels.AsReadOnly();

    /// <summary>Position of the item within its category, ascending.</summary>
    public int DisplayOrder { get; private set; }

    /// <summary>Whether the item is shown on the public menu at all.</summary>
    public bool IsVisible { get; private set; }

    /// <summary>Whether the item can be ordered right now; unavailable items stay listed as "sold out".</summary>
    public bool IsAvailable { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public static Result<MenuItem> Create(
        TenantId tenantId,
        MenuCategoryId categoryId,
        LocalizedText name,
        Money price,
        int displayOrder,
        LocalizedText? description = null)
    {
        Guard.NotDefault(tenantId);
        Guard.NotDefault(categoryId);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(price);

        var error = ValidateName(name) ?? ValidateDescription(description) ?? ValidateDisplayOrder(displayOrder);

        return error is null
            ? new MenuItem(MenuItemId.New(), tenantId, categoryId, name, description, price, displayOrder)
            : error;
    }

    public Result Rename(LocalizedText name)
    {
        ArgumentNullException.ThrowIfNull(name);

        if (ValidateName(name) is { } error)
        {
            return error;
        }

        Name = name;
        return Result.Success();
    }

    public Result ChangeDescription(LocalizedText? description)
    {
        if (ValidateDescription(description) is { } error)
        {
            return error;
        }

        Description = description;
        return Result.Success();
    }

    public void ChangePrice(Money price)
    {
        ArgumentNullException.ThrowIfNull(price);
        Price = price;
    }

    public void MoveToCategory(MenuCategoryId categoryId)
    {
        Guard.NotDefault(categoryId);
        CategoryId = categoryId;
    }

    public Result ChangeDisplayOrder(int displayOrder)
    {
        if (ValidateDisplayOrder(displayOrder) is { } error)
        {
            return error;
        }

        DisplayOrder = displayOrder;
        return Result.Success();
    }

    public void ChangeImage(AssetPath? imagePath) => ImagePath = imagePath;

    /// <summary>
    /// Replaces the allergens and the diets together: they are checked against each other, so changing one at a time
    /// could pass through a contradiction (a vegan dish with milk) even when the end result is consistent.
    /// </summary>
    public void ChangeDietaryInformation(DietaryInformation information)
    {
        ArgumentNullException.ThrowIfNull(information);

        // Lists are replaced rather than edited, so change tracking sees a new value.
        _allergens = information.Allergens is null ? null : [.. information.Allergens];
        _dietaryLabels = [.. information.Labels];
    }

    public void AttachArModel(ArModel arModel)
    {
        ArgumentNullException.ThrowIfNull(arModel);
        ArModel = arModel;
    }

    public void DetachArModel() => ArModel = null;

    public void Show() => IsVisible = true;

    public void Hide() => IsVisible = false;

    public void MarkAsAvailable() => IsAvailable = true;

    public void MarkAsSoldOut() => IsAvailable = false;

    private static Error? ValidateName(LocalizedText name) =>
        name.ExceedsLength(NameMaxLength) ? MenuItemErrors.NameTooLong : null;

    private static Error? ValidateDescription(LocalizedText? description) =>
        description?.ExceedsLength(DescriptionMaxLength) == true ? MenuItemErrors.DescriptionTooLong : null;

    private static Error? ValidateDisplayOrder(int displayOrder) =>
        displayOrder < 0 ? MenuItemErrors.DisplayOrderNegative : null;
}
