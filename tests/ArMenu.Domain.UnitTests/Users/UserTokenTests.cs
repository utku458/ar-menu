using ArMenu.Domain.UnitTests.TestSupport;
using ArMenu.Domain.Users;

namespace ArMenu.Domain.UnitTests.Users;

public sealed class UserTokenTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 9, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(UserTokenPurpose.PasswordReset, 1)]
    [InlineData(UserTokenPurpose.EmailVerification, 72)]
    public void Links_expire_according_to_what_they_allow(UserTokenPurpose purpose, int hours) =>
        UserToken.Issue(UserId.New(), purpose, Make.UserTokenHash(), Now).ExpiresAt.ShouldBe(Now.AddHours(hours));

    [Fact]
    public void A_link_is_used_once()
    {
        var hash = Make.UserTokenHash();
        var token = UserToken.Issue(UserId.New(), UserTokenPurpose.PasswordReset, hash, Now);

        token.Consume(UserTokenPurpose.PasswordReset, hash, Now.AddMinutes(5)).ShouldSucceed();

        token.ConsumedAt.ShouldBe(Now.AddMinutes(5));
        token.Consume(UserTokenPurpose.PasswordReset, hash, Now.AddMinutes(6)).ShouldFailWith(UserTokenErrors.AlreadyUsed);
    }

    [Fact]
    public void A_wrong_secret_or_purpose_is_an_invalid_link()
    {
        var hash = Make.UserTokenHash();
        var token = UserToken.Issue(UserId.New(), UserTokenPurpose.EmailVerification, hash, Now);

        token.Consume(UserTokenPurpose.EmailVerification, Make.UserTokenHash(), Now).ShouldFailWith(UserTokenErrors.InvalidLink);
        token.Consume(UserTokenPurpose.PasswordReset, hash, Now).ShouldFailWith(UserTokenErrors.InvalidLink);
        token.ConsumedAt.ShouldBeNull();
    }

    [Fact]
    public void Expired_and_superseded_links_do_nothing()
    {
        var hash = Make.UserTokenHash();
        var expired = UserToken.Issue(UserId.New(), UserTokenPurpose.PasswordReset, hash, Now);
        var superseded = UserToken.Issue(UserId.New(), UserTokenPurpose.PasswordReset, hash, Now);

        superseded.Supersede(Now);

        expired.Consume(UserTokenPurpose.PasswordReset, hash, Now + UserToken.PasswordResetLifetime).ShouldFailWith(UserTokenErrors.Expired);
        superseded.Consume(UserTokenPurpose.PasswordReset, hash, Now).ShouldFailWith(UserTokenErrors.AlreadyUsed);
    }
}
