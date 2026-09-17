using ArMenu.Domain.Memberships;
using ArMenu.Domain.Sessions;
using ArMenu.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArMenu.Infrastructure.Persistence.Configurations;

internal sealed class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.HasKey(session => new { session.TenantId, session.Id });

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(session => session.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        // A session cannot outlive the membership it was issued for: removing a member ends all of their sessions.
        builder.HasOne<TenantMembership>()
            .WithMany()
            .HasForeignKey(session => new { session.TenantId, session.UserId })
            .HasPrincipalKey(membership => new { membership.TenantId, membership.UserId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
