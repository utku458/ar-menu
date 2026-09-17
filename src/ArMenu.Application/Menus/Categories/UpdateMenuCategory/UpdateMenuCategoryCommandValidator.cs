using ArMenu.Application.MultiTenancy;
using ArMenu.Domain.Menus;
using FluentValidation;

namespace ArMenu.Application.Menus.Categories.UpdateMenuCategory;

public sealed class UpdateMenuCategoryCommandValidator : AbstractValidator<UpdateMenuCategoryCommand>
{
    public UpdateMenuCategoryCommandValidator(ITenantContext tenantContext)
    {
        RuleFor(command => command.Name)
            .NotNull()
            .MustBeMenuText(tenantContext, MenuCategory.NameMaxLength, MenuCategoryErrors.NameTooLong);

        RuleFor(command => command.Description)
            .MustBeOptionalMenuText(tenantContext, MenuCategory.DescriptionMaxLength, MenuCategoryErrors.DescriptionTooLong);
    }
}
