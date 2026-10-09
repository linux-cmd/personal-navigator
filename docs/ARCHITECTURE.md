# Architecture

Personal Navigator is a small native Windows application with no required server.

## Main components

- `SearchEngine.cs` scans names and paths, identifies project roots, scores approximate matches, and caches the index.
- `MainWindow.xaml` and `MainWindow.xaml.cs` render the map and handle navigation, search, settings, and file actions.
- `WindowsIntegration.cs` manages the global hotkey, tray behavior, and shell-related helpers.
- `NavigatorSettings.cs` stores user preferences under `%LOCALAPPDATA%\PersonalNavigator`.

## Search flow

1. Build an index from the user's Personal folder.
2. Ignore generated and dependency folders by default.
3. Expand a small local vocabulary for related concepts.
4. Score exact, token, path, prefix, and typo-tolerant matches.
5. Collapse normal results to useful project roots.
6. Return files and nested folders only in detailed mode.

The search path stays local. File contents are not uploaded or sent to a remote service.
