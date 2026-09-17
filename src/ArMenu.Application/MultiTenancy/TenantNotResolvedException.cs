namespace ArMenu.Application.MultiTenancy;

/// <summary>
/// Tenant-scoped data was accessed from a scope that is not bound to a tenant.
/// This always indicates a programming error (for example an endpoint without tenant resolution), never a user error,
/// and it is raised instead of silently returning an empty or unfiltered result.
/// </summary>
public sealed class TenantNotResolvedException : InvalidOperationException
{
    private const string DefaultMessage =
        "The current scope is not bound to a tenant. Tenant-scoped data is only accessible after tenant resolution.";

    public TenantNotResolvedException()
        : base(DefaultMessage)
    {
    }

    public TenantNotResolvedException(string message)
        : base(message)
    {
    }

    public TenantNotResolvedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
