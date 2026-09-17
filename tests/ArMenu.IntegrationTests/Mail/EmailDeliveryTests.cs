using ArMenu.Application.Abstractions.Email;
using ArMenu.Infrastructure.Mail;
using ArMenu.IntegrationTests.TestSupport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace ArMenu.IntegrationTests.Mail;

/// <summary>
/// The outbox, delivered by the real worker code. The clock stands in 2001: messages other tests queue meanwhile are not
/// due yet for these deliveries, so no test takes another's messages.
/// </summary>
public sealed class EmailDeliveryTests(PostgresDatabaseFixture database) : IAsyncDisposable
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2001, 1, 1, 9, 0, 0, TimeSpan.Zero));
    private readonly FakeEmailSender _email = new();
    private TestServices? _services;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_message_exists_only_once_its_unit_of_work_is_saved_and_is_sent_once()
    {
        var address = Address();
        await using (var scope = Services.BeginScope())
        {
            scope.Get<IEmailOutbox>().Add(Message(address), "test");
        }

        (await DeliverAllAsync()).ShouldBe(0, "never saved");

        await QueueAsync(address);
        await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => Delivery.DeliverNextAsync(Ct)));

        _email.Sent.Count(message => message.To == address).ShouldBe(1);
        (await CountAsync(address)).ShouldBe(0, "delivered messages leave the outbox");
    }

    [Fact]
    public async Task A_refused_message_is_tried_again_later_and_given_up_after_the_last_attempt()
    {
        var address = Address();
        await QueueAsync(address);
        _email.Delivers = false;

        (await Delivery.DeliverNextAsync(Ct)).ShouldBeTrue();
        (await Delivery.DeliverNextAsync(Ct)).ShouldBeFalse("the next attempt waits");
        (await CountAsync(address)).ShouldBe(1);

        foreach (var delay in EmailDelivery.RetryDelays)
        {
            _time.Advance(delay);
            (await Delivery.DeliverNextAsync(Ct)).ShouldBeTrue();
        }

        (await CountAsync(address)).ShouldBe(0, "given up: an address is not kept for a message that will never go");
        _email.Sent.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_worker_that_died_holding_a_message_gives_it_back_when_its_lease_expires()
    {
        var address = Address();
        await QueueAsync(address);
        await database.ExecuteAsOwnerAsync($"UPDATE email_outbox SET lease_expires_at = '2001-01-01 09:01:00+00' WHERE \"to\" = '{address}'", Ct);

        (await Delivery.DeliverNextAsync(Ct)).ShouldBeFalse("still leased");
        _time.Advance(TimeSpan.FromMinutes(2));
        (await Delivery.DeliverNextAsync(Ct)).ShouldBeTrue();
        _email.Sent.ShouldHaveSingleItem().To.ShouldBe(address);
    }

    public async ValueTask DisposeAsync()
    {
        if (_services is not null)
        {
            await _services.DisposeAsync();
        }
    }

    private TestServices Services => _services ??= new TestServices(
        database.RuntimeConnectionString,
        _time,
        services => services.AddSingleton<IEmailSender>(_email));

    private EmailDelivery Delivery => Services.Get<EmailDelivery>();

    private async Task QueueAsync(string address)
    {
        await using var scope = Services.BeginScope();
        scope.Get<IEmailOutbox>().Add(Message(address), "test");
        await scope.Db.SaveChangesAsync(Ct);
    }

    private async Task<int> DeliverAllAsync()
    {
        var delivered = 0;
        while (await Delivery.DeliverNextAsync(Ct))
        {
            delivered++;
        }

        return delivered;
    }

    private Task<int> CountAsync(string address) =>
        database.ScalarAsOwnerAsync<long>($"SELECT count(*) FROM email_outbox WHERE \"to\" = '{address}'", Ct).ContinueWith(task => (int)task.Result, Ct);

    private static string Address() => $"outbox-{Guid.NewGuid():N}@armenu.test";

    private static EmailMessage Message(string to) => new(to, "Merhaba", "Metin", "<p>Metin</p>", "tr");
}
