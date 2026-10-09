# Application source

This folder contains the Windows desktop application and its smoke tests.

```powershell
dotnet build .\PersonalNavigator.csproj -c Release
dotnet run --project .\tests\SmokeTest\SmokeTest.csproj -c Release
```

The app is built with WPF on .NET 8. Indexing and search happen locally. User settings and the search cache are stored under `%LOCALAPPDATA%\PersonalNavigator`.
