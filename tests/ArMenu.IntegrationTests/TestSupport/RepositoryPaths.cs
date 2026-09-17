namespace ArMenu.IntegrationTests.TestSupport;

internal static class RepositoryPaths
{
    /// <summary>The OpenAPI document the web apps generate their API types from.</summary>
    public static string OpenApiContract => Path.Combine(Root, "contracts", "openapi", "v1.json");

    /// <summary>A generated demo file (see web/tools/demo-assets), e.g. <c>models/smash-burger.glb</c>.</summary>
    public static string ProcessorContract(string fileName) => Path.Combine(Root, "contracts", "asset-processor", fileName);

    public static string DemoAsset(string relativePath) => Path.Combine(Root, "assets", "demo", relativePath);

    private static string Root { get; } = FindRoot();

    private static string FindRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ArMenu.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException($"No ArMenu.slnx found above '{AppContext.BaseDirectory}'.");
    }
}
