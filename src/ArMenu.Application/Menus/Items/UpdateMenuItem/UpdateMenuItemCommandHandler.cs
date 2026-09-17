using ArMenu.Application.MultiTenancy;
using ArMenu.Domain.Common;
using ArMenu.Domain.Menus;
using ArMenu.Domain.Pricing;
using Mediator;

namespace ArMenu.Application.Menus.Items.UpdateMenuItem;

public sealed class UpdateMenuItemCommandHandler(
    ITenantContext tenantContext,
    IMenuCategoryRepository categories,
    IMenuItemRepository items,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateMenuItemCommand, Result>
{
    public async ValueTask<Result> Handle(UpdateMenuItemCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var tenant = tenantContext.RequireTenant();

        var item = await items.GetByIdAsync(command.ItemId, cancellationToken);
        if (item is null)
        {
            return MenuItemErrors.NotFound;
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

        var price = Money.Create(command.Price, Currency.Create(tenant.Currency).Value);
        if (price.IsFailure)
        {
            return price.Error;
        }

        var dietary = DietaryInformation.FromCodes(command.Allergens, command.DietaryLabels);
        if (dietary.IsFailure)
        {
            return dietary.Error;
        }

        if (item.CategoryId != command.CategoryId)
        {
            if (!await categories.ExistsAsync(command.CategoryId, cancellationToken))
            {
                return MenuCategoryErrors.NotFound;
            }

            item.MoveToCategory(command.CategoryId);
            var moved = item.ChangeDisplayOrder(await items.GetNextDisplayOrderAsync(command.CategoryId, cancellationToken));
            if (moved.IsFailure)
            {
                return moved;
            }
        }

        var outcome = item.Rename(name.Value) is { IsFailure: true } renamed
            ? renamed
            : item.ChangeDescription(description.Value);

        if (outcome.IsFailure)
        {
            return outcome;
        }

        item.ChangePrice(price.Value);
        item.ChangeDietaryInformation(dietary.Value);

        if (command.IsVisible)
        {
            item.Show();
        }
        else
        {
            item.Hide();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
