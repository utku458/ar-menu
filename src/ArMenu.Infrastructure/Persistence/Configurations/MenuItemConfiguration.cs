using ArMenu.Domain.Menus;
using ArMenu.Domain.Pricing;
using ArMenu.Domain.Tenants;
using ArMenu.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArMenu.Infrastructure.Persistence.Configurations;

internal sealed class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> builder)
    {
        builder.HasKey(item => new { item.TenantId, item.Id });

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(item => item.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        // Composite foreign key (tenant_id, category_id) -> menu_categories (tenant_id, id): the database itself refuses
        // an item that points at another tenant's category, even if application code has a bug.
        builder.HasOne<MenuCategory>()
            .WithMany()
            .HasForeignKey(item => new { item.TenantId, item.CategoryId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.ComplexProperty(item => item.Price, price =>
        {
            price.Property(money => money.Amount).HasPrecision(Money.Precision, Money.Scale);
        });

        builder.ComplexProperty(item => item.ArModel);

        // Native text[] columns of stable codes. A null allergens column means "not declared", never "none".
        builder.PrimitiveCollection(item => item.Allergens)
            .HasField("_allergens")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .IsRequired(false)
            .ElementType(element => element.HasConversion<AllergenCodeConverter>().HasMaxLength(16));

        builder.PrimitiveCollection(item => item.DietaryLabels)
            .HasField("_dietaryLabels")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .ElementType(element => element.HasConversion<DietaryLabelCodeConverter>().HasMaxLength(16));

        // Serves the public menu query: items of a tenant grouped by category, in display order.
        builder.HasIndex(item => new { item.TenantId, item.CategoryId, item.DisplayOrder })
            .HasFilter("is_deleted = false");
    }
}
