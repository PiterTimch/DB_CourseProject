#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/common.sh"
csv_path="${1:-$COMMANDS_DIR/data/parts.csv}"
invoke_reference_import part "$csv_path"
