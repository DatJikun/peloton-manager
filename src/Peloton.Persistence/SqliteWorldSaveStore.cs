using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using Peloton.Application;
using Peloton.Domain;
using Peloton.Simulation;

namespace Peloton.Persistence;

public sealed class SqliteWorldSaveStore : IWorldSaveStore
{
    public const int SchemaVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    public void Save(string path, WorldCheckpoint checkpoint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(checkpoint);
        ValidateCheckpointState(checkpoint.GameState);

        string targetPath = Path.GetFullPath(path);
        string? directory = Path.GetDirectoryName(targetPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new IOException("Save path has no parent directory.");
        }

        Directory.CreateDirectory(directory);
        string candidatePath = targetPath + ".candidate";

        try
        {
            File.Delete(candidatePath);
            WriteCandidate(candidatePath, checkpoint);
            VerifySqlite(candidatePath);
            WorldCheckpoint verified = Load(candidatePath);
            if (verified.GameState != checkpoint.GameState ||
                verified.RacePreparation != checkpoint.RacePreparation ||
                !string.Equals(
                    WorldChecksum.Compute(verified.World),
                    WorldChecksum.Compute(checkpoint.World),
                    StringComparison.Ordinal))
            {
                throw new InvalidDataException("Save candidate verification changed the checkpoint.");
            }

            File.Move(candidatePath, targetPath, overwrite: true);
        }
        catch (SqliteException exception)
        {
            throw new IOException("SQLite save failed.", exception);
        }
    }

