using ArMenu.Domain.Menus;
using ArMenu.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArMenu.Infrastructure.Persistence.Configurations;

internal sealed class MenuCategoryConfiguration : IEntityTypeConfiguration<MenuCategory>
{
    public void Configure(EntityTypeBuilder<MenuCategory> builder)
    {
        // Tenant-leading composite key: every tenant-filtered lookup is an index seek, the key is the natural target for
        // tenant-safe composite foreign keys, and tenant_id is already the partition/distribution key if we ever shard.
        builder.HasKey(category => new { category.TenantId, category.Id });

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(category => category.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(category => new { category.TenantId, category.DisplayOrder })
            .HasFilter("is_deleted = false");
    }
}
