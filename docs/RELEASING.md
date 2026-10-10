# Release system

Git tags are the application version authority. Application changes merged into `main` use Conventional Commits.

- `fix:` produces a patch release.
- `feat:` produces a minor release.
- `feat!:` or a `BREAKING CHANGE:` footer produces a major release.
- Website, documentation, formatting, and workflow-only changes do not produce an application release.

The semantic release workflow calculates the next available version, validates the app, builds the self-contained executable, compiles and silently tests the Windows installer, verifies uninstall, writes categorized notes and a SHA-256 checksum, and publishes an immutable GitHub Release.

Major releases wait for approval through the protected GitHub environment named `major-release`. Configure at least one required reviewer in repository Settings, Environments, major-release.

Use the workflow's manual dispatch inputs for an explicit bump or a prerelease identifier such as `beta.1`. Code signing remains disabled until a valid certificate is added securely and the resulting signature is verified.
