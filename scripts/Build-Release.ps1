[CmdletBinding()]
param(
    [ValidatePattern('^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(-[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?$')]
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
New-Item -ItemType Directory -Force -Path $outputRoot,$publishDirectory,$packageDirectory | Out-Null
Get-ChildItem -LiteralPath $publishDirectory -Force | Remove-Item -Recurse -Force
Get-ChildItem -LiteralPath $packageDirectory -Force | Remove-Item -Recurse -Force
$numericVersion = $Version.Split('-')[0]
dotnet publish $project -c $Configuration -r $Runtime --self-contained true `
    "-p:Version=$Version" "-p:AssemblyVersion=$numericVersion.0" "-p:FileVersion=$numericVersion.0" `
    -p:PublishSingleFile=true -p:PublishTrimmed=false -p:DebugType=None -p:DebugSymbols=false `
    -o $publishDirectory
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed.' }
Write-Host "Published application: $publishDirectory"
