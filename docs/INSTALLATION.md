# Installing Personal Navigator

## Recommended Windows setup wizard

1. Visit [GitHub Releases](https://github.com/linux-cmd/personal-navigator/releases/latest).
2. For a release containing **PersonalNavigator-Setup-win-x64.exe**, download and run that file. The current v1.0.0 release only has a ZIP.
3. Follow the Welcome, License, Directory, Install and Finish screens.
4. Launch from the Start menu and press **Ctrl+Alt+Space**.

**Requirements:** Windows 10 or 11 x64. The installer installs per Windows user without requiring administrator access. The binaries are currently unsigned. Only download from official releases and verify against SHA256SUMS.txt where available.

**Required first-run directory:** The program currently indexes `%USERPROFILE%\Personal`. Create that folder and move or copy the projects you wish to search into it. Configuring another root folder is not yet supported.

The default installation location is `%LOCALAPPDATA%\Programs\Personal Navigator`. Setup creates Start menu shortcuts, the Send To shortcut, Explorer context menu commands, and a current-user startup entry.

## Upgrade and uninstall

Run the installer from a newer release to upgrade. Setup closes the previous application and preserves the local index and settings under `%LOCALAPPDATA%\PersonalNavigator`.

**Automatic in-app updates are not implemented.** Check the latest GitHub release manually.

Uninstall through Windows Settings → Installed apps or the Start menu's Uninstall Personal Navigator shortcut. Preferences and the cached index remain on disk until you manually remove `%LOCALAPPDATA%\PersonalNavigator`.

## Legacy ZIP / portable installation

For v1.0.0 and releases without a setup executable:

1. Download and extract `PersonalNavigator-win-x64.zip`.
2. Right-click `Install-PersonalNavigator.ps1` and choose Run with PowerShell.
3. Press **Ctrl+Alt+Space**.

If PowerShell blocks the script, inspect its contents before executing:

```powershell
powershell -ExecutionPolicy Bypass -File .\Install-PersonalNavigator.ps1
```

Alternatively run `PersonalNavigator.exe` in the extracted folder. Portable mode does not create Windows integration entries.

## Linux and macOS

Not supported by the current Windows Presentation Foundation application.
