#!/usr/bin/env python3
"""Expand peloton.wt-2026 to 8-man WT squads + 7 wildcard teams (D-049).

Idempotent: extra riders/orgs are upserted by id. Existing 72 cards stay.
"""

from __future__ import annotations

import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PACK = ROOT / "content" / "peloton.wt-2026"
ROSTER_PATH = PACK / "roster.json"
ORGS_PATH = PACK / "organizations.json"
IDENTITIES_PATH = PACK / "race-identities.json"

WT_ORGS = [
    "alpecin",
    "bahrain",
    "decathlon",
    "ef",
    "fdj",
    "ineos",
    "lidl-trek",
    "lotto",
    "movistar",
    "nsn",
    "redbull",
    "soudal",
    "jayco",
    "picnic",
    "visma",
    "uae",
    "unox",
    "astana",
]

WT_ORG_IDS = [f"organization.wt2026.{slug}" for slug in WT_ORGS]

WILDCARD_ORGS = [
    {
        "id": "organization.wt2026.israel",
        "uciCode": "IPT",
        "name": "Israel–Premier Tech",
        "country": "ISR",
        "titleSponsor": "Israel",
        "coSponsors": ["Premier Tech"],
        "bike": "Factor",
        "groupset": "Shimano",
        "division": "ProTeam",
        "licenceYearsRemaining": 1,
        "budgetBand": "mid",
        "estimatedBudgetEur": 8_000_000,
    },
    {
        "id": "organization.wt2026.tudor",
        "uciCode": "TUD",
        "name": "Tudor Pro Cycling Team",
        "country": "SUI",
        "titleSponsor": "Tudor",
        "coSponsors": [],
        "bike": "Specialized",
        "groupset": "SRAM",
        "division": "ProTeam",
        "licenceYearsRemaining": 1,
        "budgetBand": "mid",
        "estimatedBudgetEur": 8_000_000,
    },
    {
        "id": "organization.wt2026.q36",
        "uciCode": "Q36",
        "name": "Q36.5 Pro Cycling Team",
        "country": "SUI",
        "titleSponsor": "Q36.5",
        "coSponsors": [],
        "bike": "Scott",
        "groupset": "Shimano",
        "division": "ProTeam",
        "licenceYearsRemaining": 1,
        "budgetBand": "mid",
        "estimatedBudgetEur": 7_000_000,
    },
    {
        "id": "organization.wt2026.totalenergies",
        "uciCode": "TEN",
        "name": "TotalEnergies",
        "country": "FRA",
        "titleSponsor": "TotalEnergies",
        "coSponsors": [],
        "bike": "Specialized",
        "groupset": "Shimano",
        "division": "ProTeam",
        "licenceYearsRemaining": 1,
        "budgetBand": "mid",
        "estimatedBudgetEur": 7_000_000,
    },
    {
        "id": "organization.wt2026.cofidis",
        "uciCode": "COF",
        "name": "Cofidis",
        "country": "FRA",
        "titleSponsor": "Cofidis",
        "coSponsors": [],
        "bike": "Look",
        "groupset": "Shimano",
        "division": "ProTeam",
        "licenceYearsRemaining": 1,
        "budgetBand": "mid",
        "estimatedBudgetEur": 8_000_000,
    },
    {
        "id": "organization.wt2026.unibet",
        "uciCode": "TDT",
        "name": "Unibet Tietema Rockets",
        "country": "NED",
        "titleSponsor": "Unibet",
        "coSponsors": ["Tietema"],
        "bike": "Canyon",
        "groupset": "Shimano",
        "division": "ProTeam",
        "licenceYearsRemaining": 1,
        "budgetBand": "tight",
        "estimatedBudgetEur": 5_000_000,
    },
    {
        "id": "organization.wt2026.australia",
        "uciCode": "AUS",
        "name": "Australia",
        "country": "AUS",
        "titleSponsor": "Cycling Australia",
        "coSponsors": [],
        "bike": "Specialized",
        "groupset": "Shimano",
        "division": "National",
        "licenceYearsRemaining": 0,
        "budgetBand": "national",
        "estimatedBudgetEur": 2_000_000,
    },
]

