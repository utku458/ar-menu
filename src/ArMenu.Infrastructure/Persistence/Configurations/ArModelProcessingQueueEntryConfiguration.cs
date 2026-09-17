using ArMenu.Domain.ArModels;
using ArMenu.Infrastructure.ArModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArMenu.Infrastructure.Persistence.Configurations;

internal sealed class ArModelProcessingQueueEntryConfiguration : IEntityTypeConfiguration<ArModelProcessingQueueEntry>
{
    public const string TableName = "ar_model_processing_queue";

    public void Configure(EntityTypeBuilder<ArModelProcessingQueueEntry> builder)
    {
        builder.ToTable(TableName);
        builder.HasKey(entry => entry.ProcessingId);

        // An entry always points at a real processing of the same tenant.
        builder.HasOne<ArModelProcessing>()
            .WithOne()
            .HasForeignKey<ArModelProcessingQueueEntry>(entry => new { entry.TenantId, entry.ProcessingId })
            .OnDelete(DeleteBehavior.Cascade);

        // Serves the claim query: the earliest entry that is due.
        builder.HasIndex(entry => entry.AvailableAt);
    }
}
