using ArMenu.Domain.Localization;
using ArMenu.Domain.Tenants;
using ArMenu.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArMenu.Infrastructure.Persistence.Configurations;

internal sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.HasKey(tenant => tenant.Id);
        builder.Property(tenant => tenant.Id).ValueGeneratedNever();

        builder.Property(tenant => tenant.Name).HasMaxLength(Tenant.NameMaxLength);

        // Two nullable columns on the tenant row: the appearance is read on every menu request, never on its own.
        builder.ComplexProperty(tenant => tenant.Branding);

        // Resolved on every public menu request (before caching kicks in): must be a unique index lookup.
        builder.HasIndex(tenant => tenant.Slug).IsUnique();

        // Stored as a native text[] column.
        builder.PrimitiveCollection(tenant => tenant.SupportedCultures)
            .HasField("_supportedCultures")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .ElementType(element => element.HasConversion<CultureCodeConverter>().HasMaxLength(CultureCode.MaxLength));
    }
}
