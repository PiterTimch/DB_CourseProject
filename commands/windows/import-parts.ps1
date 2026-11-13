param([string]$CsvPath = (Join-Path $PSScriptRoot '..\data\parts.csv'))
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'common.psm1') -Force
Invoke-ReferenceImport -Kind part -CsvPath $CsvPath
