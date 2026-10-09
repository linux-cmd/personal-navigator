# Release and versioning policy

Use stable semantic versions: `MAJOR.MINOR.PATCH`, with Git tags named `vMAJOR.MINOR.PATCH`. The release workflow rejects other version-tag formats.

- **PATCH:** backward-compatible bug fixes and packaging repairs.
- **MINOR:** backward-compatible functionality.
- **MAJOR:** incompatible changes to behavior, settings or persisted data formats.

Update CHANGELOG.md before publishing. Never reuse a released version number or move an existing tag. The release workflow injects the version tag into the .NET app file and assembly versions.

## Maintainer checklist

1. Merge reviewed code to main after Windows CI succeeds.
2. Update CHANGELOG.md and user-facing docs.
3. Tag that tested commit with `git tag -a vX.Y.Z -m "Release vX.Y.Z"`; push with `git push origin vX.Y.Z`.
4. GitHub Actions verifies the tag, runs build and smoke tests, publishes the app, builds an NSIS installer, calculates SHA256SUMS.txt, and publishes a GitHub Release with both the installer and legacy ZIP.
5. **Manually test** fresh installation, upgrading and uninstalling on Windows 10/11. CI success does not guarantee correct UI or setup behavior.
6. Verify the published installer asset and the website download selection.

On Windows with .NET 8 and NSIS 3 installed, build the files locally:

```powershell
.\scripts\Build-Release.ps1 -Version 1.1.0
.\scripts\Build-Installer.ps1 -Version 1.1.0
```

The generated setup EXE is at `artifacts\PersonalNavigator-Setup-win-x64.exe`. Currently there is no automatic background updater, code signing, or automated semantic version bump. Major-version rollback may require data migration.
