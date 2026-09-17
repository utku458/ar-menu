using ArMenu.Application.MultiTenancy;
using ArMenu.Domain.Menus;
using FluentValidation;

namespace ArMenu.Application.Menus.Categories.CreateMenuCategory;

public sealed class CreateMenuCategoryCommandValidator : AbstractValidator<CreateMenuCategoryCommand>
{
    public CreateMenuCategoryCommandValidator(ITenantContext tenantContext)
    {
        RuleFor(command => command.Name)
            .NotNull()
            .MustBeMenuText(tenantContext, MenuCategory.NameMaxLength, MenuCategoryErrors.NameTooLong);

        RuleFor(command => command.Description)
            .MustBeOptionalMenuText(tenantContext, MenuCategory.DescriptionMaxLength, MenuCategoryErrors.DescriptionTooLong);
    }
}
