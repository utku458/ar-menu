using ArMenu.Infrastructure.Persistence;

namespace ArMenu.IntegrationTests.TestSupport;

/// <summary>Settings shared by every test host. Production defaults stay untouched; only speed and limits are relaxed.</summary>
internal static class TestConfiguration
{
    public const string JwtIssuer = "armenu-tests";
    public const string JwtAudience = "armenu-tests-dashboard";

    /// <summary>Test-only 256-bit key.</summary>
    public const string JwtSigningKey = "dGVzdC1vbmx5LXNpZ25pbmcta2V5LTMyLWJ5dGVzISE=";

    public const string AssetBaseUrl = "https://cdn.armenu.test/";

    public const string Password = "correct-horse-battery-staple";

    public const string DashboardUrl = "https://app.armenu.test/";

    // storage: real object storage for tests that upload; the others get an address that is never contacted.
    public static IReadOnlyDictionary<string, string?> For(string connectionString, StorageFixture? storage = null) => new Dictionary<string, string?>
    {
        [$"ConnectionStrings:{ConnectionStringNames.Runtime}"] = connectionString,
        ["Authentication:Jwt:Issuer"] = JwtIssuer,
        ["Authentication:Jwt:Audience"] = JwtAudience,
        ["Authentication:Jwt:SigningKey"] = JwtSigningKey,
        // Real PBKDF2 cost is pointless in tests; the algorithm and format stay identical.
        ["Authentication:PasswordHashing:Iterations"] = "1000",
        ["Assets:PublicBaseUrl"] = storage?.AssetsBaseUrl.ToString() ?? AssetBaseUrl,
        ["Storage:ServiceUrl"] = storage?.ServiceUrl.ToString() ?? "http://storage.armenu.test",
        ["Storage:AccessKey"] = StorageFixture.AccessKey,
        ["Storage:SecretKey"] = StorageFixture.SecretKey,
        ["Storage:ForcePathStyle"] = "true",
        ["Storage:UploadsBucket"] = StorageFixture.UploadsBucket,
        ["Storage:AssetsBucket"] = StorageFixture.AssetsBucket,
        // Background workers stay off: tests run jobs and cleanups explicitly, so assertions never race a worker.
        ["AssetProcessor:WorkersEnabled"] = "false",
        ["AssetProcessor:Url"] = "http://processor.armenu.test/",
        ["AssetProcessor:CrashRetryDelay"] = "00:00:00",
        ["ArModelProcessing:RetryDelay"] = "00:00:00",
        ["AssetCleanup:Enabled"] = "false",
        ["Retention:Enabled"] = "false",
        // Gauge tests publish their own samples; a host sampling the shared database would overwrite them.
        ["BacklogMetrics:Enabled"] = "false",
        // Never contacted: e-mail tests replace the sender or point it at their own SMTP server.
        ["Email:Host"] = "smtp.armenu.test",
        ["Email:FromAddress"] = "no-reply@armenu.test",
        // See FakeEmailSender: tests read the outbox instead of racing each other's workers for it.
        ["Email:OutboxWorkerEnabled"] = "false",
        ["Dashboard:Url"] = DashboardUrl,
        ["RateLimiting:AuthenticationPermitsPerMinute"] = "10000",
        ["RateLimiting:SignUpPermitsPerHour"] = "10000",
        ["RateLimiting:PublicMenuPermitsPerMinute"] = "10000",
    };
}
