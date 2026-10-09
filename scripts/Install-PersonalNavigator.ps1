[CmdletBinding()]
param(
    [string]$SourceExecutable = (Join-Path $PSScriptRoot 'PersonalNavigator.exe'),
    [switch]$NoLaunch
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $SourceExecutable -PathType Leaf)) {
    throw "PersonalNavigator.exe was not found next to this installer. Extract the full release archive before running Install-PersonalNavigator.ps1."
}

$installDirectory = Join-Path $env:LOCALAPPDATA 'Programs\Personal Navigator'
$executable = Join-Path $installDirectory 'PersonalNavigator.exe'
New-Item -ItemType Directory -Force -Path $installDirectory | Out-Null

Get-Process PersonalNavigator -ErrorAction SilentlyContinue | Stop-Process -Force
Copy-Item -LiteralPath $SourceExecutable -Destination $executable -Force

function Set-ExplorerCommand([string]$KeyPath, [string]$Arguments) {
    New-Item -Path $KeyPath -Force | Out-Null
    Set-ItemProperty -Path $KeyPath -Name '(default)' -Value ('"' + $executable + '" ' + $Arguments)
}

$backgroundKey = 'HKCU:\Software\Classes\Directory\Background\shell\PersonalNavigator'
New-Item -Path $backgroundKey -Force | Out-Null
Set-ItemProperty -Path $backgroundKey -Name 'MUIVerb' -Value 'Open Personal Map'
Set-ItemProperty -Path $backgroundKey -Name 'Icon' -Value $executable
Set-ExplorerCommand (Join-Path $backgroundKey 'command') '"%V"'

$directoryKey = 'HKCU:\Software\Classes\Directory\shell\PersonalNavigator'
New-Item -Path $directoryKey -Force | Out-Null
Set-ItemProperty -Path $directoryKey -Name 'MUIVerb' -Value 'Open Personal Map'
Set-ItemProperty -Path $directoryKey -Name 'Icon' -Value $executable
Set-ExplorerCommand (Join-Path $directoryKey 'command') '"%1"'

$runKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
Set-ItemProperty -Path $runKey -Name 'PersonalNavigator' -Value ('"' + $executable + '" --background')

$shell = New-Object -ComObject WScript.Shell
$shortcutPaths = @(
    (Join-Path ([Environment]::GetFolderPath('Programs')) 'Personal Navigator.lnk'),
    (Join-Path ([Environment]::GetFolderPath('SendTo')) 'Personal Navigator.lnk')
)

foreach ($shortcutPath in $shortcutPaths) {
    $shortcut = $shell.CreateShortcut($shortcutPath)
    $shortcut.TargetPath = $executable
    $shortcut.WorkingDirectory = $installDirectory
    $shortcut.IconLocation = $executable
    $shortcut.Description = 'Fast visual search and navigation for your Personal folder'
    $shortcut.Save()
}

Write-Host "Personal Navigator installed to $installDirectory"
Write-Host 'Press Ctrl+Alt+Space from anywhere to open it.'

if (-not $NoLaunch) {
    Start-Process -FilePath $executable
}
