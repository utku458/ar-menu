using ArMenu.Domain.Common;
using ArMenu.Domain.Menus;
using Mediator;

namespace ArMenu.Application.Menus.Items.SetMenuItemAvailability;

public sealed class SetMenuItemAvailabilityCommandHandler(IMenuItemRepository items, IUnitOfWork unitOfWork)
    : ICommandHandler<SetMenuItemAvailabilityCommand, Result>
{
    public async ValueTask<Result> Handle(SetMenuItemAvailabilityCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var item = await items.GetByIdAsync(command.ItemId, cancellationToken);
        if (item is null)
        {
            return MenuItemErrors.NotFound;
        }

        if (command.IsAvailable)
        {
            item.MarkAsAvailable();
        }
        else
        {
            item.MarkAsSoldOut();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
