using System.Globalization;
using System.Security.Cryptography;
using ArMenu.Application.Abstractions.Authentication;
using Microsoft.Extensions.Options;

namespace ArMenu.Infrastructure.Authentication;

/// <summary>
/// PBKDF2-HMAC-SHA512 through the platform's vetted implementation (<see cref="Rfc2898DeriveBytes.Pbkdf2(string, byte[], int, HashAlgorithmName, int)"/>),
/// stored in a self-describing format <c>pbkdf2-sha512$iterations$salt$hash</c> so parameters can evolve without migrations.
/// </summary>
internal sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const string Scheme = "pbkdf2-sha512";
    private const char Separator = '$';
    private const int SaltSizeInBytes = 16;
    private const int HashSizeInBytes = 32;

    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA512;

    private readonly int _iterations;
    private readonly Lazy<string> _decoyHash;

    public Pbkdf2PasswordHasher(IOptions<PasswordHashingOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _iterations = options.Value.Iterations;
        _decoyHash = new Lazy<string>(() => Hash(Convert.ToBase64String(RandomNumberGenerator.GetBytes(SaltSizeInBytes))));
    }

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSizeInBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, _iterations, Algorithm, HashSizeInBytes);

        return string.Join(
            Separator,
            Scheme,
            _iterations.ToString(CultureInfo.InvariantCulture),
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    public PasswordVerificationResult Verify(string passwordHash, string providedPassword)
    {
        ArgumentNullException.ThrowIfNull(passwordHash);
        ArgumentNullException.ThrowIfNull(providedPassword);

        if (!TryParse(passwordHash, out var iterations, out var salt, out var expectedHash))
        {
            return PasswordVerificationResult.Failed;
        }

        var actualHash = Rfc2898DeriveBytes.Pbkdf2(providedPassword, salt, iterations, Algorithm, expectedHash.Length);

        if (!CryptographicOperations.FixedTimeEquals(actualHash, expectedHash))
        {
            return PasswordVerificationResult.Failed;
        }

        return iterations < _iterations ? PasswordVerificationResult.SuccessRehashNeeded : PasswordVerificationResult.Success;
    }

    public void VerifyDecoy(string providedPassword) => _ = Verify(_decoyHash.Value, providedPassword);

    private static bool TryParse(string passwordHash, out int iterations, out byte[] salt, out byte[] hash)
    {
        iterations = 0;
        salt = hash = [];

        var parts = passwordHash.Split(Separator);
        if (parts.Length != 4 || parts[0] != Scheme ||
            !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out iterations) || iterations <= 0)
        {
            return false;
        }

        try
        {
            salt = Convert.FromBase64String(parts[2]);
            hash = Convert.FromBase64String(parts[3]);
            return salt.Length > 0 && hash.Length > 0;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
