using ArMenu.Domain.Common;
using ArMenu.Domain.Localization;
using ArMenu.Domain.Tenants;

namespace ArMenu.Domain.Menus;

/// <summary>A section of a tenant's menu, such as "Starters" or "Hot Drinks".</summary>
public sealed class MenuCategory : AggregateRoot<MenuCategoryId>, ITenantScoped, ISoftDeletable, IAuditable
{
    public const int NameMaxLength = 80;
    public const int DescriptionMaxLength = 500;

    private MenuCategory(MenuCategoryId id, TenantId tenantId, LocalizedText name, LocalizedText? description, int displayOrder)
        : base(id)
    {
        TenantId = tenantId;
        Name = name;
        Description = description;
        DisplayOrder = displayOrder;
        IsVisible = true;
    }

#pragma warning disable CS8618 // Used by EF Core; every property is populated during materialization.
    private MenuCategory()
    {
    }
#pragma warning restore CS8618

    public TenantId TenantId { get; private init; }

    public LocalizedText Name { get; private set; }

    public LocalizedText? Description { get; private set; }

    /// <summary>Position of the category in the menu, ascending.</summary>
    public int DisplayOrder { get; private set; }

    /// <summary>Whether the category is shown on the public menu. Hidden categories stay manageable by staff.</summary>
    public bool IsVisible { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public static Result<MenuCategory> Create(
        TenantId tenantId,
        LocalizedText name,
        int displayOrder,
        LocalizedText? description = null)
    {
        Guard.NotDefault(tenantId);
        ArgumentNullException.ThrowIfNull(name);

        var error = ValidateName(name) ?? ValidateDescription(description) ?? ValidateDisplayOrder(displayOrder);

        return error is null
            ? new MenuCategory(MenuCategoryId.New(), tenantId, name, description, displayOrder)
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

    public Result ChangeDisplayOrder(int displayOrder)
    {
        if (ValidateDisplayOrder(displayOrder) is { } error)
        {
            return error;
        }

        DisplayOrder = displayOrder;
        return Result.Success();
    }

    public void Show() => IsVisible = true;

    public void Hide() => IsVisible = false;

    private static Error? ValidateName(LocalizedText name) =>
        name.ExceedsLength(NameMaxLength) ? MenuCategoryErrors.NameTooLong : null;

    private static Error? ValidateDescription(LocalizedText? description) =>
        description?.ExceedsLength(DescriptionMaxLength) == true ? MenuCategoryErrors.DescriptionTooLong : null;

    private static Error? ValidateDisplayOrder(int displayOrder) =>
        displayOrder < 0 ? MenuCategoryErrors.DisplayOrderNegative : null;
}
