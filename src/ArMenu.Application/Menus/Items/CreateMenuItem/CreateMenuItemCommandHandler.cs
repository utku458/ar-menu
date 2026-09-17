using ArMenu.Application.MultiTenancy;
using ArMenu.Domain.Common;
using ArMenu.Domain.Menus;
using ArMenu.Domain.Pricing;
using Mediator;

namespace ArMenu.Application.Menus.Items.CreateMenuItem;

public sealed class CreateMenuItemCommandHandler(
    ITenantContext tenantContext,
    IMenuCategoryRepository categories,
    IMenuItemRepository items,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreateMenuItemCommand, Result<MenuItemId>>
{
    public async ValueTask<Result<MenuItemId>> Handle(CreateMenuItemCommand command, CancellationToken cancellationToken)
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

        // The category lookup is tenant-scoped: another tenant's category id is simply "not found".
        if (!await categories.ExistsAsync(command.CategoryId, cancellationToken))
        {
            return MenuCategoryErrors.NotFound;
        }

        var displayOrder = await items.GetNextDisplayOrderAsync(command.CategoryId, cancellationToken);

        var item = MenuItem.Create(tenant.Id, command.CategoryId, name.Value, price.Value, displayOrder, description.Value);
        if (item.IsFailure)
        {
            return item.Error;
        }

        item.Value.ChangeDietaryInformation(dietary.Value);
        items.Add(item.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return item.Value.Id;
    }
}
