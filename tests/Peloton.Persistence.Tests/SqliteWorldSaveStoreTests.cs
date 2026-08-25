using System;
using System.IO;
using Microsoft.Data.Sqlite;
using Peloton.Application;
using Peloton.Content;
using Peloton.Infrastructure;
using Peloton.Persistence;
using Peloton.Simulation;
using Xunit;

namespace Peloton.Persistence.Tests;

public sealed class SqliteWorldSaveStoreTests
{
    [Fact]
    public void SaveLoadPreservesChecksumAndEnvelopeIdentity()
    {
        using TemporaryDirectory temp = new();
        string savePath = Path.Combine(temp.Path, "career.peloton");
        GameApplication source = CreateApplication();
        Assert.True(source.Execute(new CreateWorldCommand("scenario.peloton.skeleton", 777)).Succeeded);
        Assert.True(source.Execute(new AdvanceDayCommand()).Succeeded);
        string expectedChecksum = WorldChecksum.Compute(source.World!);

        Assert.True(source.Execute(new SaveGameCommand(savePath)).Succeeded);

        WorldCheckpoint storedCheckpoint = new SqliteWorldSaveStore().Load(savePath);
        Assert.Equal(GameState.Management, storedCheckpoint.GameState);
        Assert.Equivalent(source.World, storedCheckpoint.World, strict: true);

        GameApplication loaded = CreateApplication();
        Assert.True(loaded.Execute(new LoadGameCommand(savePath)).Succeeded);
        Assert.Equal(expectedChecksum, WorldChecksum.Compute(loaded.World!));
        Assert.Equal(GameState.Management, loaded.State);

        using SqliteConnection connection = new($"Data Source={savePath};Mode=ReadOnly;Pooling=False");
        connection.Open();
        Assert.Equal("1", ReadMetadata(connection, "schema_version"));
        Assert.Equal(source.World!.ContentIdentity.AggregateHash, ReadMetadata(connection, "content_identity"));
        Assert.Equal(source.World.RulesIdentity, ReadMetadata(connection, "rules_identity"));
    }

    [Fact]
    public void FailedLoadDoesNotReplaceAttachedWorld()
    {
        using TemporaryDirectory temp = new();
        GameApplication application = CreateApplication();
        Assert.True(application.Execute(new CreateWorldCommand("scenario.peloton.skeleton", 91)).Succeeded);
        string checksum = WorldChecksum.Compute(application.World!);

        CommandResult result = application.Execute(new LoadGameCommand(Path.Combine(temp.Path, "missing.peloton")));

        Assert.False(result.Succeeded);
        Assert.Equal(checksum, WorldChecksum.Compute(application.World!));
        Assert.Equal(GameState.Management, application.State);
    }

    [Fact]
    public void MalformedSchemaVersionDoesNotReplaceAttachedWorld()
    {
        using TemporaryDirectory temp = new();
        string savePath = Path.Combine(temp.Path, "malformed-schema.peloton");
        GameApplication application = CreateApplication();
        Assert.True(application.Execute(new CreateWorldCommand("scenario.peloton.skeleton", 19)).Succeeded);
        Assert.True(application.Execute(new SaveGameCommand(savePath)).Succeeded);
        string checksum = WorldChecksum.Compute(application.World!);

        using (SqliteConnection connection = new($"Data Source={savePath};Mode=ReadWrite;Pooling=False"))
        {
            connection.Open();
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = "UPDATE save_metadata SET value = 'not-a-version' WHERE key = 'schema_version'";
            command.ExecuteNonQuery();
        }

        CommandResult result = application.Execute(new LoadGameCommand(savePath));

        Assert.False(result.Succeeded);
        Assert.Equal("LOAD_FAILED", result.ReasonCode);
        Assert.Equal(GameState.Management, application.State);
        Assert.Equal(checksum, WorldChecksum.Compute(application.World!));
    }

    private static string ReadMetadata(SqliteConnection connection, string key)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT value FROM save_metadata WHERE key = $key";
        command.Parameters.AddWithValue("$key", key);
        return (string)command.ExecuteScalar()!;
    }

    private static GameApplication CreateApplication()
    {
        return ApplicationFactory.Create(Path.Combine(FindRepositoryRoot(), "content"));
    }

    private static string FindRepositoryRoot()
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
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"peloton-save-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public void Dispose()
    {
        Directory.Delete(Path, recursive: true);
    }
}
