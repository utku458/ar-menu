namespace ArMenu.Domain.Tenants;

public enum TenantStatus
{
    /// <summary>The tenant's menu is publicly served and its staff can manage it.</summary>
    Active = 0,

    /// <summary>
    /// Access is blocked (e.g. unpaid subscription, abuse). Data is retained so the tenant can be reactivated.
    /// </summary>
    Suspended = 1,

    /// <summary>
    /// The business was closed when its only owner deleted their account. Nobody can sign in or see its menu; printed QR
    /// codes keep pointing at a menu that no longer exists rather than at someone else's, so the slug is never reused.
    /// </summary>
    Closed = 2,
}
