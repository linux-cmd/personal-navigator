# Contributing

Thanks for taking the time to improve Personal Navigator.

## Before opening a change

- Search existing issues first.
- Keep changes focused on one problem.
- Preserve local-only indexing and search unless a discussion has agreed otherwise.
- Add or update a smoke-test assertion when behavior changes.

## Local checks

Run these commands on Windows with the .NET 8 SDK:

```powershell
dotnet build .\program\PersonalNavigator.csproj -c Release
dotnet run --project .\program\tests\SmokeTest\SmokeTest.csproj -c Release
```

Pull requests should explain the user-facing change, testing performed, and any effect on indexing or Windows integration.
