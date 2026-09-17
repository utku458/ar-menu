using ArMenu.Application.Abstractions.Persistence;
using ArMenu.Application.MultiTenancy;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ArMenu.Api.Http;

/// <summary>
/// Last line of error handling. Expected failures never get here (they are results); this maps the few exceptions with
/// a meaningful HTTP status and turns everything else into an opaque 500 that leaks no internals.
/// </summary>
internal sealed partial class GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    private const int ClientClosedRequest = 499;

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            httpContext.Response.StatusCode = ClientClosedRequest;
            return true;
        }

        var (statusCode, code, detail) = exception switch
        {
            ConcurrencyConflictException => (StatusCodes.Status409Conflict, "concurrency_conflict", "The resource was changed by another request. Reload it and try again."),
            UniqueConstraintViolationException => (StatusCodes.Status409Conflict, "conflict", "The request conflicts with existing data."),
            BadHttpRequestException badRequest => (badRequest.StatusCode, "bad_request", "The request could not be read."),
            _ => (StatusCodes.Status500InternalServerError, "internal_error", "An unexpected error occurred."),
        };

        if (exception is TenantIsolationViolationException)
        {
            LogTenantIsolationViolation(logger, exception, httpContext.Request.Method, httpContext.Request.Path);
        }
        else if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            LogUnhandledException(logger, exception, httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = statusCode;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Detail = detail,
                Extensions = { ["code"] = code },
            },
        });
    }

    [LoggerMessage(Level = LogLevel.Critical, Message = "Tenant isolation violation blocked while handling {Method} {Path}")]
    private static partial void LogTenantIsolationViolation(ILogger logger, Exception exception, string method, string path);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception while handling {Method} {Path}")]
    private static partial void LogUnhandledException(ILogger logger, Exception exception, string method, string path);
}
