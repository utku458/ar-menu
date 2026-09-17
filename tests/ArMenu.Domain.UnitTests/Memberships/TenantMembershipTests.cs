using ArMenu.Domain.Memberships;
using ArMenu.Domain.Tenants;
using ArMenu.Domain.UnitTests.TestSupport;
using ArMenu.Domain.Users;

namespace ArMenu.Domain.UnitTests.Memberships;

public sealed class TenantMembershipTests
{
    [Fact]
    public void Members_move_between_manager_and_staff()
    {
        var membership = TenantMembership.Create(TenantId.New(), UserId.New(), TenantRole.Staff);

        membership.ChangeRole(TenantRole.Manager).ShouldSucceed();

        membership.Role.ShouldBe(TenantRole.Manager);
        membership.EnsureRemovable().ShouldSucceed();
    }

    [Theory]
    [InlineData(TenantRole.Owner)]
    [InlineData((TenantRole)42)]
    public void Nobody_is_made_owner_by_a_role_change(TenantRole role) =>
        TenantMembership.Create(TenantId.New(), UserId.New(), TenantRole.Manager).ChangeRole(role)
            .ShouldFailWith(MembershipErrors.RoleNotAssignable);

    [Fact]
    public void The_owner_keeps_their_role_and_their_place()
    {
        var owner = TenantMembership.Create(TenantId.New(), UserId.New(), TenantRole.Owner);

        owner.ChangeRole(TenantRole.Staff).ShouldFailWith(MembershipErrors.OwnerUnchangeable);
        owner.EnsureRemovable().ShouldFailWith(MembershipErrors.OwnerUnchangeable);
        owner.Role.ShouldBe(TenantRole.Owner);
    }

    [Theory]
    [InlineData(TenantRole.Manager)]
    [InlineData(TenantRole.Staff)]
    public void The_owner_hands_the_business_over_and_stays_as_a_manager(TenantRole successorRole)
    {
        var tenantId = TenantId.New();
        var owner = TenantMembership.Create(tenantId, UserId.New(), TenantRole.Owner);
        var successor = TenantMembership.Create(tenantId, UserId.New(), successorRole);

        owner.HandOverOwnershipTo(successor).ShouldSucceed();

        successor.Role.ShouldBe(TenantRole.Owner);
        owner.Role.ShouldBe(TenantRole.Manager);
        owner.HandOverOwnershipTo(successor).ShouldFailWith(MembershipErrors.NotOwner);
    }

    [Fact]
    public void Ownership_cannot_go_to_the_owner_or_leave_the_business()
    {
        var tenantId = TenantId.New();
        var owner = TenantMembership.Create(tenantId, UserId.New(), TenantRole.Owner);

        owner.HandOverOwnershipTo(owner).ShouldFailWith(MembershipErrors.AlreadyOwner);
        owner.Role.ShouldBe(TenantRole.Owner);

        var elsewhere = TenantMembership.Create(TenantId.New(), UserId.New(), TenantRole.Manager);
        Should.Throw<InvalidOperationException>(() => owner.HandOverOwnershipTo(elsewhere));
    }
}
