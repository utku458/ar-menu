using ArMenu.Domain.Common;

namespace ArMenu.Domain.Users;

public readonly record struct UserTokenId(Guid Value) : IStronglyTypedId<UserTokenId>
{
    public static UserTokenId New() => new(Guid.CreateVersion7());

    public static UserTokenId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
