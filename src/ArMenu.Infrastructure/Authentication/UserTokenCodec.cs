using System.Diagnostics.CodeAnalysis;
using ArMenu.Application.Abstractions.Authentication;
using ArMenu.Domain.Users;

namespace ArMenu.Infrastructure.Authentication;

/// <summary>Secrets of links e-mailed to users, in the <see cref="OpaqueToken"/> format keyed by token id.</summary>
internal sealed class UserTokenCodec : IUserTokenCodec
{
    public UserTokenSecret GenerateSecret()
    {
        var secret = OpaqueToken.GenerateSecret();
        return new UserTokenSecret(secret, UserTokenHash.Create(OpaqueToken.HashSecret(secret)).Value);
    }

    public string Encode(UserTokenId tokenId, UserTokenSecret secret)
    {
        ArgumentNullException.ThrowIfNull(secret);
        return OpaqueToken.Encode(tokenId.Value, secret.Value);
    }

    public bool TryDecode(string? token, out UserTokenId tokenId, [NotNullWhen(true)] out UserTokenHash? secretHash)
    {
        var decoded = OpaqueToken.TryDecode(token, out var id, out var hash);
        tokenId = decoded ? UserTokenId.From(id) : default;
        secretHash = decoded ? UserTokenHash.Create(hash).Value : null;
        return decoded;
    }
}
