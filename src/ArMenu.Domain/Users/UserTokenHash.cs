using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using ArMenu.Domain.Common;

namespace ArMenu.Domain.Users;

/// <summary>SHA-256 hash (base64url) of the secret in a link e-mailed to a user. The secret itself is never stored.</summary>
public sealed partial record UserTokenHash
{
    public const int Length = 43;

    private UserTokenHash(string value) => Value = value;

    public string Value { get; }

    public static Result<UserTokenHash> Create(string? value) =>
        value is not null && Pattern().IsMatch(value)
            ? new UserTokenHash(value)
            : UserTokenErrors.HashInvalid;

    /// <summary>Constant-time comparison, so response timing reveals nothing about how much of a hash matched.</summary>
    public bool Matches(UserTokenHash other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(Value), Encoding.ASCII.GetBytes(other.Value));
    }

    public override string ToString() => "UserTokenHash { *** }";

    [GeneratedRegex("^[A-Za-z0-9_-]{43}$", RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();
}
