using System;
using System.IO;
using System.Linq;
using Peloton.Application;
using Peloton.Content;
using Peloton.Domain;
using Peloton.Persistence;
using Peloton.Simulation.Race;
using Xunit;

namespace Peloton.Client.Godot.Tests;

public sealed class CareerShellHostTests
{
    private const long GateSeed = 91234;
    private const string AlphaLeaderOriginId = "rider.race-prototype.alpha-leader";
    private const string BetaLeaderOriginId = "rider.race-prototype.beta-leader";

    [Fact]
    public void CareerHubUiFilesAreGone()
    {
        string godotRoot = Path.Combine(RepositoryRoot(), "src", "Peloton.Client.Godot");
        Assert.False(File.Exists(Path.Combine(godotRoot, "CareerHub.tscn")));
        Assert.False(File.Exists(Path.Combine(godotRoot, "CareerHubHost.cs")));
        Assert.False(File.Exists(Path.Combine(godotRoot, "CareerHubScreen.cs")));
    }

    [Fact]
    public void OpenWorldTourStartsPreSeasonWithWorldRosterAndCalendar()
    {
        using TemporaryDirectory temp = new();
        CareerShellHost host = CreateHost(temp.Path);
        Assert.Equal(GameState.MainMenu, host.State);

        Assert.True(host.OpenWorldTour("organization.wt2026.uae").Succeeded);
        Assert.True(host.BeginPreSeasonPlanning().Succeeded);
        Assert.Equal(GameState.PreSeasonPlanningFlow, host.State);
        PreSeasonPlanningProjection plan = Assert.IsType<PreSeasonPlanningProjection>(host.PreSeasonPlanning);
        Assert.True(plan.Races.Count >= 30);
        Assert.Contains(plan.Races, race => race.Title.Contains("Tour Down Under", StringComparison.Ordinal));

        Assert.True(host.ConfirmPreSeasonPlan().Succeeded);
        Assert.Equal(GameState.Management, host.State);
        ClubRosterProjection roster = Assert.IsType<ClubRosterProjection>(host.ClubRoster);
        Assert.Contains(roster.Riders, rider => rider.Name.Contains("Pogačar", StringComparison.Ordinal));
        Assert.DoesNotContain(roster.Riders, rider => rider.Name.Contains("Beskid", StringComparison.Ordinal));
        Assert.Contains(host.Calendar, entry => entry.Title.Contains("Tour Down Under", StringComparison.Ordinal));
        Assert.True(host.IsWorldTourWorld);
    }

    [Fact]
    public void OpenWorldTourIneosShowsEmployerUpcomingAndMarketRiders()
    {
        using TemporaryDirectory temp = new();
        CareerShellHost host = CreateHost(temp.Path);
        Assert.True(host.OpenWorldTour("organization.wt2026.ineos").Succeeded);
        Assert.True(host.BeginPreSeasonPlanning().Succeeded);
        Assert.Contains("INEOS", host.EmployerName, StringComparison.Ordinal);
        Assert.True(host.ConfirmPreSeasonPlan().Succeeded);

        Assert.Contains("INEOS", host.Day!.EmployerName, StringComparison.Ordinal);
        Assert.Contains("INEOS", host.EmployerName, StringComparison.Ordinal);
        Assert.True(host.UpcomingEvents.Count <= 5);
        Assert.Contains("Tour Down Under", host.UpcomingEvents[0].Name, StringComparison.Ordinal);
        Assert.NotEmpty(host.MarketRiders);
        Assert.Contains(
            host.MarketRiders,
            rider => string.Equals(rider.OrganizationOriginId, "organization.wt2026.uae", StringComparison.Ordinal));
    }

    [Fact]
    public void OpenWorldTourClubFinanceShowsUaeSponsorAndEuroCash()
    {
        using TemporaryDirectory temp = new();
        CareerShellHost host = CreateHost(temp.Path);
        Assert.True(host.OpenWorldTour("organization.wt2026.uae").Succeeded);
        Assert.True(host.BeginPreSeasonPlanning().Succeeded);
        Assert.True(host.ConfirmPreSeasonPlan().Succeeded);
        Assert.Equal(GameState.Management, host.State);

        ClubFinanceProjection finance = Assert.IsType<ClubFinanceProjection>(host.ClubFinance);
        Assert.Contains("UAE", finance.TitleSponsorName, StringComparison.Ordinal);
        Assert.Equal(0, finance.CashEur);
        Assert.True(finance.WageBillAnnual > 0);
        Assert.DoesNotContain("Beskid", finance.TitleSponsorName, StringComparison.Ordinal);
        Assert.DoesNotContain("Vetter", finance.TitleSponsorName, StringComparison.Ordinal);
    }

