using ArMenu.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArMenu.Infrastructure.Persistence.Configurations;

/// <summary>Links e-mailed to users belong to global identities, so, like users, the table has no tenant.</summary>
internal sealed class UserTokenConfiguration : IEntityTypeConfiguration<UserToken>
{
    public void Configure(EntityTypeBuilder<UserToken> builder)
    {
        builder.HasKey(token => token.Id);
        builder.Property(token => token.Id).ValueGeneratedNever();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Serves "the unused links of a user for a purpose", looked up whenever a new link replaces them.
        builder.HasIndex(token => new { token.UserId, token.Purpose }).HasFilter("consumed_at IS NULL");
    }
}
