using System.Diagnostics.CodeAnalysis;
using ArMenu.Domain.Sessions;

namespace ArMenu.Application.Abstractions.Authentication;

/// <summary>Creates and parses opaque refresh tokens of the form <c>{sessionId}.{secret}</c>.</summary>
public interface IRefreshTokenCodec
{
    /// <summary>A new cryptographically random secret and the hash under which it is stored.</summary>
    RefreshTokenSecret GenerateSecret();

    string Encode(UserSessionId sessionId, RefreshTokenSecret secret);

    /// <summary>Parses a token and hashes its secret. Returns <see langword="false"/> for anything malformed.</summary>
    bool TryDecode(string? refreshToken, out UserSessionId sessionId, [NotNullWhen(true)] out RefreshTokenHash? secretHash);
}

public sealed record RefreshTokenSecret(string Value, RefreshTokenHash Hash)
{
    public override string ToString() => "RefreshTokenSecret { *** }";
}
