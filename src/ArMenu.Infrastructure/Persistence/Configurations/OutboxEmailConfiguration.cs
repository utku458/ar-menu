using ArMenu.Infrastructure.Mail;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArMenu.Infrastructure.Persistence.Configurations;

/// <summary>Messages to people, not to businesses: like users, the table has no tenant.</summary>
internal sealed class OutboxEmailConfiguration : IEntityTypeConfiguration<OutboxEmail>
{
    public const string TableName = "email_outbox";

    public void Configure(EntityTypeBuilder<OutboxEmail> builder)
    {
        builder.ToTable(TableName);
        builder.HasKey(email => email.Id);
        builder.Property(email => email.Id).ValueGeneratedNever();
        builder.Ignore(email => email.Message);

        builder.Property(email => email.Template).HasMaxLength(64);
        builder.Property(email => email.To).HasMaxLength(ArMenu.Domain.Users.Email.MaxLength);
        builder.Property(email => email.Subject).HasMaxLength(256);
        builder.Property(email => email.Language).HasMaxLength(12);

        // Serves the claim query: the earliest message that is due.
        builder.HasIndex(email => email.AvailableAt);
    }
}
