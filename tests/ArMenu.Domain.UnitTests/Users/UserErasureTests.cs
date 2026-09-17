using ArMenu.Domain.UnitTests.TestSupport;
using ArMenu.Domain.Users;

namespace ArMenu.Domain.UnitTests.Users;

public sealed class UserErasureTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Erasing_an_account_leaves_nothing_that_identifies_the_person()
    {
        var user = User.Register(Make.EmailAddress("ayse@armenu.test"), "Ayşe Yılmaz", "hash").ShouldSucceed();
        user.MarkEmailVerified(Now.AddDays(-3));
        user.RecordSuccessfulSignIn(Now.AddHours(-1));

        user.Erase(Now);

        user.IsErased.ShouldBeTrue();
        user.ErasedAt.ShouldBe(Now);
        user.FullName.ShouldBeEmpty();
        user.Email.Value.ShouldBe($"erased-{user.Id.Value:N}@erased.invalid");
        user.PasswordHash.ShouldNotBe("hash");
        user.EmailVerifiedAt.ShouldBeNull();
        user.LastSignedInAt.ShouldBeNull();
        user.IsSessionOutdated(Now.AddSeconds(-1)).ShouldBeTrue();
    }

    [Fact]
    public void Erasing_twice_keeps_the_first_time()
    {
        var user = User.Register(Make.EmailAddress(), "Ayşe", "hash").ShouldSucceed();

        user.Erase(Now);
        user.Erase(Now.AddDays(1));

        user.ErasedAt.ShouldBe(Now);
    }
}
