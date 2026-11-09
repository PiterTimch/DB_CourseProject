#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/common.sh"
load_project_environment
invoke_project_psql "$PGMAINTENANCE_DB" "$COMMANDS_DIR/sql/create-database.sql"
invoke_project_psql "$PGDATABASE" "$COMMANDS_DIR/sql/schema.sql"
