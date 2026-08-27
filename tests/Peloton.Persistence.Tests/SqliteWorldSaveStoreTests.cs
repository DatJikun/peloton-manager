using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Peloton.Application;
using Peloton.Content;
using Peloton.Domain;
using Peloton.Infrastructure;
using Peloton.Persistence;
using Peloton.Simulation;
using Xunit;

namespace Peloton.Persistence.Tests;

public sealed class SqliteWorldSaveStoreTests
{
    private static readonly string[] LastRaceJsonProperties =
    {
        "finishOrder",
        "routeId",
        "winnerId",
    };

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
        Peloton.Domain.WorldState loadedWorld = Assert.IsType<Peloton.Domain.WorldState>(loaded.World);
        Assert.Equal(expectedChecksum, WorldChecksum.Compute(loadedWorld));
        Assert.Equal(12, loadedWorld.CalendarPeriodDays);
        Assert.Contains(loadedWorld.LastDayNotes, note => note.Contains("worked the day", StringComparison.Ordinal));
        Assert.Equal(GameState.Management, loaded.State);
        Assert.Equal(
            source.World!.EntityIdHighWaterMark + 1,
            loadedWorld.AllocateEntityId().Value);

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

    [Fact]
    public void OfficialRaceRoundTripKeepsSchemaVersionOneAndLastRaceJsonShape()
    {
        using TemporaryDirectory temp = new();
        string savePath = Path.Combine(temp.Path, "post-race.peloton");
        GameApplication source = CreateApplication();
        Assert.True(source.Execute(new CreateWorldCommand("scenario.peloton.skeleton", 717)).Succeeded);
        Assert.True(source.Execute(new PrepareRaceCommand()).Succeeded);
        Assert.True(source.Execute(new ConfirmRacePreparationPlanCommand()).Succeeded);
        Assert.True(source.Execute(new StartRaceCommand(
            Path.Combine(temp.Path, "pre-race.peloton"),
            "race-scenario.peloton.prototype-v0")).Succeeded);
        CompleteRace(source);
        Assert.True(source.Execute(new SaveGameCommand(savePath)).Succeeded);

        using SqliteConnection connection = new($"Data Source={savePath};Mode=ReadOnly;Pooling=False");
        connection.Open();
        Assert.Equal("1", ReadMetadata(connection, "schema_version"));
        string payload = ReadSnapshot(connection);
        Assert.Contains("\"lastRace\":{\"routeId\":", payload, StringComparison.Ordinal);
        Assert.Contains("\"winnerId\":", payload, StringComparison.Ordinal);
        Assert.Contains("\"finishOrder\":", payload, StringComparison.Ordinal);
        using JsonDocument document = JsonDocument.Parse(payload);
        string[] lastRaceProperties = document.RootElement
            .GetProperty("world")
            .GetProperty("lastRace")
            .EnumerateObject()
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(LastRaceJsonProperties, lastRaceProperties);

        WorldCheckpoint loaded = new SqliteWorldSaveStore().Load(savePath);
        Assert.Equivalent(source.World!.LastRace, loaded.World.LastRace, strict: true);
        Assert.Equal(1, loaded.World.RaceCount);
    }

    [Fact]
    public void ConfirmedPreparationRoundTripKeepsSessionPlanAtSchemaVersionOne()
    {
        using TemporaryDirectory temp = new();
        string savePath = Path.Combine(temp.Path, "confirmed-prep.peloton");
        GameApplication source = CreateApplication();
        Assert.True(source.Execute(new CreateWorldCommand("scenario.peloton.skeleton", 717)).Succeeded);
        Assert.True(source.Execute(new PrepareRaceCommand()).Succeeded);
        Assert.True(source.Execute(new ConfirmRacePreparationPlanCommand()).Succeeded);
        string worldChecksum = WorldChecksum.Compute(source.World!);

        Assert.True(source.Execute(new SaveGameCommand(savePath)).Succeeded);

        using (SqliteConnection connection = new($"Data Source={savePath};Mode=ReadOnly;Pooling=False"))
        {
            connection.Open();
            Assert.Equal("1", ReadMetadata(connection, "schema_version"));
        }

        WorldCheckpoint stored = new SqliteWorldSaveStore().Load(savePath);
        Assert.Equal(GameState.RacePreparationFlow, stored.GameState);
        RacePreparationCheckpoint plan = Assert.IsType<RacePreparationCheckpoint>(stored.RacePreparation);
        Assert.Equal("race-scenario.peloton.prototype-v0", plan.RaceScenarioId);
        Assert.True(plan.PlanConfirmed);
        Assert.Equal(worldChecksum, WorldChecksum.Compute(stored.World));

        GameApplication loaded = CreateApplication();
        Assert.True(loaded.Execute(new LoadGameCommand(savePath)).Succeeded);
        Assert.True(loaded.RacePreparation!.PlanConfirmed);
        Assert.True(loaded.Execute(new StartRaceCommand(
            Path.Combine(temp.Path, "reloaded-pre-race.peloton"),
            "race-scenario.peloton.prototype-v0")).Succeeded);
        Assert.Equal(GameState.RaceLive, loaded.State);
    }

    private static string ReadMetadata(SqliteConnection connection, string key)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT value FROM save_metadata WHERE key = $key";
        command.Parameters.AddWithValue("$key", key);
        return (string)command.ExecuteScalar()!;
    }

    private static string ReadSnapshot(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT payload FROM world_snapshot WHERE singleton_id = 1";
        return (string)command.ExecuteScalar()!;
    }

    private static void CompleteRace(GameApplication application)
    {
        for (int barrier = 0; barrier < 32 && application.State == GameState.RaceLive; barrier++)
        {
            Assert.True(application.Execute(new AdvanceRaceCommand()).Succeeded);
            if (application.PendingRaceDecision is PendingRaceDecision decision)
            {
                Assert.True(application.Execute(new RespondToRaceDecisionCommand(
                    decision.RequestId,
                    decision.AuthorityId,
                    decision.DelegatedDefaultOption)).Succeeded);
            }
        }

        Assert.Equal(GameState.RaceResultsFlow, application.State);
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
