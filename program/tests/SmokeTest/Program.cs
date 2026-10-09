using PersonalNavigator;

string fixtureRoot = Path.Combine(Path.GetTempPath(), $"PersonalNavigator-Smoke-{Guid.NewGuid():N}");

try
{
    string[] areas = ["Archives", "Engineering", "Games", "Learning", "Media", "Projects", "Tools"];
    foreach (string area in areas) Directory.CreateDirectory(Path.Combine(fixtureRoot, area));

    string echo = Path.Combine(fixtureRoot, "Engineering", "Bionic Hand", "Software", "ECHO");
    Directory.CreateDirectory(Path.Combine(echo, "Firmware"));
    Directory.CreateDirectory(Path.Combine(echo, "3D Prints & Exports"));
    File.WriteAllText(Path.Combine(echo, "package.json"), "{}");
    File.WriteAllText(Path.Combine(echo, "Firmware", "serial_test.py"), "print('serial test')");

    string classMate = Path.Combine(fixtureRoot, "Projects", "Apps & Web", "ClassMate");
    Directory.CreateDirectory(classMate);
    File.WriteAllText(Path.Combine(classMate, "package.json"), "{}");

    string fortnite = Path.Combine(fixtureRoot, "Games", "Backups", "Fortnite Settings");
    Directory.CreateDirectory(fortnite);
    File.WriteAllText(Path.Combine(fortnite, "GameUserSettings.ini"), "[Settings]");

    var settings = new NavigatorSettings { IncludeFiles = true, MaxResults = 12 };
    var engine = new SearchEngine(fixtureRoot);
    var buildTimer = System.Diagnostics.Stopwatch.StartNew();
    int count = await engine.RebuildAsync(settings);
    buildTimer.Stop();

    if (count < 15) throw new Exception($"Index unexpectedly small: {count}");
    var top = engine.Children(fixtureRoot, 20);
    foreach (string area in areas)
        if (!top.Any(x => x.Name.Equals(area, StringComparison.OrdinalIgnoreCase)))
            throw new Exception($"Missing top-level area: {area}");

    var searchTimer = System.Diagnostics.Stopwatch.StartNew();
    var bionic = engine.Search("bionic hand", deep: false, limit: 12);
    searchTimer.Stop();
    if (!bionic.Any(x => x.Name.Contains("ECHO", StringComparison.OrdinalIgnoreCase)))
        throw new Exception("Related search did not surface ECHO for 'bionic hand'.");
    if (bionic.Any(x => x.Name.Equals("Personal Navigator", StringComparison.OrdinalIgnoreCase)))
        throw new Exception("Related search returned an unrelated project.");

    var typo = engine.Search("clas mate", deep: false, limit: 12);
    if (!typo.Any(x => x.Name.Contains("ClassMate", StringComparison.OrdinalIgnoreCase)))
        throw new Exception("Typo-tolerant search did not surface ClassMate.");

    var academic = engine.Search("academic study", deep: false, limit: 12);
    if (!academic.Any(x => x.Name.Contains("ClassMate", StringComparison.OrdinalIgnoreCase)))
        throw new Exception("Related search did not connect academic study to ClassMate.");

    var settingsResult = engine.Search("fortnite settings", deep: false, limit: 12);
    if (!settingsResult.Any(x => x.RelativePath.Contains("Fortnite", StringComparison.OrdinalIgnoreCase)))
        throw new Exception("Fortnite settings search did not surface the backup group.");

    var detailed = engine.Search("serial test", deep: true, limit: 25);
    if (!detailed.Any(x => x.Name.Contains("serial_test", StringComparison.OrdinalIgnoreCase)))
        throw new Exception("Detailed search did not surface serial_test.py.");

    Console.WriteLine($"PASS index={count:N0} build={buildTimer.ElapsedMilliseconds}ms search={searchTimer.ElapsedMilliseconds}ms");
    Console.WriteLine("PASS top-level map");
    Console.WriteLine("PASS bionic hand -> ECHO");
    Console.WriteLine("PASS typo search -> ClassMate");
    Console.WriteLine("PASS academic study -> ClassMate");
    Console.WriteLine("PASS Fortnite settings grouping");
    Console.WriteLine("PASS detailed file search -> serial_test.py");
}
finally
{
    if (Directory.Exists(fixtureRoot)) Directory.Delete(fixtureRoot, recursive: true);
}
