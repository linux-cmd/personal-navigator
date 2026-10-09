using System;
using System.Collections.Generic;

namespace PersonalNavigator;

public sealed record IndexEntry(
    string Name,
    string Path,
    string RelativePath,
    bool IsDirectory,
    string ProjectRoot,
    long Size,
    DateTime ModifiedUtc,
    string Extension,
    string SearchText);

public sealed record SearchResult(
    string Name,
    string Path,
    string RelativePath,
    bool IsDirectory,
    double Score,
    string Reason,
    int MatchCount = 1);

public sealed class IndexCache
{
    public int Version { get; set; } = 2;
    public DateTime CreatedUtc { get; set; }
    public List<IndexEntry> Entries { get; set; } = [];
}
