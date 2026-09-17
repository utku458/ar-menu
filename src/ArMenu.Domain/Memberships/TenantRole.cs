namespace ArMenu.Domain.Memberships;

/// <summary>What a member may do inside a tenant. Roles are per tenant: the same user can own one business and staff another.</summary>
public enum TenantRole
{
    /// <summary>Full control, including tenant settings and staff.</summary>
    Owner = 0,

    /// <summary>Manages the menu: categories, items, prices and AR models.</summary>
    Manager = 1,

    /// <summary>Front-of-house staff: can mark items as sold out or available again.</summary>
    Staff = 2,
}
