# Personal Navigator instructions

## Versioning

- Git release tags are authoritative. Never hardcode a published version into website content.
- Use Conventional Commits for application changes: `fix:` for patch, `feat:` for minor, and `feat!:` or `BREAKING CHANGE:` for major.
- Website-only, documentation-only, formatting-only, and workflow-only work must not release the application.
- Never move, replace, or reuse an existing version tag or versioned binary.
- Major releases require approval through the protected `major-release` GitHub environment.

## Required validation

- Run `dotnet build .\program\PersonalNavigator.csproj -c Release`.
- Run `dotnet run --project .\program\tests\SmokeTest\SmokeTest.csproj -c Release`.
- Installer changes require a compiled installer, silent install, installed version check, and silent uninstall.
- Run `node --check .\website\app.js` for website script changes.
- Do not claim code signing unless a real certificate is configured and the signature is verified.

## Distribution

- Pass the calculated version to MSBuild and NSIS so application metadata, installer metadata, tag, and release agree.
- Publish only the versioned setup executable and `SHA256SUMS.txt` after validation.
- Keep earlier releases available.
- Website downloads and history must come from actual GitHub Release assets. Never use GitHub source archives as application downloads.

## Website

- Maintain separate Home, Features, Download, Changelog, Documentation, About, Support, and Roadmap pages.
- Keep navigation, responsive layout, titles, metadata, loading states, and release API fallback consistent.
- Describe implemented behavior only.
