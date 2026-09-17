using ArMenu.Application.Abstractions.Email;
using ArMenu.Infrastructure.Persistence;

namespace ArMenu.Infrastructure.Mail;

/// <summary>Adds messages to the scope's unit of work, and wakes this instance's worker once they are committed.</summary>
internal sealed class EmailOutbox(ArMenuDbContext dbContext, EmailOutboxSignal signal, TimeProvider timeProvider) : IEmailOutbox
{
    private bool _listening;

    public void Add(EmailMessage message, string template)
    {
        dbContext.Add(OutboxEmail.For(message, template, timeProvider.GetUtcNow()));

        if (!_listening)
        {
            // Other instances find the message by polling; this one need not wait for its next poll.
            dbContext.SavedChanges += (_, _) => signal.Notify();
            _listening = true;
        }
    }
}

/// <summary>Wakes the delivery worker of this process when a message was committed here.</summary>
internal sealed class EmailOutboxSignal : IDisposable
{
    private readonly SemaphoreSlim _signal = new(0, 1);

    public void Notify()
    {
        if (_signal.CurrentCount == 0)
        {
            try
            {
                _signal.Release();
            }
            catch (SemaphoreFullException)
            {
                // Another notification got there first.
            }
        }
    }

    public Task<bool> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken) => _signal.WaitAsync(timeout, cancellationToken);

    public void Dispose() => _signal.Dispose();
}
