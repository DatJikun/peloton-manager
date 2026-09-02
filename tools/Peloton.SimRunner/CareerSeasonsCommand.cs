using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Peloton.Application;
using Peloton.Domain;
using Peloton.Infrastructure;
using Peloton.Simulation;

namespace Peloton.SimRunner;

public sealed record CareerSeasonsOptions(
    string ScenarioId,
    long Seed,
    int Years,
    string ContentRoot,
    string? EmployerOrganizationOriginId)
{
    public static CareerSeasonsOptions Parse(string[] args)
    {
        Dictionary<string, string> values = new(StringComparer.Ordinal);
        for (int index = 1; index < args.Length; index++)
        {
            string option = args[index];
            if (!option.StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException("Options must start with '--'.", nameof(args));
            }

            if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Option '{option}' requires a value.", nameof(args));
            }

            values[option[2..]] = args[++index];
        }

        string scenario = Required(values, "scenario");
        int years = int.Parse(Required(values, "years"), CultureInfo.InvariantCulture);
        long seed = long.Parse(Required(values, "seed"), CultureInfo.InvariantCulture);
        values.TryGetValue("employer", out string? employer);
        string contentRoot = values.TryGetValue("content-root", out string? configuredRoot)
            ? configuredRoot
            : Path.Combine(Environment.CurrentDirectory, "content");
        return new CareerSeasonsOptions(
            scenario,
            seed,
            years,
            Path.GetFullPath(contentRoot),
            employer);
    }

    private static string Required(Dictionary<string, string> values, string key)
    {
        if (!values.TryGetValue(key, out string? value) || string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"Required option '{key}' is missing.");
        }

        return value;
    }
}

public static class CareerSeasonsCommand
{
    public static int Execute(CareerSeasonsOptions options, TextWriter output, TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);
        GameApplication application = ApplicationFactory.Create(options.ContentRoot);
        CommandResult create = application.Execute(
            new CreateWorldCommand(options.ScenarioId, options.Seed, options.EmployerOrganizationOriginId));
        if (!create.Succeeded)
        {
            error.WriteLine($"crashed=true reason={create.ReasonCode}");
            return 1;
        }

        CareerDayCommand.EnsurePlayerSkipsRacesForLongSoak(application);
        WorldState world = application.World!;
        int startSeasonYear = world.SeasonYear;
        for (int seasonIndex = 0; seasonIndex < options.Years; seasonIndex++)
        {
            int targetDay = checked((seasonIndex + 1) * world.FinancialYearDays);
            while (world.CurrentDate.DayNumber < targetDay)
            {
                if (application.State == GameState.PreSeasonPlanningFlow)
                {
                    CareerDayCommand.EnsurePlayerSkipsRacesForLongSoak(application);
                }

                CommandResult advanced = application.ExecuteCalendarDaySkippingRaces();
                if (!advanced.Succeeded)
                {
                    error.WriteLine($"crashed=true reason={advanced.ReasonCode}");
                    return 1;
                }
            }

            WriteSeasonLine(output, world, startSeasonYear + seasonIndex);
        }

        output.WriteLine("crashed=false");
        return 0;
    }

    private static void WriteSeasonLine(TextWriter output, WorldState world, int seasonYear)
    {
        Dictionary<WorldEntityId, Person> personsById = world.Persons.ToDictionary(person => person.Id);
        int oldestAge = world.RiderCareers
            .Where(career => !career.IsRetired)
            .Select(career =>
            {
                Person person = personsById[career.PersonId];
                return person.BirthYear is int birthYear ? world.SeasonYear - birthYear : 0;
            })
            .DefaultIfEmpty(0)
            .Max();
        RiderCareer[] best = world.RiderCareers
            .Where(career => !career.IsRetired)
            .OrderByDescending(career => RiderRatingQueries.FromPhysiology(career, career.PotentialOvr).Ovr)
            .ThenBy(career => career.Id.Value)
            .Take(3)
            .ToArray();
        output.WriteLine(
            string.Create(
                CultureInfo.InvariantCulture,
                $"season={seasonYear} checksum={WorldChecksum.Compute(world)} riders={world.LivingRiderCount} retired={world.RiderCareers.Count(career => career.IsRetired)} neo={world.RiderCareers.Count(career => career.OriginDefinitionId.StartsWith("rider.generated.", StringComparison.Ordinal) && !career.IsRetired)} oldestAge={oldestAge} bestOvr={FormatBest(best)}"));
    }

    private static string FormatBest(IReadOnlyList<RiderCareer> riders)
    {
        if (riders.Count == 0)
        {
            return "-";
        }

        return string.Join(
            "|",
            riders.Select(career =>
                RiderRatingQueries.FromPhysiology(career, career.PotentialOvr).Ovr.ToString(CultureInfo.InvariantCulture)));
    }
}
