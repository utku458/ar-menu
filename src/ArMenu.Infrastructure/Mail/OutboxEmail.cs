using ArMenu.Application.Abstractions.Email;

namespace ArMenu.Infrastructure.Mail;

/// <summary>
/// A message waiting to be delivered. It holds an address and, for account e-mails, a link secret, so it lives only until
/// it is delivered or given up on: rows are deleted then, never kept as a log.
/// </summary>
internal sealed class OutboxEmail
{
    private OutboxEmail()
    {
    }

    /// <summary>A version 7 UUID, so messages queued together keep their order.</summary>
    public Guid Id { get; private init; }

    public string Template { get; private init; } = "";

    public string To { get; private init; } = "";

    public string Subject { get; private init; } = "";

    public string TextBody { get; private init; } = "";

    public string HtmlBody { get; private init; } = "";

    public string Language { get; private init; } = "";

    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>Earliest time a worker may try again.</summary>
    public DateTimeOffset AvailableAt { get; private set; }

    /// <summary>Until then, the worker that claimed the message owns it; an expired lease means the worker died.</summary>
    public DateTimeOffset? LeaseExpiresAt { get; private set; }

    public int Attempts { get; private set; }

    public EmailMessage Message => new(To, Subject, TextBody, HtmlBody, Language);

    public static OutboxEmail For(EmailMessage message, string template, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(template);

        return new OutboxEmail
        {
            Id = Guid.CreateVersion7(now),
            Template = template,
            To = message.To,
            Subject = message.Subject,
            TextBody = message.TextBody,
            HtmlBody = message.HtmlBody,
            Language = message.Language,
            CreatedAt = now,
            AvailableAt = now,
        };
    }
}
