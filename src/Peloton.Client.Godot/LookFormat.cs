using System;
using System.Globalization;

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
        return ("ROK", year.ToString(CultureInfo.InvariantCulture));
    }

    public static string ManagerInitials(string? managerName)
    {
        if (string.IsNullOrWhiteSpace(managerName))
        {
            return "MN";
        }

        return CareerLookCatalog.Initials(managerName);
    }
}