# (slot, name, nationality, birthYear, archetype, extras)
WT_EXTRAS: dict[str, list[tuple]] = {
    "alpecin": [
        ("support-3", "Kaden Groves", "AUS", 1998, "sprinter", {"peakPowerW": 1600, "criticalPowerW": 368, "cdAM2": 0.31, "bodyMassKg": 76, "annualWage": 220_000}),
        ("support-4", "Silvan Dillier", "SUI", 1990, "diesel", {"annualWage": 140_000}),
        ("support-5", "Jonas Rickaert", "BEL", 1994, "classics", {"annualWage": 130_000}),
        ("support-6", "Timo Kielich", "BEL", 1999, "classics", {"annualWage": 110_000}),
    ],
    "bahrain": [
        ("support-3", "Matevž Govekar", "SLO", 2000, "sprinter", {"annualWage": 150_000}),
        ("support-4", "Rainer Kepplinger", "AUT", 1997, "gc", {"annualWage": 140_000}),
        ("support-5", "Torstein Træen", "NOR", 1995, "gc", {"annualWage": 130_000}),
        ("support-6", "Fran Miholjević", "CRO", 2002, "neo", {"annualWage": 95_000}),
    ],
    "decathlon": [
        ("support-3", "Sam Bennett", "IRL", 1990, "sprinter", {"peakPowerW": 1580, "annualWage": 200_000}),
        ("support-4", "Dries De Bondt", "BEL", 1991, "classics", {"annualWage": 130_000}),
        ("support-5", "Taco van der Hoorn", "NED", 1993, "classics", {"annualWage": 120_000}),
        ("support-6", "Georg Zimmermann", "GER", 1997, "gc", {"annualWage": 140_000}),
    ],
    "ef": [
        ("support-3", "Alberto Bettiol", "ITA", 1993, "classics", {"annualWage": 180_000}),
        ("support-4", "Owain Doull", "GBR", 1993, "diesel", {"annualWage": 120_000}),
        ("support-5", "James Shaw", "GBR", 1996, "gc", {"annualWage": 130_000}),
        ("support-6", "Madis Mihkels", "EST", 2003, "neo", {"annualWage": 100_000}),
    ],
    "fdj": [
        ("support-3", "Lewis Askey", "GBR", 2001, "classics", {"annualWage": 140_000}),
        ("support-4", "Clément Russo", "FRA", 1995, "diesel", {"annualWage": 120_000}),
        ("support-5", "Enzo Paleni", "FRA", 2003, "neo", {"annualWage": 90_000}),
        ("support-6", "Reuben Thompson", "NZL", 2001, "gc", {"annualWage": 110_000}),
    ],
    "ineos": [
        ("support-3", "Connor Swift", "GBR", 1995, "diesel", {"annualWage": 150_000}),
        ("support-4", "Ben Swift", "GBR", 1987, "classics", {"annualWage": 140_000}),
        ("support-5", "Omar Fraile", "ESP", 1990, "gc", {"annualWage": 130_000}),
        ("support-6", "Salvatore Puccio", "ITA", 1989, "diesel", {"annualWage": 120_000}),
    ],
    "lidl-trek": [
        ("support-3", "Edward Theuns", "BEL", 1991, "sprinter", {"annualWage": 160_000}),
        ("support-4", "Jasper Stuyven", "BEL", 1992, "classics", {"annualWage": 180_000}),
        ("support-5", "Tim Declercq", "BEL", 1989, "diesel", {"annualWage": 140_000}),
        ("support-6", "Daan Hoole", "NED", 1999, "tt", {"annualWage": 120_000}),
    ],
    "lotto": [
        ("support-3", "Alec Segaert", "BEL", 2003, "tt", {"annualWage": 110_000}),
        ("support-4", "Harm Vanhoucke", "BEL", 1997, "gc", {"annualWage": 130_000}),
        ("support-5", "Lionel Taminiaux", "BEL", 1996, "sprinter", {"annualWage": 120_000}),
        ("support-6", "Jenno Berckmoes", "BEL", 2001, "neo", {"annualWage": 95_000}),
    ],
    "movistar": [
        ("support-3", "Pelayo Sánchez", "ESP", 2000, "classics", {"annualWage": 130_000}),
        ("support-4", "Iván Romeo", "ESP", 2003, "neo", {"annualWage": 100_000}),
        ("support-5", "Jorge Arcas", "ESP", 1992, "diesel", {"annualWage": 110_000}),
        ("support-6", "Einer Rubio", "COL", 1998, "gc", {"annualWage": 150_000}),
    ],
    "nsn": [
        ("support-3", "Tobias Foss", "NOR", 1997, "tt", {"annualWage": 180_000}),
        ("support-4", "Dylan van Baarle", "NED", 1992, "classics", {"annualWage": 200_000}),
        ("support-5", "Mike Teunissen", "NED", 1992, "classics", {"annualWage": 140_000}),
        ("support-6", "Fabio Jakobsen", "NED", 1996, "sprinter", {"peakPowerW": 1620, "annualWage": 180_000}),
    ],
    "redbull": [
        ("support-3", "Sam Welsford", "AUS", 1996, "sprinter", {"peakPowerW": 1650, "criticalPowerW": 378, "cdAM2": 0.315, "bodyMassKg": 78, "annualWage": 220_000}),
        ("support-4", "Gianni Moscon", "ITA", 1994, "classics", {"annualWage": 160_000}),
        ("support-5", "Aleksandr Vlasov", "RUS", 1996, "gc", {"annualWage": 200_000}),
        ("support-6", "Nico Denz", "GER", 1994, "diesel", {"annualWage": 140_000}),
    ],
    "soudal": [
        ("support-3", "Mattia Cattaneo", "ITA", 1990, "tt", {"annualWage": 160_000}),
        ("support-4", "Pieter Serry", "BEL", 1988, "diesel", {"annualWage": 120_000}),
        ("support-5", "Gianni Marchand", "BEL", 1990, "gc", {"annualWage": 110_000}),
        ("support-6", "Stan Van Tricht", "BEL", 1999, "neo", {"annualWage": 95_000}),
    ],
    "jayco": [
        ("support-3", "Campbell Stewart", "AUS", 1998, "sprinter", {"annualWage": 140_000}),
        ("support-4", "Luke Durbridge", "AUS", 1991, "tt", {"annualWage": 150_000}),
        ("support-5", "Elmar Reinders", "NED", 1992, "diesel", {"annualWage": 120_000}),
        ("support-6", "Chris Harper", "AUS", 1994, "gc", {"annualWage": 140_000}),
    ],
    "picnic": [
        ("support-3", "Julius van den Berg", "NED", 1996, "diesel", {"annualWage": 120_000}),
        ("support-4", "Gijs Leemreize", "NED", 1999, "gc", {"annualWage": 130_000}),
        ("support-5", "Kevin Vermaerke", "USA", 2000, "gc", {"annualWage": 140_000}),
        ("support-6", "Alex Molenaar", "NED", 1999, "classics", {"annualWage": 100_000}),
    ],
    "visma": [
        ("support-3", "Sepp Kuss", "USA", 1994, "gc", {"annualWage": 220_000}),
        ("support-4", "Tiesj Benoot", "BEL", 1994, "classics", {"annualWage": 200_000}),
        ("support-5", "Christophe Laporte", "FRA", 1992, "classics", {"annualWage": 180_000}),
        ("support-6", "Attila Valter", "HUN", 1998, "gc", {"annualWage": 160_000}),
    ],
    "uae": [
        ("support-3", "Tim Wellens", "BEL", 1991, "classics", {"annualWage": 180_000}),
        ("support-4", "Nils Politt", "GER", 1994, "diesel", {"annualWage": 160_000}),
        ("support-5", "Pavel Sivakov", "FRA", 1997, "gc", {"annualWage": 170_000}),
        ("support-6", "Domen Novak", "SLO", 1995, "gc", {"annualWage": 130_000}),
    ],
    "unox": [
        ("support-3", "Andreas Leknessund", "NOR", 1999, "gc", {"annualWage": 140_000}),
        ("support-4", "Odd Christian Eiking", "NOR", 1994, "gc", {"annualWage": 130_000}),
        ("support-5", "Rasmus Bøgh Wallin", "DEN", 1996, "classics", {"annualWage": 110_000}),
        ("support-6", "Erik Resell", "NOR", 1996, "diesel", {"annualWage": 100_000}),
    ],
    "astana": [
        ("support-3", "Davide Ballerini", "ITA", 1994, "classics", {"annualWage": 140_000}),
        ("support-4", "Michael Mørkøv", "DEN", 1985, "sprinter", {"annualWage": 130_000}),
        ("support-5", "Gleb Syritsa", "RUS", 2000, "sprinter", {"annualWage": 110_000}),
        ("support-6", "Harold Tejada", "COL", 1997, "gc", {"annualWage": 130_000}),
    ],
}

