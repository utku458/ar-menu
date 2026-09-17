-- Runtime database role for the API.
--
-- It is deliberately NOT a superuser, NOT the owner of any table and has NO BYPASSRLS attribute, so PostgreSQL
-- applies the tenant row-level security policies to every statement it runs. Migrations run as the schema owner.
--
-- The password below is for local development and automated tests only. Other environments provision this role
-- through infrastructure tooling with a secret.

DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'armenu_app') THEN
        CREATE ROLE armenu_app LOGIN PASSWORD 'armenu_app' NOSUPERUSER NOCREATEDB NOCREATEROLE NOBYPASSRLS;
    END IF;
END
$$;

GRANT USAGE ON SCHEMA public TO armenu_app;

-- Tables do not exist yet: they are created later by migrations running as the role executing this script.
-- Default privileges give the runtime role data access to those future tables, never DDL rights.
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO armenu_app;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT USAGE, SELECT ON SEQUENCES TO armenu_app;
