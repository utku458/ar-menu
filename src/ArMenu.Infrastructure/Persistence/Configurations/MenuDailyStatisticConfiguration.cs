using ArMenu.Domain.Tenants;
using ArMenu.Infrastructure.Statistics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArMenu.Infrastructure.Persistence.Configurations;

internal sealed class MenuDailyStatisticConfiguration : IEntityTypeConfiguration<MenuDailyStatistic>
{
    public const string TableName = "menu_daily_statistics";

    public void Configure(EntityTypeBuilder<MenuDailyStatistic> builder)
    {
        builder.ToTable(TableName);

        // The upsert's conflict target, and the order every read scans in (a tenant's days).
        builder.HasKey(statistic => new { statistic.TenantId, statistic.Day, statistic.Event, statistic.MenuItemId });

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(statistic => statistic.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(statistic => statistic.Event).HasMaxLength(32);
    }
}
