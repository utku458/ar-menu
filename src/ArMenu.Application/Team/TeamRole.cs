using System.Text.Json.Serialization;
using ArMenu.Domain.Memberships;

namespace ArMenu.Application.Team;

/// <summary>A member's role as the API names it, the same names access tokens carry.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<TeamRole>))]
public enum TeamRole
{
    Owner = TenantRole.Owner,
    Manager = TenantRole.Manager,
    Staff = TenantRole.Staff,
}

public static class TeamRoles
{
    public static TenantRole ToTenantRole(this TeamRole role) => (TenantRole)role;

    public static TeamRole ToTeamRole(this TenantRole role) => (TeamRole)role;
}
