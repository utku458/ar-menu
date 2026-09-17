using ArMenu.Domain.Common;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ArMenu.Infrastructure.Persistence.Converters;

/// <summary>Stores any <see cref="IStronglyTypedId{TSelf}"/> as a native PostgreSQL <c>uuid</c>.</summary>
internal sealed class StronglyTypedIdConverter<TId>()
    : ValueConverter<TId, Guid>(id => id.Value, value => Factory(value))
    where TId : struct, IStronglyTypedId<TId>
{
    // Expression trees cannot call static abstract interface members directly; a cached delegate bridges the gap.
    private static readonly Func<Guid, TId> Factory = TId.From;
}
