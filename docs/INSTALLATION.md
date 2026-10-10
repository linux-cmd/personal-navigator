# Installing Personal Navigator

## Recommended Windows setup wizard

1. Visit [GitHub Releases](https://github.com/linux-cmd/personal-navigator/releases/latest).
2. Download and run **PersonalNavigator-Setup-&lt;version&gt;-win-x64.exe**.
3. Follow the Welcome, License, Directory, Install and Finish screens.
4. Launch from the Start menu and press **Ctrl+Alt+Space**.

**Requirements:** Windows 10 or 11 x64. The default Program Files installation requests administrator permission. The binaries are currently unsigned. Only download from official releases and verify against SHA256SUMS.txt.

**Required first-run directory:** The program currently indexes `%USERPROFILE%\Personal`. Create that folder and move or copy the projects you wish to search into it. Configuring another root folder is not yet supported.

The default installation location is `%ProgramFiles%\Personal Navigator`. Setup creates Start menu and Send To shortcuts, optional desktop and startup entries, Explorer context menu commands, and a normal Installed Apps entry.

## Upgrade and uninstall

Run the installer from a newer release to upgrade. Setup closes the previous application and preserves the local index and settings under `%LOCALAPPDATA%\PersonalNavigator`.

**Automatic in-app updates are not implemented.** Check the latest GitHub release manually.

Uninstall through Windows Settings → Installed apps or the Start menu's Uninstall Personal Navigator shortcut. Preferences and the cached index remain on disk until you manually remove `%LOCALAPPDATA%\PersonalNavigator`.

## Linux and macOS

Not supported by the current Windows Presentation Foundation application.
