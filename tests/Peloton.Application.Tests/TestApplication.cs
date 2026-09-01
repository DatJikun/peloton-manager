using System;
using System.IO;
using Peloton.Application;
using Peloton.Content;
using Peloton.Domain;
using Peloton.Infrastructure;
using Peloton.Persistence;

namespace Peloton.Application.Tests;

internal static class TestApplication
{
    public static string ContentRoot => Path.Combine(FindRepositoryRoot(), "content");

    public static GameApplication Create()
    {
        return ApplicationFactory.Create(ContentRoot);
    }

    public static string RunTenSeasons(long seed)
    {
        using TemporaryDirectory temp = new();
        GameApplication application = Create();
        CommandResult create = application.Execute(new CreateWorldCommand("scenario.peloton.skeleton", seed));
        if (!create.Succeeded)
        {
            throw new InvalidOperationException(create.ReasonCode);
        }

        SkeletonCareerRunner runner = new(application);
        SkeletonRunReport report = runner.Run(10, temp.Path);
        AssertReport(report);
        return report.Checksum;
    }

    private static void AssertReport(SkeletonRunReport report)
    {
        if (report.Crashed || report.RaceCount != 10 || report.WorldDay != 120)
        {
            throw new InvalidOperationException("Ten-season skeleton report violated its contract.");
        }
    }

    internal static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "PelotonManager.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root.");
    }
}

internal sealed class TemporaryDirectory : IDisposable
{
    public TemporaryDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"peloton-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public void Dispose()
    {
        Directory.Delete(Path, recursive: true);
    }
}
