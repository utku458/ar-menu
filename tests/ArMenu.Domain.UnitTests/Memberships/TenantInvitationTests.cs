using ArMenu.Domain.Memberships;
using ArMenu.Domain.Tenants;
using ArMenu.Domain.UnitTests.TestSupport;
using ArMenu.Domain.Users;

namespace ArMenu.Domain.UnitTests.Memberships;

public sealed class TenantInvitationTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 9, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(TenantRole.Manager)]
    [InlineData(TenantRole.Staff)]
    public void Managers_and_staff_can_be_invited_for_a_week(TenantRole role)
    {
        var invitation = Invite(Make.InvitationHash(), role);

        invitation.Status.ShouldBe(InvitationStatus.Pending);
        invitation.Role.ShouldBe(role);
        invitation.ExpiresAt.ShouldBe(Now + TimeSpan.FromDays(7));
        invitation.IsOpen(Now).ShouldBeTrue();
    }

    [Theory]
    [InlineData(TenantRole.Owner)]
    [InlineData((TenantRole)42)]
    public void Nobody_is_invited_to_be_owner(TenantRole role) =>
        TenantInvitation.Create(TenantId.New(), Make.EmailAddress(), role, UserId.New(), Make.InvitationHash(), Now)
            .ShouldFailWith(InvitationErrors.OwnerNotInvitable);

    [Fact]
    public void Accepting_with_the_link_ends_the_invitation()
    {
        var hash = Make.InvitationHash();
        var invitation = Invite(hash);

        invitation.Accept(hash, Now.AddDays(2)).ShouldSucceed();

        invitation.Status.ShouldBe(InvitationStatus.Accepted);
        invitation.RespondedAt.ShouldBe(Now.AddDays(2));
        invitation.Accept(hash, Now.AddDays(2)).ShouldFailWith(InvitationErrors.NoLongerPending);
    }

    [Fact]
    public void A_wrong_secret_is_indistinguishable_from_an_unknown_invitation()
    {
        var invitation = Invite(Make.InvitationHash());

        invitation.Accept(Make.InvitationHash(), Now).ShouldFailWith(InvitationErrors.InvalidLink);
        invitation.Status.ShouldBe(InvitationStatus.Pending);
    }

    [Fact]
    public void An_expired_link_cannot_be_used()
    {
        var hash = Make.InvitationHash();
        var invitation = Invite(hash);

        invitation.Accept(hash, Now + TenantInvitation.Lifetime).ShouldFailWith(InvitationErrors.Expired);
        invitation.IsOpen(Now + TenantInvitation.Lifetime).ShouldBeFalse();
    }

    [Fact]
    public void Sending_again_replaces_the_link_and_the_deadline()
    {
        var original = Make.InvitationHash();
        var replacement = Make.InvitationHash();
        var invitation = Invite(original);
        var later = Now.AddDays(10);

        invitation.Renew(replacement, later).ShouldSucceed();

        invitation.SentAt.ShouldBe(later);
        invitation.ExpiresAt.ShouldBe(later + TenantInvitation.Lifetime);
        invitation.Accept(original, later).ShouldFailWith(InvitationErrors.InvalidLink);
        invitation.Accept(replacement, later).ShouldSucceed();
    }

    [Fact]
    public void A_revoked_invitation_can_neither_be_used_nor_sent_again()
    {
        var hash = Make.InvitationHash();
        var invitation = Invite(hash);

        invitation.Revoke(Now).ShouldSucceed();

        invitation.Status.ShouldBe(InvitationStatus.Revoked);
        invitation.Accept(hash, Now).ShouldFailWith(InvitationErrors.NoLongerPending);
        invitation.Renew(Make.InvitationHash(), Now).ShouldFailWith(InvitationErrors.NoLongerPending);
        invitation.Revoke(Now).ShouldFailWith(InvitationErrors.NoLongerPending);
    }

    private static TenantInvitation Invite(InvitationTokenHash hash, TenantRole role = TenantRole.Staff) =>
        TenantInvitation.Create(TenantId.New(), Make.EmailAddress("chef@armenu.test"), role, UserId.New(), hash, Now).ShouldSucceed();
}
