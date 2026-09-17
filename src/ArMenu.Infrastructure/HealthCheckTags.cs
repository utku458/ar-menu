namespace ArMenu.Infrastructure;

public static class HealthCheckTags
{
    /// <summary>Checks that must pass before the instance receives traffic (e.g. database connectivity).</summary>
    public const string Ready = "ready";
}
