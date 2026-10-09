[CmdletBinding()]
param(
    [ValidatePattern('^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)$')]
    [string]$Version = '1.0.0',
    [string]$Configuration = 'Release',
    [ValidateSet('win-x64')]
    [string]$Runtime = 'win-x64'
)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot 'program\PersonalNavigator.csproj'
$outputRoot = Join-Path $repoRoot 'artifacts'
$publishDirectory = Join-Path $outputRoot $Runtime
$packageDirectory = Join-Path $outputRoot 'package'
$archive = Join-Path $outputRoot "PersonalNavigator-$Runtime.zip"
New-Item -ItemType Directory -Force -Path $outputRoot,$publishDirectory,$packageDirectory | Out-Null
Get-ChildItem -LiteralPath $publishDirectory -Force | Remove-Item -Recurse -Force
Get-ChildItem -LiteralPath $packageDirectory -Force | Remove-Item -Recurse -Force
dotnet publish $project -c $Configuration -r $Runtime --self-contained true `
    "-p:Version=$Version" "-p:AssemblyVersion=$Version.0" "-p:FileVersion=$Version.0" `
    -p:PublishSingleFile=true -p:PublishTrimmed=false -p:DebugType=None -p:DebugSymbols=false `
    -o $publishDirectory
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed.' }
Copy-Item -LiteralPath (Join-Path $publishDirectory 'PersonalNavigator.exe') -Destination $packageDirectory
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Install-PersonalNavigator.ps1') -Destination $packageDirectory
Copy-Item -LiteralPath (Join-Path $repoRoot 'docs\INSTALLATION.md') -Destination (Join-Path $packageDirectory 'README.md')
if (Test-Path -LiteralPath $archive) { Remove-Item -LiteralPath $archive -Force }
Compress-Archive -Path (Join-Path $packageDirectory '*') -DestinationPath $archive -CompressionLevel Optimal
Write-Host "Release package: $archive"
