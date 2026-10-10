[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(-[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?$')]
    [string]$Version
)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$publish = Join-Path $repoRoot 'artifacts\win-x64'
$output = Join-Path $repoRoot 'artifacts'
$compiler = Join-Path ${env:ProgramFiles(x86)} 'NSIS\makensis.exe'
if (-not (Test-Path -LiteralPath $compiler -PathType Leaf)) {
    $command = Get-Command makensis -ErrorAction SilentlyContinue
    if ($command) { $compiler = $command.Source }
    else { throw 'NSIS 3.x is required. Install with Chocolatey or Scoop.' }
}
if (-not (Test-Path -LiteralPath (Join-Path $publish 'PersonalNavigator.exe'))) {
    throw 'Published application not found. Run Build-Release.ps1 first.'
}
$numericVersion = $Version.Split('-')[0]
& $compiler "/DAPP_VERSION=$Version" "/DAPP_FILE_VERSION=$numericVersion.0" "/DPUBLISH_DIR=$publish" "/DOUTPUT_DIR=$output" "/DSOURCE_ROOT=$repoRoot" (Join-Path $PSScriptRoot 'PersonalNavigator.nsi')
if ($LASTEXITCODE -ne 0) { throw "NSIS build failed: $LASTEXITCODE" }
$installer = Join-Path $output "PersonalNavigator-Setup-$Version-win-x64.exe"
if (-not (Test-Path -LiteralPath $installer -PathType Leaf)) { throw 'NSIS did not produce an installer.' }
Write-Host "Installer: $installer"
