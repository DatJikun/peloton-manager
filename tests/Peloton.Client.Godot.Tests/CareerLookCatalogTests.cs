using System.Linq;
using Xunit;

namespace Peloton.Client.Godot.Tests;

public sealed class CareerLookCatalogTests
{
    [Fact]
    public void CatalogCarriesThePocDensityWithoutTouchingWorldNames()
    {
        Assert.Equal(10, CareerLookCatalog.Riders.Count);
        Assert.Equal("Kowalczyk", CareerLookCatalog.Riders[0].Last);
        Assert.Equal(84, CareerLookCatalog.Riders[0].Rate);
        Assert.Equal(6, CareerLookCatalog.Staff.Count);
        Assert.Equal(3, CareerLookCatalog.Sponsors.Count);
        Assert.Equal(6, CareerLookCatalog.Transfers.Count);
        Assert.Equal(5, CareerLookCatalog.UpcomingRaces.Count);
        Assert.Equal("mila-torino", CareerLookCatalog.UpcomingRaces[0].Id);
        Assert.Equal("Milano–Torino", CareerLookCatalog.CalendarRace("mila-torino")!.Name);
        Assert.Equal(3, CareerLookCatalog.CalendarRace("mila-torino")!.Month);
        Assert.Equal(12, CareerLookCatalog.CalendarRace("mila-torino")!.Day);
        Assert.Contains(CareerLookCatalog.Cells(CareerLookCatalog.Months[1]), cell => cell.IsToday && cell.Day == 11);
        Assert.Equal(2, CareerLookCatalog.Scouts().Count);
        Assert.Equal("38 000 zł", CareerLookCatalog.Zloty(38000));
        Assert.Equal("+120 000 zł", CareerLookCatalog.SignedZloty(120000));
        Assert.Equal("PK", CareerLookCatalog.Initials("Piotr Kowalczyk"));
        Assert.Equal("PW", CareerLookCatalog.Initials("dr Piotr Wysocki"));
        Assert.Equal("★★★★☆", CareerLookCatalog.Stars(4));
        Assert.False(string.IsNullOrWhiteSpace(CareerLookCatalog.NotInWorld));
        Assert.Contains("laboratorium", CareerLookCatalog.Banner);
    }

    [Fact]
    public void SortsRidersAndTransfersWithoutMutatingTheCatalog()
    {
        LookSort byRate = new("rate", -1);
        Assert.Equal("Kowalczyk", CareerLookCatalog.SortedRiders(byRate)[0].Last);
        Assert.Equal("Lemaire", CareerLookCatalog.SortedTransfers(byRate)[0].Last);
        Assert.Equal("Kowalczyk", CareerLookCatalog.Riders[0].Last);
        Assert.Equal("Martin", CareerLookCatalog.Transfers[0].Last);
        LookSort byName = CareerLookCatalog.Toggle(new("last", 1), "last");
        Assert.Equal(-1, byName.Dir);
        Assert.Equal("Barski", CareerLookCatalog.SortedRiders(new("last", 1))[0].Last);
    }

    [Fact]
    public void LookNumbersAreNotWorldPeopleLabels()
    {
        Assert.DoesNotContain(CareerLookCatalog.Riders, rider => rider.FullName.Contains("Skeleton"));
        Assert.DoesNotContain(CareerLookCatalog.Staff, person => person.Name.Contains("OVR"));
        Assert.Equal(412300, CareerLookCatalog.SeasonBudget);
        Assert.True(CareerLookCatalog.FitScore(CareerLookCatalog.Riders[0]) > 0);
        Assert.Equal(42, CareerLookCatalog.Cells(CareerLookCatalog.Months[1]).Count);
        Assert.NotNull(CareerLookCatalog.Cells(CareerLookCatalog.Months[1]).First(cell => cell.Race?.Id == "mila-torino"));
    }
}
