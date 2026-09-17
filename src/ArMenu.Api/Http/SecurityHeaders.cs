namespace ArMenu.Api.Http;

/// <summary>
/// Response headers for an API that only ever returns data: nothing it sends may be rendered as a page, framed, or leak
/// the URL it was called from.
/// </summary>
internal static class SecurityHeaders
{
    public const string ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'; base-uri 'none'";

    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.Use((context, next) =>
        {
            var headers = context.Response.Headers;
            headers.XContentTypeOptions = "nosniff";
            headers["Referrer-Policy"] = "no-referrer";

            // The API reference UI (Development only) is the one HTML page served here.
            if (!context.Request.Path.StartsWithSegments("/scalar"))
            {
                headers.ContentSecurityPolicy = ContentSecurityPolicy;
            }

            return next(context);
        });
}