WILDCARD_RIDERS: dict[str, list[tuple]] = {
    "israel": [
        ("leader", "Derek Gee", "CAN", 1997, "gc", {"criticalPowerW": 400, "peakPowerW": 1100, "annualWage": 700_000, "wageBand": "leader"}),
        ("card", "Corbin Strong", "NZL", 2000, "classics", {"annualWage": 280_000, "wageBand": "leader"}),
        ("support-1", "Jake Stewart", "GBR", 1999, "sprinter", {"annualWage": 160_000}),
        ("support-2", "Dylan Teuns", "BEL", 1992, "gc", {"annualWage": 200_000}),
        ("support-3", "Hugo Houle", "CAN", 1990, "diesel", {"annualWage": 140_000}),
        ("support-4", "Krists Neilands", "LAT", 1994, "classics", {"annualWage": 130_000}),
        ("support-5", "Riley Sheehan", "USA", 2000, "neo", {"annualWage": 100_000}),
        ("support-6", "Nick Schultz", "AUS", 1994, "gc", {"annualWage": 120_000}),
    ],
    "tudor": [
        ("leader", "Marc Hirschi", "SUI", 1998, "classics", {"criticalPowerW": 405, "peakPowerW": 1280, "annualWage": 800_000, "wageBand": "leader"}),
        ("card", "Arvid de Kleijn", "NED", 1994, "sprinter", {"peakPowerW": 1580, "annualWage": 220_000, "wageBand": "sprinter"}),
        ("support-1", "Michael Storer", "AUS", 1997, "gc", {"annualWage": 200_000}),
        ("support-2", "Marco Haller", "AUT", 1991, "classics", {"annualWage": 150_000}),
        ("support-3", "Marius Mayrhofer", "GER", 2000, "sprinter", {"annualWage": 130_000}),
        ("support-4", "Alberto Dainese", "ITA", 1998, "sprinter", {"annualWage": 140_000}),
        ("support-5", "Luc Wirtgen", "LUX", 1998, "gc", {"annualWage": 110_000}),
        ("support-6", "Sebastian Molano", "COL", 1994, "sprinter", {"annualWage": 120_000}),
    ],
    "q36": [
        ("leader", "Matteo Sobrero", "ITA", 1997, "tt", {"criticalPowerW": 395, "annualWage": 450_000, "wageBand": "leader"}),
        ("card", "Xandro Meurisse", "BEL", 1992, "classics", {"annualWage": 200_000}),
        ("support-1", "Damien Howson", "AUS", 1992, "gc", {"annualWage": 160_000}),
        ("support-2", "Nickolas Zukowsky", "CAN", 1998, "tt", {"annualWage": 120_000}),
        ("support-3", "Kamil Gradek", "POL", 1990, "diesel", {"annualWage": 110_000}),
        ("support-4", "Rory Townsend", "IRL", 1995, "classics", {"annualWage": 110_000}),
        ("support-5", "David de la Cruz", "ESP", 1989, "gc", {"annualWage": 140_000}),
        ("support-6", "Sjoerd Bax", "NED", 1996, "gc", {"annualWage": 120_000}),
    ],
    "totalenergies": [
        ("leader", "Mathieu Burgaudeau", "FRA", 1998, "classics", {"annualWage": 400_000, "wageBand": "leader"}),
        ("card", "Anthony Turgis", "FRA", 1994, "classics", {"annualWage": 250_000}),
        ("support-1", "Steff Cras", "BEL", 1996, "gc", {"annualWage": 180_000}),
        ("support-2", "Emilien Jeannière", "FRA", 1998, "sprinter", {"annualWage": 140_000}),
        ("support-3", "Sandy Dujardin", "FRA", 1997, "sprinter", {"annualWage": 120_000}),
        ("support-4", "Fabien Grellier", "FRA", 1994, "diesel", {"annualWage": 110_000}),
        ("support-5", "Jordan Jegat", "FRA", 1999, "gc", {"annualWage": 100_000}),
        ("support-6", "Pierre Latour", "FRA", 1993, "gc", {"annualWage": 140_000}),
    ],
    "cofidis": [
        ("leader", "Ion Izagirre", "ESP", 1989, "gc", {"criticalPowerW": 392, "annualWage": 500_000, "wageBand": "leader"}),
        ("card", "Stefano Oldani", "ITA", 1998, "classics", {"annualWage": 220_000}),
        ("support-1", "Alexis Renard", "FRA", 1999, "sprinter", {"annualWage": 140_000}),
        ("support-2", "Piet Allegaert", "BEL", 1995, "classics", {"annualWage": 130_000}),
        ("support-3", "Axel Zingle", "FRA", 1998, "sprinter", {"annualWage": 150_000}),
        ("support-4", "Benjamin Thomas", "FRA", 1995, "tt", {"annualWage": 160_000}),
        ("support-5", "Guillaume Martin", "FRA", 1993, "gc", {"criticalPowerW": 400, "annualWage": 280_000, "wageBand": "leader"}),
        ("support-6", "Jesús Herrada", "ESP", 1990, "gc", {"annualWage": 140_000}),
    ],
    "unibet": [
        ("leader", "Hartthijs de Vries", "NED", 1996, "gc", {"annualWage": 180_000, "wageBand": "leader"}),
        ("card", "Timo de Jong", "NED", 1999, "classics", {"annualWage": 120_000}),
        ("support-1", "Jelle Wolsink", "NED", 2001, "neo", {"annualWage": 90_000}),
        ("support-2", "Axel van der Tuuk", "NED", 2000, "tt", {"annualWage": 95_000}),
        ("support-3", "Abram Stockman", "BEL", 1996, "diesel", {"annualWage": 90_000}),
        ("support-4", "Adne van Engelen", "NED", 1993, "gc", {"annualWage": 90_000}),
        ("support-5", "Kevin van Melsen", "BEL", 1987, "diesel", {"annualWage": 85_000}),
        ("support-6", "Martijn Budding", "NED", 1995, "diesel", {"annualWage": 80_000}),
    ],
    "australia": [
        ("leader", "Luke Plapp", "AUS", 2000, "gc", {"criticalPowerW": 398, "annualWage": 250_000, "wageBand": "leader"}),
        ("card", "Fergus Browning", "AUS", 2004, "gc", {"criticalPowerW": 360, "peakPowerW": 980, "bodyMassKg": 62, "birthYear": 2004, "annualWage": 80_000, "wageBand": "neo"}),
        ("support-1", "Kelland O'Brien", "AUS", 1998, "tt", {"annualWage": 120_000}),
        ("support-2", "Simon Clarke", "AUS", 1986, "classics", {"annualWage": 110_000}),
        ("support-3", "Jack Haig", "AUS", 1993, "gc", {"annualWage": 160_000}),
        ("support-4", "Jarrad Drizners", "AUS", 1999, "diesel", {"annualWage": 90_000}),
        ("support-5", "Patrick Eddy", "AUS", 2003, "sprinter", {"annualWage": 85_000}),
        ("support-6", "Liam Slock", "AUS", 2000, "diesel", {"annualWage": 80_000}),
    ],
}

