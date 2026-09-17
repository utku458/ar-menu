using ArMenu.Domain.Common;

namespace ArMenu.Domain.Memberships;

public readonly record struct TenantInvitationId(Guid Value) : IStronglyTypedId<TenantInvitationId>
{
    public static TenantInvitationId New() => new(Guid.CreateVersion7());

    public static TenantInvitationId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
