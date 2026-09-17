using System.Text.Encodings.Web;
using System.Text.Unicode;
using ArMenu.Application.Abstractions.Email;

namespace ArMenu.Application.Emails;

/// <summary>
/// The one layout of every transactional e-mail: a greeting, a few sentences, at most one button, and notes. Callers pass plain
/// text; everything is HTML-encoded here, so a business name can never inject markup.
/// </summary>
internal static class EmailLayout
{
    // Encodes what HTML requires and nothing more: Turkish letters stay readable in the source of the message.
    private static readonly HtmlEncoder Html = HtmlEncoder.Create(UnicodeRanges.All);

    public static EmailMessage Compose(
        string to,
        string language,
        string subject,
        string greeting,
        string body,
        string? action,
        Uri? actionUrl,
        IReadOnlyList<string> notes)
    {
        string[] paragraphs = action is not null && actionUrl is not null
            ? [greeting, body, $"{action}: {actionUrl.AbsoluteUri}", string.Join("\n", notes)]
            : [greeting, body, string.Join("\n", notes)];
        var textBody = string.Join("\n\n", paragraphs);
        var htmlAction = action is not null && actionUrl is not null
            ? $"""<p style="margin:0 0 24px"><a href="{Encode(actionUrl.AbsoluteUri)}" style="display:inline-block;background:#c2410c;color:#ffffff;text-decoration:none;font-weight:600;padding:12px 20px;border-radius:8px">{Encode(action)}</a></p>"""
            : string.Empty;

        var htmlNotes = string.Concat(notes.Select(note =>
            $"""<p style="margin:0 0 8px;font-size:13px;color:#57534e;line-height:1.5">{Encode(note)}</p>"""));
        var htmlBody = $"""
            <!doctype html>
            <html lang="{Encode(language)}">
            <body style="margin:0;padding:24px;background:#f6f4ef;font-family:-apple-system,'Segoe UI',Roboto,Arial,sans-serif;color:#1c1917">
              <div style="max-width:520px;margin:0 auto;background:#ffffff;border-radius:12px;padding:32px">
                <p style="margin:0 0 16px">{Encode(greeting)}</p>
                <p style="margin:0 0 24px;line-height:1.5">{Encode(body)}</p>
                {htmlAction}
                {htmlNotes}
              </div>
            </body>
            </html>
            """;

        return new EmailMessage(to, subject, textBody, htmlBody, language);
    }

    private static string Encode(string value) => Html.Encode(value);
}
