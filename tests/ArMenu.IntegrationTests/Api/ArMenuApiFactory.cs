using ArMenu.Api.Endpoints;
using ArMenu.IntegrationTests.TestSupport;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace ArMenu.IntegrationTests.Api;

/// <summary>Hosts the real API pipeline in memory against the test database, connected as the runtime role.</summary>
public sealed class ArMenuApiFactory(
    PostgresDatabaseFixture database,
    StorageFixture? storage = null,
    Action<IServiceCollection>? configureServices = null,
    IReadOnlyDictionary<string, string?>? settings = null) : WebApplicationFactory<Program>
{
    /// <summary>
    /// A client that keeps cookies across requests, like a browser. The https base address matters: refresh token
    /// cookies are <c>Secure</c> and would otherwise never be sent back.
    /// </summary>
    public HttpClient CreateBrowserClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"),
        HandleCookies = true,
        AllowAutoRedirect = false,
    });

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Any environment but Development: the development database initializer must not run against the test server.
        builder.UseEnvironment("Testing");

        foreach (var (key, value) in TestConfiguration.For(database.RuntimeConnectionString, storage).Concat(settings ?? new Dictionary<string, string?>()))
        {
            builder.UseSetting(key, value);
        }

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IEndpointModule, TenantProbeEndpoints>();
            configureServices?.Invoke(services);
        });
    }
}
