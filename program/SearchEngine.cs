using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PersonalNavigator;

public sealed class SearchEngine
{
    private const int CacheVersion = 2;
    private readonly object _gate = new();
    private List<IndexEntry> _entries = [];
    private readonly Dictionary<string, IndexEntry> _byPath = new(StringComparer.OrdinalIgnoreCase);

    public string RootPath { get; }
    public int EntryCount { get { lock (_gate) return _entries.Count; } }
    public event Action<int>? IndexUpdated;

    public SearchEngine(string rootPath) => RootPath = Path.GetFullPath(rootPath).TrimEnd(Path.DirectorySeparatorChar);

    public bool TryLoadCache()
    {
        try
        {
            if (!File.Exists(NavigatorSettings.CachePath)) return false;
            var cache = JsonSerializer.Deserialize<IndexCache>(File.ReadAllText(NavigatorSettings.CachePath));
            if (cache is null || cache.Version != CacheVersion || cache.Entries.Count == 0) return false;
            ReplaceEntries(cache.Entries);
            return true;
        }
        catch { return false; }
    }

    public async Task<int> RebuildAsync(NavigatorSettings settings, CancellationToken cancellationToken = default)
    {
        var entries = await Task.Run(() => BuildEntries(settings, cancellationToken), cancellationToken);
        ReplaceEntries(entries);
        _ = Task.Run(() => SaveCache(entries));
        IndexUpdated?.Invoke(entries.Count);
        return entries.Count;
    }

    private void ReplaceEntries(List<IndexEntry> entries)
    {
        lock (_gate)
        {
            _entries = entries;
            _byPath.Clear();
            foreach (var entry in entries) _byPath[entry.Path] = entry;
        }
    }

