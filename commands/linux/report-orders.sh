#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/common.sh"
load_project_environment
invoke_application_psql "$PGDATABASE" "$COMMANDS_DIR/sql/report-orders.sql"
