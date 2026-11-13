$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'common.psm1') -Force
Import-ProjectEnvironment
if ($env:PGDATABASE -eq $env:PGMAINTENANCE_DB) { throw 'PGDATABASE cannot be the maintenance database.' }
Invoke-ProjectPsql -Database $env:PGMAINTENANCE_DB -SqlFile (Join-Path $PSScriptRoot '..\sql\drop-database.sql') -Variables @("database_name=$env:PGDATABASE")
