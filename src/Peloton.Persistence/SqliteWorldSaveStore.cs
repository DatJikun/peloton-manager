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
    public const int SchemaVersion = 11;

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
        string Country = "",
        string Division = "Skeleton",
        int LicenceYearsRemaining = 0,
        string TitleSponsor = "",
        string Bike = "",
        string Groupset = "",
        long EstimatedBudgetEur = 0,
        long CashEur = 0,
        long TitleSponsorAnnualFeeEur = 0)
    {
        public Organization ToDomain() => new(
            Id,
            OriginDefinitionId,
            Name,
            DaysSimulated,
            Country,
            Division,
            LicenceYearsRemaining,
            TitleSponsor,
            Bike,
            Groupset,
            EstimatedBudgetEur,
            CashEur,
            TitleSponsorAnnualFeeEur);
    }

    private sealed record RaceDto(
        string RouteId,
        WorldEntityId WinnerId,
        IReadOnlyList<WorldEntityId> FinishOrder)
    {
        public RaceSummary ToDomain() => new(RouteId, WinnerId, FinishOrder);
    }

    private sealed record RiderCareerResultDto(
        string RaceContentId,
        int DayNumber,
        int Place,
        bool DidNotFinish)
    {
        public RiderCareerResult ToDomain() =>
            new(RaceContentId, DayNumber, Place, DidNotFinish);
    }

    private sealed record RiderCareerDto(
        WorldEntityId Id,
        WorldEntityId PersonId,
        WorldEntityId? OrganizationId,
        string OriginDefinitionId,
        double CriticalPowerW,
        double WPrimeCapacityJ,
        double PeakPowerW,
        double WPrimeRecoveryJPerSecond,
        double LowIntensityDurability,
        double HighIntensityDurability,
        double BodyMassKg,
        double SystemMassKg,
        double CdARoadM2,
        double CdATtM2,
        double BaseCrr,
        double Positioning,
        double Handling,
        double TacticalAwareness,
        double Form01,
        double Freshness01,
        double Fatigue01,
        double Loyalty01,
        int PotentialOvr,
        IReadOnlyList<RiderCareerResultDto>? Results = null,
        bool IsRetired = false,
        WorldEntityId? RetiredFromOrganizationId = null)
    {
        public RiderCareer ToDomain() => new(
            Id,
            PersonId,
            OrganizationId,
            OriginDefinitionId,
            CriticalPowerW,
            WPrimeCapacityJ,
            PeakPowerW,
            WPrimeRecoveryJPerSecond,
            LowIntensityDurability,
            HighIntensityDurability,
            BodyMassKg,
            SystemMassKg,
            CdARoadM2,
            BaseCrr,
            Positioning,
            Handling,
            TacticalAwareness,
            Form01,
            Freshness01,
            Fatigue01,
            Loyalty01,
            PotentialOvr,
            (Results ?? Array.Empty<RiderCareerResultDto>()).Select(result => result.ToDomain()),
            CdATtM2,
            IsRetired,
            RetiredFromOrganizationId);

        public static RiderCareerDto FromDomain(RiderCareer career) => new(
            career.Id,
            career.PersonId,
            career.OrganizationId,
            career.OriginDefinitionId,
            career.CriticalPowerW,
            career.WPrimeCapacityJ,
            career.PeakPowerW,
            career.WPrimeRecoveryJPerSecond,
            career.LowIntensityDurability,
            career.HighIntensityDurability,
            career.BodyMassKg,
            career.SystemMassKg,
            career.CdARoadM2,
            career.CdATtM2,
            career.BaseCrr,
            career.Positioning,
            career.Handling,
            career.TacticalAwareness,
            career.Form01,
            career.Freshness01,
            career.Fatigue01,
            career.Loyalty01,
            career.PotentialOvr,
            career.Results
                .Select(result => new RiderCareerResultDto(
                    result.RaceContentId,
                    result.DayNumber,
                    result.Place,
                    result.DidNotFinish))
                .ToArray(),
            career.IsRetired,
            career.RetiredFromOrganizationId);
    }

    private sealed record RiderContractDto(
        WorldEntityId Id,
        WorldEntityId RiderCareerId,
        WorldEntityId OrganizationId,
        int AnnualWage,
        WorldDate StartDate,
        WorldDate EndDate)
    {
        public RiderContract ToDomain() =>
            new(Id, RiderCareerId, OrganizationId, AnnualWage, StartDate, EndDate);

        public static RiderContractDto FromDomain(RiderContract contract) =>
            new(
                contract.Id,
                contract.RiderCareerId,
                contract.OrganizationId,
                contract.AnnualWage,
                contract.StartDate,
                contract.EndDate);
    }

    private sealed record OrganizationRaceEntryDto(
        WorldEntityId OrganizationId,
        string RaceContentId,
        bool Entered,
        WorldEntityId? DesignatedLeaderId = null)
    {
        public OrganizationRaceEntry ToDomain() =>
            new(OrganizationId, RaceContentId, Entered, DesignatedLeaderId);

        public static OrganizationRaceEntryDto FromDomain(OrganizationRaceEntry entry) =>
            new(entry.OrganizationId, entry.RaceContentId, entry.Entered, entry.DesignatedLeaderId);
    }

    private sealed record CourseSampleDto(
        double DistanceM,
        double ElevationM,
        double WidthM,
        double HeadingDegrees,
        CourseSurface Surface,
        double Curvature01,
        double Exposure01);

    private sealed record CourseProfileDto(
        WorldEntityId CourseProfileId,
        string OriginDefinitionId,
        string RaceContentId,
        int SeasonYear,
        int StageIndex,
        string Name,
        CourseKind Kind,
        string Country,
        double SampleSpacingM,
        IReadOnlyList<CourseSampleDto> Samples,
        double LengthM,
        double ElevationGainM,
        double ElevationLossM,
        double CobbleM,
        double GravelM,
        double MaxGradient,
        double MinGradient,
        ClassifiedStageType ClassifiedStageType)
    {
        public CourseProfile ToDomain() => new(
            CourseProfileId,
            OriginDefinitionId,
            RaceContentId,
            SeasonYear,
            StageIndex,
            Name,
            Kind,
            Country,
            SampleSpacingM,
            Samples.Select(sample => new CourseSampleVertex(
                sample.DistanceM,
                sample.ElevationM,
                sample.WidthM,
                sample.HeadingDegrees,
                sample.Surface,
                sample.Curvature01,
                sample.Exposure01)).ToArray(),
            LengthM,
            ElevationGainM,
            ElevationLossM,
            CobbleM,
            GravelM,
            MaxGradient,
            MinGradient,
            ClassifiedStageType);

        public static CourseProfileDto FromDomain(CourseProfile profile) => new(
            profile.CourseProfileId,
            profile.OriginDefinitionId,
            profile.RaceContentId,
            profile.SeasonYear,
            profile.StageIndex,
            profile.Name,
            profile.Kind,
            profile.Country,
            profile.SampleSpacingM,
            profile.Samples.Select(sample => new CourseSampleDto(
                sample.DistanceM,
                sample.ElevationM,
                sample.WidthM,
                sample.HeadingDegrees,
                sample.Surface,
                sample.Curvature01,
                sample.Exposure01)).ToArray(),
            profile.LengthM,
            profile.ElevationGainM,
            profile.ElevationLossM,
            profile.CobbleM,
            profile.GravelM,
            profile.MaxGradient,
            profile.MinGradient,
            profile.ClassifiedStageType);
    }

    private sealed record RiderStageTimeDto(
        string RaceContentId,
        int StageIndex,
        WorldEntityId RiderId,
        double FinishTimeSeconds)
    {
        public RiderStageTime ToDomain() =>
            new(RaceContentId, StageIndex, RiderId, FinishTimeSeconds);

        public static RiderStageTimeDto FromDomain(RiderStageTime time) =>
            new(time.RaceContentId, time.StageIndex, time.RiderId, time.FinishTimeSeconds);
    }

    private sealed record CalendarEntryDto(
        WorldEntityId Id,
        int DayNumber,
        CalendarEntryKind Kind,
        string Title,
        string? OfficialResult = null,
        bool ResultAcknowledged = false,
        string? RaceContentId = null,
        int StageIndex = 1,
        WorldEntityId? CourseProfileId = null)
    {
        public CalendarEntry ToDomain() => new(
            Id,
            DayNumber,
            Kind,
            Title,
            OfficialResult,
            ResultAcknowledged,
            RaceContentId,
            StageIndex,
            CourseProfileId);
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
        IReadOnlyList<RiderCareerDto>? RiderCareers = null,
        IReadOnlyList<OrganizationRaceEntryDto>? OrganizationRaceEntries = null,
        IReadOnlyList<RiderContractDto>? RiderContracts = null,
        IReadOnlyList<CourseProfileDto>? CourseProfiles = null,
        IReadOnlyList<RiderStageTimeDto>? RiderStageTimes = null,
        bool GeneratePeriodicRaces = true,
        int FinancialYearDays = 365,
        int SeasonYear = 2026,
        int SeasonStartDayNumber = 0,
        IReadOnlyList<RaceIdentityConstraints>? RaceIdentities = null,
        IReadOnlyList<CalendarRaceDetail>? CalendarRaceDetails = null,
        IReadOnlyList<string>? DismissedInboxIdentities = null,
        string? SeasonSummaryInboxBody = null,
        int? SeasonSummaryInboxYear = null)
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
                        organization.Country,
                        organization.Division,
                        organization.LicenceYearsRemaining,
                        organization.TitleSponsor,
                        organization.Bike,
                        organization.Groupset,
                        organization.EstimatedBudgetEur,
                        organization.CashEur,
                        organization.TitleSponsorAnnualFeeEur))
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
                        entry.ResultAcknowledged,
                        entry.RaceContentId,
                        entry.StageIndex,
                        entry.CourseProfileId))
                    .ToArray(),
                world.RiderCareers.Select(RiderCareerDto.FromDomain).ToArray(),
                world.OrganizationRaceEntries
                    .Select(OrganizationRaceEntryDto.FromDomain)
                    .ToArray(),
                world.RiderContracts.Select(RiderContractDto.FromDomain).ToArray(),
                world.CourseProfiles.Select(CourseProfileDto.FromDomain).ToArray(),
                world.RiderStageTimes.Select(RiderStageTimeDto.FromDomain).ToArray(),
                world.GeneratePeriodicRaces,
                world.FinancialYearDays,
                world.SeasonYear,
                world.SeasonStartDayNumber,
                world.RaceIdentities.ToArray(),
                world.CalendarRaceDetails.ToArray(),
                world.DismissedInboxIdentities.ToArray(),
                world.SeasonSummaryInboxBody,
                world.SeasonSummaryInboxYear);
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
                (RiderCareers ?? Array.Empty<RiderCareerDto>())
                    .Select(career => career.ToDomain()),
                (OrganizationRaceEntries ?? Array.Empty<OrganizationRaceEntryDto>())
                    .Select(entry => entry.ToDomain()),
                (RiderContracts ?? Array.Empty<RiderContractDto>())
                    .Select(contract => contract.ToDomain()),
                (CourseProfiles ?? Array.Empty<CourseProfileDto>())
                    .Select(profile => profile.ToDomain()),
                (RiderStageTimes ?? Array.Empty<RiderStageTimeDto>())
                    .Select(time => time.ToDomain()),
                GeneratePeriodicRaces,
                FinancialYearDays > 0 ? FinancialYearDays : (GeneratePeriodicRaces ? (CalendarPeriodDays > 0 ? CalendarPeriodDays : 12) : 365),
                SeasonYear > 0 ? SeasonYear : 2026,
                SeasonStartDayNumber,
                RaceIdentities ?? Array.Empty<RaceIdentityConstraints>(),
                CalendarRaceDetails ?? Array.Empty<CalendarRaceDetail>(),
                DismissedInboxIdentities ?? Array.Empty<string>(),
                SeasonSummaryInboxBody,
                SeasonSummaryInboxYear);
        }
    }
}
