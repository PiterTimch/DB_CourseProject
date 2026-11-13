$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'common.psm1') -Force
Import-ProjectEnvironment
Invoke-ProjectPsql -Database $env:PGMAINTENANCE_DB -SqlFile (Join-Path $PSScriptRoot '..\sql\create-database.sql')
Invoke-ProjectPsql -Database $env:PGDATABASE -SqlFile (Join-Path $PSScriptRoot '..\sql\schema.sql')
