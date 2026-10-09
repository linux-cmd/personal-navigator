# Personal Navigator

Personal Navigator is a compact Windows utility that turns a large personal folder into a visual, searchable map. It favors useful project roots over long lists of individual files, while keeping deeper results one click away.

Created and maintained by **Abhijay Panwar**.

[Download for Windows](https://github.com/linux-cmd/personal-navigator/releases/latest/download/PersonalNavigator-win-x64.zip) | [Website](https://linux-cmd.github.io/personal-navigator/) | [Installation guide](docs/INSTALLATION.md)

## What it does

- Opens from anywhere with `Ctrl+Alt+Space`.
- Adds **Open Personal Map** to the File Explorer context menu.
- Finds related projects from approximate language, not only exact names.
- Groups normal results by project root to keep the map readable.
- Switches to deeper file and subfolder results when requested.
- Runs locally and keeps the index on the computer.
- Exposes file types, exclusions, animation, startup, and display controls in settings.

For example, searching for `bionic hand` can surface an ECHO project even when those words are not in its folder name. Typo-tolerant matching also handles searches such as `clas mate`.

## Install

1. Download `PersonalNavigator-win-x64.zip` from the latest release.
2. Extract the archive.
3. Run `Install-PersonalNavigator.ps1` with PowerShell.
4. Press `Ctrl+Alt+Space`.

The build is self-contained for 64-bit Windows 10 and Windows 11. See the [full installation guide](docs/INSTALLATION.md) for SmartScreen and manual-run details.

## Repository layout

| Folder | Purpose |
| --- | --- |
| `program` | WPF desktop application and smoke tests |
| `website` | Static product website for GitHub Pages or Vercel |
| `scripts` | Local installation and release packaging |
| `docs` | Installation and architecture notes |
| `.github` | CI, release, Pages, and contribution workflows |

## Development

Requirements: Windows, Git, and the .NET 8 SDK.

```powershell
dotnet build .\program\PersonalNavigator.csproj -c Release
dotnet run --project .\program\tests\SmokeTest\SmokeTest.csproj -c Release
.\scripts\Build-Release.ps1
```

The app intentionally avoids a server, database, package manager, and web runtime. The published Windows release is a single self-contained executable.

## Platform status

The desktop app currently supports Windows x64. Linux and macOS are listed on the website as planned work because the current WPF interface is Windows-specific. Contributions toward a cross-platform interface are welcome.

## License

Personal Navigator is available under the [MIT License](LICENSE).
