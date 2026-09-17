using System.Globalization;
using ArMenu.Application.Abstractions.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ArMenu.Infrastructure.Authentication;

internal sealed class JwtAccessTokenIssuer(IOptions<JwtOptions> options, TimeProvider timeProvider) : IAccessTokenIssuer
{
    private static readonly JsonWebTokenHandler TokenHandler = new();

    private readonly JwtOptions _options = options.Value;
    private readonly SigningCredentials _signingCredentials = new(options.Value.CreateSigningKey(), SecurityAlgorithms.HmacSha256);

    public AccessToken Issue(AccessTokenSubject subject)
    {
        ArgumentNullException.ThrowIfNull(subject);

        var now = timeProvider.GetUtcNow();
        var expiresAt = now + _options.AccessTokenLifetime;

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = _signingCredentials,
            Claims = new Dictionary<string, object>
            {
                [ArMenuClaimTypes.UserId] = subject.UserId.Value.ToString(),
                [ArMenuClaimTypes.Email] = subject.Email,
                [ArMenuClaimTypes.EmailVerified] = subject.EmailVerified,
                [ArMenuClaimTypes.Name] = subject.FullName,
                [ArMenuClaimTypes.TenantId] = subject.TenantId.Value.ToString(),
                [ArMenuClaimTypes.Role] = subject.Role.ToString(),
                [ArMenuClaimTypes.SessionId] = subject.SessionId.Value.ToString(),
                [JwtRegisteredClaimNames.Jti] = Guid.CreateVersion7().ToString("N", CultureInfo.InvariantCulture),
            },
        };

        return new AccessToken(TokenHandler.CreateToken(descriptor), expiresAt);
    }
}
