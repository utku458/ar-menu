using ArMenu.Domain.Common;
using ArMenu.Domain.Menus;
using Mediator;

namespace ArMenu.Application.Menus.Items.DetachMenuItemArModel;

public sealed class DetachMenuItemArModelCommandHandler(IMenuItemRepository items, IUnitOfWork unitOfWork)
    : ICommandHandler<DetachMenuItemArModelCommand, Result>
{
    public async ValueTask<Result> Handle(DetachMenuItemArModelCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var item = await items.GetByIdAsync(command.ItemId, cancellationToken);
        if (item is null)
        {
            return MenuItemErrors.NotFound;
        }

        item.DetachArModel();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
