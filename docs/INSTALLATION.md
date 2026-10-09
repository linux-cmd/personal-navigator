# Installation

## Windows 10 and Windows 11

1. Download `PersonalNavigator-win-x64.zip` from the [latest GitHub release](https://github.com/linux-cmd/personal-navigator/releases/latest).
2. Right-click the archive, choose **Extract All**, and open the extracted folder.
3. Right-click `Install-PersonalNavigator.ps1` and choose **Run with PowerShell**.
4. Press `Ctrl+Alt+Space` to open Personal Navigator.

The installer copies the application to `%LOCALAPPDATA%\Programs\Personal Navigator`, creates Start Menu and Send To shortcuts, enables startup for the current user, and adds **Open Personal Map** to File Explorer folder menus. Administrator access is not required.

If Windows blocks the script, open PowerShell in the extracted folder and run:

```powershell
powershell -ExecutionPolicy Bypass -File .\Install-PersonalNavigator.ps1
```

The release is currently unsigned, so Windows SmartScreen may ask for confirmation. Review the source and the release workflow if you want to verify how it was built.

## Portable use

Run `PersonalNavigator.exe` directly from the extracted folder. Portable use skips the Explorer shortcuts and automatic startup.

## Linux and macOS

There is no supported desktop build yet. The current interface uses Windows Presentation Foundation and Windows shell integration.
