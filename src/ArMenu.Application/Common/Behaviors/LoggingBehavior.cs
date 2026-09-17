using System.Diagnostics;
using ArMenu.Domain.Common;
using Mediator;
using Microsoft.Extensions.Logging;

namespace ArMenu.Application.Common.Behaviors;

/// <summary>
/// Logs the outcome and duration of every use case. Only the message type and error code are logged, never the message
/// itself: commands carry passwords, tokens and personal data.
/// </summary>
public sealed partial class LoggingBehavior<TMessage, TResponse>(ILogger<LoggingBehavior<TMessage, TResponse>> logger)
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
    where TResponse : Result
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        var startedAt = Stopwatch.GetTimestamp();
        var response = await next(message, cancellationToken);
        var elapsedMilliseconds = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;

        if (response.IsFailure)
        {
            LogFailed(logger, typeof(TMessage).Name, response.Error.Code, elapsedMilliseconds);
        }
        else
        {
            LogSucceeded(logger, typeof(TMessage).Name, elapsedMilliseconds);
        }

        return response;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "{MessageType} failed with {ErrorCode} in {ElapsedMilliseconds:0.0} ms")]
    private static partial void LogFailed(ILogger logger, string messageType, string errorCode, double elapsedMilliseconds);

    [LoggerMessage(Level = LogLevel.Debug, Message = "{MessageType} succeeded in {ElapsedMilliseconds:0.0} ms")]
    private static partial void LogSucceeded(ILogger logger, string messageType, double elapsedMilliseconds);
}