    [Fact]
    public void OpenSkeletonClubFinanceShowsSkeletonSponsorNotBeskid()
    {
        using TemporaryDirectory temp = new();
        CareerShellHost host = CreateHost(temp.Path);
        Assert.True(host.OpenSkeleton(GateSeed).Succeeded);

        ClubFinanceProjection finance = Assert.IsType<ClubFinanceProjection>(host.ClubFinance);
        Assert.Equal("Skeleton Sponsor", finance.TitleSponsorName);
        Assert.Equal(2_000_000, finance.TitleSponsorAnnualFeeEur);
        Assert.DoesNotContain("Beskid", finance.TitleSponsorName, StringComparison.Ordinal);
    }

    [Fact]
    public void ContractOfferAtThresholdUpdatesRosterWage()
    {
        using TemporaryDirectory temp = new();
        CareerShellHost host = CreateHost(temp.Path);
        Assert.True(host.OpenSkeleton(GateSeed).Succeeded);
        ClubRosterEntry alphaLeader = Assert.Single(
            host.ClubRoster!.Riders,
            rider => string.Equals(rider.OriginDefinitionId, AlphaLeaderOriginId, StringComparison.Ordinal));
        int threshold = ContractNegotiationQueries.ComputeAcceptThreshold(
            alphaLeader.AnnualWage,
            alphaLeader.Loyalty01);

        Assert.True(host.BeginContractNegotiation(alphaLeader.RiderCareerId).Succeeded);
        Assert.True(host.SetContractOffer(threshold, 10_000).Succeeded);
        Assert.True(host.ConfirmContractOffer().Succeeded);
        Assert.Null(host.ContractNegotiation);

        ClubRosterEntry updated = Assert.Single(
            host.ClubRoster!.Riders,
            rider => rider.RiderCareerId == alphaLeader.RiderCareerId);
        Assert.Equal(threshold, updated.AnnualWage);
    }

    [Fact]
    public void TooLowContractOfferIsRejectedWithoutChangingWage()
    {
        using TemporaryDirectory temp = new();
        CareerShellHost host = CreateHost(temp.Path);
        Assert.True(host.OpenSkeleton(GateSeed).Succeeded);
        ClubRosterEntry alphaLeader = Assert.Single(
            host.ClubRoster!.Riders,
            rider => string.Equals(rider.OriginDefinitionId, AlphaLeaderOriginId, StringComparison.Ordinal));
        int wageBefore = alphaLeader.AnnualWage;

        Assert.True(host.BeginContractNegotiation(alphaLeader.RiderCareerId).Succeeded);
        Assert.True(host.SetContractOffer(100_000, 10_000).Succeeded);
        CommandResult confirm = host.ConfirmContractOffer();
        Assert.False(confirm.Succeeded);
        Assert.Equal("CONTRACT_OFFER_REJECTED", confirm.ReasonCode);
        Assert.Null(host.ContractNegotiation);

        ClubRosterEntry after = Assert.Single(
            host.ClubRoster!.Riders,
            rider => rider.RiderCareerId == alphaLeader.RiderCareerId);
        Assert.Equal(wageBefore, after.AnnualWage);
    }

    [Fact]
    public void OpenSkeletonStaysInManagementWithHubCalendarAndPeople()
    {
        using TemporaryDirectory temp = new();
        CareerShellHost host = CreateHost(temp.Path);

        Assert.True(host.OpenSkeleton(GateSeed).Succeeded);
        Assert.Equal(GameState.Management, host.State);
        CareerDayProjection day = Assert.IsType<CareerDayProjection>(host.Day);
        Assert.Equal(0, day.DayNumber);
        Assert.Equal(HubPrimaryActionIds.AdvanceDay, day.PrimaryAction);
        Assert.Equal("Advance Day", day.PrimaryLabel);
        Assert.Equal("Skeleton Manager", day.ManagerName);
        Assert.Equal("red", day.EmployerName);
        Assert.Contains(host.Calendar, entry => entry.Title == "Skeleton race");
        Assert.Single(host.Calendar);
        Assert.DoesNotContain(host.People, person => person.Name.Contains("OVR", StringComparison.Ordinal));
        Assert.NotEmpty(host.Organizations);
        Assert.False(host.Settings.WatchFilmEnabled);
    }

    [Fact]
    public void FollowPrimaryAdvancesDayThenEntersPreparationOnRaceDue()
    {
        using TemporaryDirectory temp = new();
        CareerShellHost host = CreateHost(temp.Path);
        Assert.True(host.OpenSkeleton().Succeeded);

        while (host.Day is { RaceDueToday: false })
        {
            Assert.True(host.FollowPrimary().Succeeded);
            Assert.Equal(GameState.Management, host.State);
        }

        CareerDayProjection due = Assert.IsType<CareerDayProjection>(host.Day);
        Assert.Equal(HubPrimaryActionIds.RaceNext, due.PrimaryAction);
        Assert.Equal("Race next", due.PrimaryLabel);
        Assert.Contains(host.Inbox, item => item.Category == "race-due");
        Assert.Equal(
            "INBOX_SOURCE_CANNOT_BE_DISMISSED",
            host.ArchiveInbox(host.Inbox[0].Identity).ReasonCode);

        Assert.True(host.FollowPrimary().Succeeded);
        Assert.Equal(GameState.RacePreparationFlow, host.State);
        Assert.Null(host.Day);
        RacePreparationProjection prep = Assert.IsType<RacePreparationProjection>(host.Preparation);
        Assert.Equal("Skeleton race", prep.Title);
        Assert.Equal(4, prep.Squad.Count);
        Assert.Contains(prep.Squad, rider => host.RiderDisplayName(rider) == "Alpha Leader");
    }

