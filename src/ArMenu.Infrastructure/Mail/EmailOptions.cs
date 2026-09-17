using MailKit.Security;
using Microsoft.Extensions.Options;

namespace ArMenu.Infrastructure.Mail;

/// <summary>The SMTP server transactional e-mail goes through: Mailpit locally, a provider's relay in production.</summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string Host { get; set; } = "";

    public int Port { get; set; } = 587;

    /// <summary>TLS mode; <c>StartTls</c> by default, so credentials never travel in the clear.</summary>
    public SecureSocketOptions Security { get; set; } = SecureSocketOptions.StartTls;

    public string? UserName { get; set; }

    public string? Password { get; set; }

    public string FromAddress { get; set; } = "";

    public string FromName { get; set; } = "ArMenu";

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Whether this instance delivers queued e-mail. Every instance may; they share the outbox safely.</summary>
    public bool OutboxWorkerEnabled { get; set; } = true;

    /// <summary>How often the outbox is checked for messages queued by other instances or due for another attempt.</summary>
    public TimeSpan OutboxPollInterval { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>How long a claimed message is reserved for its worker; longer than one SMTP attempt can take.</summary>
    public TimeSpan OutboxLease { get; set; } = TimeSpan.FromMinutes(2);
}

internal sealed class EmailOptionsValidator : IValidateOptions<EmailOptions>
{
    public ValidateOptionsResult Validate(string? name, EmailOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        List<string> failures = [];

        if (string.IsNullOrWhiteSpace(options.Host))
        {
            failures.Add($"{EmailOptions.SectionName}:Host is required.");
        }

        if (options.Port is < 1 or > 65535)
        {
            failures.Add($"{EmailOptions.SectionName}:Port must be between 1 and 65535.");
        }

        if (ArMenu.Domain.Users.Email.Create(options.FromAddress).IsFailure)
        {
            failures.Add($"{EmailOptions.SectionName}:FromAddress must be an e-mail address.");
        }

        if (options.Timeout <= TimeSpan.Zero)
        {
            failures.Add($"{EmailOptions.SectionName}:Timeout must be positive.");
        }

        if (options.OutboxPollInterval <= TimeSpan.Zero || options.OutboxLease <= options.Timeout)
        {
            failures.Add($"{EmailOptions.SectionName}:OutboxPollInterval must be positive and OutboxLease longer than Timeout.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
