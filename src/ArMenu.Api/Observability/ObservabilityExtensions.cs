using System.Reflection;
using ArMenu.Application.Common.Diagnostics;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace ArMenu.Api.Observability;

/// <summary>
/// Traces, metrics and logs through OpenTelemetry. Everything is configured with the standard <c>OTEL_*</c> environment
/// variables; without <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> nothing is exported and logs go to the console only.
/// </summary>
internal static class ObservabilityExtensions
{
    public const string ServiceName = "armenu-api";

    public static IServiceCollection AddObservability(this IServiceCollection services, IConfiguration configuration)
    {
        var version = typeof(ObservabilityExtensions).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        var telemetry = services.AddOpenTelemetry()
            // OTEL_SERVICE_NAME and OTEL_RESOURCE_ATTRIBUTES (for example deployment.environment.name) still win.
            .ConfigureResource(resource => resource.AddService(ServiceName, serviceVersion: version))
            .WithTracing(tracing => tracing
                .SetSampler(new ParentBasedSampler(new SkipBackgroundClientSpansSampler()))
                .AddSource(ArMenuTelemetry.Name)
                .AddAspNetCoreInstrumentation(options => options.Filter = context => !context.Request.Path.StartsWithSegments("/health"))
                .AddHttpClientInstrumentation()
                .AddNpgsql())
            .WithMetrics(metrics => metrics
                .AddMeter(ArMenuTelemetry.Name)
                .AddAspNetCoreInstrumentation()
                // Built into .NET: rate limiter rejections, unhandled exceptions, outgoing HTTP, GC and thread pool.
                .AddMeter("Microsoft.AspNetCore.RateLimiting", "Microsoft.AspNetCore.Diagnostics", "System.Net.Http", "System.Runtime")
                .AddNpgsqlInstrumentation())
            .WithLogging(configureBuilder: null, configureOptions: options =>
            {
                options.IncludeFormattedMessage = true;
                options.IncludeScopes = true;
            });

        if (!string.IsNullOrWhiteSpace(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            telemetry.UseOtlpExporter();
        }

        return services;
    }

    /// <summary>
    /// Drops client spans that start a trace of their own: the queue polls of background workers, every few seconds on
    /// every instance. Work they find runs inside its own span, and requests are traced as usual.
    /// </summary>
    internal sealed class SkipBackgroundClientSpansSampler : Sampler
    {
        public override SamplingResult ShouldSample(in SamplingParameters samplingParameters) =>
            new(samplingParameters.Kind == System.Diagnostics.ActivityKind.Client ? SamplingDecision.Drop : SamplingDecision.RecordAndSample);
    }
}
