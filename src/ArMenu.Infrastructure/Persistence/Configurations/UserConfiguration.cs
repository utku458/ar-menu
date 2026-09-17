using ArMenu.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArMenu.Infrastructure.Persistence.Configurations;

/// <summary>
/// Users are global identities (one person, many tenants), so the table has no tenant_id and no row-level security.
/// It is only reached through lookups by id or e-mail; tenant access is governed by memberships.
/// </summary>
internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(user => user.Id);
        builder.Property(user => user.Id).ValueGeneratedNever();

        builder.HasIndex(user => user.Email).IsUnique();

        builder.Property(user => user.FullName).HasMaxLength(User.FullNameMaxLength);
        builder.Property(user => user.PasswordHash).HasMaxLength(256);
    }
}
