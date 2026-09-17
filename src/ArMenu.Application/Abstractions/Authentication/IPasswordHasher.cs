namespace ArMenu.Application.Abstractions.Authentication;

public interface IPasswordHasher
{
    string Hash(string password);

    PasswordVerificationResult Verify(string passwordHash, string providedPassword);

    /// <summary>
    /// Spends the same work as a real verification against an internal decoy hash. Call it when the account does not
    /// exist, so response times do not reveal which e-mail addresses are registered.
    /// </summary>
    void VerifyDecoy(string providedPassword);
}

public enum PasswordVerificationResult
{
    Failed = 0,
    Success = 1,

    /// <summary>The password is correct but was hashed with outdated parameters and should be re-hashed.</summary>
    SuccessRehashNeeded = 2,
}