ARCHETYPE_BANDS = {
    "sprinter": dict(criticalPowerW=365, wPrimeCapacityJ=33000, peakPowerW=1560, wPrimeRecoveryJPerSecond=42, lowIntensityDurability=0.84, highIntensityDurability=0.82, bodyMassKg=74, systemMassKg=8.0, cdAM2=0.315, baseCrr=0.004, positioning=0.86, handling=0.80, tacticalAwareness=0.78, potentialOvr=78, wageBand="domestique"),
    "classics": dict(criticalPowerW=375, wPrimeCapacityJ=31000, peakPowerW=1280, wPrimeRecoveryJPerSecond=43, lowIntensityDurability=0.86, highIntensityDurability=0.84, bodyMassKg=72, systemMassKg=8.0, cdAM2=0.29, baseCrr=0.004, positioning=0.82, handling=0.84, tacticalAwareness=0.80, potentialOvr=80, wageBand="domestique"),
    "gc": dict(criticalPowerW=385, wPrimeCapacityJ=26000, peakPowerW=1050, wPrimeRecoveryJPerSecond=42, lowIntensityDurability=0.88, highIntensityDurability=0.84, bodyMassKg=64, systemMassKg=8.0, cdAM2=0.28, baseCrr=0.0039, positioning=0.74, handling=0.76, tacticalAwareness=0.80, potentialOvr=82, wageBand="domestique"),
    "diesel": dict(criticalPowerW=372, wPrimeCapacityJ=25000, peakPowerW=1120, wPrimeRecoveryJPerSecond=41, lowIntensityDurability=0.90, highIntensityDurability=0.80, bodyMassKg=76, systemMassKg=8.0, cdAM2=0.30, baseCrr=0.004, positioning=0.78, handling=0.76, tacticalAwareness=0.76, potentialOvr=76, wageBand="domestique"),
    "tt": dict(criticalPowerW=400, wPrimeCapacityJ=24000, peakPowerW=1180, wPrimeRecoveryJPerSecond=42, lowIntensityDurability=0.88, highIntensityDurability=0.80, bodyMassKg=78, systemMassKg=8.0, cdAM2=0.245, baseCrr=0.0038, positioning=0.70, handling=0.74, tacticalAwareness=0.78, potentialOvr=80, wageBand="domestique"),
    "neo": dict(criticalPowerW=370, wPrimeCapacityJ=25000, peakPowerW=1040, wPrimeRecoveryJPerSecond=40, lowIntensityDurability=0.84, highIntensityDurability=0.80, bodyMassKg=63, systemMassKg=8.0, cdAM2=0.285, baseCrr=0.004, positioning=0.72, handling=0.74, tacticalAwareness=0.72, potentialOvr=84, wageBand="neo"),
}

