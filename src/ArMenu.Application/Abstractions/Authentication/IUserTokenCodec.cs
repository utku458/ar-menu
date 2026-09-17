using System.Diagnostics.CodeAnalysis;
using ArMenu.Domain.Users;

namespace ArMenu.Application.Abstractions.Authentication;

/// <summary>Creates and parses the opaque secrets of links e-mailed to users, of the form <c>{tokenId}.{secret}</c>.</summary>
public interface IUserTokenCodec
{
    UserTokenSecret GenerateSecret();

    string Encode(UserTokenId tokenId, UserTokenSecret secret);

    /// <summary>Parses a token and hashes its secret. Returns <see langword="false"/> for anything malformed.</summary>
    bool TryDecode(string? token, out UserTokenId tokenId, [NotNullWhen(true)] out UserTokenHash? secretHash);
}

public sealed record UserTokenSecret(string Value, UserTokenHash Hash)
{
    public override string ToString() => "UserTokenSecret { *** }";
}
