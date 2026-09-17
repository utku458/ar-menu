using ArMenu.Application.Abstractions.Authentication;
using ArMenu.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ArMenu.Api.Authentication;

/// <summary>Validates access tokens with exactly the parameters the issuer uses, and nothing looser.</summary>
internal sealed class JwtBearerOptionsSetup(IOptions<JwtOptions> jwtOptions) : IConfigureNamedOptions<JwtBearerOptions>
{
    public void Configure(string? name, JwtBearerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (name != JwtBearerDefaults.AuthenticationScheme)
        {
            return;
        }

        var jwt = jwtOptions.Value;

        // Keep claim names as issued ("sub", "role") instead of legacy SOAP-style URIs.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = jwt.CreateSigningKey(),
            // Pinning the algorithm rules out algorithm-confusion attacks ("alg": "none", RS/HS swaps).
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = ArMenuClaimTypes.Name,
            RoleClaimType = ArMenuClaimTypes.Role,
        };
    }

    public void Configure(JwtBearerOptions options) => Configure(Options.DefaultName, options);
}
