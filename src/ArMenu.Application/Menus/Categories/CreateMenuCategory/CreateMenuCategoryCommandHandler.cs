using ArMenu.Application.MultiTenancy;
using ArMenu.Domain.Common;
using ArMenu.Domain.Menus;
using Mediator;

namespace ArMenu.Application.Menus.Categories.CreateMenuCategory;

public sealed class CreateMenuCategoryCommandHandler(
    ITenantContext tenantContext,
    IMenuCategoryRepository categories,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreateMenuCategoryCommand, Result<MenuCategoryId>>
{
    public async ValueTask<Result<MenuCategoryId>> Handle(CreateMenuCategoryCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var tenant = tenantContext.RequireTenant();

        var name = MenuTranslations.Create(command.Name, tenant);
        if (name.IsFailure)
        {
            return name.Error;
        }

        var description = MenuTranslations.CreateOptional(command.Description, tenant);
        if (description.IsFailure)
        {
            return description.Error;
        }

        var displayOrder = await categories.GetNextDisplayOrderAsync(cancellationToken);

        var category = MenuCategory.Create(tenant.Id, name.Value, displayOrder, description.Value);
        if (category.IsFailure)
        {
            return category.Error;
        }

        categories.Add(category.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return category.Value.Id;
    }
}
