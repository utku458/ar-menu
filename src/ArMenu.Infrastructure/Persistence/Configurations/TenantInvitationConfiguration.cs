using ArMenu.Domain.Memberships;
using ArMenu.Domain.Tenants;
using ArMenu.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArMenu.Infrastructure.Persistence.Configurations;

internal sealed class TenantInvitationConfiguration : IEntityTypeConfiguration<TenantInvitation>
{
    public void Configure(EntityTypeBuilder<TenantInvitation> builder)
    {
        builder.HasKey(invitation => new { invitation.TenantId, invitation.Id });

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(invitation => invitation.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(invitation => invitation.InvitedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // At most one pending invitation per address per business, even when two owners click at once.
        builder.HasIndex(invitation => new { invitation.TenantId, invitation.Email })
            .IsUnique()
            .HasFilter("status = 'Pending'");

        builder.HasIndex(invitation => invitation.InvitedBy);
    }
}
