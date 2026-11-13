$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'common.psm1') -Force
Import-ProjectEnvironment
Invoke-ProjectPsql -Database $env:PGDATABASE -SqlFile (Join-Path $PSScriptRoot '..\sql\report-employee-cars.sql')
