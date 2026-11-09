#!/usr/bin/env bash
set -euo pipefail

COMMANDS_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJECT_ROOT="$(cd "$COMMANDS_DIR/.." && pwd)"

load_project_environment() {
    if [[ ! -f "$PROJECT_ROOT/.env" ]]; then
        printf '%s\n' 'Missing .env file. Copy .env.example to .env and set PG_ADMIN_PASSWORD.' >&2
        return 1
    fi
    set -a
    source "$PROJECT_ROOT/.env"
    set +a
    local required=(PGHOST PGPORT PGDATABASE PGMAINTENANCE_DB PG_ADMIN_USER PG_ADMIN_PASSWORD APP_DB_USER APP_DB_PASSWORD)
    local key
    for key in "${required[@]}"; do
        if [[ -z "${!key:-}" ]]; then
            printf 'Missing value for %s in .env.\n' "$key" >&2
            return 1
        fi
    done
    if [[ "$PG_ADMIN_PASSWORD" == "$APP_DB_PASSWORD" ]]; then
        printf '%s\n' 'PG_ADMIN_PASSWORD and APP_DB_PASSWORD must be different.' >&2
        return 1
    fi
    if ! command -v psql >/dev/null 2>&1; then
        printf '%s\n' 'psql was not found in PATH. Install PostgreSQL client tools.' >&2
        return 1
    fi
    export PGPASSWORD="$PG_ADMIN_PASSWORD"
}

invoke_project_psql() {
    local database="$1"
    local sql_file="$2"
    shift 2
    load_project_environment
    psql --no-psqlrc --no-password --set ON_ERROR_STOP=1 --host "$PGHOST" --port "$PGPORT" --username "$PG_ADMIN_USER" --dbname "$database" "$@" --file "$sql_file"
}

invoke_reference_import() {
    local kind="$1"
    local csv_path="$2"
    local resolved_csv
    local safe_csv
    local runtime_directory="$COMMANDS_DIR/.runtime"
    local sql_file
    load_project_environment
    resolved_csv="$(realpath "$csv_path")"
    safe_csv="${resolved_csv//\'/\'\'}"
    mkdir -p "$runtime_directory"
    sql_file="$(mktemp "$runtime_directory/import-${kind}-XXXXXX.sql")"
    if [[ "$kind" == service ]]; then
        cat > "$sql_file" <<SQL
CREATE TEMP TABLE import_service (name text NOT NULL, price numeric(12, 2) NOT NULL);
\\copy import_service (name, price) FROM '$safe_csv' WITH (FORMAT csv, HEADER true, ENCODING 'UTF8')
INSERT INTO service (name, price) SELECT name, price FROM import_service ON CONFLICT (name) DO UPDATE SET price = EXCLUDED.price;
SQL
    else
        cat > "$sql_file" <<SQL
CREATE TEMP TABLE import_part (name text NOT NULL, article text NOT NULL, price numeric(12, 2) NOT NULL);
\\copy import_part (name, article, price) FROM '$safe_csv' WITH (FORMAT csv, HEADER true, ENCODING 'UTF8')
INSERT INTO part (name, article, price) SELECT name, article, price FROM import_part ON CONFLICT (article) DO UPDATE SET name = EXCLUDED.name, price = EXCLUDED.price;
SQL
    fi
    if invoke_project_psql "$PGDATABASE" "$sql_file"; then
        rm -f -- "$sql_file"
    else
        rm -f -- "$sql_file"
        return 1
    fi
}
