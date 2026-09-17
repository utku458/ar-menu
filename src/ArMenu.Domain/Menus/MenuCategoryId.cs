using ArMenu.Domain.Common;

namespace ArMenu.Domain.Menus;

public readonly record struct MenuCategoryId(Guid Value) : IStronglyTypedId<MenuCategoryId>
{
    public static MenuCategoryId New() => new(Guid.CreateVersion7());

    public static MenuCategoryId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
