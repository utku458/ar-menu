using System.Diagnostics;
using ArMenu.Application.Common.Diagnostics;
using ArMenu.Domain.Common;
using Mediator;

namespace ArMenu.Application.Common.Behaviors;

/// <summary>
/// Traces every use case as a span and records its duration and outcome. Like logging, only the message type and the
/// error code are recorded, never the message.
/// </summary>
public sealed class TelemetryBehavior<TMessage, TResponse>(ArMenuMetrics metrics) : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
    where TResponse : Result
{
    private static readonly string UseCase = typeof(TMessage).Name;

    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        using var activity = ArMenuTelemetry.ActivitySource.StartActivity(UseCase);
        activity?.SetTag(ArMenuTelemetry.Tags.UseCase, UseCase);
        var startedAt = Stopwatch.GetTimestamp();

        try
        {
            var response = await next(message, cancellationToken);
            var errorCode = response.IsFailure ? response.Error.Code : null;

            if (errorCode is not null)
            {
                // Expected failures (validation, not found) are outcomes, not faults: the span is tagged, not errored.
                activity?.SetTag(ArMenuTelemetry.Tags.ErrorType, errorCode);
            }

            metrics.UseCaseCompleted(UseCase, Stopwatch.GetElapsedTime(startedAt), errorCode);
            return response;
        }
        catch (Exception exception)
        {
            activity?.SetStatus(ActivityStatusCode.Error, exception.GetType().Name);
            metrics.UseCaseCompleted(UseCase, Stopwatch.GetElapsedTime(startedAt), exception.GetType().FullName);
            throw;
        }
    }
}
