using ArMenu.Application.MultiTenancy;
using ArMenu.Domain.Common;
using ArMenu.Domain.Menus;
using Mediator;

namespace ArMenu.Application.Menus.Categories.UpdateMenuCategory;

public sealed class UpdateMenuCategoryCommandHandler(
    ITenantContext tenantContext,
    IMenuCategoryRepository categories,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateMenuCategoryCommand, Result>
{
    public async ValueTask<Result> Handle(UpdateMenuCategoryCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var tenant = tenantContext.RequireTenant();

        var category = await categories.GetByIdAsync(command.CategoryId, cancellationToken);
        if (category is null)
        {
            return MenuCategoryErrors.NotFound;
        }

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

        var outcome = category.Rename(name.Value) is { IsFailure: true } renamed
            ? renamed
            : category.ChangeDescription(description.Value);

        if (outcome.IsFailure)
        {
            return outcome;
        }

        if (command.IsVisible)
        {
            category.Show();
        }
        else
        {
            category.Hide();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
