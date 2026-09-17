using ArMenu.Application.Abstractions.Authentication;
using ArMenu.Infrastructure.Authentication;
using Microsoft.Extensions.Options;

namespace ArMenu.IntegrationTests.Authentication;

public sealed class Pbkdf2PasswordHasherTests
{
    private const string Password = "correct-horse-battery-staple";

    [Fact]
    public void A_hash_verifies_the_password_it_was_made_from_and_nothing_else()
    {
        var hasher = CreateHasher();
        var hash = hasher.Hash(Password);

        hasher.Verify(hash, Password).ShouldBe(PasswordVerificationResult.Success);
        hasher.Verify(hash, Password + "!").ShouldBe(PasswordVerificationResult.Failed);
    }

    [Fact]
    public void Hashes_are_salted_and_self_describing()
    {
        var hasher = CreateHasher(iterations: 1_000);

        var first = hasher.Hash(Password);
        var second = hasher.Hash(Password);

        first.ShouldNotBe(second);
        first.ShouldStartWith("pbkdf2-sha512$1000$");
        first.ShouldNotContain(Password);
    }

    [Fact]
    public void Hashes_made_with_fewer_iterations_are_flagged_for_upgrade()
    {
        var legacyHash = CreateHasher(iterations: 1_000).Hash(Password);

        CreateHasher(iterations: 2_000).Verify(legacyHash, Password).ShouldBe(PasswordVerificationResult.SuccessRehashNeeded);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-hash")]
    [InlineData("md5$1000$c2FsdA==$aGFzaA==")]
    [InlineData("pbkdf2-sha512$lots$c2FsdA==$aGFzaA==")]
    [InlineData("pbkdf2-sha512$-5$c2FsdA==$aGFzaA==")]
    [InlineData("pbkdf2-sha512$1000$!!not base64!!$aGFzaA==")]
    public void Malformed_hashes_fail_verification_instead_of_throwing(string storedHash)
    {
        CreateHasher().Verify(storedHash, Password).ShouldBe(PasswordVerificationResult.Failed);
    }

    [Fact]
    public void Decoy_verification_does_real_work_without_side_effects()
    {
        Should.NotThrow(() => CreateHasher().VerifyDecoy(Password));
    }

    [Fact]
    public void Production_default_follows_owasp_guidance()
    {
        new PasswordHashingOptions().Iterations.ShouldBeGreaterThanOrEqualTo(210_000);
    }

    private static Pbkdf2PasswordHasher CreateHasher(int iterations = 1_000) =>
        new(Options.Create(new PasswordHashingOptions { Iterations = iterations }));
}
