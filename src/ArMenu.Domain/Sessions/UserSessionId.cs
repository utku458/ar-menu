using ArMenu.Domain.Common;

namespace ArMenu.Domain.Sessions;

public readonly record struct UserSessionId(Guid Value) : IStronglyTypedId<UserSessionId>
{
    public static UserSessionId New() => new(Guid.CreateVersion7());

    public static UserSessionId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
