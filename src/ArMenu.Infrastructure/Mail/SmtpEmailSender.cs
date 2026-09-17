using System.Diagnostics;
using ArMenu.Application.Abstractions.Email;
using ArMenu.Application.Common.Diagnostics;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace ArMenu.Infrastructure.Mail;

/// <summary>
/// Sends e-mail over SMTP with MailKit (System.Net.Mail's SmtpClient is not recommended for new code). One connection per
/// message: invitations are rare, and nothing is left half-open between them.
/// </summary>
internal sealed partial class SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task<bool> TrySendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        var settings = options.Value;

        using var activity = ArMenuTelemetry.ActivitySource.StartActivity("SendEmail", ActivityKind.Client);
        activity?.SetTag("server.address", settings.Host);
        activity?.SetTag("server.port", settings.Port);

        try
        {
            using var mime = ToMime(message, settings);
            using var client = new SmtpClient { Timeout = (int)settings.Timeout.TotalMilliseconds };

            await client.ConnectAsync(settings.Host, settings.Port, settings.Security, cancellationToken);
            if (!string.IsNullOrEmpty(settings.UserName))
            {
                await client.AuthenticateAsync(settings.UserName, settings.Password ?? "", cancellationToken);
            }

            await client.SendAsync(mime, cancellationToken);
            await client.DisconnectAsync(quit: true, cancellationToken);
            return true;
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            // The address is personal data: only the server and the reason are logged.
            activity?.SetStatus(ActivityStatusCode.Error, exception.GetType().Name);
            LogDeliveryFailed(logger, exception, settings.Host, settings.Port);
            return false;
        }
    }

    private static MimeMessage ToMime(EmailMessage message, EmailOptions settings)
    {
        var mime = new MimeMessage
        {
            Subject = message.Subject,
            Body = new BodyBuilder { TextBody = message.TextBody, HtmlBody = message.HtmlBody }.ToMessageBody(),
        };

        mime.From.Add(new MailboxAddress(settings.FromName, settings.FromAddress));
        mime.To.Add(MailboxAddress.Parse(message.To));
        mime.Headers.Add(HeaderId.ContentLanguage, message.Language);
        // Tells auto-responders (vacation replies) not to answer a message nobody reads.
        mime.Headers.Add("Auto-Submitted", "auto-generated");

        return mime;
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "E-mail delivery through {Host}:{Port} failed")]
    private static partial void LogDeliveryFailed(ILogger logger, Exception exception, string host, int port);
}
