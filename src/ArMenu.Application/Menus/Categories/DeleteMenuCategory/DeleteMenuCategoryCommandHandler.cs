using ArMenu.Domain.Common;
using ArMenu.Domain.Menus;
using Mediator;

namespace ArMenu.Application.Menus.Categories.DeleteMenuCategory;

public sealed class DeleteMenuCategoryCommandHandler(
    IMenuCategoryRepository categories,
    IMenuItemRepository items,
    IUnitOfWork unitOfWork)
    : ICommandHandler<DeleteMenuCategoryCommand, Result>
{
    public async ValueTask<Result> Handle(DeleteMenuCategoryCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var category = await categories.GetByIdAsync(command.CategoryId, cancellationToken);
        if (category is null)
        {
            return MenuCategoryErrors.NotFound;
        }

        if (await items.AnyInCategoryAsync(category.Id, cancellationToken))
        {
            return MenuCategoryErrors.NotEmpty;
        }

        categories.Remove(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
