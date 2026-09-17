using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace ArMenu.Api.RateLimiting;

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>Sign-in, refresh and sign-out attempts per client per minute. Slows down credential stuffing.</summary>
    public int AuthenticationPermitsPerMinute { get; set; } = 10;

    /// <summary>New businesses per client per hour.</summary>
    public int SignUpPermitsPerHour { get; set; } = 5;

    /// <summary>
    /// Public menu requests per client per minute. Generous on purpose: a full restaurant shares one Wi-Fi address.
    /// </summary>
    public int PublicMenuPermitsPerMinute { get; set; } = 300;
}

internal static class RateLimitingPolicies
{
    public const string Authentication = "authentication";
    public const string SignUp = "sign-up";
    public const string PublicMenu = "public-menu";

    public static IServiceCollection AddArMenuRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RateLimitingOptions>().Bind(configuration.GetSection(RateLimitingOptions.SectionName));

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = WriteRejectionAsync;

            options.AddPolicy(Authentication, httpContext => RateLimitPartition.GetFixedWindowLimiter(
                ClientKey(httpContext),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = LimitsOf(httpContext).AuthenticationPermitsPerMinute,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                }));

            options.AddPolicy(SignUp, httpContext => RateLimitPartition.GetFixedWindowLimiter(
                ClientKey(httpContext),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = LimitsOf(httpContext).SignUpPermitsPerHour,
                    Window = TimeSpan.FromHours(1),
                    QueueLimit = 0,
                }));

            // Token bucket: absorbs the burst of a table scanning the QR code together, then refills steadily.
            options.AddPolicy(PublicMenu, httpContext => RateLimitPartition.GetTokenBucketLimiter(
                ClientKey(httpContext),
                _ =>
                {
                    var permitsPerMinute = LimitsOf(httpContext).PublicMenuPermitsPerMinute;
                    return new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = permitsPerMinute,
                        TokensPerPeriod = Math.Max(1, permitsPerMinute / 6),
                        ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                        QueueLimit = 0,
                    };
                }));
        });

        return services;
    }

    /// <summary>Applies a rate limiting policy and documents the 429 problem response it produces.</summary>
    public static TBuilder WithRateLimit<TBuilder>(this TBuilder builder, string policyName)
        where TBuilder : IEndpointConventionBuilder =>
        builder
            .RequireRateLimiting(policyName)
            .WithMetadata(new ProducesResponseTypeMetadata(
                StatusCodes.Status429TooManyRequests,
                typeof(ProblemDetails),
                ["application/problem+json"]));

    // Behind a reverse proxy, the forwarded client address of a trusted proxy (see ReverseProxyOptions).
    private static string ClientKey(HttpContext httpContext) =>
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private static RateLimitingOptions LimitsOf(HttpContext httpContext) =>
        httpContext.RequestServices.GetRequiredService<IOptions<RateLimitingOptions>>().Value;

    private static async ValueTask WriteRejectionAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
        }

        await context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>().WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context.HttpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Detail = "Too many requests. Try again later.",
                Extensions = { ["code"] = "rate_limited" },
            },
        });
    }
}
