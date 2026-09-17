using ArMenu.Domain.Common;
using ArMenu.Domain.Menus;
using Mediator;

namespace ArMenu.Application.Menus.Items.ReorderMenuItems;

public sealed class ReorderMenuItemsCommandHandler(
    IMenuCategoryRepository categories,
    IMenuItemRepository items,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ReorderMenuItemsCommand, Result>
{
    public async ValueTask<Result> Handle(ReorderMenuItemsCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (!await categories.ExistsAsync(command.CategoryId, cancellationToken))
        {
            return MenuCategoryErrors.NotFound;
        }

        var existing = await items.ListByCategoryAsync(command.CategoryId, cancellationToken);

        var reordered = DisplayOrdering.Apply(
            existing,
            command.ItemIds,
            item => item.Id,
            (item, position) => item.ChangeDisplayOrder(position));

        if (reordered.IsFailure)
        {
            return reordered;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