MONUMENTS = {
    "race.wt2026.roubaix",
    "race.wt2026.ronde",
    "race.wt2026.milano_sanremo",
    "race.wt2026.lombardia",
    "race.wt2026.lbl",
}
GRAND_TOURS = {"race.wt2026.tdf", "race.wt2026.giro", "race.wt2026.vuelta"}
TDU = "race.wt2026.tour_down_under"
OTHER_WILDCARDS = [
    "organization.wt2026.israel",
    "organization.wt2026.tudor",
    "organization.wt2026.q36",
    "organization.wt2026.totalenergies",
]
ALL_WILDCARDS = [org["id"] for org in WILDCARD_ORGS]


def unit(seed: str) -> float:
    digest = hashlib.sha256(seed.encode()).hexdigest()
    return int(digest[:8], 16) / 0xFFFFFFFF


def jitter(base: float, seed: str, span: float) -> float:
    return round(base + (unit(seed) - 0.5) * span, 3)


def make_rider(org_slug: str, slot: str, name: str, nationality: str, birth_year: int, archetype: str, extra: dict) -> dict:
    rider_id = f"rider.wt2026.{org_slug}.{slot}"
    band = dict(ARCHETYPE_BANDS[archetype])
    wage_band = extra.get("wageBand", band.pop("wageBand", "domestique"))
    rider = {
        "id": rider_id,
        "name": name,
        "organizationId": f"organization.wt2026.{org_slug}",
        "nationality": nationality,
        "birthYear": extra.get("birthYear", birth_year),
        "archetype": archetype,
        "wageBand": wage_band,
        "criticalPowerW": extra.get("criticalPowerW", jitter(band["criticalPowerW"], rider_id + ":cp", 8)),
        "wPrimeCapacityJ": extra.get("wPrimeCapacityJ", int(jitter(band["wPrimeCapacityJ"], rider_id + ":w", 1500))),
        "peakPowerW": extra.get("peakPowerW", int(jitter(band["peakPowerW"], rider_id + ":pmax", 40))),
        "wPrimeRecoveryJPerSecond": extra.get("wPrimeRecoveryJPerSecond", jitter(band["wPrimeRecoveryJPerSecond"], rider_id + ":rec", 2)),
        "lowIntensityDurability": extra.get("lowIntensityDurability", jitter(band["lowIntensityDurability"], rider_id + ":low", 0.04)),
        "highIntensityDurability": extra.get("highIntensityDurability", jitter(band["highIntensityDurability"], rider_id + ":high", 0.04)),
        "bodyMassKg": extra.get("bodyMassKg", jitter(band["bodyMassKg"], rider_id + ":kg", 3)),
        "systemMassKg": extra.get("systemMassKg", band["systemMassKg"]),
        "cdAM2": extra.get("cdAM2", jitter(band["cdAM2"], rider_id + ":cda", 0.01)),
        "baseCrr": extra.get("baseCrr", band["baseCrr"]),
        "positioning": extra.get("positioning", jitter(band["positioning"], rider_id + ":pos", 0.06)),
        "handling": extra.get("handling", jitter(band["handling"], rider_id + ":han", 0.06)),
        "tacticalAwareness": extra.get("tacticalAwareness", jitter(band["tacticalAwareness"], rider_id + ":tac", 0.06)),
        "annualWage": extra.get("annualWage", 120_000),
        "contractEndDay": extra.get("contractEndDay", 365 + int(unit(rider_id + ":end") * 365)),
        "potentialOvr": extra.get("potentialOvr", int(jitter(band["potentialOvr"], rider_id + ":ovr", 4))),
    }
    for key in ("positioning", "handling", "tacticalAwareness", "lowIntensityDurability", "highIntensityDurability"):
        rider[key] = min(0.99, max(0.55, float(rider[key])))
    if rider["peakPowerW"] < rider["criticalPowerW"]:
        rider["peakPowerW"] = int(rider["criticalPowerW"]) + 40
    return rider