    private void SaveCache(List<IndexEntry> entries)
    {
        try
        {
            Directory.CreateDirectory(NavigatorSettings.DataDirectory);
            string temporary = NavigatorSettings.CachePath + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(new IndexCache
            {
                Version = CacheVersion,
                CreatedUtc = DateTime.UtcNow,
                Entries = entries
            }));
            File.Move(temporary, NavigatorSettings.CachePath, true);
        }
        catch { }
    }

    private List<IndexEntry> BuildEntries(NavigatorSettings settings, CancellationToken token)
    {
        var result = new List<IndexEntry>(24_000);
        var excluded = settings.ExcludedFolderSet();
        var allowedExtensions = settings.AllowedExtensionSet();
        var stack = new Stack<(string Path, string? InheritedRoot)>();
        stack.Push((RootPath, null));

        while (stack.Count > 0)
        {
            token.ThrowIfCancellationRequested();
            var (directory, inheritedRoot) = stack.Pop();
            DirectoryInfo info;
            try { info = new DirectoryInfo(directory); }
            catch { continue; }

            if (!directory.Equals(RootPath, StringComparison.OrdinalIgnoreCase))
            {
                if (excluded.Contains(info.Name)) continue;
                if (!settings.IncludeHidden && IsHidden(info.Attributes)) continue;
                if (info.Attributes.HasFlag(FileAttributes.ReparsePoint)) continue;
            }

            string relative = Path.GetRelativePath(RootPath, directory);
            bool markerRoot = IsProjectMarkerDirectory(directory);
            string projectRoot = markerRoot ? directory : inheritedRoot ?? DefaultProjectRoot(directory);
            result.Add(CreateEntry(info.Name, directory, relative == "." ? string.Empty : relative,
                true, projectRoot, 0, SafeModified(info), string.Empty));

            try
            {
                foreach (string childDirectory in Directory.EnumerateDirectories(directory))
                    stack.Push((childDirectory, markerRoot ? directory : inheritedRoot));

                if (!settings.IncludeFiles) continue;
                foreach (string file in Directory.EnumerateFiles(directory))
                {
                    token.ThrowIfCancellationRequested();
                    var fileInfo = new FileInfo(file);
                    if (!settings.IncludeHidden && IsHidden(fileInfo.Attributes)) continue;
                    if (allowedExtensions.Count > 0 && !allowedExtensions.Contains(fileInfo.Extension)) continue;
                    string fileRelative = Path.GetRelativePath(RootPath, file);
                    result.Add(CreateEntry(fileInfo.Name, file, fileRelative, false, projectRoot,
                        SafeSize(fileInfo), SafeModified(fileInfo), fileInfo.Extension));
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (IOException) { }
        }

        return result;
    }

    private IndexEntry CreateEntry(string name, string path, string relative, bool directory,
        string projectRoot, long size, DateTime modified, string extension)
    {
        string searchable = Normalize($"{name} {relative} {extension} {SemanticAliases(name + " " + relative)}");
        return new(name, path, relative, directory, projectRoot, size, modified, extension, searchable);
    }

    private string DefaultProjectRoot(string path)
    {
        string relative = Path.GetRelativePath(RootPath, path);
        if (relative == ".") return RootPath;
        string[] parts = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        int depth = Math.Min(parts.Length, 3);
        if (parts.Length >= 4 && parts[0].Equals("Engineering", StringComparison.OrdinalIgnoreCase)
                              && parts[1].Equals("Bionic Hand", StringComparison.OrdinalIgnoreCase)
                              && parts[2].Equals("Software", StringComparison.OrdinalIgnoreCase)) depth = 4;
        if (parts.Length >= 4 && parts[0].Equals("Games", StringComparison.OrdinalIgnoreCase)
                              && parts[1].Equals("Game Development", StringComparison.OrdinalIgnoreCase)
                              && parts[2].Equals("Unreal Projects", StringComparison.OrdinalIgnoreCase)) depth = 4;
        if (parts.Length <= 2) depth = parts.Length;
        return Path.Combine(new[] { RootPath }.Concat(parts.Take(depth)).ToArray());
    }

    private static bool IsProjectMarkerDirectory(string directory)
    {
        try
        {
            if (Directory.Exists(Path.Combine(directory, ".git"))) return true;
            string[] exact = ["package.json", "pyproject.toml", "CMakeLists.txt", "Cargo.toml", "go.mod"];
            if (exact.Any(x => File.Exists(Path.Combine(directory, x)))) return true;
            return Directory.EnumerateFiles(directory, "*.sln", SearchOption.TopDirectoryOnly).Any()
                || Directory.EnumerateFiles(directory, "*.uproject", SearchOption.TopDirectoryOnly).Any()
                || Directory.EnumerateFiles(directory, "*.csproj", SearchOption.TopDirectoryOnly).Any()
                || Directory.EnumerateFiles(directory, "*.kicad_pro", SearchOption.TopDirectoryOnly).Any();
        }
        catch { return false; }
    }

    public List<SearchResult> Search(string query, bool deep, int limit)
    {
        query = Normalize(query);
        if (query.Length == 0) return [];
        string[] queryTokens = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        List<IndexEntry> snapshot;
        lock (_gate) snapshot = _entries;

        var scored = snapshot.AsParallel()
            .WithDegreeOfParallelism(Math.Max(1, Math.Min(8, Environment.ProcessorCount - 1)))
            .Select(entry =>
            {
                double score = Score(entry, query, queryTokens, out string reason);
                return (Entry: entry, Score: score, Reason: reason);
            })
            .Where(x => x.Score >= 34)
            .ToList();

        if (deep)
        {
            return scored.OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.Entry.IsDirectory)
                .ThenBy(x => x.Entry.RelativePath.Length)
                .Take(limit)
                .Select(x => new SearchResult(x.Entry.Name, x.Entry.Path, x.Entry.RelativePath,
                    x.Entry.IsDirectory, x.Score, x.Reason))
                .ToList();
        }

        var groups = scored.GroupBy(x => x.Entry.ProjectRoot, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var best = g.OrderByDescending(x => x.Score).First();
                string root = g.Key;
                string name = new DirectoryInfo(root).Name;
                bool exists = Directory.Exists(root);
                string relative = Path.GetRelativePath(RootPath, root);
                double score = best.Score + Math.Min(18, Math.Log2(g.Count() + 1) * 4);
                string reason = g.Count() == 1 ? best.Reason : $"{g.Count():N0} related matches";
                return new SearchResult(name, root, relative, exists, score, reason, g.Count());
            })
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.RelativePath.Length)
            .Take(limit)
            .ToList();
        return groups;
    }

    public List<SearchResult> Children(string path, int limit)
    {
        try
        {
            return Directory.EnumerateFileSystemEntries(path)
                .Select(p =>
                {
                    bool directory = Directory.Exists(p);
                    return new SearchResult(Path.GetFileName(p), p, Path.GetRelativePath(RootPath, p),
                        directory, directory ? 100 : 90, directory ? "Folder" : "File");
                })
                .OrderByDescending(x => x.IsDirectory)
                .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .Take(limit)
                .ToList();
        }
        catch { return []; }
    }

    private static double Score(IndexEntry entry, string query, string[] queryTokens, out string reason)
    {
        string name = Normalize(Path.GetFileNameWithoutExtension(entry.Name));
        string relative = Normalize(entry.RelativePath);
        string search = entry.SearchText;
        double score = 0;
        bool directSignal = false;
        bool phraseSignal = false;
        reason = "Fuzzy match";

        if (name == query) { score += 310; reason = "Exact name"; directSignal = phraseSignal = true; }
        else if (name.StartsWith(query, StringComparison.Ordinal)) { score += 205; reason = "Name starts with query"; directSignal = phraseSignal = true; }
        else if (name.Contains(query, StringComparison.Ordinal)) { score += 170; reason = "Name contains query"; directSignal = phraseSignal = true; }
        if (relative.Contains(query, StringComparison.Ordinal)) { score += 125; reason = "Path match"; directSignal = phraseSignal = true; }
        if (search.Contains(query, StringComparison.Ordinal)) { score += 95; reason = "Related term"; directSignal = phraseSignal = true; }

        int exactTokens = queryTokens.Count(q => ContainsToken(search, q));
        if (exactTokens > 0) directSignal = true;
        if (exactTokens == queryTokens.Length) score += 90 + exactTokens * 12;
        else score += exactTokens * 20;

        double combinedNameSimilarity = Similarity(query.Replace(" ", string.Empty), name.Replace(" ", string.Empty));
        if (!phraseSignal && exactTokens < queryTokens.Length && queryTokens.Length > 1 && combinedNameSimilarity < .72)
            return 0;
        if (!directSignal)
        {
            double required = queryTokens.Length > 1 ? .72 : .58;
            if (combinedNameSimilarity < required) return 0;
            directSignal = true;
            score += combinedNameSimilarity * 125;
            reason = "Close name match";
        }
        else if (!phraseSignal && combinedNameSimilarity >= .72)
        {
            score += combinedNameSimilarity * 70;
            reason = "Close name match";
        }
        score += TrigramSimilarity(query, name) * 48;
        if (entry.IsDirectory) score += 9;
        return score;
    }

    private static string SemanticAliases(string value)
    {
        string normalized = Normalize(value);
        (string[] Triggers, string Aliases, string[] Exclusions)[] groups =
        [
            (["echo", "bionic", "prosthetic", "glove", "hand tracking"], "echo bionic prosthetic hand glove tracking robotics", ["minecraft"]),
            (["classmate"], "classmate school academic student study assignment notes", []),
            (["fortnite", "gameusersettings"], "fortnite fn settings config configuration engine gameusersettings", []),
            (["minecraft", "fabric", "forge", "nbt"], "minecraft modding fabric forge nbt resourcepack", []),
            (["website", "frontend", "react", "next", "vite"], "website web html css frontend react next vite browser", []),
            (["jarvis", "deep learning", "machine learning", "neural"], "machine learning deep automation assistant voice jarvis neural", []),
            (["cad", "blender", "freecad", "kicad"], "cad 3d blender freecad kicad model design printing", []),
            (["tiktok", "youtube", "glitchmc", "video projects"], "video tiktok youtube editing shorts media glitchmc content", []),
            (["backup", "archive", "earlier copy", "prior version"], "backup archive old previous earlier copy", []),
            (["arduino", "wokwi", "firmware", "microcontroller"], "arduino embedded firmware wokwi electronics serial microcontroller", [])
        ];
        var aliases = new StringBuilder();
        foreach (var group in groups)
            if (!group.Exclusions.Any(word => normalized.Contains(word, StringComparison.Ordinal))
                && group.Triggers.Any(word => normalized.Contains(word, StringComparison.Ordinal)))
                aliases.Append(' ').Append(group.Aliases);
        return aliases.ToString();
    }

    private static string Normalize(string value)
    {
        value = value.ToLowerInvariant();
        var builder = new StringBuilder(value.Length);
        bool lastSpace = true;
        foreach (char character in value)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                lastSpace = false;
            }
            else if (!lastSpace)
            {
                builder.Append(' ');
                lastSpace = true;
            }
        }
        return builder.ToString().Trim();
    }

    private static double Similarity(string a, string b)
    {
        if (a == b) return 1;
        if (a.Length == 0 || b.Length == 0) return 0;
        int[] previous = Enumerable.Range(0, b.Length + 1).ToArray();
        int[] current = new int[b.Length + 1];
        for (int i = 1; i <= a.Length; i++)
        {
            current[0] = i;
            for (int j = 1; j <= b.Length; j++)
                current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1),
                    previous[j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
            (previous, current) = (current, previous);
        }
        return 1.0 - (double)previous[b.Length] / Math.Max(a.Length, b.Length);
    }

    private static bool ContainsToken(string text, string token)
    {
        int start = 0;
        while (start < text.Length)
        {
            int index = text.IndexOf(token, start, StringComparison.Ordinal);
            if (index < 0) return false;
            bool leftBoundary = index == 0 || text[index - 1] == ' ';
            int end = index + token.Length;
            bool rightBoundary = end == text.Length || text[end] == ' ';
            if (leftBoundary && rightBoundary) return true;
            start = index + 1;
        }
        return false;
    }

    private static double TrigramSimilarity(string a, string b)
    {
        if (a.Length < 3 || b.Length < 3) return a == b ? 1 : 0;
        var left = Enumerable.Range(0, a.Length - 2).Select(i => a.Substring(i, 3)).ToHashSet();
        var right = Enumerable.Range(0, b.Length - 2).Select(i => b.Substring(i, 3)).ToHashSet();
        int intersection = left.Count(right.Contains);
        int union = left.Count + right.Count - intersection;
        return union == 0 ? 0 : (double)intersection / union;
    }

    private static bool IsHidden(FileAttributes attributes) => attributes.HasFlag(FileAttributes.Hidden)
        || attributes.HasFlag(FileAttributes.System);
    private static DateTime SafeModified(FileSystemInfo info) { try { return info.LastWriteTimeUtc; } catch { return DateTime.MinValue; } }
    private static long SafeSize(FileInfo info) { try { return info.Length; } catch { return 0; } }
}
