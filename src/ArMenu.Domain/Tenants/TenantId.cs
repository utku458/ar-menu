using ArMenu.Domain.Common;

namespace ArMenu.Domain.Tenants;

public readonly record struct TenantId(Guid Value) : IStronglyTypedId<TenantId>
{
    public static TenantId New() => new(Guid.CreateVersion7());

    public static TenantId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
