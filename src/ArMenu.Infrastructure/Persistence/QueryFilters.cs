namespace ArMenu.Infrastructure.Persistence;

/// <summary>Names of the global query filters, for selectively disabling one via <c>IgnoreQueryFilters([...])</c>.</summary>
public static class QueryFilters
{
    /// <summary>Restricts tenant-scoped entities to the tenant bound to the current scope.</summary>
    /// <remarks>
    /// Never disable this filter in request-handling code; row-level security would still block the rows. The single
    /// exception is listing a user's own workspaces, which a dedicated policy allows (see GetMyWorkspacesQueryHandler).
    /// </remarks>
    public const string Tenant = nameof(Tenant);

    /// <summary>Hides soft-deleted entities.</summary>
    public const string SoftDelete = nameof(SoftDelete);
}
