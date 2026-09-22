using System.Text.RegularExpressions;
using ArMenu.Domain.Common;

namespace ArMenu.Domain.Users;

/// <summary>
/// The name a person signs in with, when their account was opened by an administrator rather than by e-mail.
/// Unique across the whole platform: signing in asks for nothing else, so the name alone has to find the account,
/// and through it the business.
/// </summary>
/// <remarks>
/// Normalized (trimmed, lower-case) like <see cref="Email"/>, so that "Kasa" and "kasa" cannot become two accounts or
/// dodge the uniqueness constraint. The alphabet is deliberately the one of an e-mail local part — letters, digits,
/// dot, hyphen, underscore — which is what lets such an account keep an address in the reserved <c>.invalid</c> domain
/// (see <see cref="User.OpenWithUserName"/>).
/// </remarks>
public sealed partial record UserName
{
    public const int MinLength = 3;
    public const int MaxLength = 32;

    private UserName(string value) => Value = value;

    public string Value { get; }

    public static Result<UserName> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return UserErrors.UserNameRequired;
        }

        var normalized = value.Trim().ToLowerInvariant();

        return normalized.Length is >= MinLength and <= MaxLength && Pattern().IsMatch(normalized)
            ? new UserName(normalized)
            : UserErrors.UserNameInvalid;
    }

    public override string ToString() => Value;

    // Starts and ends with a letter or digit, so a name can never be "." or "-admin-" or look like a path.
    [GeneratedRegex("^[a-z0-9]([a-z0-9._-]*[a-z0-9])?$", RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();
}
