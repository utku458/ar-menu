namespace ArMenu.Infrastructure.Persistence;

public static class ConnectionStringNames
{
    /// <summary>
    /// Runtime connection. Must use a role that is neither superuser, table owner nor BYPASSRLS,
    /// otherwise PostgreSQL skips row-level security for it.
    /// </summary>
    public const string Runtime = "ArMenu";

    /// <summary>Schema-owner connection used only to apply migrations. Falls back to <see cref="Runtime"/>.</summary>
    public const string Migrations = "ArMenuMigrations";
}
