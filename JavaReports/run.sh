#!/usr/bin/env bash
set -euo pipefail
repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repository_root"
if [[ -z "${JAVA_HOME:-}" ]]; then
    printf '%s\n' 'JAVA_HOME must point to JDK 25.' >&2
    exit 1
fi
"$JAVA_HOME/bin/java" -version
mvn -f JavaReports/pom.xml javafx:run
