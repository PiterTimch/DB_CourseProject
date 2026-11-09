#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/common.sh"
load_project_environment
if [[ "$PGDATABASE" == "$PGMAINTENANCE_DB" ]]; then
    printf '%s\n' 'PGDATABASE cannot be the maintenance database.' >&2
    exit 1
fi
invoke_project_psql "$PGMAINTENANCE_DB" "$COMMANDS_DIR/sql/drop-database.sql" --set "database_name=$PGDATABASE"
