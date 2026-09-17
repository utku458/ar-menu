namespace ArMenu.Api.MultiTenancy;

internal static class TenantResolutionExtensions
{
    public const string DefaultRouteParameterName = "tenant";

    /// <summary>Binds requests to the tenant whose slug is in the <paramref name="routeParameterName"/> route value.</summary>
    /// <exception cref="InvalidOperationException">At startup, when the route template lacks that parameter.</exception>
    public static TBuilder RequireTenantFromRoute<TBuilder>(
        this TBuilder builder,
        string routeParameterName = DefaultRouteParameterName)
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(routeParameterName);

        builder.Add(endpointBuilder =>
        {
            // Fail fast on a misconfigured route instead of answering 404 to every request at runtime.
            if (endpointBuilder is RouteEndpointBuilder { RoutePattern: var pattern } &&
                pattern.GetParameter(routeParameterName) is null)
            {
                throw new InvalidOperationException(
                    $"Endpoint '{pattern.RawText}' resolves its tenant from route parameter '{routeParameterName}', " +
                    "but its route template does not define it.");
            }

            endpointBuilder.Metadata.Add(new RouteTenantResolutionStrategy(routeParameterName));
        });

        return builder;
    }

    /// <summary>
    /// Binds requests to the tenant of the authenticated user's token. Combine with <c>RequireAuthorization()</c>.
    /// </summary>
    public static TBuilder RequireTenantFromClaims<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder =>
        builder.WithMetadata(ClaimsTenantResolutionStrategy.Instance);

    /// <summary>
    /// Adds tenant resolution to the pipeline. Must run after routing (it reads endpoint metadata) and after
    /// authentication and authorization (claims must be available, and anonymous callers must get 401 first).
    /// </summary>
    public static IApplicationBuilder UseTenantResolution(this IApplicationBuilder app) =>
        app.UseMiddleware<TenantResolutionMiddleware>();
}
