#!/bin/sh
# Prepares ArMenu's PostgreSQL database on Coolify: the runtime role, then the schema.
#
# Schema changes ship as a reviewed migration bundle run before the new API starts, never as a side effect of
# application startup (see src/ArMenu.Api/Dockerfile). Coolify runs no migration job, so this is that step, run by hand
# from a machine with Docker, from the repository root, BEFORE the API version that needs the new schema deploys.
#
#   ssh -N -L 15432:<postgres-container-ip>:5432 root@46.225.37.55 &   # the database is not public
#   PGHOST=host.docker.internal PGPORT=15432 PGUSER=postgres PGPASSWORD='…' PGDATABASE=railway \
#   ARMENU_APP_PASSWORD='…' deploy/coolify/migrate.sh
#
# The database is reachable only inside the server's Docker network, hence the SSH tunnel; host.docker.internal is
# how the psql and migration containers below reach that tunnel on the machine running this script. The container
# IP comes from `docker inspect <container> --format '{{range .NetworkSettings.Networks}}{{.IPAddress}} {{end}}'`.
# The database is named `railway` for historical reasons (kept when the data moved).
# ARMENU_APP_PASSWORD is the password the API will use; put the same value
# in the API service's connection string.
#
# Re-running is safe: the role is created once and its password reset, and the bundle applies only new migrations.
set -eu

: "${PGHOST:?set PGHOST to the tunnel host, usually host.docker.internal}"
: "${PGPORT:?set PGPORT to the local tunnel port}"
# No apostrophes in these messages: inside ${VAR:?...} the shell reads one as an opening quote.
: "${PGUSER:?set PGUSER to the schema owner, usually postgres}"
: "${PGPASSWORD:?set PGPASSWORD}"
: "${PGDATABASE:?set PGDATABASE (the database is named railway)}"
: "${ARMENU_APP_PASSWORD:?set ARMENU_APP_PASSWORD to the password the API will connect with}"

DB_SSL="${DB_SSL:-Require}"
POSTGRES_IMAGE="${POSTGRES_IMAGE:-postgres:18-alpine}"

echo "==> Creating the runtime role on $PGHOST:$PGPORT/$PGDATABASE"

# The same role as deploy/postgres/init/01-create-app-role.sql, with a real password: no superuser, owner of nothing
# and NOBYPASSRLS, so PostgreSQL applies the tenant row-level security policies to every statement the API runs.
#
# \gexec rather than a DO block: psql does not interpolate its variables inside dollar-quoted strings, so the
# password could not be passed into one safely. quote_literal escapes it properly.
docker run --rm -i -e PGPASSWORD "$POSTGRES_IMAGE" \
  psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$PGDATABASE" \
    -v ON_ERROR_STOP=1 -v app_password="$ARMENU_APP_PASSWORD" -f - <<'SQL'
SELECT 'CREATE ROLE armenu_app LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOBYPASSRLS PASSWORD '
       || quote_literal(:'app_password')
WHERE NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'armenu_app')
\gexec

SELECT 'ALTER ROLE armenu_app PASSWORD ' || quote_literal(:'app_password')
\gexec

GRANT USAGE ON SCHEMA public TO armenu_app;

-- Data access to the tables migrations are about to create, never DDL rights. These apply to objects created by the
-- role running this script, which is also the role the migration bundle runs as.
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO armenu_app;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT USAGE, SELECT ON SEQUENCES TO armenu_app;

-- Re-running after the schema exists: without this, a second run would leave the role unable to read the tables the
-- first run created.
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO armenu_app;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO armenu_app;
SQL

echo "==> Building the migration bundle image"
docker build --file src/ArMenu.Api/Dockerfile --target migrations --tag armenu-migrations .

echo "==> Applying migrations as $PGUSER"
docker run --rm armenu-migrations \
  --connection "Host=$PGHOST;Port=$PGPORT;Database=$PGDATABASE;Username=$PGUSER;Password=$PGPASSWORD;SSL Mode=$DB_SSL;Trust Server Certificate=true"

echo "==> Done. The API may now connect as armenu_app."
