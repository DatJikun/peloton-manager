using System;
using System.IO;

namespace Peloton.Client.Godot;

public static class WatchContentPath
{
    public static string FindContentRoot()
    {
        string? fromEnvironment = Environment.GetEnvironmentVariable("PELOTON_CONTENT_ROOT");
        if (!string.IsNullOrWhiteSpace(fromEnvironment) && Directory.Exists(fromEnvironment))
        {
            return Path.GetFullPath(fromEnvironment);
        }

        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null)
        {
            string slnContent = Path.Combine(current.FullName, "content");
            if (File.Exists(Path.Combine(current.FullName, "PelotonManager.sln")) &&
                Directory.Exists(slnContent))
            {
                return slnContent;
            }

            current = current.Parent;
        }

        string fromWorkingDirectory = Path.GetFullPath(
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "content"));
        if (Directory.Exists(fromWorkingDirectory))
        {
            return fromWorkingDirectory;
        }

        throw new DirectoryNotFoundException("Could not locate the Peloton content root.");
    }
}
