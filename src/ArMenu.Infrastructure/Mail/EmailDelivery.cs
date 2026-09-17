using ArMenu.Application.Abstractions.Email;
using ArMenu.Application.Common.Diagnostics;
using ArMenu.Infrastructure.Persistence;
using ArMenu.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArMenu.Infrastructure.Mail;

/// <summary>
/// Delivers the next due message. Claims use <c>FOR UPDATE SKIP LOCKED</c> and a lease, like the model processing queue:
/// instances share the outbox without sending a message twice, and a message whose worker died is picked up again.
/// </summary>
internal sealed partial class EmailDelivery(
    IServiceScopeFactory scopeFactory,
    IEmailSender sender,
    ArMenuMetrics metrics,
    IOptions<EmailOptions> options,
    TimeProvider timeProvider,
    ILogger<EmailDelivery> logger)
{
    /// <summary>Pauses before the 2nd, 3rd... attempts: a mail server outage of a few hours still ends in delivery.</summary>
    public static readonly TimeSpan[] RetryDelays =
        [TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(30), TimeSpan.FromHours(2)];

    private static readonly string ClaimSql = $"""
        UPDATE {OutboxEmailConfiguration.TableName} AS email
        SET lease_expires_at = {"{0}"}, attempts = email.attempts + 1
        FROM (
            SELECT id
            FROM {OutboxEmailConfiguration.TableName}
            WHERE available_at <= {"{1}"} AND (lease_expires_at IS NULL OR lease_expires_at <= {"{1}"})
            ORDER BY available_at, id
            LIMIT 1
            FOR UPDATE SKIP LOCKED
        ) AS due
        WHERE email.id = due.id
        RETURNING email.*
        """;

    /// <returns>Whether a message was due; <see langword="false"/> means the outbox has nothing to send now.</returns>
    public async Task<bool> DeliverNextAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var outbox = scope.ServiceProvider.GetRequiredService<ArMenuDbContext>().Set<OutboxEmail>();
        var now = timeProvider.GetUtcNow();

        var claimed = (await outbox.FromSqlRaw(ClaimSql, now + options.Value.OutboxLease, now).AsNoTracking().ToListAsync(cancellationToken))
            .SingleOrDefault();
        if (claimed is null)
        {
            return false;
        }

        var sent = await sender.TrySendAsync(claimed.Message, cancellationToken);
        metrics.EmailAttempted(claimed.Template, sent);

        if (sent || claimed.Attempts > RetryDelays.Length)
        {
            if (!sent)
            {
                LogGaveUp(logger, claimed.Template, claimed.Attempts);
            }

            await outbox.Where(email => email.Id == claimed.Id).ExecuteDeleteAsync(CancellationToken.None);
        }
        else
        {
            var retryAt = timeProvider.GetUtcNow() + RetryDelays[claimed.Attempts - 1];
            await outbox.Where(email => email.Id == claimed.Id).ExecuteUpdateAsync(
                setters => setters.SetProperty(email => email.AvailableAt, retryAt).SetProperty(email => email.LeaseExpiresAt, (DateTimeOffset?)null),
                CancellationToken.None);
        }

        return true;
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Gave up delivering a {Template} e-mail after {Attempts} attempts")]
    private static partial void LogGaveUp(ILogger logger, string template, int attempts);
}

/// <summary>Sends outbox messages: at once when they were committed on this instance, otherwise on the next poll.</summary>
internal sealed partial class EmailDeliveryWorker(
    EmailDelivery delivery,
    EmailOutboxSignal signal,
    IOptions<EmailOptions> options,
    ILogger<EmailDeliveryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.OutboxWorkerEnabled)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!await delivery.DeliverNextAsync(stoppingToken))
                {
                    await signal.WaitAsync(options.Value.OutboxPollInterval, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                // The database is unavailable: back off instead of spinning.
                LogPollFailed(logger, exception);
                try
                {
                    await Task.Delay(options.Value.OutboxPollInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Looking for e-mails to deliver failed")]
    private static partial void LogPollFailed(ILogger logger, Exception exception);
}
