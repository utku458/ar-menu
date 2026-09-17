using System.Text.RegularExpressions;
using ArMenu.Domain.Common;

namespace ArMenu.Domain.Users;

/// <summary>
/// An e-mail address used as a sign-in name, normalized (trimmed, lower-case) so that lookups and the uniqueness
/// constraint cannot be bypassed with different casing.
/// </summary>
public sealed partial record Email
{
    public const int MaxLength = 254;

    private Email(string value) => Value = value;

    public string Value { get; }

    public static Result<Email> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return UserErrors.EmailRequired;
        }

        var normalized = value.Trim().ToLowerInvariant();

        return normalized.Length <= MaxLength && Pattern().IsMatch(normalized)
            ? new Email(normalized)
            : UserErrors.EmailInvalid;
    }

    public override string ToString() => Value;

    // Deliberately pragmatic: one '@', no whitespace, a dot in the domain. Deliverability is proven by e-mail, not regex.
    [GeneratedRegex(@"^[^@\s]{1,64}@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();
}
