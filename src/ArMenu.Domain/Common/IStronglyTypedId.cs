namespace ArMenu.Domain.Common;

/// <summary>
/// A <see cref="Guid"/>-backed identifier with a dedicated type, so that a <c>MenuItemId</c> can never be passed where a
/// <c>TenantId</c> is expected. In a multi-tenant system this turns a whole class of data-leak bugs into compile errors.
/// </summary>
/// <remarks>
/// Identifiers are UUIDv7 values: time-ordered (index-friendly B-tree inserts) yet not enumerable like integer keys,
/// so they do not reveal business volume or invite IDOR-style guessing.
/// </remarks>
public interface IStronglyTypedId<TSelf>
    where TSelf : struct, IStronglyTypedId<TSelf>
{
    Guid Value { get; }

    static abstract TSelf From(Guid value);
}
