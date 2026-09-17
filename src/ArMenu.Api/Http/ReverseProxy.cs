using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;

namespace ArMenu.Api.Http;

/// <summary>
/// The proxies in front of the API (load balancer, CDN) whose <c>X-Forwarded-For</c> and <c>X-Forwarded-Proto</c> are
/// believed. Rate limits are per client address and refresh cookies are <c>Secure</c>, so both depend on it: trusting
/// every sender would let anyone pick their own address and dodge the limits.
/// </summary>
public sealed class ReverseProxyOptions
{
    public const string SectionName = "ReverseProxy";

    /// <summary>Addresses of trusted proxies, such as <c>10.0.0.4</c>.</summary>
    public IList<string> KnownProxies { get; } = [];

    /// <summary>Networks of trusted proxies in CIDR notation, such as <c>10.0.0.0/16</c> for a load balancer's subnet.</summary>
    public IList<string> KnownNetworks { get; } = [];

    /// <summary>Trusted proxies a request passes through: 1 behind a load balancer, 2 behind a CDN and a load balancer.</summary>
    public int ForwardLimit { get; set; } = 1;
}

internal static class ReverseProxyExtensions
{
    public static IServiceCollection AddReverseProxySupport(this IServiceCollection services, IConfiguration configuration)
    {
        var proxies = configuration.GetSection(ReverseProxyOptions.SectionName).Get<ReverseProxyOptions>() ?? new ReverseProxyOptions();

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = proxies.ForwardLimit;
            // Loopback stays trusted (the defaults), for a proxy running beside the API.
            foreach (var proxy in proxies.KnownProxies)
            {
                options.KnownProxies.Add(IPAddress.TryParse(proxy, out var address) ? address : throw Invalid(proxy));
            }

            foreach (var network in proxies.KnownNetworks)
            {
                options.KnownIPNetworks.Add(System.Net.IPNetwork.TryParse(network, out var parsed) ? parsed : throw Invalid(network));
            }
        });

        // Fail at startup, not at the first request, on a typo in the list.
        services.AddOptions<ForwardedHeadersOptions>().ValidateOnStart();
        services.AddSingleton<IValidateOptions<ForwardedHeadersOptions>>(new ValidateOptions<ForwardedHeadersOptions>(
            Options.DefaultName,
            _ => proxies.ForwardLimit > 0,
            $"{ReverseProxyOptions.SectionName}:ForwardLimit must be positive."));

        return services;
    }

    private static InvalidOperationException Invalid(string value) =>
        new($"'{value}' in {ReverseProxyOptions.SectionName} is not a valid address or network.");
}
