using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using ArMenu.Domain.Common;

namespace ArMenu.Domain.Sessions;

/// <summary>
/// SHA-256 hash (base64url) of a refresh token secret. Only hashes are stored, so a leaked database backup contains no
/// usable tokens.
/// </summary>
public sealed partial record RefreshTokenHash
{
    public const int Length = 43;

    private RefreshTokenHash(string value) => Value = value;

    public string Value { get; }

    public static Result<RefreshTokenHash> Create(string? value) =>
        value is not null && Pattern().IsMatch(value)
            ? new RefreshTokenHash(value)
            : SessionErrors.RefreshTokenHashInvalid;

    /// <summary>Constant-time comparison, so response timing reveals nothing about how much of a hash matched.</summary>
    public bool Matches(RefreshTokenHash other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(Value), Encoding.ASCII.GetBytes(other.Value));
    }

    public override string ToString() => "RefreshTokenHash { *** }";

    [GeneratedRegex("^[A-Za-z0-9_-]{43}$", RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();
}
