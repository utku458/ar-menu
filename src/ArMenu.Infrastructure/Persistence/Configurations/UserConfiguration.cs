using ArMenu.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArMenu.Infrastructure.Persistence.Configurations;

/// <summary>
/// Users are global identities (one person, many tenants), so the table has no tenant_id and no row-level security.
/// It is only reached through lookups by id, e-mail or user name; tenant access is governed by memberships.
/// </summary>
internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(user => user.Id);
        builder.Property(user => user.Id).ValueGeneratedNever();

        builder.HasIndex(user => user.Email).IsUnique();

        // E-mail accounts have no user name. PostgreSQL never counts NULLs as duplicates in a unique index, so the
        // filter is not what keeps them apart; it keeps them out of an index that only user-name sign-in reads.
        builder.HasIndex(user => user.UserName).IsUnique().HasFilter("user_name IS NOT NULL");

        builder.Property(user => user.FullName).HasMaxLength(User.FullNameMaxLength);
        builder.Property(user => user.PasswordHash).HasMaxLength(256);
    }
}
