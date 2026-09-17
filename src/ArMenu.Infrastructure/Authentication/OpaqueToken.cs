using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace ArMenu.Infrastructure.Authentication;

/// <summary>
/// Shared format of the bearer secrets the API hands out (refresh tokens, invitation links): <c>{id:N}.{secret}</c>,
/// where the secret is 256 random bits in base64url. The id makes lookups a primary-key seek; only a SHA-256 hash of
/// the secret is ever stored.
/// </summary>
internal static class OpaqueToken
{
    public const int SecretLength = 43;

    private const int SecretSizeInBytes = 32;
    private const int IdLength = 32;
    private const char Separator = '.';

    public static string GenerateSecret() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(SecretSizeInBytes));

    public static string HashSecret(string secret) =>
        Base64Url.EncodeToString(SHA256.HashData(Encoding.ASCII.GetBytes(secret)));

    public static string Encode(Guid id, string secret) =>
        string.Create(CultureInfo.InvariantCulture, $"{id:N}{Separator}{secret}");

    public static bool TryDecode(string? token, out Guid id, [NotNullWhen(true)] out string? secretHash)
    {
        id = default;
        secretHash = null;

        if (token is null ||
            token.Length != IdLength + 1 + SecretLength ||
            token[IdLength] != Separator ||
            !Guid.TryParseExact(token.AsSpan(0, IdLength), "N", out id))
        {
            return false;
        }

        var secret = token[(IdLength + 1)..];
        if (!Base64Url.IsValid(secret))
        {
            id = default;
            return false;
        }

        secretHash = HashSecret(secret);
        return true;
    }
}
