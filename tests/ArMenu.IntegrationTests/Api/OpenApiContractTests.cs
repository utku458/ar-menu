using ArMenu.IntegrationTests.TestSupport;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;

namespace ArMenu.IntegrationTests.Api;

/// <summary>
/// The committed OpenAPI document is the contract between the API and the web apps, which generate their TypeScript
/// types from it. An API change that alters the contract must show up in review next to the client changes it needs.
/// </summary>
public sealed class OpenApiContractTests(PostgresDatabaseFixture database) : IAsyncLifetime
{
    // The contract describes the API as deployed by default, and self-service sign-up is off by default; the other
    // test hosts turn it on only because it is the shortest way to a business.
    private readonly ArMenuApiFactory _factory = new(database, settings: new Dictionary<string, string?>
    {
        ["Onboarding:SignUpEnabled"] = "false",
    });

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // CI must fail on drift; locally the fresh document is written so the change can be reviewed as a diff.
    private static bool IsContinuousIntegration => string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase);

    [Fact]
    public async Task Committed_contract_matches_the_api()
    {
        var provider = _factory.Services.GetRequiredKeyedService<IOpenApiDocumentProvider>("v1");
        var document = await provider.GetOpenApiDocumentAsync(Ct);
        var actual = Normalize(await document.SerializeAsJsonAsync(OpenApiSpecVersion.OpenApi3_1, Ct));

        var contractPath = RepositoryPaths.OpenApiContract;
        var committed = File.Exists(contractPath) ? Normalize(await File.ReadAllTextAsync(contractPath, Ct)) : null;

        if (committed == actual)
        {
            return;
        }

        if (!IsContinuousIntegration)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(contractPath)!);
            await File.WriteAllTextAsync(contractPath, actual, Ct);
        }

        Assert.Fail(
            $"The API contract changed and '{contractPath}' is out of date. " +
            (IsContinuousIntegration
                ? "Run the tests locally to update it."
                : "It has been rewritten: review the diff, then run `pnpm --dir web generate:api` and commit both."));
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _factory.DisposeAsync();

    private static string Normalize(string json) => json.ReplaceLineEndings("\n").TrimEnd() + "\n";
}
