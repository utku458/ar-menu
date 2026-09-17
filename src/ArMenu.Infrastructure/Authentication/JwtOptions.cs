using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ArMenu.Infrastructure.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Authentication:Jwt";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    /// <summary>Base64-encoded HMAC-SHA256 key of at least 256 bits. Supplied by a secret store, never committed.</summary>
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Short on purpose: access tokens cannot be revoked, so their lifetime bounds the damage of a leak.</summary>
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(15);

    public SymmetricSecurityKey CreateSigningKey() => new(Convert.FromBase64String(SigningKey));
}

internal sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    private const int MinimumKeySizeInBytes = 32;

    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            failures.Add($"{JwtOptions.SectionName}:Issuer is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            failures.Add($"{JwtOptions.SectionName}:Audience is required.");
        }

        if (!IsStrongKey(options.SigningKey))
        {
            failures.Add($"{JwtOptions.SectionName}:SigningKey must be a base64-encoded key of at least 256 bits.");
        }

        if (options.AccessTokenLifetime <= TimeSpan.Zero || options.AccessTokenLifetime > TimeSpan.FromHours(1))
        {
            failures.Add($"{JwtOptions.SectionName}:AccessTokenLifetime must be between zero and one hour.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static bool IsStrongKey(string? base64Key)
    {
        var buffer = new byte[Math.Max(base64Key?.Length ?? 0, 1)];
        return !string.IsNullOrWhiteSpace(base64Key) &&
               Convert.TryFromBase64String(base64Key, buffer, out var bytesWritten) &&
               bytesWritten >= MinimumKeySizeInBytes;
    }
}
