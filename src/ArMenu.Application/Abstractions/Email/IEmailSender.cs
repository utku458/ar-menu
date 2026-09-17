namespace ArMenu.Application.Abstractions.Email;

/// <summary>Delivers transactional e-mail.</summary>
public interface IEmailSender
{
    /// <summary>
    /// Hands the message to the mail server. Returns <see langword="false"/> when it could not be delivered; the
    /// implementation logs why. Callers decide what a lost e-mail means for them, typically offering to send again.
    /// </summary>
    Task<bool> TrySendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}

/// <param name="To">Recipient address.</param>
/// <param name="Subject">Plain-text subject.</param>
/// <param name="TextBody">The whole message as plain text, for clients that do not render HTML.</param>
/// <param name="HtmlBody">The same message as HTML; every dynamic value in it must already be encoded.</param>
/// <param name="Language">BCP 47 language of the content, such as <c>tr</c>.</param>
public sealed record EmailMessage(string To, string Subject, string TextBody, string HtmlBody, string Language)
{
    public override string ToString() => $"EmailMessage {{ Subject = {Subject} }}";
}
