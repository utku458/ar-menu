using ArMenu.Domain.ArModels;
using ArMenu.Domain.Menus;
using ArMenu.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArMenu.Infrastructure.Persistence.Configurations;

internal sealed class ArModelProcessingConfiguration : IEntityTypeConfiguration<ArModelProcessing>
{
    public void Configure(EntityTypeBuilder<ArModelProcessing> builder)
    {
        builder.HasKey(processing => new { processing.TenantId, processing.Id });

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(processing => processing.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<MenuItem>()
            .WithMany()
            .HasForeignKey(processing => new { processing.TenantId, processing.MenuItemId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(processing => processing.FailureCode).HasMaxLength(ArModelProcessing.FailureCodeMaxLength);

        // Read whole and written whole: one jsonb document instead of a dozen columns nothing queries.
        builder.ComplexProperty(processing => processing.Report, report => report.ToJson());

        builder.Ignore(processing => processing.IsFinished);

        // Serves "the latest processing of an item" and "unfinished processings of an item".
        builder.HasIndex(processing => new { processing.TenantId, processing.MenuItemId, processing.CreatedAt });
    }
}
