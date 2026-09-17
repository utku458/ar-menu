namespace ArMenu.Infrastructure.Authentication;

public sealed class PasswordHashingOptions
{
    public const string SectionName = "Authentication:PasswordHashing";

    /// <summary>
    /// PBKDF2-HMAC-SHA512 iterations; 210,000 follows OWASP's password storage guidance. Raising the value later is safe:
    /// older hashes keep verifying and are upgraded at the next successful sign-in.
    /// </summary>
    public int Iterations { get; set; } = 210_000;
}
