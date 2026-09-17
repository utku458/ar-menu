using System.Diagnostics.CodeAnalysis;
using ArMenu.Application.Abstractions.Authentication;
using ArMenu.Domain.Sessions;

namespace ArMenu.Infrastructure.Authentication;

/// <summary>Refresh tokens in the <see cref="OpaqueToken"/> format, keyed by session id.</summary>
internal sealed class RefreshTokenCodec : IRefreshTokenCodec
{
    public RefreshTokenSecret GenerateSecret()
    {
        var secret = OpaqueToken.GenerateSecret();
        return new RefreshTokenSecret(secret, RefreshTokenHash.Create(OpaqueToken.HashSecret(secret)).Value);
    }

    public string Encode(UserSessionId sessionId, RefreshTokenSecret secret)
    {
        ArgumentNullException.ThrowIfNull(secret);
        return OpaqueToken.Encode(sessionId.Value, secret.Value);
    }

    public bool TryDecode(string? refreshToken, out UserSessionId sessionId, [NotNullWhen(true)] out RefreshTokenHash? secretHash)
    {
        var decoded = OpaqueToken.TryDecode(refreshToken, out var id, out var hash);
        sessionId = decoded ? UserSessionId.From(id) : default;
        secretHash = decoded ? RefreshTokenHash.Create(hash).Value : null;
        return decoded;
    }
}
