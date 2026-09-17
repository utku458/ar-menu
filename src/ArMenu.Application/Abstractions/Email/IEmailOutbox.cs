namespace ArMenu.Application.Abstractions.Email;

/// <summary>
/// E-mail sent in the background, saved with the unit of work that asks for it: the message exists exactly when the
/// change it announces was committed, survives restarts, and is delivered, with retries, by whichever instance is free.
/// Also for messages whose sending must not be observable in the response (a reset e-mail must not reveal, by timing,
/// that an address has an account).
/// </summary>
public interface IEmailOutbox
{
    /// <summary>Queues the message; it is written by the next save. <paramref name="template"/> names its kind for metrics.</summary>
    void Add(EmailMessage message, string template);
}
