using ArMenu.Domain.Common;
using ArMenu.Domain.Menus;
using Mediator;

namespace ArMenu.Application.Menus.Categories.ReorderMenuCategories;

public sealed class ReorderMenuCategoriesCommandHandler(IMenuCategoryRepository categories, IUnitOfWork unitOfWork)
    : ICommandHandler<ReorderMenuCategoriesCommand, Result>
{
    public async ValueTask<Result> Handle(ReorderMenuCategoriesCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var existing = await categories.ListAsync(cancellationToken);

        var reordered = DisplayOrdering.Apply(
            existing,
            command.CategoryIds,
            category => category.Id,
            (category, position) => category.ChangeDisplayOrder(position));

        if (reordered.IsFailure)
        {
            return reordered;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
