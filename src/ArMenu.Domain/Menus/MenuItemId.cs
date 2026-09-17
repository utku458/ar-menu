using ArMenu.Domain.Common;

namespace ArMenu.Domain.Menus;

public readonly record struct MenuItemId(Guid Value) : IStronglyTypedId<MenuItemId>
{
    public static MenuItemId New() => new(Guid.CreateVersion7());

    public static MenuItemId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
