using System;
using System.Collections.Generic;
using System.Linq;

namespace Peloton.Simulation.Race;

public enum RouteTerrainKind
{
    Flat,
    Climb,
    Descent,
    Rolling,
    Crosswind,
}

public sealed record RouteProfileKnot(
    double T,
    double Shape,
    double WidthScale,
    double WindScale);

public sealed record RouteProfileTemplate(
    string Id,
    RouteTerrainKind Kind,
    int Variant,
    string Label,
    IReadOnlyList<RouteProfileKnot> Knots);

public static class RouteProfileLibrary
{
    public const int VariantsPerKind = 3;

    public static IReadOnlyList<RouteProfileTemplate> All { get; } = BuildAll();

    public static RouteProfileTemplate Get(RouteTerrainKind kind, int variant)
    {
        if (variant < 0 || variant >= VariantsPerKind)
        {
            throw new ArgumentOutOfRangeException(nameof(variant));
        }

        return All.Single(template => template.Kind == kind && template.Variant == variant);
    }

    private static IReadOnlyList<RouteProfileTemplate> BuildAll()
    {
        return new[]
        {
            Template(
                "flat.pancake-rollers",
                RouteTerrainKind.Flat,
                0,
                "Płaskie z drobnymi falami",
                (0.00, 0.0, 1.00, 1.00),
                (0.12, 1.8, 1.00, 1.00),
                (0.28, -0.6, 1.00, 1.00),
                (0.45, 2.4, 1.00, 1.00),
                (0.62, 0.4, 1.00, 1.00),
                (0.78, -1.2, 1.00, 1.00),
                (0.90, 0.8, 1.00, 1.00),
                (1.00, 0.0, 1.00, 1.00)),
            Template(
                "flat.false-flats",
                RouteTerrainKind.Flat,
                1,
                "Fałszywy płaskownik",
                (0.00, 0.0, 1.00, 1.00),
                (0.15, 4.5, 1.00, 1.00),
                (0.35, 9.0, 0.96, 1.00),
                (0.50, 7.0, 1.00, 1.00),
                (0.70, 2.0, 1.00, 1.00),
                (0.88, -1.0, 1.00, 1.00),
                (1.00, 0.0, 1.00, 1.00)),
            Template(
                "flat.descent-then-drag",
                RouteTerrainKind.Flat,
                2,
                "Lekki zjazd i przeciągnięcie",
                (0.00, 0.0, 1.00, 1.00),
                (0.20, -6.0, 1.00, 1.00),
                (0.40, -8.0, 1.00, 1.00),
                (0.55, -5.0, 1.00, 1.00),
                (0.75, -1.0, 1.00, 1.00),
                (1.00, 0.0, 1.00, 1.00)),
            Template(
                "climb.two-ramp-col",
                RouteTerrainKind.Climb,
                0,
                "Przełęcz z dwoma rampami",
                (0.00, 0.00, 1.00, 1.00),
                (0.10, 0.06, 1.00, 1.00),
                (0.22, 0.18, 0.96, 1.00),
                (0.34, 0.38, 0.94, 1.00),
                (0.46, 0.48, 0.97, 1.00),
                (0.55, 0.54, 1.00, 1.00),
                (0.68, 0.74, 0.93, 1.00),
                (0.82, 0.90, 0.94, 1.00),
                (0.93, 0.98, 0.97, 1.00),
                (1.00, 1.00, 1.00, 1.00)),
            Template(
                "climb.step-walls",
                RouteTerrainKind.Climb,
                1,
                "Stopnie i ścianki",
                (0.00, 0.00, 1.00, 1.00),
                (0.12, 0.08, 1.00, 1.00),
                (0.18, 0.10, 1.00, 1.00),
                (0.32, 0.32, 0.92, 1.00),
                (0.42, 0.38, 1.00, 1.00),
                (0.50, 0.42, 1.00, 1.00),
                (0.64, 0.68, 0.90, 1.00),
                (0.74, 0.74, 0.98, 1.00),
                (0.88, 0.94, 0.93, 1.00),
                (1.00, 1.00, 1.00, 1.00)),
            Template(
                "climb.irregular-alpine",
                RouteTerrainKind.Climb,
                2,
                "Nieregularny alpejski",
                (0.00, 0.00, 1.00, 1.00),
                (0.15, 0.08, 1.00, 1.00),
                (0.28, 0.18, 0.98, 1.00),
                (0.40, 0.28, 0.96, 1.00),
                (0.52, 0.52, 0.90, 1.00),
                (0.64, 0.70, 0.92, 1.00),
                (0.76, 0.78, 1.00, 1.00),
                (0.86, 0.84, 1.00, 1.00),
                (0.94, 0.96, 0.93, 1.00),
                (1.00, 1.00, 1.00, 1.00)),
            Template(
                "descent.technical-hairpins",
                RouteTerrainKind.Descent,
                0,
                "Techniczny ze spłaszczeniami",
                (0.00, 0.00, 1.00, 1.00),
                (0.12, 0.22, 0.94, 1.00),
                (0.20, 0.26, 0.90, 1.00),
                (0.38, 0.52, 0.94, 1.00),
                (0.48, 0.56, 0.90, 1.00),
                (0.70, 0.84, 0.96, 1.00),
                (0.82, 0.90, 1.00, 1.00),
                (1.00, 1.00, 1.00, 1.00)),
            Template(
                "descent.fast-straight",
                RouteTerrainKind.Descent,
                1,
                "Szybki prosty zjazd",
                (0.00, 0.00, 1.00, 1.00),
                (0.15, 0.18, 1.00, 1.00),
                (0.40, 0.50, 1.00, 1.00),
                (0.70, 0.82, 1.00, 1.00),
                (0.88, 0.94, 1.00, 1.00),
                (1.00, 1.00, 1.00, 1.00)),
            Template(
                "descent.two-step",
                RouteTerrainKind.Descent,
                2,
                "Dwa progi ze spłaszczeniem",
                (0.00, 0.00, 1.00, 1.00),
                (0.22, 0.40, 0.96, 1.00),
                (0.40, 0.46, 1.00, 1.00),
                (0.55, 0.52, 1.00, 1.00),
                (0.80, 0.88, 0.95, 1.00),
                (1.00, 1.00, 1.00, 1.00)),
            Template(
                "rolling.classic-waves",
                RouteTerrainKind.Rolling,
                0,
                "Klasyczne fale",
                (0.00, 0.0, 1.00, 1.00),
                (0.11, 12.0, 0.98, 1.00),
                (0.22, 0.0, 1.00, 1.00),
                (0.33, 10.0, 0.97, 1.00),
                (0.44, -2.0, 1.00, 1.00),
                (0.55, 14.0, 0.96, 1.00),
                (0.66, 1.0, 1.00, 1.00),
                (0.78, 8.0, 0.98, 1.00),
                (0.90, -1.0, 1.00, 1.00),
                (1.00, 0.0, 1.00, 1.00)),
            Template(
                "rolling.punchy-bergs",
                RouteTerrainKind.Rolling,
                1,
                "Krótkie ścianki",
                (0.00, 0.0, 1.00, 1.00),
                (0.08, 2.0, 1.00, 1.00),
                (0.16, 18.0, 0.90, 1.00),
                (0.28, 4.0, 1.00, 1.00),
                (0.40, -2.0, 1.00, 1.00),
                (0.48, 16.0, 0.90, 1.00),
                (0.62, 2.0, 1.00, 1.00),
                (0.78, 14.0, 0.92, 1.00),
                (0.90, 3.0, 1.00, 1.00),
                (1.00, 0.0, 1.00, 1.00)),
            Template(
                "rolling.long-ardennes",
                RouteTerrainKind.Rolling,
                2,
                "Długie fale",
                (0.00, 0.0, 1.00, 1.00),
                (0.20, 16.0, 0.97, 1.00),
                (0.40, -4.0, 1.00, 1.00),
                (0.60, 14.0, 0.97, 1.00),
                (0.80, -2.0, 1.00, 1.00),
                (1.00, 0.0, 1.00, 1.00)),
            Template(
                "crosswind.exposed-pinch",
                RouteTerrainKind.Crosswind,
                0,
                "Odsłonięty odcinek z przewężeniem",
                (0.00, 0.0, 1.00, 1.00),
                (0.20, 1.5, 0.85, 1.05),
                (0.40, 0.5, 0.58, 1.15),
                (0.50, 0.8, 0.52, 1.20),
                (0.65, 0.2, 0.62, 1.12),
                (0.85, 1.0, 0.88, 1.04),
                (1.00, 0.0, 1.00, 1.00)),
            Template(
                "crosswind.coastal-dunes",
                RouteTerrainKind.Crosswind,
                1,
                "Nadmorskie wydmy",
                (0.00, 0.0, 1.00, 1.00),
                (0.15, 4.0, 0.90, 1.05),
                (0.30, 1.0, 0.70, 1.10),
                (0.45, 6.0, 0.55, 1.18),
                (0.60, 2.0, 0.60, 1.15),
                (0.78, 5.0, 0.80, 1.06),
                (1.00, 0.0, 1.00, 1.00)),
            Template(
                "crosswind.dyke",
                RouteTerrainKind.Crosswind,
                2,
                "Wał z przewężeniem",
                (0.00, 0.0, 1.00, 1.00),
                (0.18, 5.0, 0.85, 1.08),
                (0.35, 8.0, 0.55, 1.20),
                (0.50, 8.5, 0.50, 1.25),
                (0.68, 7.0, 0.62, 1.15),
                (0.85, 3.0, 0.88, 1.05),
                (1.00, 0.0, 1.00, 1.00)),
        };
    }

    private static RouteProfileTemplate Template(
        string id,
        RouteTerrainKind kind,
        int variant,
        string label,
        params (double T, double Shape, double WidthScale, double WindScale)[] knots)
    {
        RouteProfileKnot[] mapped = knots
            .Select(knot => new RouteProfileKnot(knot.T, knot.Shape, knot.WidthScale, knot.WindScale))
            .ToArray();
        if (mapped.Length < 2 || mapped[0].T != 0.0 || mapped[^1].T != 1.0)
        {
            throw new InvalidOperationException($"Template {id} must start at t=0 and end at t=1.");
        }

        return new RouteProfileTemplate(id, kind, variant, label, mapped);
    }
}
