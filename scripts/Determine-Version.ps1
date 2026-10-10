[CmdletBinding()]
param(
  [ValidateSet('auto','patch','minor','major')][string]$RequestedBump = 'auto',
  [string]$Prerelease = '',
  [string]$OutputFile = $env:GITHUB_OUTPUT
)
$ErrorActionPreference = 'Stop'
$lastTag = git tag --list 'v[0-9]*' --sort=-version:refname | Select-Object -First 1
if (-not $lastTag) { $lastTag = 'v0.0.0' }
$base = $lastTag.TrimStart('v').Split('-')[0].Split('.') | ForEach-Object { [int]$_ }
$range = if ($lastTag -eq 'v0.0.0') { 'HEAD' } else { "$lastTag..HEAD" }
$log = @(git log $range --format='%s%n%b') -join "`n"
$bump = $RequestedBump
if ($bump -eq 'auto') {
  if ($log -match '(?m)^[a-z]+(?:\([^)]*\))?!:' -or $log -match '(?m)^BREAKING CHANGE:') { $bump = 'major' }
  elseif ($log -match '(?m)^feat(?:\([^)]*\))?:') { $bump = 'minor' }
  elseif ($log -match '(?m)^(fix|perf|security)(?:\([^)]*\))?:') { $bump = 'patch' }
  else { $bump = 'none' }
}
if ($bump -eq 'none') { @('should_release=false',"previous_tag=$lastTag",'bump=none') | Add-Content $OutputFile; exit 0 }
switch ($bump) { 'major'{$base[0]++;$base[1]=0;$base[2]=0}; 'minor'{$base[1]++;$base[2]=0}; 'patch'{$base[2]++} }
$version = $base -join '.'
if ($Prerelease) { if ($Prerelease -notmatch '^[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*$') { throw 'Invalid prerelease identifier.' }; $version += "-$Prerelease" }
if (git tag --list "v$version") { throw "Tag v$version already exists." }
$isPrerelease = if ($Prerelease) { 'true' } else { 'false' }
@('should_release=true',"previous_tag=$lastTag","bump=$bump","version=$version","tag=v$version","prerelease=$isPrerelease") | Add-Content $OutputFile
