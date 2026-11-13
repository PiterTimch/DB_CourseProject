$script:ProjectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$script:CommandsRoot = Join-Path $script:ProjectRoot 'commands'

function Import-ProjectEnvironment {
    $envFile = Join-Path $script:ProjectRoot '.env'
    if (-not (Test-Path -LiteralPath $envFile)) {
        throw "Missing .env file. Copy .env.example to .env and set PG_ADMIN_PASSWORD."
    }

    foreach ($line in Get-Content -LiteralPath $envFile) {
        $entry = $line.Trim()
        if (-not $entry -or $entry.StartsWith('#')) { continue }
        $separator = $entry.IndexOf('=')
        if ($separator -lt 1) { continue }
        $key = $entry.Substring(0, $separator).Trim()
        $value = $entry.Substring($separator + 1).Trim()
        if ($value.Length -ge 2 -and (($value.StartsWith('"') -and $value.EndsWith('"')) -or ($value.StartsWith("'") -and $value.EndsWith("'")))) {
            $value = $value.Substring(1, $value.Length - 2)
        }
        [Environment]::SetEnvironmentVariable($key, $value, 'Process')
    }

    $required = @('PGHOST', 'PGPORT', 'PGDATABASE', 'PGMAINTENANCE_DB', 'PG_ADMIN_USER', 'PG_ADMIN_PASSWORD', 'APP_DB_USER', 'APP_DB_PASSWORD')
    foreach ($key in $required) {
        if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($key, 'Process'))) {
            throw "Missing value for $key in .env."
        }
    }

    if ([Environment]::GetEnvironmentVariable('PG_ADMIN_PASSWORD', 'Process') -eq [Environment]::GetEnvironmentVariable('APP_DB_PASSWORD', 'Process')) {
        throw 'PG_ADMIN_PASSWORD and APP_DB_PASSWORD must be different.'
    }

    if (-not (Get-Command psql -ErrorAction SilentlyContinue)) {
        throw 'psql was not found in PATH. Install PostgreSQL client tools and add psql to PATH.'
    }

    $env:PGPASSWORD = [Environment]::GetEnvironmentVariable('PG_ADMIN_PASSWORD', 'Process')
}

function Invoke-ProjectPsql {
    param(
        [Parameter(Mandatory = $true)][string]$Database,
        [string]$SqlFile,
        [string[]]$Variables = @()
    )

    Import-ProjectEnvironment
    $arguments = @('-X', '-w', '--set', 'ON_ERROR_STOP=1', '--host', $env:PGHOST, '--port', $env:PGPORT, '--username', $env:PG_ADMIN_USER, '--dbname', $Database)
    foreach ($variable in $Variables) { $arguments += @('--set', $variable) }
    if ($SqlFile) { $arguments += @('--file', $SqlFile) }
    & psql @arguments
    if ($LASTEXITCODE -ne 0) { throw "psql failed with exit code $LASTEXITCODE." }
}

function Invoke-ReferenceImport {
    param(
        [Parameter(Mandatory = $true)][ValidateSet('service', 'part')][string]$Kind,
        [Parameter(Mandatory = $true)][string]$CsvPath
    )

    Import-ProjectEnvironment
    $resolvedCsv = (Resolve-Path -LiteralPath $CsvPath -ErrorAction Stop).Path.Replace('\', '/')
    $quotedCsv = $resolvedCsv.Replace("'", "''")
    $runtimeDirectory = Join-Path $script:CommandsRoot '.runtime'
    New-Item -ItemType Directory -Path $runtimeDirectory -Force | Out-Null
    $sqlFile = Join-Path $runtimeDirectory ("import-{0}-{1}.sql" -f $Kind, [guid]::NewGuid().ToString('N'))

    if ($Kind -eq 'service') {
        $sql = @"
CREATE TEMP TABLE import_service (name text NOT NULL, price numeric(12, 2) NOT NULL);
\copy import_service (name, price) FROM '$quotedCsv' WITH (FORMAT csv, HEADER true, ENCODING 'UTF8')
INSERT INTO service (name, price) SELECT name, price FROM import_service ON CONFLICT (name) DO UPDATE SET price = EXCLUDED.price;
"@
    } else {
        $sql = @"
CREATE TEMP TABLE import_part (name text NOT NULL, article text NOT NULL, price numeric(12, 2) NOT NULL);
\copy import_part (name, article, price) FROM '$quotedCsv' WITH (FORMAT csv, HEADER true, ENCODING 'UTF8')
INSERT INTO part (name, article, price) SELECT name, article, price FROM import_part ON CONFLICT (article) DO UPDATE SET name = EXCLUDED.name, price = EXCLUDED.price;
"@
    }

    [System.IO.File]::WriteAllText($sqlFile, $sql, [System.Text.UTF8Encoding]::new($false))
    try {
        Invoke-ProjectPsql -Database $env:PGDATABASE -SqlFile $sqlFile
    } finally {
        Remove-Item -LiteralPath $sqlFile -Force -ErrorAction SilentlyContinue
    }
}

Export-ModuleMember -Function Import-ProjectEnvironment, Invoke-ProjectPsql, Invoke-ReferenceImport
