using System;
using System.Collections.Generic;
using System.IO;

namespace Peloton.Client.Godot;

public static class WatchContentPath
{
    public static string FindContentRoot()
    {
        string? fromEnvironment = Environment.GetEnvironmentVariable("PELOTON_CONTENT_ROOT");
        if (!string.IsNullOrWhiteSpace(fromEnvironment) && LooksLikeContentRoot(fromEnvironment))
        {
            return Path.GetFullPath(fromEnvironment);
        }

        foreach (string candidate in PackagedCandidates())
        {
            if (LooksLikeContentRoot(candidate))
            {
                return Path.GetFullPath(candidate);
            }
        }

        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null)
        {
            string slnContent = Path.Combine(current.FullName, "content");
            if (File.Exists(Path.Combine(current.FullName, "PelotonManager.sln")) &&
                LooksLikeContentRoot(slnContent))
            {
                return slnContent;
            }

            current = current.Parent;
        }

        string fromWorkingDirectory = Path.GetFullPath(
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "content"));
        if (LooksLikeContentRoot(fromWorkingDirectory))
        {
            return fromWorkingDirectory;
        }

        throw new DirectoryNotFoundException("Could not locate the Peloton content root.");
    }

    public static string PlaytestSavePath(string fileName, bool runningInEditor, string? executablePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        if (runningInEditor)
        {
            return Path.Combine(Path.GetTempPath(), fileName);
        }

        string? directory = Path.GetDirectoryName(executablePath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            directory = AppContext.BaseDirectory;
        }

        string saves = Path.Combine(directory, "saves");
        Directory.CreateDirectory(saves);
        return Path.Combine(saves, fileName);
    }

    public static bool LooksLikeContentRoot(string path)
    {
        return !string.IsNullOrWhiteSpace(path) &&
            File.Exists(Path.Combine(path, "peloton.skeleton", "pack.json"));
    }

    private static string[] PackagedCandidates()
    {
        List<string> candidates = new()
        {
            Path.Combine(AppContext.BaseDirectory, "content"),
            Path.Combine(Directory.GetCurrentDirectory(), "content"),
        };
        DirectoryInfo? parent = Directory.GetParent(AppContext.BaseDirectory);
        if (parent is not null)
        {
            candidates.Insert(1, Path.Combine(parent.FullName, "content"));
        }

        return candidates.ToArray();
    }
}