def upsert_orgs(document: dict) -> None:
    by_id = {org["id"]: org for org in document["organizations"]}
    for org in WILDCARD_ORGS:
        by_id[org["id"]] = org
    document["organizations"] = list(by_id.values())


def upsert_riders(document: dict) -> None:
    by_id = {rider["id"]: rider for rider in document["riders"]}
    for slug, extras in WT_EXTRAS.items():
        for slot, name, nat, year, arch, extra in extras:
            rider = make_rider(slug, slot, name, nat, year, arch, extra)
            by_id[rider["id"]] = rider
    for slug, extras in WILDCARD_RIDERS.items():
        for slot, name, nat, year, arch, extra in extras:
            rider = make_rider(slug, slot, name, nat, year, arch, extra)
            by_id[rider["id"]] = rider
    document["riders"] = list(by_id.values())


def patch_identities(document: dict) -> None:
    for race in document["races"]:
        race_id = race["raceContentId"]
        if race_id in GRAND_TOURS:
            race["startersPerTeam"] = 8
            race["inviteOrganizationIds"] = WT_ORG_IDS + OTHER_WILDCARDS
        elif race_id in MONUMENTS:
            race["startersPerTeam"] = 7
            race["inviteOrganizationIds"] = WT_ORG_IDS + ALL_WILDCARDS
        elif race_id == TDU:
            race["startersPerTeam"] = 7
            race["inviteOrganizationIds"] = WT_ORG_IDS + [
                "organization.wt2026.israel",
                "organization.wt2026.australia",
            ]
        else:
            race["startersPerTeam"] = 7
            race["inviteOrganizationIds"] = WT_ORG_IDS + OTHER_WILDCARDS


def main() -> None:
    roster = json.loads(ROSTER_PATH.read_text())
    orgs = json.loads(ORGS_PATH.read_text())
    identities = json.loads(IDENTITIES_PATH.read_text())
    upsert_riders(roster)
    upsert_orgs(orgs)
    patch_identities(identities)
    ROSTER_PATH.write_text(json.dumps(roster, indent=2, ensure_ascii=False) + "\n")
    ORGS_PATH.write_text(json.dumps(orgs, indent=2, ensure_ascii=False) + "\n")
    IDENTITIES_PATH.write_text(json.dumps(identities, indent=2, ensure_ascii=False) + "\n")
    print(f"riders={len(roster['riders'])} orgs={len(orgs['organizations'])} races={len(identities['races'])}")


if __name__ == "__main__":
    main()