    public WorldCheckpoint Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        string sourcePath = Path.GetFullPath(path);
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException("Save file does not exist.", sourcePath);
        }

        try
        {
            using SqliteConnection connection = Open(sourcePath, SqliteOpenMode.ReadOnly);
            VerifySqlite(connection);
            if (!int.TryParse(
                    ReadMetadata(connection, "schema_version"),
                    System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out int schemaVersion))
            {
                throw new InvalidDataException("Save schema version is malformed.");
            }

            if (schemaVersion != SchemaVersion)
            {
                throw new InvalidDataException($"Unsupported save schema version {schemaVersion}.");
            }

            string payload = ReadSnapshot(connection);
            SaveSnapshotDto snapshot = JsonSerializer.Deserialize<SaveSnapshotDto>(payload, JsonOptions)
                ?? throw new InvalidDataException("Save snapshot is empty.");
            ValidateCheckpointState(snapshot.GameState);
            WorldState world = snapshot.World.ToDomain();
            if (!string.Equals(
                    ReadMetadata(connection, "content_identity"),
                    world.ContentIdentity.AggregateHash,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    ReadMetadata(connection, "rules_identity"),
                    world.RulesIdentity,
                    StringComparison.Ordinal))
            {
                throw new InvalidDataException("Save identity metadata does not match its snapshot.");
            }

            string expectedChecksum = ReadMetadata(connection, "world_checksum");
            string actualChecksum = WorldChecksum.Compute(world);
            if (!string.Equals(expectedChecksum, actualChecksum, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"Save world checksum does not match its snapshot. Expected {expectedChecksum}, actual {actualChecksum}.");
            }

            return new WorldCheckpoint(snapshot.GameState, world, snapshot.RacePreparation);
        }
        catch (SqliteException exception)
        {
            throw new InvalidDataException("SQLite save could not be read.", exception);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Save snapshot JSON is invalid.", exception);
        }
    }

    private static void WriteCandidate(string candidatePath, WorldCheckpoint checkpoint)
    {
        using SqliteConnection connection = Open(candidatePath, SqliteOpenMode.ReadWriteCreate);
        using SqliteTransaction transaction = connection.BeginTransaction();
        using (SqliteCommand schema = connection.CreateCommand())
        {
            schema.Transaction = transaction;
            schema.CommandText = """
                CREATE TABLE save_metadata (
                    key TEXT NOT NULL PRIMARY KEY,
                    value TEXT NOT NULL
                );
                CREATE TABLE world_snapshot (
                    singleton_id INTEGER NOT NULL PRIMARY KEY CHECK (singleton_id = 1),
                    payload TEXT NOT NULL
                );
                """;
            schema.ExecuteNonQuery();
        }

        WriteMetadata(connection, transaction, "save_format", "peloton-manager-sqlite");
        WriteMetadata(
            connection,
            transaction,
            "schema_version",
            SchemaVersion.ToString(System.Globalization.CultureInfo.InvariantCulture));
        WriteMetadata(connection, transaction, "content_identity", checkpoint.World.ContentIdentity.AggregateHash);
        WriteMetadata(connection, transaction, "rules_identity", checkpoint.World.RulesIdentity);
        WriteMetadata(connection, transaction, "world_checksum", WorldChecksum.Compute(checkpoint.World));

        SaveSnapshotDto snapshot = new(
            checkpoint.GameState,
            WorldSnapshotDto.FromDomain(checkpoint.World),
            checkpoint.RacePreparation);
        using (SqliteCommand insert = connection.CreateCommand())
        {
            insert.Transaction = transaction;
            insert.CommandText = "INSERT INTO world_snapshot(singleton_id, payload) VALUES (1, $payload)";
            insert.Parameters.AddWithValue("$payload", JsonSerializer.Serialize(snapshot, JsonOptions));
            insert.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    private static void WriteMetadata(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string key,
        string value)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT INTO save_metadata(key, value) VALUES ($key, $value)";
        command.Parameters.AddWithValue("$key", key);
        command.Parameters.AddWithValue("$value", value);
        command.ExecuteNonQuery();
    }

    private static string ReadMetadata(SqliteConnection connection, string key)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT value FROM save_metadata WHERE key = $key";
        command.Parameters.AddWithValue("$key", key);
        return command.ExecuteScalar() as string
            ?? throw new InvalidDataException($"Save metadata '{key}' is missing.");
    }

    private static string ReadSnapshot(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT payload FROM world_snapshot WHERE singleton_id = 1";
        return command.ExecuteScalar() as string
            ?? throw new InvalidDataException("Save world snapshot is missing.");
    }

    private static SqliteConnection Open(string path, SqliteOpenMode mode)
    {
        SqliteConnectionStringBuilder builder = new()
        {
            DataSource = path,
            Mode = mode,
            Cache = SqliteCacheMode.Private,
            Pooling = false,
        };
        SqliteConnection connection = new(builder.ToString());
        connection.Open();
        return connection;
    }

    private static void VerifySqlite(string path)
    {
        using SqliteConnection connection = Open(path, SqliteOpenMode.ReadOnly);
        VerifySqlite(connection);
    }

    private static void VerifySqlite(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "PRAGMA quick_check";
        string? result = command.ExecuteScalar() as string;
        if (!string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("SQLite integrity check failed.");
        }
    }

    private static void ValidateCheckpointState(GameState state)
    {
        if (state is GameState.MainMenu or GameState.NewGameFlow or GameState.LoadingWorld or GameState.RaceLive)
        {
            throw new InvalidDataException($"GameState '{state}' is not a legal world checkpoint.");
        }
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        JsonSerializerOptions options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private sealed record SaveSnapshotDto(
        GameState GameState,
        WorldSnapshotDto World,
        RacePreparationCheckpoint? RacePreparation = null);

    private sealed record OrganizationDto(
        WorldEntityId Id,
        string OriginDefinitionId,
        string Name,
        int DaysSimulated,
        string RacePrototypeTeamId = "")
    {
        public Organization ToDomain() => new(Id, OriginDefinitionId, Name, DaysSimulated, RacePrototypeTeamId);
    }

    private sealed record RaceDto(
        string RouteId,
        WorldEntityId WinnerId,
        IReadOnlyList<WorldEntityId> FinishOrder)
    {
        public RaceSummary ToDomain() => new(RouteId, WinnerId, FinishOrder);
    }

    private sealed record CalendarEntryDto(
        WorldEntityId Id,
        int DayNumber,
        CalendarEntryKind Kind,
        string Title,
        string? OfficialResult = null,
        bool ResultAcknowledged = false)
    {
        public CalendarEntry ToDomain() => new(Id, DayNumber, Kind, Title, OfficialResult, ResultAcknowledged);
    }

    private sealed record WorldSnapshotDto(
        string WorldId,
        long MasterSeed,
        int RngContractVersion,
        WorldDate CurrentDate,
        ContentIdentity ContentIdentity,
        string RulesIdentity,
        IReadOnlyList<RulesModuleIdentity> RulesModules,
        long EntityIdHighWaterMark,
        IReadOnlyList<Person> Persons,
        IReadOnlyList<ManagerCareer> ManagerCareers,
        IReadOnlyList<Employment> Employments,
        IReadOnlyList<OrganizationDto> Organizations,
        IReadOnlyList<DecisionAuthority> DecisionAuthorities,
        int RaceCount,
        RaceDto? LastRace,
        int CalendarPeriodDays,
        int LastCompletedRaceDay,
        IReadOnlyList<string> LastDayNotes,
        IReadOnlyList<CalendarEntryDto>? CalendarEntries = null,
        IReadOnlyList<RosterRider>? RosterRiders = null)
    {
        public static WorldSnapshotDto FromDomain(WorldState world)
        {
            return new WorldSnapshotDto(
                world.WorldId,
                world.MasterSeed,
                world.RngContractVersion,
                world.CurrentDate,
                world.ContentIdentity,
                world.RulesIdentity,
                world.RulesModules.ToArray(),
                world.EntityIdHighWaterMark,
                world.Persons.ToArray(),
                world.ManagerCareers.ToArray(),
                world.Employments.ToArray(),
                world.Organizations
                    .Select(organization => new OrganizationDto(
                        organization.Id,
                        organization.OriginDefinitionId,
                        organization.Name,
                        organization.DaysSimulated,
                        organization.RacePrototypeTeamId))
                    .ToArray(),
                world.DecisionAuthorities.ToArray(),
                world.RaceCount,
                world.LastRace is null
                    ? null
                    : new RaceDto(
                        world.LastRace.RouteId,
                        world.LastRace.WinnerId,
                        world.LastRace.FinishOrder),
                world.CalendarPeriodDays,
                world.LastCompletedRaceDay,
                world.LastDayNotes.ToArray(),
                world.CalendarEntries
                    .Select(entry => new CalendarEntryDto(
                        entry.Id,
                        entry.DayNumber,
                        entry.Kind,
                        entry.Title,
                        entry.OfficialResult,
                        entry.ResultAcknowledged))
                    .ToArray(),
                world.RosterRiders.ToArray());
        }

        public WorldState ToDomain()
        {
            return new WorldState(
                WorldId,
                MasterSeed,
                RngContractVersion,
                CurrentDate,
                ContentIdentity,
                RulesIdentity,
                RulesModules,
                EntityIdHighWaterMark,
                Persons,
                ManagerCareers,
                Employments,
                Organizations.Select(organization => organization.ToDomain()),
                DecisionAuthorities,
                RaceCount,
                LastRace?.ToDomain(),
                CalendarPeriodDays > 0 ? CalendarPeriodDays : 12,
                LastCompletedRaceDay,
                LastDayNotes ?? Array.Empty<string>(),
                (CalendarEntries ?? Array.Empty<CalendarEntryDto>())
                    .Select(entry => entry.ToDomain()),
                RosterRiders ?? Array.Empty<RosterRider>());
        }
    }
}
