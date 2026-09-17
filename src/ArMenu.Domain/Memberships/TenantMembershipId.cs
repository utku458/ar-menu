using ArMenu.Domain.Common;

namespace ArMenu.Domain.Memberships;

public readonly record struct TenantMembershipId(Guid Value) : IStronglyTypedId<TenantMembershipId>
{
    public static TenantMembershipId New() => new(Guid.CreateVersion7());

    public static TenantMembershipId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
