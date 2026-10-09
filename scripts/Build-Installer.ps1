[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)$')]
    [string]$Version
)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$publish = Join-Path $repoRoot 'artifacts\win-x64'
$output = Join-Path $repoRoot 'artifacts'
$compiler = Join-Path ${env:ProgramFiles(x86)} 'NSIS\makensis.exe'
if (-not (Test-Path -LiteralPath $compiler -PathType Leaf)) {
    throw 'NSIS 3.x is required. Install with: choco install nsis -y'
}
if (-not (Test-Path -LiteralPath (Join-Path $publish 'PersonalNavigator.exe'))) {
    throw 'Published application not found. Run Build-Release.ps1 first.'
}
& $compiler "/DAPP_VERSION=$Version" "/DPUBLISH_DIR=$publish" "/DOUTPUT_DIR=$output" "/DSOURCE_ROOT=$repoRoot" (Join-Path $PSScriptRoot 'PersonalNavigator.nsi')
if ($LASTEXITCODE -ne 0) { throw "NSIS build failed: $LASTEXITCODE" }
$installer = Join-Path $output 'PersonalNavigator-Setup-win-x64.exe'
if (-not (Test-Path -LiteralPath $installer -PathType Leaf)) { throw 'NSIS did not produce an installer.' }
Write-Host "Installer: $installer"
