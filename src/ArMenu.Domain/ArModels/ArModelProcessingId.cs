using ArMenu.Domain.Common;

namespace ArMenu.Domain.ArModels;

public readonly record struct ArModelProcessingId(Guid Value) : IStronglyTypedId<ArModelProcessingId>
{
    public static ArModelProcessingId New() => new(Guid.CreateVersion7());

    public static ArModelProcessingId From(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