    [Fact]
    public void RaceNextSimulatesToResultsTableByDefault()
    {
        using TemporaryDirectory temp = new();
        CareerShellHost host = CreateHost(temp.Path);
        Assert.True(host.OpenSkeleton().Succeeded);
        AdvanceToRaceDue(host);

        Assert.True(host.FollowPrimary().Succeeded);
        Assert.False(host.Settings.WatchFilmEnabled);
        Assert.True(host.RunRace().Succeeded);
        Assert.Equal(GameState.RaceResultsFlow, host.State);
        Assert.Null(host.Watch);
        RaceResultProjection result = Assert.IsType<RaceResultProjection>(host.Result);
        Assert.Equal("Skeleton race", result.Title);
        Assert.Equal(BetaLeaderOriginId, result.WinnerLabel);
        Assert.Equal("Beta Leader", host.RiderDisplayName(result.WinnerId));
        Assert.Equal(3, host.ResultTeams.Count);
        Assert.Contains(
            result.FinishOrder,
            row => host.RiderDisplayName(row.RiderId) == "Alpha Card" && row.OrganizationName == "red");
        Assert.Equal(12, host.VisibleResultTable.Count);

        OrganizationNameProjection red = host.ResultTeams.Single(team => team.Name == "red");
        host.SetResultTeamFilter(red.Id);
        Assert.Equal(4, host.VisibleResultTable.Count);
        Assert.All(host.VisibleResultTable, row => Assert.Equal("red", row.OrganizationName));
        host.SetResultTeamFilter(null);
        Assert.Equal(12, host.VisibleResultTable.Count);

        Assert.True(host.ContinueOutcome().Succeeded);
        Assert.Equal(GameState.RaceDebriefFlow, host.State);
        Assert.True(host.ContinueOutcome().Succeeded);
        Assert.Equal(GameState.Management, host.State);
        Assert.Contains(host.Calendar, entry => entry.OfficialResult is not null);
    }

    [Fact]
    public void WatchFilmSettingIsOffByDefaultAndOptInOpensWatch()
    {
        using TemporaryDirectory temp = new();
        CareerShellHost host = CreateHost(temp.Path);
        Assert.True(host.OpenSkeleton().Succeeded);
        Assert.False(host.Settings.WatchFilmEnabled);
        host.SetWatchFilmEnabled(true);
        Assert.True(host.Settings.WatchFilmEnabled);

        CareerShellHost reloaded = CreateHost(temp.Path);
        Assert.True(reloaded.Settings.WatchFilmEnabled);
        Assert.True(reloaded.OpenSkeleton().Succeeded);

        AdvanceToRaceDue(reloaded);
        Assert.True(reloaded.FollowPrimary().Succeeded);
        Assert.True(reloaded.RunRace().Succeeded);
        Assert.Equal(GameState.RaceLive, reloaded.State);
        Assert.NotNull(reloaded.Watch);
        Assert.Contains(reloaded.Watch!.Interpolated!.Riders, rider => rider.Name == "Alpha Leader");
    }

    [Fact]
    public void SaveAndLoadRoundTripManagementDay()
    {
        using TemporaryDirectory temp = new();
        CareerShellHost host = CreateHost(temp.Path);
        Assert.True(host.OpenSkeleton().Succeeded);
        Assert.True(host.FollowPrimary().Succeeded);
        int dayNumber = host.Day!.DayNumber;
        Assert.True(host.Save().Succeeded);

        CareerShellHost loaded = CreateHost(temp.Path);
        Assert.True(loaded.Load().Succeeded);
        Assert.Equal(GameState.Management, loaded.State);
        Assert.Equal(dayNumber, loaded.Day!.DayNumber);
        Assert.Equal(host.Day.EmployerName, loaded.Day.EmployerName);
    }

    private static void AdvanceToRaceDue(CareerShellHost host)
    {
        for (int day = 0; day < 32 && host.Day is { RaceDueToday: false }; day++)
        {
            Assert.True(host.FollowPrimary().Succeeded);
        }

        Assert.True(host.Day!.RaceDueToday);
    }

    private static CareerShellHost CreateHost(string directory)
    {
        GameApplication application = new(
            new JsonScenarioCatalog(ContentRoot()),
            new JsonRacePrototypeCatalog(ContentRoot()),
            new SqliteWorldSaveStore(),
            new PrototypeRaceEngine());
        return new CareerShellHost(
            application,
            Path.Combine(directory, "career.peloton"),
            Path.Combine(directory, "pre-race.peloton"));
    }

    private static string ContentRoot()
    {
        return Path.Combine(RepositoryRoot(), "content");
    }

    private static string RepositoryRoot()
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
