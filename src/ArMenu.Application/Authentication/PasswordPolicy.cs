namespace ArMenu.Application.Authentication;

/// <summary>
/// Length-based policy per OWASP ASVS (V2.1): at least 12 characters, no composition rules that push users towards
/// predictable patterns. The upper bound keeps hashing cost predictable.
/// </summary>
public static class PasswordPolicy
{
    public const int MinLength = 12;
    public const int MaxLength = 128;
}
