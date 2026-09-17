using ArMenu.Domain.UnitTests.TestSupport;
using ArMenu.Domain.Users;

namespace ArMenu.Domain.UnitTests.Users;

public sealed class UserTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 14, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Register_trims_the_name_and_starts_unlocked()
    {
        var user = User.Register(Make.EmailAddress(), "  Deniz Yılmaz ", "hash").ShouldSucceed();

        user.FullName.ShouldBe("Deniz Yılmaz");
        user.IsLockedOut(Now).ShouldBeFalse();
        user.FailedSignInAttempts.ShouldBe(0);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_requires_a_name(string fullName)
    {
        User.Register(Make.EmailAddress(), fullName, "hash").ShouldFailWith(UserErrors.FullNameRequired);
    }

    [Fact]
    public void Register_without_a_password_hash_is_a_programming_error()
    {
        Should.Throw<ArgumentException>(() => User.Register(Make.EmailAddress(), "Deniz", " "));
    }

    [Fact]
    public void Consecutive_failed_sign_ins_lock_the_account_for_the_lockout_duration()
    {
        var user = NewUser();

        for (var attempt = 1; attempt < User.MaxFailedSignInAttempts; attempt++)
        {
            user.RecordFailedSignIn(Now);
            user.IsLockedOut(Now).ShouldBeFalse();
        }

        user.RecordFailedSignIn(Now);

        user.IsLockedOut(Now).ShouldBeTrue();
        user.IsLockedOut(Now + User.LockoutDuration - TimeSpan.FromSeconds(1)).ShouldBeTrue();
        user.IsLockedOut(Now + User.LockoutDuration).ShouldBeFalse();
    }

    [Fact]
    public void Failures_during_a_lockout_do_not_extend_it()
    {
        var user = NewUser();
        LockOut(user);
        var lockedUntil = user.LockedOutUntil;

        user.RecordFailedSignIn(Now + TimeSpan.FromMinutes(5));

        user.LockedOutUntil.ShouldBe(lockedUntil);
    }

    [Fact]
    public void A_successful_sign_in_resets_failures_and_records_the_time()
    {
        var user = NewUser();
        user.RecordFailedSignIn(Now);
        user.RecordFailedSignIn(Now);

        user.RecordSuccessfulSignIn(Now);

        user.FailedSignInAttempts.ShouldBe(0);
        user.LastSignedInAt.ShouldBe(Now);
    }

    private static User NewUser() => User.Register(Make.EmailAddress(), "Deniz Yılmaz", "hash").ShouldSucceed();

    private static void LockOut(User user)
    {
        for (var attempt = 0; attempt < User.MaxFailedSignInAttempts; attempt++)
        {
            user.RecordFailedSignIn(Now);
        }
    }
}
