using ArMenu.Domain.Sessions;
using ArMenu.Domain.Tenants;
using ArMenu.Domain.UnitTests.TestSupport;
using ArMenu.Domain.Users;

namespace ArMenu.Domain.UnitTests.Sessions;

public sealed class UserSessionTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 14, 10, 0, 0, TimeSpan.Zero);
    private static readonly SessionLifetime Lifetime = new(idleTimeout: TimeSpan.FromDays(14), absoluteLifetime: TimeSpan.FromDays(30));

    [Fact]
    public void Start_sets_the_idle_and_absolute_deadlines()
    {
        var session = Start(Make.Hash());

        session.ExpiresAt.ShouldBe(Now + Lifetime.IdleTimeout);
        session.AbsoluteExpiresAt.ShouldBe(Now + Lifetime.AbsoluteLifetime);
        session.IsActive(Now).ShouldBeTrue();
    }

    [Fact]
    public void Rotate_replaces_the_token_and_slides_the_idle_deadline()
    {
        var original = Make.Hash();
        var replacement = Make.Hash();
        var session = Start(original);
        var later = Now + TimeSpan.FromDays(3);

        session.Rotate(original, replacement, later, Lifetime).ShouldSucceed();

        session.IsCurrentRefreshToken(replacement).ShouldBeTrue();
        session.IsCurrentRefreshToken(original).ShouldBeFalse();
        session.ExpiresAt.ShouldBe(later + Lifetime.IdleTimeout);
    }

    [Fact]
    public void Rotation_never_extends_a_session_beyond_its_absolute_lifetime()
    {
        var token = Make.Hash();
        var session = Start(token);

        // Refreshed every 10 days: always within the idle timeout, so only the absolute cap can end the session.
        for (var day = 10; day < 30; day += 10)
        {
            var next = Make.Hash();
            session.Rotate(token, next, Now + TimeSpan.FromDays(day), Lifetime).ShouldSucceed();
            token = next;
        }

        session.ExpiresAt.ShouldBe(session.AbsoluteExpiresAt);
        session.Rotate(token, Make.Hash(), session.AbsoluteExpiresAt, Lifetime).ShouldFailWith(SessionErrors.Expired);
    }

    [Fact]
    public void Presenting_the_just_replaced_token_during_the_grace_period_is_rejected_without_revoking()
    {
        var original = Make.Hash();
        var replacement = Make.Hash();
        var session = Start(original);
        session.Rotate(original, replacement, Now, Lifetime).ShouldSucceed();

        session.Rotate(original, Make.Hash(), Now + TimeSpan.FromSeconds(5), Lifetime)
            .ShouldFailWith(SessionErrors.RefreshTokenSuperseded);

        session.RevokedAt.ShouldBeNull();
        session.IsCurrentRefreshToken(replacement).ShouldBeTrue();
    }

    [Fact]
    public void Replaying_a_replaced_token_after_the_grace_period_revokes_the_whole_session()
    {
        var original = Make.Hash();
        var replacement = Make.Hash();
        var session = Start(original);
        session.Rotate(original, replacement, Now, Lifetime).ShouldSucceed();
        var replayedAt = Now + UserSession.ConcurrentRefreshGracePeriod;

        session.Rotate(original, Make.Hash(), replayedAt, Lifetime).ShouldFailWith(SessionErrors.RefreshTokenReused);

        session.RevokedAt.ShouldBe(replayedAt);
        session.RevocationReason.ShouldBe(SessionRevocationReason.RefreshTokenReused);
        session.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<RefreshTokenReuseDetectedDomainEvent>();

        // The legitimate holder of the newest token is signed out as well: nobody can tell who the thief is.
        session.Rotate(replacement, Make.Hash(), replayedAt, Lifetime).ShouldFailWith(SessionErrors.Revoked);
    }

    [Fact]
    public void Replaying_an_older_token_revokes_the_session_even_inside_the_grace_period()
    {
        var first = Make.Hash();
        var second = Make.Hash();
        var third = Make.Hash();
        var session = Start(first);
        session.Rotate(first, second, Now, Lifetime).ShouldSucceed();
        session.Rotate(second, third, Now, Lifetime).ShouldSucceed();

        session.Rotate(first, Make.Hash(), Now, Lifetime).ShouldFailWith(SessionErrors.RefreshTokenReused);

        session.RevokedAt.ShouldNotBeNull();
    }

    [Fact]
    public void An_unknown_token_for_the_session_is_treated_as_reuse()
    {
        var session = Start(Make.Hash());

        session.Rotate(Make.Hash(), Make.Hash(), Now, Lifetime).ShouldFailWith(SessionErrors.RefreshTokenReused);

        session.IsActive(Now).ShouldBeFalse();
    }

    [Fact]
    public void Idle_sessions_expire()
    {
        var token = Make.Hash();
        var session = Start(token);

        session.Rotate(token, Make.Hash(), Now + Lifetime.IdleTimeout, Lifetime).ShouldFailWith(SessionErrors.Expired);
    }

    [Fact]
    public void Revoke_is_idempotent_and_keeps_the_first_reason()
    {
        var session = Start(Make.Hash());

        session.Revoke(Now, SessionRevocationReason.SignedOut);
        session.Revoke(Now + TimeSpan.FromMinutes(1), SessionRevocationReason.AccessRevoked);

        session.RevokedAt.ShouldBe(Now);
        session.RevocationReason.ShouldBe(SessionRevocationReason.SignedOut);
    }

    [Fact]
    public void Lifetime_idle_timeout_cannot_exceed_the_absolute_lifetime()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new SessionLifetime(TimeSpan.FromDays(31), TimeSpan.FromDays(30)));
        Should.Throw<ArgumentOutOfRangeException>(() => new SessionLifetime(TimeSpan.Zero, TimeSpan.FromDays(30)));
    }

    [Fact]
    public void Refresh_token_hashes_only_accept_sha256_base64url_digests()
    {
        RefreshTokenHash.Create("too-short").ShouldFailWith(SessionErrors.RefreshTokenHashInvalid);
        RefreshTokenHash.Create(new string('+', RefreshTokenHash.Length)).ShouldFailWith(SessionErrors.RefreshTokenHashInvalid);
        Make.Hash().ToString().ShouldNotContain(Make.Hash().Value);
    }

    private static UserSession Start(RefreshTokenHash hash) =>
        UserSession.Start(TenantId.New(), UserId.New(), hash, Now, Lifetime);
}
