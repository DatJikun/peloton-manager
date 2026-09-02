using System;
using System.Collections.Generic;
using System.Globalization;
using Peloton.Application;

namespace Peloton.Client.Godot;

internal static class LookFormat
{
    public static (string Prefix, string? Accent) RaceCountdownPill(int daysUntilRace)
    {
        if (daysUntilRace <= 0)
        {
            return ("BRAK WYŚCIGU", null);
        }

        string daysLabel = daysUntilRace == 1 ? "1 DZIEŃ" : $"{daysUntilRace} DNI";
        return ("WYŚCIG ZA ", daysLabel);
    }

    public static (string Prefix, string Accent) YearPillParts(int year)
    {
        return ("ROK ", year.ToString(CultureInfo.InvariantCulture));
    }

    public static string DateChipLabel(int dayNumber)
    {
        DateOnly date = CareerCalendarDates.ToDate(dayNumber);
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{CareerCalendarDates.FormatWeekdayShort(dayNumber)} {date.Day:00}.{date.Month:00}");
    }

    public static string EventMetaLine(SeasonEventProjection item, int todayDay, bool worldTour)
    {
        List<string> parts = new();
        if (worldTour)
        {
            parts.Add("WORLDTOUR");
        }

        parts.Add(item.StageCount > 1
            ? string.Create(CultureInfo.InvariantCulture, $"{item.StageCount} ETAPÓW")
            : "JEDNODNIOWY");
        int daysUntil = item.StartDay - todayDay;
        if (daysUntil > 0)
        {
            parts.Add(string.Create(CultureInfo.InvariantCulture, $"ZA {daysUntil} DNI"));
        }
        else if (daysUntil == 0)
        {
            parts.Add("DZIŚ");
        }

        return string.Join(" · ", parts);
    }

    public static string EventStatusLabel(string status) =>
        status switch
        {
            "scheduled" => "ZAPLANOWANY",
            "due" => "DZIŚ",
            "completed" => "ZAKOŃCZONY",
            _ => status.ToUpperInvariant(),
        };

    public static string EventCategoryLabel(SeasonEventProjection item, bool worldTour) =>
        worldTour ? "WORLDTOUR" : item.StageCount > 1 ? "WYŚCIG WIELOETAPOWY" : "JEDNODNIOWY";

    public static string ManagerInitials(string? managerName)
    {
        if (string.IsNullOrWhiteSpace(managerName))
        {
            return "MN";
        }

        return CareerLookCatalog.Initials(managerName);
    }
}
