using ArMenu.Domain.Memberships;
using ArMenu.Domain.Tenants;
using ArMenu.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArMenu.Infrastructure.Persistence.Configurations;

internal sealed class TenantMembershipConfiguration : IEntityTypeConfiguration<TenantMembership>
{
    public void Configure(EntityTypeBuilder<TenantMembership> builder)
    {
        builder.HasKey(membership => new { membership.TenantId, membership.Id });

        // One membership per user per tenant; also the target of the sessions' composite foreign key.
        builder.HasAlternateKey(membership => new { membership.TenantId, membership.UserId });

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(membership => membership.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(membership => membership.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(membership => membership.UserId);
    }
}
