using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace PersonalNavigator;

public sealed class NavigatorSettings
{
    public bool IncludeFiles { get; set; } = true;
    public bool IncludeHidden { get; set; }
    public bool Animate { get; set; } = true;
    public bool AlwaysOnTop { get; set; } = true;
    public bool StartWithWindows { get; set; } = true;
    public bool DefaultDeepSearch { get; set; }
    public int MaxResults { get; set; } = 10;
    public double NodeScale { get; set; } = 1.0;
    public string AllowedExtensions { get; set; } = "";
    public string ExcludedFolders { get; set; } = ".git,node_modules,.next,.gradle,.venv,venv,build,dist,bin,obj,app,Binaries,DerivedDataCache,Intermediate,Saved,.vs,__pycache__,.pytest_cache";

    public static string DataDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PersonalNavigator");

    public static string SettingsPath => Path.Combine(DataDirectory, "settings.json");
    public static string CachePath => Path.Combine(DataDirectory, "index.json");

    public static NavigatorSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
                return JsonSerializer.Deserialize<NavigatorSettings>(File.ReadAllText(SettingsPath)) ?? new();
        }
        catch { }
        return new();
    }

    public void Save()
    {
        Directory.CreateDirectory(DataDirectory);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }

    public HashSet<string> ExcludedFolderSet() => ExcludedFolders
        .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    public HashSet<string> AllowedExtensionSet() => AllowedExtensions
        .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
        .Select(x => x.StartsWith('.') ? x : "." + x)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);
}
