using ArMenu.Api.Authentication;
using ArMenu.Application.Abstractions.Authentication;
using ArMenu.Domain.Memberships;
using ArMenu.Domain.Sessions;
using ArMenu.Domain.Tenants;
using ArMenu.Domain.Users;
using ArMenu.Infrastructure.Authentication;
using ArMenu.IntegrationTests.TestSupport;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ArMenu.IntegrationTests.Authentication;

/// <summary>Contract between the token issuer (Infrastructure) and the bearer validation (API).</summary>
public sealed class JwtAccessTokenIssuerTests
{
    private static readonly JwtOptions Settings = new()
    {
        Issuer = TestConfiguration.JwtIssuer,
        Audience = TestConfiguration.JwtAudience,
        SigningKey = TestConfiguration.JwtSigningKey,
        AccessTokenLifetime = TimeSpan.FromMinutes(15),
    };

    private static readonly AccessTokenSubject Subject = new(
        UserId.New(),
        "owner@armenu.test",
        true,
        "Deniz Yılmaz",
        TenantId.New(),
        TenantRole.Manager,
        UserSessionId.New());

    [Fact]
    public async Task Issued_tokens_pass_the_api_validation_and_carry_the_tenant_scope()
    {
        var accessToken = new JwtAccessTokenIssuer(Options.Create(Settings), TimeProvider.System).Issue(Subject);

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(accessToken.Value, ApiValidationParameters());

        result.IsValid.ShouldBeTrue(result.Exception?.Message);
        result.Claims[ArMenuClaimTypes.UserId].ShouldBe(Subject.UserId.Value.ToString());
        result.Claims[ArMenuClaimTypes.TenantId].ShouldBe(Subject.TenantId.Value.ToString());
        result.Claims[ArMenuClaimTypes.Role].ShouldBe(nameof(TenantRole.Manager));
        result.Claims[ArMenuClaimTypes.EmailVerified].ShouldBe(true);
        result.Claims[ArMenuClaimTypes.SessionId].ShouldBe(Subject.SessionId.Value.ToString());
        accessToken.ExpiresAt.ShouldBe(DateTimeOffset.UtcNow + Settings.AccessTokenLifetime, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Tokens_signed_with_another_key_are_rejected()
    {
        var forgerSettings = new JwtOptions
        {
            Issuer = Settings.Issuer,
            Audience = Settings.Audience,
            SigningKey = Convert.ToBase64String(new byte[32]),
        };
        var forged = new JwtAccessTokenIssuer(Options.Create(forgerSettings), TimeProvider.System).Issue(Subject);

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(forged.Value, ApiValidationParameters());

        result.IsValid.ShouldBeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("c2hvcnQta2V5")]
    [InlineData("not base64 at all")]
    public void Weak_or_missing_signing_keys_fail_options_validation(string signingKey)
    {
        var result = new JwtOptionsValidator().Validate(name: null, new JwtOptions
        {
            Issuer = Settings.Issuer,
            Audience = Settings.Audience,
            SigningKey = signingKey,
        });

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("SigningKey");
    }

    private static TokenValidationParameters ApiValidationParameters()
    {
        var bearerOptions = new JwtBearerOptions();
        new JwtBearerOptionsSetup(Options.Create(Settings)).Configure(JwtBearerDefaults.AuthenticationScheme, bearerOptions);
        return bearerOptions.TokenValidationParameters;
    }
}
