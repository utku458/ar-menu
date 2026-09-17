using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using ArMenu.Application.Abstractions.Email;
using Npgsql;

namespace ArMenu.IntegrationTests.TestSupport;

/// <summary>
/// A mail server in memory; set <see cref="Delivers"/> to false to play one that is down. With a database, it also
/// collects the messages the API queued in the outbox for an address.
/// </summary>
/// <remarks>
/// Test hosts run no outbox worker: they share one database, and a worker of one test would deliver another test's
/// messages through its own fake. Delivery itself is tested in EmailDeliveryTests, on a clock no other test uses.
/// </remarks>
public sealed partial class FakeEmailSender(PostgresDatabaseFixture? database = null) : IEmailSender
{
    private readonly ConcurrentQueue<EmailMessage> _sent = new();

    public bool Delivers { get; set; } = true;

    public IReadOnlyList<EmailMessage> Sent => [.. _sent];

    public Task<bool> TrySendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (Delivers)
        {
            _sent.Enqueue(message);
        }

        return Task.FromResult(Delivers);
    }

    /// <summary>
    /// The latest of <paramref name="count"/> messages to <paramref name="to"/> queued in the outbox (account e-mails are
    /// sent in the background); they are taken out of it, as a delivery would.
    /// </summary>
    public async Task<EmailMessage> WaitForMessageToAsync(string to, int count = 1)
    {
        var owner = database ?? throw new InvalidOperationException("Construct the sender with the database to read the outbox.");
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (Sent.Count(message => message.To == to) < count)
        {
            await using var connection = new NpgsqlConnection(owner.OwnerConnectionString);
            await connection.OpenAsync(timeout.Token);
            await using var command = new NpgsqlCommand(
                "DELETE FROM email_outbox WHERE \"to\" = @to RETURNING \"to\", subject, text_body, html_body, language, id", connection);
            command.Parameters.AddWithValue("to", to);
            await using (var reader = await command.ExecuteReaderAsync(timeout.Token))
            {
                List<(Guid Id, EmailMessage Message)> taken = [];
                while (await reader.ReadAsync(timeout.Token))
                {
                    taken.Add((reader.GetGuid(5), new EmailMessage(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4))));
                }

                foreach (var (_, message) in taken.OrderBy(message => message.Id))
                {
                    _sent.Enqueue(message);
                }
            }

            if (Sent.Count(message => message.To == to) < count)
            {
                await Task.Delay(50, timeout.Token);
            }
        }

        return Sent.Last(message => message.To == to);
    }

    /// <summary>The secret of the dashboard link to <paramref name="page"/> in <paramref name="message"/>.</summary>
    public static string TokenIn(EmailMessage message, string page)
    {
        ArgumentNullException.ThrowIfNull(message);
        var match = Regex.Match(message.TextBody, $@"https://\S+/{page}#(\S+)");
        match.Success.ShouldBeTrue($"no {page} link in the e-mail");
        return match.Groups[1].Value;
    }

    /// <summary>The invitation link of the latest message to <paramref name="to"/>.</summary>
    public Uri LinkSentTo(string to)
    {
        var message = Sent.Last(candidate => candidate.To == to);
        return new Uri(InvitationLink().Match(message.TextBody).Value);
    }

    [GeneratedRegex(@"https://\S+/join#\S+")]
    private static partial Regex InvitationLink();
}
