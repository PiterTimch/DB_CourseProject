$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'common.psm1') -Force
Import-ProjectEnvironment
Invoke-ApplicationPsql -Database $env:PGDATABASE -SqlFile (Join-Path $PSScriptRoot '..\sql\report-orders.sql')
