using ArMenu.Domain.UnitTests.TestSupport;
using ArMenu.Domain.Users;

namespace ArMenu.Domain.UnitTests.Users;

public sealed class UserPasswordResetTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Resetting_the_password_ends_the_lockout_and_outdates_earlier_sessions()
    {
        var user = User.Register(Make.EmailAddress(), "Ayşe", "old-hash").ShouldSucceed();
        for (var attempt = 0; attempt < User.MaxFailedSignInAttempts; attempt++)
        {
            user.RecordFailedSignIn(Now);
        }

        user.IsLockedOut(Now).ShouldBeTrue();

        user.ResetPassword("new-hash", Now.AddMinutes(1));

        user.PasswordHash.ShouldBe("new-hash");
        user.IsLockedOut(Now.AddMinutes(1)).ShouldBeFalse();
        user.IsSessionOutdated(Now).ShouldBeTrue();
        user.IsSessionOutdated(Now.AddMinutes(2)).ShouldBeFalse();
    }

    [Fact]
    public void A_reset_link_proves_the_address_and_verification_keeps_its_first_time()
    {
        var user = User.Register(Make.EmailAddress(), "Ayşe", "hash").ShouldSucceed();
        user.IsEmailVerified.ShouldBeFalse();
        user.IsSessionOutdated(Now).ShouldBeFalse();

        user.MarkEmailVerified(Now);
        user.ResetPassword("new-hash", Now.AddDays(1));

        user.EmailVerifiedAt.ShouldBe(Now);
    }
}
