using System.Diagnostics;
using ArMenu.Application.MultiTenancy;
using Microsoft.AspNetCore.Mvc;

namespace ArMenu.Api.MultiTenancy;

/// <summary>
/// Binds each request to its tenant before the endpoint executes, using the strategy the endpoint declares.
/// Requests to tenant-bound endpoints never reach the endpoint without an active tenant.
/// </summary>
internal sealed partial class TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger)
{
    public async Task InvokeAsync(
        HttpContext httpContext,
        ITenantLookup tenantLookup,
        ITenantContextSetter tenantContextSetter,
        IProblemDetailsService problemDetailsService)
    {
        var strategy = httpContext.GetEndpoint()?.Metadata.GetMetadata<ITenantResolutionStrategy>();
        if (strategy is null)
        {
            // Not a tenant-bound endpoint (health checks, sign-in, platform endpoints...).
            await next(httpContext);
            return;
        }

        var tenant = await strategy.ResolveAsync(httpContext, tenantLookup);

        // Unknown and suspended tenants get the same answer: a public menu URL must not reveal whether a business
        // is a former customer, and staff of a suspended tenant are locked out even while their tokens are valid.
        if (tenant is not { IsActive: true })
        {
            LogTenantNotResolved(logger, strategy.GetType().Name);
            await WriteTenantNotFoundAsync(httpContext, problemDetailsService);
            return;
        }

        tenantContextSetter.SetTenant(tenant);
        Activity.Current?.SetTag("tenant.id", tenant.Id.Value);

        using (logger.BeginScope(new Dictionary<string, object> { ["TenantId"] = tenant.Id.Value, ["TenantSlug"] = tenant.Slug }))
        {
            await next(httpContext);
        }
    }

    private static async Task WriteTenantNotFoundAsync(HttpContext httpContext, IProblemDetailsService problemDetailsService)
    {
        httpContext.Response.StatusCode = StatusCodes.Status404NotFound;

        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Tenant not found",
                Detail = "No active tenant matches this request.",
                Extensions = { ["code"] = "tenant.not_found" },
            },
        });
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "No active tenant could be resolved by {Strategy}")]
    private static partial void LogTenantNotResolved(ILogger logger, string strategy);
}
