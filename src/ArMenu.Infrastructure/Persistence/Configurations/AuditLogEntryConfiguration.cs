using ArMenu.Domain.Tenants;
using ArMenu.Domain.Users;
using ArMenu.Infrastructure.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArMenu.Infrastructure.Persistence.Configurations;

internal sealed class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public const string TableName = "audit_log_entries";

    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.ToTable(TableName);

        // Reads are "a business's latest entries, before a cursor": exactly the primary key's order.
        builder.HasKey(entry => new { entry.TenantId, entry.Id });
        builder.Property(entry => entry.Id).ValueGeneratedNever();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(entry => entry.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        // Accounts are erased, never deleted, so an actor always exists.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(entry => entry.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(entry => entry.Action).HasMaxLength(16);
        builder.Property(entry => entry.SubjectType).HasMaxLength(32);
        builder.Property(entry => entry.Changes).HasColumnType("jsonb");
    }
}
