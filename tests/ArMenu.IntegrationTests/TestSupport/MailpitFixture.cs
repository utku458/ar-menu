using System.Net.Http.Json;
using System.Text.Json.Serialization;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace ArMenu.IntegrationTests.TestSupport;

/// <summary>A disposable SMTP server with an HTTP API to read what arrived (Mailpit, as in docker-compose).</summary>
public sealed class MailpitFixture : IAsyncLifetime
{
    private const int SmtpPort = 1025;
    private const int HttpPort = 8025;

    private readonly IContainer _container = new ContainerBuilder("axllent/mailpit:v1.31")
        .WithPortBinding(SmtpPort, assignRandomHostPort: true)
        .WithPortBinding(HttpPort, assignRandomHostPort: true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request => request.ForPort(HttpPort).ForPath("/readyz")))
        .Build();

    private HttpClient _api = null!;

    public string Host => _container.Hostname;

    public int Port => _container.GetMappedPublicPort(SmtpPort);

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        _api = new HttpClient { BaseAddress = new Uri($"http://{_container.Hostname}:{_container.GetMappedPublicPort(HttpPort)}/") };
    }

    /// <summary>The latest message to <paramref name="to"/>, with its text and HTML bodies.</summary>
    public async Task<MailpitMessage> LatestMessageToAsync(string to, CancellationToken cancellationToken)
    {
        var list = await _api.GetFromJsonAsync<MessageList>($"api/v1/search?query=to:{Uri.EscapeDataString(to)}", cancellationToken);
        list!.Messages.ShouldNotBeEmpty();
        var summary = list.Messages[0];
        return (await _api.GetFromJsonAsync<MailpitMessage>($"api/v1/message/{summary.Id}", cancellationToken))!;
    }

    public async ValueTask DisposeAsync()
    {
        _api?.Dispose();
        await _container.DisposeAsync();
    }

    private sealed record MessageList([property: JsonPropertyName("messages")] IReadOnlyList<MessageSummary> Messages);

    private sealed record MessageSummary([property: JsonPropertyName("ID")] string Id);
}

public sealed record MailpitMessage(
    [property: JsonPropertyName("Subject")] string Subject,
    [property: JsonPropertyName("From")] MailpitAddress From,
    [property: JsonPropertyName("Text")] string Text,
    [property: JsonPropertyName("HTML")] string Html);

public sealed record MailpitAddress([property: JsonPropertyName("Name")] string Name, [property: JsonPropertyName("Address")] string Address);
