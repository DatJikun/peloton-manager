using System;
using System.Globalization;

namespace Peloton.Application;

public static class CareerCalendarDates
{
    public static readonly DateOnly Epoch = new(2026, 1, 1);

    private static readonly string[] MonthNamesLong =
    [
        "stycznia",
        "lutego",
        "marca",
        "kwietnia",
        "maja",
        "czerwca",
        "lipca",
        "sierpnia",
        "września",
        "października",
        "listopada",
        "grudnia",
    ];

    private static readonly string[] MonthAbbr =
    [
        "STY",
        "LUT",
        "MAR",
        "KWI",
        "MAJ",
        "CZE",
        "LIP",
        "SIE",
        "WRZ",
        "PAŹ",
        "LIS",
        "GRU",
    ];

    private static readonly string[] MonthNamesNav =
    [
        "STYCZEŃ",
        "LUTY",
        "MARZEC",
        "KWIECIEŃ",
        "MAJ",
        "CZERWIEC",
        "LIPIEC",
        "SIERPIEŃ",
        "WRZESIEŃ",
        "PAŹDZIERNIK",
        "LISTOPAD",
        "GRUDZIEŃ",
    ];

    private static readonly string[] WeekdayShort = ["PN", "WT", "ŚR", "CZW", "PT", "SO", "NIE"];

    private static readonly CultureInfo Polish = CultureInfo.GetCultureInfo("pl-PL");

    public static DateOnly ToDate(int dayNumber) => Epoch.AddDays(dayNumber);

    public static string FormatLong(int dayNumber)
    {
        DateOnly date = ToDate(dayNumber);
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{date.Day} {MonthNamesLong[date.Month - 1]} {date.Year}");
    }

    public static string FormatSlab(int dayNumber)
    {
        DateOnly date = ToDate(dayNumber);
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{date.Day} {MonthAbbr[date.Month - 1]}");
    }

    public static string FormatWeekdayShort(int dayNumber)
    {
        int index = ToDate(dayNumber).DayOfWeek switch
        {
            DayOfWeek.Monday => 0,
            DayOfWeek.Tuesday => 1,
            DayOfWeek.Wednesday => 2,
            DayOfWeek.Thursday => 3,
            DayOfWeek.Friday => 4,
            DayOfWeek.Saturday => 5,
            _ => 6,
        };

        return WeekdayShort[index];
    }

    public static string FormatWeekdayLong(int dayNumber)
    {
        string name = ToDate(dayNumber).ToString("dddd", Polish);
        if (name.Length == 0)
        {
            return name;
        }

        return char.ToUpper(name[0], Polish) + name[1..];
    }

    public static string FormatMonthNav(int year, int month) =>
        string.Create(CultureInfo.InvariantCulture, $"{MonthNamesNav[month - 1]} {year}");

    public static string FormatRange(int startDay, int endDay)
    {
        if (startDay == endDay)
        {
            return FormatLong(startDay);
        }

        DateOnly start = ToDate(startDay);
        DateOnly end = ToDate(endDay);
        if (start.Year == end.Year && start.Month == end.Month)
        {
            return string.Create(
                CultureInfo.InvariantCulture,
                $"{start.Day}–{end.Day} {MonthNamesLong[start.Month - 1]} {start.Year}");
        }

        if (start.Year == end.Year)
        {
            return string.Create(
                CultureInfo.InvariantCulture,
                $"{start.Day} {MonthNamesLong[start.Month - 1]} – {end.Day} {MonthNamesLong[end.Month - 1]} {start.Year}");
        }

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{FormatLong(startDay)} – {FormatLong(endDay)}");
    }

    public static int DayNumberFromDate(DateOnly date) => date.DayNumber - Epoch.DayNumber;

    public static (int Year, int Month) MonthFromDayNumber(int dayNumber)
    {
        DateOnly date = ToDate(dayNumber);
        return (date.Year, date.Month);
    }
}
