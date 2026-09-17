using ArMenu.Domain.Sessions;
using ArMenu.Infrastructure.Authentication;

namespace ArMenu.IntegrationTests.Authentication;

public sealed class RefreshTokenCodecTests
{
    private readonly RefreshTokenCodec _codec = new();

    [Fact]
    public void Encoded_tokens_decode_to_their_session_and_the_stored_hash()
    {
        var sessionId = UserSessionId.New();
        var secret = _codec.GenerateSecret();

        var token = _codec.Encode(sessionId, secret);

        _codec.TryDecode(token, out var decodedSessionId, out var secretHash).ShouldBeTrue();
        decodedSessionId.ShouldBe(sessionId);
        secretHash!.Matches(secret.Hash).ShouldBeTrue();
        token.ShouldNotContain(secret.Hash.Value);
    }

    [Fact]
    public void Secrets_are_unique_256_bit_values()
    {
        var first = _codec.GenerateSecret();
        var second = _codec.GenerateSecret();

        first.Value.ShouldNotBe(second.Value);
        first.Value.Length.ShouldBe(43);
        first.ToString().ShouldNotContain(first.Value);
    }

    [Fact]
    public void A_tampered_secret_does_not_match_the_stored_hash()
    {
        var sessionId = UserSessionId.New();
        var secret = _codec.GenerateSecret();
        var token = _codec.Encode(sessionId, secret);

        // Tamper with a middle character: all six of its bits are significant, unlike the final character of the secret.
        var position = token.Length - 10;
        var tampered = string.Concat(token.AsSpan(0, position), token[position] == 'A' ? "B" : "A", token.AsSpan(position + 1));

        _codec.TryDecode(tampered, out _, out var tamperedHash).ShouldBeTrue();
        tamperedHash!.Matches(secret.Hash).ShouldBeFalse();
    }

    public static TheoryData<string> MalformedTokens { get; } = new()
    {
        "not-a-token",
        SessionPart,
        $"{SessionPart}:{new string('A', 43)}",
        $"zz{SessionPart[2..]}.{new string('A', 43)}",
        $"{SessionPart}.{new string('A', 42)}+",
        $"{SessionPart}.{new string('A', 44)}",
        $"{SessionPart}.{new string('A', 42)}",

        // Non-canonical encoding (unused low bits set in the final character): the same secret must have one spelling only.
        $"{SessionPart}.{new string('A', 42)}B",
    };

    private static string SessionPart => "0198a1f2c0de7c3b9a1e5f6a7b8c9d0e";

    [Fact]
    public void The_well_formed_counterpart_of_the_malformed_samples_is_accepted()
    {
        _codec.TryDecode($"{SessionPart}.{new string('A', 43)}", out _, out _).ShouldBeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [MemberData(nameof(MalformedTokens))]
    public void Malformed_tokens_are_rejected(string? token)
    {
        _codec.TryDecode(token, out _, out var secretHash).ShouldBeFalse();
        secretHash.ShouldBeNull();
    }
}
