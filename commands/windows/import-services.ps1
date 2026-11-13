param([string]$CsvPath = (Join-Path $PSScriptRoot '..\data\services.csv'))
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'common.psm1') -Force
Invoke-ReferenceImport -Kind service -CsvPath $CsvPath
