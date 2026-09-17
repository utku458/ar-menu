using ArMenu.Domain.Common;
using ArMenu.Domain.Menus;
using Mediator;

namespace ArMenu.Application.Menus.Items.DeleteMenuItem;

public sealed class DeleteMenuItemCommandHandler(IMenuItemRepository items, IUnitOfWork unitOfWork)
    : ICommandHandler<DeleteMenuItemCommand, Result>
{
    public async ValueTask<Result> Handle(DeleteMenuItemCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var item = await items.GetByIdAsync(command.ItemId, cancellationToken);
        if (item is null)
        {
            return MenuItemErrors.NotFound;
        }

        items.Remove(item);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
