using ArMenu.Application.Common.Validation;
using ArMenu.Application.MultiTenancy;
using ArMenu.Domain.Menus;
using FluentValidation;

namespace ArMenu.Application.Menus.Items.CreateMenuItem;

public sealed class CreateMenuItemCommandValidator : AbstractValidator<CreateMenuItemCommand>
{
    public CreateMenuItemCommandValidator(ITenantContext tenantContext)
    {
        RuleFor(command => command.Name)
            .NotNull()
            .MustBeMenuText(tenantContext, MenuItem.NameMaxLength, MenuItemErrors.NameTooLong);

        RuleFor(command => command.Description)
            .MustBeOptionalMenuText(tenantContext, MenuItem.DescriptionMaxLength, MenuItemErrors.DescriptionTooLong);

        RuleFor(command => command.Price).MustBeValid(MenuItemRules.ValidatePriceAmount);

        this.AddDietaryRules(command => command.Allergens, command => command.DietaryLabels);
    }
}
