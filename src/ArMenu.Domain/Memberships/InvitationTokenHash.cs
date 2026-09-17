using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using ArMenu.Domain.Common;

namespace ArMenu.Domain.Memberships;

/// <summary>
/// SHA-256 hash (base64url) of an invitation link's secret. The link itself is only ever in the e-mail: the database
/// holds nothing that lets anyone join a business.
/// </summary>
/// <remarks>A type of its own, so an invitation can never be checked against a refresh token hash by mistake.</remarks>
public sealed partial record InvitationTokenHash
{
    public const int Length = 43;

    private InvitationTokenHash(string value) => Value = value;

    public string Value { get; }

    public static Result<InvitationTokenHash> Create(string? value) =>
        value is not null && Pattern().IsMatch(value)
            ? new InvitationTokenHash(value)
            : InvitationErrors.TokenHashInvalid;

    /// <summary>Constant-time comparison, so response timing reveals nothing about how much of a hash matched.</summary>
    public bool Matches(InvitationTokenHash other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(Value), Encoding.ASCII.GetBytes(other.Value));
    }

    public override string ToString() => "InvitationTokenHash { *** }";

    [GeneratedRegex("^[A-Za-z0-9_-]{43}$", RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();
}
