namespace ArMenu.Application.MultiTenancy;

/// <summary>
/// A scope bound to one tenant attempted to touch data owned by another tenant (or to switch tenants).
/// The operation is aborted before anything reaches the database; treat occurrences as security incidents.
/// </summary>
public sealed class TenantIsolationViolationException : InvalidOperationException
{
    public TenantIsolationViolationException()
        : base("The operation would cross a tenant boundary and was rejected.")
    {
    }

    public TenantIsolationViolationException(string message)
        : base(message)
    {
    }

    public TenantIsolationViolationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
