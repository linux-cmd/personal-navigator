[CmdletBinding()]
param([string]$PreviousTag,[string]$Version,[string]$OutputPath)
$range = if ($PreviousTag -eq 'v0.0.0') { 'HEAD' } else { "$PreviousTag..HEAD" }
$groups = [ordered]@{'Breaking Changes'=@();'Added'=@();'Fixed'=@();'Improved'=@();'Security'=@()}
foreach ($line in @(git log $range --format='%s')) {
  if ($line -match '^[a-z]+(?:\([^)]*\))?!:\s*(.+)$') {$groups['Breaking Changes'] += $Matches[1]}
  elseif ($line -match '^feat(?:\([^)]*\))?:\s*(.+)$') {$groups['Added'] += $Matches[1]}
  elseif ($line -match '^fix(?:\([^)]*\))?:\s*(.+)$') {$groups['Fixed'] += $Matches[1]}
  elseif ($line -match '^security(?:\([^)]*\))?:\s*(.+)$') {$groups['Security'] += $Matches[1]}
  elseif ($line -match '^(perf|refactor)(?:\([^)]*\))?:\s*(.+)$') {$groups['Improved'] += $Matches[2]}
}
$content = @("# Personal Navigator $Version",'')
foreach ($group in $groups.GetEnumerator()) { if ($group.Value.Count) { $content += "## $($group.Key)"; $content += ''; $content += $group.Value | ForEach-Object { "- $_" }; $content += '' } }
$content += @('## Install','','Download the versioned Windows setup executable and follow the wizard.')
Set-Content -LiteralPath $OutputPath -Value $content -Encoding utf8
