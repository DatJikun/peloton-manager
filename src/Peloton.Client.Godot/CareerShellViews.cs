using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using Peloton.Application;
using Peloton.Domain;

namespace Peloton.Client.Godot;

public sealed partial class CareerShellScreen
{
    private void BuildDesk()
    {
        content!.AddChild(LookBanner());
        content.AddChild(BuildWorldStrip());

        HBoxContainer top = Row();
        VBoxContainer list = Panel("01  NADCHODZĄCE WYŚCIGI", BuildUpcomingList());
        list.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        list.SizeFlagsStretchRatio = 5;
        VBoxContainer raceCol = new();
        raceCol.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        raceCol.SizeFlagsStretchRatio = 7;
        raceCol.AddThemeConstantOverride("separation", 14);
        raceCol.AddChild(Panel("02  WYŚCIG", BuildUpcomingDetail()));
        if (host!.Preparation is not null)
        {
            raceCol.AddChild(Panel("PRZYGOTOWANIE", BuildPrepSeats()));
        }

        raceCol.AddChild(Panel("03  INBOX", BuildDeskInbox()));
        top.AddChild(list);
        top.AddChild(raceCol);
        content.AddChild(top);

        HBoxContainer mid = Row();
        VBoxContainer squad = Panel("SKŁAD — OCENA", BuildDeskSquad());
        squad.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        squad.SizeFlagsStretchRatio = 7;
        VBoxContainer results = Panel("OSTATNIE WYNIKI", BuildRecentResults());
        results.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        results.SizeFlagsStretchRatio = 5;
        mid.AddChild(squad);
        mid.AddChild(results);
        content.AddChild(mid);

        HBoxContainer bottom = Row();
        bottom.AddChild(Stretch(Panel("RANKING", BuildRanking()), 4));
        bottom.AddChild(Stretch(Panel("FINANSE · TYDZIEŃ", BuildWeekFinance()), 4));
        bottom.AddChild(Stretch(Panel("NOTATKI SZTABU", BuildStaffNotes()), 4));
        content.AddChild(bottom);
    }

    private VBoxContainer BuildWorldStrip()
    {
        VBoxContainer box = new();
        box.AddThemeConstantOverride("separation", 6);
        CareerDayProjection? day = host!.Day;
        box.AddChild(LookChrome.Body(
            day is null
                ? "Świat szkieletu jest poza biurkiem (przygotowanie / etap)."
                : string.Create(
                    CultureInfo.InvariantCulture,
                    $"Świat: dzień {day.DayNumber} · {day.PrimaryLabel} · wyścigów {day.RaceCount} · pracodawca {day.EmployerName}"),
            13,
            LookChrome.Black,
            bold: true));
        foreach (CalendarEntryProjection entry in host.Calendar)
        {
            box.AddChild(LookChrome.Body(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"kalendarz świata: dzień {entry.DayNumber} · {entry.Title} · {entry.Status}"),
                12,
                LookChrome.Gray));
        }

        return Panel("ŚWIAT SZKIELETU", box);
    }

    private VBoxContainer BuildUpcomingList()
    {
        VBoxContainer list = new();
        list.AddThemeConstantOverride("separation", 6);
        Button more = LookChrome.Solid("pełny kalendarz ›", () => Show(View.Calendar), LookChrome.Paper, LookChrome.Black, compact: true);
        list.AddChild(more);
        foreach (CalendarEntryProjection entry in host!.Calendar)
        {
            CalendarEntryProjection captured = entry;
            bool active = lookRaceId == captured.Title ||
                (lookRaceId == "mila-torino" && captured == host.Calendar[0]);
            Color fg = active ? LookChrome.Paper : LookChrome.Black;
            PanelContainer row = LookChrome.ClickRow(active, () =>
            {
                lookRaceId = captured.Title;
                RebuildContent();
            });
            HBoxContainer inner = new();
            inner.AddThemeConstantOverride("separation", 10);
            Label date = LookChrome.Body(
                string.Create(CultureInfo.InvariantCulture, $"dzień {captured.DayNumber}"),
                12,
                fg,
                bold: true);
            date.CustomMinimumSize = new Vector2(84, 0);
            VBoxContainer names = new();
            names.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            names.AddChild(LookChrome.Body(captured.Title, 14, fg, bold: true));
            names.AddChild(LookChrome.Body(
                string.Create(CultureInfo.InvariantCulture, $"{captured.Kind} · {captured.Status}"),
                11,
                active ? LookChrome.Hair : LookChrome.Gray));
            inner.AddChild(date);
            inner.AddChild(names);
            row.AddChild(inner);
            list.AddChild(row);
        }

        return list;
    }

    private VBoxContainer BuildUpcomingDetail()
    {
        VBoxContainer box = new();
        box.AddThemeConstantOverride("separation", 8);
        CalendarEntryProjection? entry = host!.Calendar.FirstOrDefault(item => item.Title == lookRaceId)
            ?? host.Calendar.FirstOrDefault();
        if (entry is null)
        {
            box.AddChild(LookChrome.Body("Kalendarz świata jest pusty.", 13, LookChrome.Gray));
            return box;
        }

        HBoxContainer head = new();
        head.AddThemeConstantOverride("separation", 10);
        Label name = LookChrome.Display(entry.Title.ToUpperInvariant(), 26, LookChrome.Black);
        name.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        head.AddChild(name);
        head.AddChild(LookChrome.Chip(entry.Kind, "inv"));
        box.AddChild(head);
        box.AddChild(LookChrome.Body(
            string.Create(CultureInfo.InvariantCulture, $"dzień {entry.DayNumber} · {entry.Status}"),
            13,
            LookChrome.Gray,
            bold: true));
        if (!string.IsNullOrWhiteSpace(entry.OfficialResult))
        {
            box.AddChild(LookChrome.Kv("Wynik", entry.OfficialResult));
        }

        RacePreparationProjection? prep = host.Preparation;
        if (prep is not null)
        {
            box.AddChild(LookChrome.Kv("Cel", prep.Objective));
            box.AddChild(LookChrome.Body("Czwórka z Beskid–Vetter. Wybierz Leader i Card.", 12, LookChrome.Gray));
        }

        return box;
    }

    private VBoxContainer BuildPrepSeats()
    {
        VBoxContainer box = new();
        box.AddThemeConstantOverride("separation", 8);
        RacePreparationProjection? prep = host!.Preparation;
        if (prep is null)
        {
            return box;
        }

        box.AddChild(LookChrome.Body(
            string.Create(CultureInfo.InvariantCulture, $"{prep.Title} · {prep.Objective}"),
            13,
            LookChrome.Black,
            bold: true));
        foreach (SquadSeat seat in prep.Seats)
        {
            SquadSeat captured = seat;
            box.AddChild(LookChrome.Solid(
                $"{captured.Name} · {captured.Role} — {captured.Why}",
                () =>
                {
                    string next = captured.Role switch
                    {
                        SquadRoles.Worker => SquadRoles.Card,
                        SquadRoles.Card => SquadRoles.Leader,
                        _ => SquadRoles.Worker,
                    };
                    Apply(host.AssignRole(captured.RiderId, next));
                },
                LookChrome.Paper,
                LookChrome.Black,
                compact: true));
        }

        return box;
    }

    private VBoxContainer BuildDeskInbox()
    {
        VBoxContainer box = new();
        box.AddThemeConstantOverride("separation", 8);
        foreach (InboxItemProjection item in host!.Inbox)
        {
            InboxItemProjection captured = item;
            VBoxContainer mail = new();
            mail.AddThemeConstantOverride("separation", 4);
            mail.AddChild(LookChrome.Chip("świat · " + captured.Category, "inv"));
            mail.AddChild(LookChrome.Body(captured.Body, 14, LookChrome.Black, bold: true));
            if (captured.Category == "race-due")
            {
                mail.AddChild(LookChrome.Body("Skrzynka nie otwiera wyścigu. Użyj Race next.", 12, LookChrome.Gray));
            }
            else
            {
                mail.AddChild(LookChrome.Solid("Archiwizuj", () =>
                {
                    CommandResult result = host.ArchiveInbox(captured.Identity);
                    ShowToast(result.Succeeded ? "Zarchiwizowano." : Reason(result.ReasonCode));
                    Refresh();
                }, LookChrome.Paper, LookChrome.Black, compact: true));
            }

            box.AddChild(WrapCard(mail));
        }

        foreach (LookMail mail in CareerLookCatalog.DeskMail)
        {
            VBoxContainer card = new();
            card.AddThemeConstantOverride("separation", 4);
            HBoxContainer head = new();
            head.AddThemeConstantOverride("separation", 8);
            head.AddChild(LookChrome.Chip(mail.Index, mail.Urgent ? "red" : string.Empty));
            Label subj = LookChrome.Body(mail.Subject, 14, LookChrome.Black, bold: true);
            subj.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            head.AddChild(subj);
            head.AddChild(LookChrome.Body(mail.When, 11, LookChrome.Gray, bold: true));
            card.AddChild(head);
            if (!string.IsNullOrEmpty(mail.From))
            {
                card.AddChild(LookChrome.Body(mail.From, 12, LookChrome.Team, bold: true));
            }

            if (!string.IsNullOrEmpty(mail.Body))
            {
                Label body = LookChrome.Body(mail.Body, 13, LookChrome.Black);
                body.AutowrapMode = TextServer.AutowrapMode.WordSmart;
                card.AddChild(body);
            }

            box.AddChild(WrapCard(card));
        }

        return box;
    }

    private VBoxContainer BuildDeskSquad()
    {
        VBoxContainer box = new();
        box.AddThemeConstantOverride("separation", 4);
        HBoxContainer heads = new();
        heads.AddThemeConstantOverride("separation", 6);
        foreach ((string Label, string Key) col in new[] { ("Zawodnik", "name"), ("Rola", "role"), ("Ocena", "rate"), ("Trend", "trend"), ("Status", "statusK") })
        {
            string key = col.Key;
            heads.AddChild(SortHead(col.Label, key, deskSquadSort, () =>
            {
                int fresh = key is "name" or "role" ? 1 : -1;
                deskSquadSort = CareerLookCatalog.Toggle(deskSquadSort, key, fresh);
                RebuildContent();
            }));
        }

        box.AddChild(heads);
        foreach (LookDeskRider rider in CareerLookCatalog.SortedDeskSquad(deskSquadSort))
        {
            HBoxContainer row = new();
            row.AddThemeConstantOverride("separation", 8);
            row.AddChild(Cell(rider.Name, true));
            row.AddChild(Cell(rider.Role, false));
            row.AddChild(Cell(CareerLookCatalog.Stars(rider.Rate) + "  " + rider.Rate + "/" + rider.Pot, true));
            row.AddChild(Cell(CareerLookCatalog.Trend(rider.Trend), false));
            row.AddChild(LookChrome.Chip(rider.Status, rider.Healthy ? "ok" : "warn"));
            box.AddChild(row);
        }

        box.AddChild(LookChrome.Solid("pełny skład ›", () => Show(View.Squad), LookChrome.Paper, LookChrome.Black, compact: true));
        return box;
    }

    private VBoxContainer BuildRecentResults()
    {
        VBoxContainer box = new();
        box.AddThemeConstantOverride("separation", 8);
        if (host!.Result is RaceResultProjection result)
        {
            box.AddChild(LookChrome.Body(
                string.Create(CultureInfo.InvariantCulture, $"{result.Title} · wygrał {result.WinnerLabel}"),
                13,
                LookChrome.Black,
                bold: true));
            foreach (string headline in result.Headlines)
            {
                Label line = LookChrome.Body(headline, 12, LookChrome.Gray);
                line.AutowrapMode = TextServer.AutowrapMode.WordSmart;
                box.AddChild(line);
            }

            HBoxContainer filters = new();
            filters.AddThemeConstantOverride("separation", 6);
            filters.AddChild(FilterChip("Wszyscy", null, host.ResultTeamFilter is null));
            foreach (RaceResultTeam team in result.Teams)
            {
                RaceResultTeam captured = team;
                filters.AddChild(FilterChip(
                    captured.Name,
                    captured.Id,
                    host.ResultTeamFilter == captured.Id));
            }

            box.AddChild(filters);
            foreach (RaceResultPlacement row in host.VisibleResultTable)
            {
                HBoxContainer line = new();
                line.AddThemeConstantOverride("separation", 10);
                Label pos = LookChrome.Display(
                    row.Place.ToString(CultureInfo.InvariantCulture),
                    22,
                    row.Place == 1 ? LookChrome.Team : LookChrome.Black);
                pos.CustomMinimumSize = new Vector2(48, 0);
                VBoxContainer meta = new();
                meta.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                meta.AddChild(LookChrome.Body(row.Label, 14, LookChrome.Black, bold: true));
                meta.AddChild(LookChrome.Body(row.TeamName, 11, LookChrome.Gray));
                line.AddChild(pos);
                line.AddChild(meta);
                box.AddChild(line);
            }

            return box;
        }

        if (host.Debrief is RaceDebriefProjection debrief)
        {
            box.AddChild(LookChrome.Body(debrief.Objective, 13, LookChrome.Black, bold: true));
            foreach (string note in debrief.Notes)
            {
                Label line = LookChrome.Body(note, 12, LookChrome.Gray);
                line.AutowrapMode = TextServer.AutowrapMode.WordSmart;
                box.AddChild(line);
            }

            return box;
        }

        box.AddChild(LookChrome.Body(
            "Po wyścigu tu będzie tabela miejsc. Możesz filtrować po zespole.",
            13,
            LookChrome.Gray));
        return box;
    }

    private Button FilterChip(string caption, WorldEntityId? teamId, bool selected)
    {
        Button button = LookChrome.Solid(
            caption,
            () =>
            {
                host?.SetResultTeamFilter(teamId);
                Refresh();
            },
            selected ? LookChrome.Team : LookChrome.Paper,
            selected ? LookChrome.TeamOn : LookChrome.Black,
            compact: true);
        button.Disabled = selected;
        return button;
    }

    private static VBoxContainer BuildRanking()
    {
        VBoxContainer box = new();
        box.AddThemeConstantOverride("separation", 4);
        foreach (LookRankRow row in CareerLookCatalog.Ranking)
        {
            HBoxContainer line = new();
            Color fg = row.Mine ? LookChrome.Paper : LookChrome.Black;
            PanelContainer wrap = LookChrome.ClickRow(row.Mine, () => { });
            wrap.MouseFilter = MouseFilterEnum.Ignore;
            Label pos = LookChrome.Body(row.Place.ToString(CultureInfo.InvariantCulture), 13, fg, bold: true);
            pos.CustomMinimumSize = new Vector2(24, 0);
            Label team = LookChrome.Body(row.Team, 13, fg, bold: row.Mine);
            team.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            Label pts = LookChrome.Body(row.Points.ToString(CultureInfo.InvariantCulture), 13, fg, bold: true);
            line.AddChild(pos);
            line.AddChild(team);
            line.AddChild(pts);
            wrap.AddChild(line);
            box.AddChild(wrap);
        }

        box.AddChild(LookChrome.Body("UCI Europe Tour · ranking zespołów · po 08.03", 11, LookChrome.Gray));
        return box;
    }

    private VBoxContainer BuildWeekFinance()
    {
        VBoxContainer box = new();
        box.AddThemeConstantOverride("separation", 6);
        foreach (LookFinanceRow row in CareerLookCatalog.WeekFinance)
        {
            box.AddChild(LookChrome.Kv(row.Label, CareerLookCatalog.SignedZloty(row.Amount)));
        }

        box.AddChild(LookChrome.Hairline());
        box.AddChild(LookChrome.Kv("Bilans tygodnia", CareerLookCatalog.SignedZloty(CareerLookCatalog.WeekBalance)));
        box.AddChild(LookChrome.Kv("Budżet sezonu", CareerLookCatalog.SignedZloty(CareerLookCatalog.SeasonBudget)));
        box.AddChild(LookChrome.Solid("księga ›", () => Show(View.Finance), LookChrome.Paper, LookChrome.Black, compact: true));
        return box;
    }

    private static VBoxContainer BuildStaffNotes()
    {
        VBoxContainer box = new();
        box.AddThemeConstantOverride("separation", 8);
        foreach (LookNote note in CareerLookCatalog.StaffNotes)
        {
            VBoxContainer card = new();
            card.AddThemeConstantOverride("separation", 2);
            card.AddChild(LookChrome.Body(note.Who, 11, note.Urgent ? LookChrome.Red : LookChrome.Team, bold: true));
            Label text = LookChrome.Body(note.Text, 13, LookChrome.Black);
            text.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            card.AddChild(text);
            box.AddChild(card);
        }

        return box;
    }

    private void BuildSquad()
    {
        content!.AddChild(LookBanner());
        HBoxContainer grid = Row();
        VBoxContainer table = Panel("KADRA", BuildSquadTable());
        table.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        table.SizeFlagsStretchRatio = 7;
        VBoxContainer card = Panel("KARTA ZAWODNIKA", BuildRiderCard());
        card.CustomMinimumSize = new Vector2(340, 0);
        card.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        card.SizeFlagsStretchRatio = 5;
        grid.AddChild(table);
        grid.AddChild(card);
        content.AddChild(grid);

        if (host!.People.Count > 0)
        {
            VBoxContainer world = new();
            world.AddThemeConstantOverride("separation", 4);
            world.AddChild(LookChrome.Body("Ludzie ze szkieletu świata (bez OVR):", 12, LookChrome.Gray, bold: true));
            foreach (PersonNameProjection person in host.People)
            {
                world.AddChild(LookChrome.Body(person.Name, 13, LookChrome.Black, bold: true));
            }

            content.AddChild(Panel("SKŁAD ŚWIATA", world));
        }
    }

    private VBoxContainer BuildSquadTable()
    {
        VBoxContainer box = new();
        box.AddThemeConstantOverride("separation", 4);
        HBoxContainer heads = new();
        foreach ((string Label, string Key) col in new[]
                 {
                     ("Imię", "first"), ("Nazwisko", "last"), ("Profil", "role"), ("Wiek", "age"),
                     ("OVR", "rate"), ("POT", "pot"), ("Forma", "form"), ("Zmęcz.", "fatigue"), ("Kontrakt", "contractEnd"),
                 })
        {
            string key = col.Key;
            heads.AddChild(SortHead(col.Label, key, squadSort, () =>
            {
                squadSort = CareerLookCatalog.Toggle(squadSort, key);
                RebuildContent();
            }));
        }

        heads.AddChild(LookChrome.Body("Status", 11, LookChrome.Gray, bold: true));
        box.AddChild(heads);
        foreach (LookRider rider in CareerLookCatalog.SortedRiders(squadSort))
        {
            LookRider captured = rider;
            bool selected = captured.Id == selectedRiderId;
            Color fg = selected ? LookChrome.Paper : LookChrome.Black;
            PanelContainer row = LookChrome.ClickRow(selected, () =>
            {
                selectedRiderId = captured.Id;
                negotiating = false;
                RebuildContent();
            });
            HBoxContainer inner = new();
            inner.AddChild(Cell(captured.First, true, fg));
            inner.AddChild(Cell(captured.Last + "\n" + captured.Nat, true, fg));
            inner.AddChild(Cell(captured.Role, false, fg));
            inner.AddChild(Cell(captured.Age.ToString(CultureInfo.InvariantCulture), false, fg));
            inner.AddChild(Cell(captured.Rate.ToString(CultureInfo.InvariantCulture), true, fg));
            inner.AddChild(Cell(captured.Pot.ToString(CultureInfo.InvariantCulture), false, fg));
            inner.AddChild(Cell(captured.Form.ToString(CultureInfo.InvariantCulture), false, fg));
            inner.AddChild(Cell(captured.Fatigue + "%", false, fg));
            inner.AddChild(Cell(captured.ContractEnd[^4..], false, fg));
            inner.AddChild(LookChrome.Chip(captured.Status == "Zdrowy" ? "zdrowy" : "uraz", captured.Status == "Zdrowy" ? "ok" : "warn"));
            row.AddChild(inner);
            box.AddChild(row);
        }

        return box;
    }

    private VBoxContainer BuildRiderCard()
    {
        LookRider? rider = CareerLookCatalog.Rider(selectedRiderId) ?? CareerLookCatalog.Riders[0];
        VBoxContainer box = new();
        box.AddThemeConstantOverride("separation", 8);
        box.AddChild(ProfileHead(rider.FullName, $"{rider.Nat} · {rider.Age} lat · {rider.Role}"));
        box.AddChild(LookChrome.Kv("OVR / POT", rider.Rate + " / " + rider.Pot));
        box.AddChild(LookChrome.Kv("Forma / morale", rider.Form + " / " + rider.Morale));
        box.AddChild(LookChrome.Kv("Wartość", CareerLookCatalog.Zloty(rider.Value)));
        foreach ((string Label, int Value) stat in new[]
                 {
                     ("Góry", rider.Mountain), ("Pagórki", rider.Hill), ("Sprint", rider.Sprint), ("TT", rider.Tt),
                     ("Bruk", rider.Cobbles), ("Wytrzymałość", rider.Stamina), ("Regeneracja", rider.Recovery), ("Forma", rider.Form),
                 })
        {
            box.AddChild(LookChrome.Stat(stat.Label, stat.Value));
        }

        box.AddChild(LookChrome.Display("KONTRAKT", 12, LookChrome.Team));
        box.AddChild(LookChrome.Kv("Ważny do", rider.ContractEnd));
        box.AddChild(LookChrome.Kv("Pensja", CareerLookCatalog.Zloty(rider.Salary) + " / mies."));
        box.AddChild(LookChrome.Kv("Premia", rider.Bonus));
        box.AddChild(LookChrome.Kv("Agent", rider.Agent));
        if (negotiating)
        {
            box.AddChild(LookChrome.Body("Oferta kontraktowa (rysunek)", 12, LookChrome.Gray, bold: true));
            int offer = (int)Math.Round(rider.Salary * 1.12);
            box.AddChild(LookChrome.Kv("Propozycja pensji", CareerLookCatalog.Zloty(offer) + " / mies."));
            box.AddChild(LookChrome.Solid("Złóż ofertę", () => ShowToast(CareerLookCatalog.NotInWorld), LookChrome.Team, LookChrome.TeamOn, compact: true));
        }

        HBoxContainer actions = new();
        actions.AddThemeConstantOverride("separation", 8);
        actions.AddChild(LookChrome.Solid(negotiating ? "Zamknij negocjacje" : "Negocjuj kontrakt", () =>
        {
            negotiating = !negotiating;
            RebuildContent();
        }, LookChrome.Team, LookChrome.TeamOn, compact: true));
        actions.AddChild(LookChrome.Solid("Zwolnij z zespołu", () => ShowToast(CareerLookCatalog.NotInWorld), LookChrome.Red, LookChrome.Paper, compact: true));
        box.AddChild(actions);
        return box;
    }

    private void BuildStaff()
    {
        content!.AddChild(LookBanner());
        HBoxContainer grid = Row();
        VBoxContainer list = new();
        list.AddThemeConstantOverride("separation", 8);
        foreach (LookStaff person in CareerLookCatalog.Staff)
        {
            LookStaff captured = person;
            bool selected = captured.Id == staffSelected;
            Color fg = selected ? LookChrome.Paper : LookChrome.Black;
            PanelContainer row = LookChrome.ClickRow(selected, () =>
            {
                staffSelected = captured.Id;
                RebuildContent();
            });
            HBoxContainer inner = new();
            inner.AddThemeConstantOverride("separation", 10);
            inner.AddChild(LookChrome.Avatar(captured.Name, mini: true));
            VBoxContainer names = new();
            names.AddChild(LookChrome.Body(captured.Name, 14, fg, bold: true));
            names.AddChild(LookChrome.Body(captured.Job + " · " + captured.Rating + "/100", 12, selected ? LookChrome.Hair : LookChrome.Gray));
            inner.AddChild(names);
            row.AddChild(inner);
            list.AddChild(row);
        }

        grid.AddChild(Stretch(Panel("PRACOWNICY", list), 7));
        VBoxContainer staffCard = Stretch(Panel("PROFIL PRACOWNIKA", BuildStaffCard()), 5);
        staffCard.CustomMinimumSize = new Vector2(340, 0);
        grid.AddChild(staffCard);
        content.AddChild(grid);
    }

    private VBoxContainer BuildStaffCard()
    {
        LookStaff person = CareerLookCatalog.StaffMember(staffSelected) ?? CareerLookCatalog.Staff[0];
        VBoxContainer box = new();
        box.AddThemeConstantOverride("separation", 8);
        box.AddChild(ProfileHead(person.Name, person.Job + " · " + person.Nat));
        box.AddChild(LookChrome.Kv("Specjalizacja", person.Spec));
        box.AddChild(LookChrome.Kv("Ocena", person.Rating + "/100"));
        foreach (LookSkill skill in person.Skills)
        {
            box.AddChild(LookChrome.Stat(skill.Name, skill.Value));
        }

        box.AddChild(LookChrome.Kv("Ważny do", person.Contract));
        box.AddChild(LookChrome.Kv("Koszt", person.Cost));
        HBoxContainer actions = new();
        actions.AddThemeConstantOverride("separation", 8);
        actions.AddChild(LookChrome.Solid("Negocjuj kontrakt", () => ShowToast(CareerLookCatalog.NotInWorld), LookChrome.Team, LookChrome.TeamOn, compact: true));
        actions.AddChild(LookChrome.Solid("Zwolnij", () => ShowToast(CareerLookCatalog.NotInWorld), LookChrome.Red, LookChrome.Paper, compact: true));
        box.AddChild(actions);
        return box;
    }

    private void BuildCalendar()
    {
        content!.AddChild(LookBanner());
        LookCalendarMonth month = CareerLookCatalog.Months[monthIndex];
        IReadOnlyList<LookCalendarRace> monthRaces = CareerLookCatalog.CalendarRaces
            .Where(race => race.Year == month.Year && race.Month == month.Month)
            .ToArray();
        if (monthRaces.All(race => race.Id != lookCalRaceId))
        {
            lookCalRaceId = monthRaces.Count > 0 ? monthRaces[0].Id : lookCalRaceId;
        }

        HBoxContainer grid = Row();
        grid.AddChild(Stretch(Panel("KALENDARZ WYŚCIGÓW", BuildMonthGrid(month)), 8));
        grid.AddChild(Stretch(Panel("WYŚCIG", BuildCalendarRaceDetail()), 4));
        content.AddChild(grid);
        content.AddChild(BuildWorldStrip());
    }

    private VBoxContainer BuildMonthGrid(LookCalendarMonth month)
    {
        VBoxContainer box = new();
        box.AddThemeConstantOverride("separation", 8);
        HBoxContainer nav = new();
        nav.AddThemeConstantOverride("separation", 12);
        Button prev = LookChrome.Solid("‹", () =>
        {
            if (monthIndex > 0)
            {
                monthIndex--;
                RebuildContent();
            }
        }, LookChrome.Paper, LookChrome.Black, compact: true);
        prev.Disabled = monthIndex == 0;
        Button next = LookChrome.Solid("›", () =>
        {
            if (monthIndex < CareerLookCatalog.Months.Count - 1)
            {
                monthIndex++;
                RebuildContent();
            }
        }, LookChrome.Paper, LookChrome.Black, compact: true);
        next.Disabled = monthIndex == CareerLookCatalog.Months.Count - 1;
        Label title = LookChrome.Display(month.Label.ToUpperInvariant(), 22, LookChrome.Black);
        title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        title.HorizontalAlignment = HorizontalAlignment.Center;
        nav.AddChild(prev);
        nav.AddChild(title);
        nav.AddChild(next);
        box.AddChild(nav);

        HBoxContainer head = LookEqualCell.Strip();
        foreach (string dow in new[] { "Pon", "Wt", "Śr", "Czw", "Pt", "Sob", "Nie" })
        {
            LookEqualCell slot = new(LookEqualCell.HeadHeight);
            Label label = LookChrome.Body(dow, 11, LookChrome.Gray, bold: true);
            label.HorizontalAlignment = HorizontalAlignment.Center;
            label.VerticalAlignment = VerticalAlignment.Center;
            label.ClipText = true;
            slot.AddChild(label);
            head.AddChild(slot);
        }

        box.AddChild(head);
        IReadOnlyList<LookCalendarCell> cells = CareerLookCatalog.Cells(month);
        VBoxContainer weeks = new();
        weeks.AddThemeConstantOverride("separation", 4);
        weeks.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        for (int week = 0; week < 6; week++)
        {
            HBoxContainer row = LookEqualCell.Strip();
            for (int col = 0; col < LookEqualCell.CalendarColumns; col++)
            {
                row.AddChild(BuildDayCell(cells[(week * LookEqualCell.CalendarColumns) + col]));
            }

            weeks.AddChild(row);
        }

        box.AddChild(weeks);
        return box;
    }

    private LookEqualCell BuildDayCell(LookCalendarCell cell)
    {
        LookEqualCell slot = new(LookEqualCell.DayHeight);
        bool selected = cell.Race is not null && cell.Race.Id == lookCalRaceId;
        PanelContainer panel = new();
        panel.AddThemeStyleboxOverride(
            "panel",
            LookChrome.ChipBox(cell.IsToday ? LookChrome.Paper : selected ? LookChrome.Black : LookChrome.White));
        if (cell.OutsideMonth)
        {
            panel.Modulate = new Color(1, 1, 1, 0.45f);
        }

        if (cell.Race is LookCalendarRace race)
        {
            LookCalendarRace captured = race;
            panel.MouseDefaultCursorShape = CursorShape.PointingHand;
            panel.GuiInput += e =>
            {
                if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
                {
                    lookCalRaceId = captured.Id;
                    RebuildContent();
                    panel.AcceptEvent();
                }
            };
        }

        VBoxContainer inner = new();
        inner.AddThemeConstantOverride("separation", 2);
        Color numColor = selected ? LookChrome.Paper : LookChrome.Gray;
        Label number = LookChrome.Body(cell.Day.ToString(CultureInfo.InvariantCulture), 11, numColor, bold: true);
        number.ClipText = true;
        inner.AddChild(number);
        if (cell.Race is LookCalendarRace eventRace)
        {
            Color eventFg = selected ? LookChrome.Black : LookChrome.Paper;
            Label name = LookChrome.Body(eventRace.Name, 10, eventFg, bold: true);
            name.AutowrapMode = TextServer.AutowrapMode.Arbitrary;
            name.ClipText = true;
            name.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            name.SizeFlagsVertical = SizeFlags.ExpandFill;
            name.MouseFilter = MouseFilterEnum.Ignore;
            Label cat = LookChrome.Body(eventRace.Category, 10, eventFg);
            cat.ClipText = true;
            cat.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            cat.MouseFilter = MouseFilterEnum.Ignore;
            ColorRect chip = LookChrome.Block(eventRace.Category == "Monument" ? LookChrome.Red : LookChrome.Team);
            if (selected)
            {
                chip.Color = LookChrome.Paper;
            }

            chip.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            chip.SizeFlagsVertical = SizeFlags.ExpandFill;
            chip.MouseFilter = MouseFilterEnum.Ignore;
            VBoxContainer ev = new();
            ev.SetAnchorsPreset(LayoutPreset.FullRect);
            ev.OffsetLeft = 4;
            ev.OffsetTop = 3;
            ev.OffsetRight = -4;
            ev.OffsetBottom = -3;
            ev.MouseFilter = MouseFilterEnum.Ignore;
            ev.AddChild(name);
            ev.AddChild(cat);
            chip.AddChild(ev);
            inner.AddChild(chip);
        }
        else
        {
            Control spacer = new();
            spacer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            spacer.SizeFlagsVertical = SizeFlags.ExpandFill;
            spacer.MouseFilter = MouseFilterEnum.Ignore;
            inner.AddChild(spacer);
        }

        MarginContainer pad = new();
        pad.AddThemeConstantOverride("margin_left", 6);
        pad.AddThemeConstantOverride("margin_top", 4);
        pad.AddThemeConstantOverride("margin_right", 6);
        pad.AddThemeConstantOverride("margin_bottom", 4);
        pad.AddChild(inner);
        panel.AddChild(pad);
        slot.AddChild(panel);
        return slot;
    }

    private VBoxContainer BuildCalendarRaceDetail()
    {
        LookCalendarRace? race = CareerLookCatalog.CalendarRace(lookCalRaceId);
        VBoxContainer box = new();
        box.AddThemeConstantOverride("separation", 8);
        if (race is null)
        {
            box.AddChild(LookChrome.Body("Brak wyścigu w tym miesiącu.", 13, LookChrome.Gray));
            return box;
        }

        HBoxContainer head = new();
        Label title = LookChrome.Display(race.Name.ToUpperInvariant(), 22, LookChrome.Black);
        title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        title.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        head.AddChild(title);
        head.AddChild(LookChrome.Chip(race.Category, race.Category == "Monument" ? "inv" : "ok"));
        box.AddChild(head);
        box.AddChild(LookChrome.Body(
            string.Create(CultureInfo.InvariantCulture, $"{race.Day:00}.{race.Month:00}.{race.Year} · {race.Category} · {race.DistanceKm} km"),
            12,
            LookChrome.Gray,
            bold: true));
        LookRaceMap map = new();
        map.SetRace(race);
        box.AddChild(map);
        box.AddChild(LookChrome.Kv("Trasa", race.Route));
        Label desc = LookChrome.Body(race.Desc, 13, LookChrome.Black);
        desc.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        box.AddChild(desc);
        box.AddChild(LookChrome.Body("Najlepiej pasujący zawodnicy", 11, LookChrome.Gray, bold: true));
        foreach (int id in race.FitIds)
        {
            LookRider? rider = CareerLookCatalog.Rider(id);
            if (rider is null)
            {
                continue;
            }

            box.AddChild(LookChrome.Kv(rider.FullName + " · " + rider.Role, CareerLookCatalog.FitScore(rider).ToString(CultureInfo.InvariantCulture)));
        }

        box.AddChild(LookChrome.Solid("Ustaw skład", () => ShowToast(CareerLookCatalog.NotInWorld), LookChrome.Team, LookChrome.TeamOn, compact: true));
        return box;
    }

    private void BuildSponsors()
    {
        content!.AddChild(LookBanner());
        HBoxContainer grid = Row();
        VBoxContainer list = new();
        list.AddThemeConstantOverride("separation", 8);
        foreach (LookSponsor sponsor in CareerLookCatalog.Sponsors)
        {
            LookSponsor captured = sponsor;
            bool selected = captured.Id == selectedSponsorId;
            Color fg = selected ? LookChrome.Paper : LookChrome.Black;
            PanelContainer row = LookChrome.ClickRow(selected, () =>
            {
                selectedSponsorId = captured.Id;
                RebuildContent();
            });
            VBoxContainer inner = new();
            inner.AddChild(LookChrome.Body(captured.Name, 15, fg, bold: true));
            inner.AddChild(LookChrome.Body(captured.Tier + " · " + captured.Value, 12, selected ? LookChrome.Hair : LookChrome.Gray));
            row.AddChild(inner);
            list.AddChild(row);
        }

        LookSponsor current = CareerLookCatalog.Sponsor(selectedSponsorId) ?? CareerLookCatalog.Sponsors[0];
        VBoxContainer detail = new();
        detail.AddThemeConstantOverride("separation", 8);
        detail.AddChild(LookChrome.Display(current.Name.ToUpperInvariant(), 22, LookChrome.Black));
        detail.AddChild(LookChrome.Body(current.Tier, 13, LookChrome.Gray, bold: true));
        detail.AddChild(LookChrome.Kv("Wartość", current.Value));
        detail.AddChild(LookChrome.Kv("Do", current.Until));
        detail.AddChild(LookChrome.Kv("Relacja", current.Mood + "/100"));
        detail.AddChild(LookChrome.Kv("Status", "aktywny"));
        detail.AddChild(LookChrome.Solid("Rozmawiaj o przedłużeniu", () => ShowToast(CareerLookCatalog.NotInWorld), LookChrome.Team, LookChrome.TeamOn, compact: true));

        VBoxContainer goals = new();
        goals.AddThemeConstantOverride("separation", 8);
        for (int i = 0; i < current.Goals.Length; i++)
        {
            HBoxContainer row = new();
            Label text = LookChrome.Body(current.Goals[i], 13, LookChrome.Black, bold: true);
            text.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            text.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            row.AddChild(text);
            row.AddChild(LookChrome.Chip(i == 0 ? "w toku" : "cel", i == 0 ? "ok" : string.Empty));
            goals.AddChild(row);
        }

        grid.AddChild(Stretch(Panel("PARTNERZY", list), 4));
        grid.AddChild(Stretch(Panel("UMOWA", detail), 4));
        grid.AddChild(Stretch(Panel("CELE", goals), 4));
        content.AddChild(grid);
    }

    private void BuildFinance()
    {
        content!.AddChild(LookBanner());
        HBoxContainer top = Row();
        VBoxContainer budget = new();
        budget.AddThemeConstantOverride("separation", 8);
        budget.AddChild(LookChrome.Display(CareerLookCatalog.SignedZloty(CareerLookCatalog.SeasonBudget), 32, LookChrome.Black));
        budget.AddChild(LookChrome.Body("wolne środki · 2026", 12, LookChrome.Gray, bold: true));
        budget.AddChild(LookChrome.Kv("Prognoza 31.12", "+188 000 zł"));
        budget.AddChild(LookChrome.Kv("Przychody", "7,20 mln zł"));
        budget.AddChild(LookChrome.Kv("Koszty", "6,79 mln zł"));
        top.AddChild(Stretch(Panel("BUDŻET", budget), 4));

        VBoxContainer expenses = new();
        expenses.AddThemeConstantOverride("separation", 8);
        HBoxContainer bar = new();
        bar.CustomMinimumSize = new Vector2(0, 18);
        foreach (LookExpenseSlice slice in CareerLookCatalog.Expenses)
        {
            ColorRect block = LookChrome.Block(new Color(slice.ColorHex));
            block.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            block.SizeFlagsStretchRatio = slice.Percent;
            bar.AddChild(block);
        }

        expenses.AddChild(bar);
        expenses.AddChild(LookChrome.Body("6,79 mln zł · koszty sezonu", 13, LookChrome.Black, bold: true));
        foreach (LookExpenseSlice slice in CareerLookCatalog.Expenses)
        {
            expenses.AddChild(LookChrome.Kv(slice.Label + " · " + slice.Percent + "%", slice.Amount));
        }

        top.AddChild(Stretch(Panel("WYDATKI", expenses), 8));
        content.AddChild(top);

        VBoxContainer ledger = new();
        ledger.AddThemeConstantOverride("separation", 4);
        ledger.AddChild(HeaderRow("Data", "Operacja", "Kategoria", "Kwota"));
        foreach (LookLedgerRow row in CareerLookCatalog.Ledger)
        {
            ledger.AddChild(HeaderRow(row.Date, row.Operation, row.Category, CareerLookCatalog.SignedZloty(row.Amount)));
        }

        content.AddChild(Panel("KSIĘGA OPERACJI", ledger));
    }

    private void BuildScouting()
    {
        content!.AddChild(LookBanner());
        VBoxContainer form = new();
        form.AddThemeConstantOverride("separation", 8);
        OptionButton scout = new();
        foreach (LookStaff person in CareerLookCatalog.Scouts())
        {
            scout.AddItem(person.Name, person.Id);
        }

        OptionButton region = Combo(CareerLookCatalog.ScoutRegions);
        OptionButton focus = Combo(CareerLookCatalog.ScoutFoci);
        OptionButton days = new();
        foreach (int day in CareerLookCatalog.ScoutDurations)
        {
            days.AddItem(day + " dni", day);
        }

        days.Selected = 1;
        form.AddChild(Labeled("Skaut", scout));
        form.AddChild(Labeled("Region", region));
        form.AddChild(Labeled("Profil", focus));
        form.AddChild(Labeled("Długość", days));
        form.AddChild(LookChrome.Solid("Wyślij skauta", () =>
        {
            int scoutId = scout.GetSelectedId();
            string regionName = region.GetItemText(region.Selected);
            string focusName = focus.GetItemText(focus.Selected);
            int length = days.GetSelectedId();
            scoutMissions.Add(new LookScoutMission(scoutMissions.Count + 10, scoutId, regionName, focusName, length, length, "W toku"));
            ShowToast(CareerLookCatalog.NotInWorld + " Misja zostaje tylko na ekranie.");
            RebuildContent();
        }, LookChrome.Team, LookChrome.TeamOn, compact: true));

        VBoxContainer missions = new();
        missions.AddThemeConstantOverride("separation", 8);
        foreach (LookScoutMission mission in scoutMissions)
        {
            LookStaff? person = CareerLookCatalog.StaffMember(mission.ScoutId);
            int done = (int)Math.Round((1 - (mission.DaysLeft / (double)mission.Total)) * 100);
            VBoxContainer card = new();
            card.AddChild(LookChrome.Body((person?.Name ?? "?") + " · " + mission.Region, 14, LookChrome.Black, bold: true));
            card.AddChild(LookChrome.Body(
                string.Create(CultureInfo.InvariantCulture, $"{mission.Focus} · pozostało {mission.DaysLeft} dni · {done}% raportu"),
                12,
                LookChrome.Gray));
            missions.AddChild(WrapCard(card));
        }

        VBoxContainer reports = new();
        reports.AddThemeConstantOverride("separation", 8);
        foreach (LookScoutReport report in CareerLookCatalog.Reports)
        {
            LookScoutReport captured = report;
            bool selected = captured.Id == reportSelected;
            Color fg = selected ? LookChrome.Paper : LookChrome.Black;
            PanelContainer row = LookChrome.ClickRow(selected, () =>
            {
                reportSelected = captured.Id;
                RebuildContent();
            });
            VBoxContainer inner = new();
            inner.AddChild(LookChrome.Body(captured.Mission, 14, fg, bold: true));
            inner.AddChild(LookChrome.Body(captured.Date + " · " + captured.Prospects.Length + " zawodników", 12, selected ? LookChrome.Hair : LookChrome.Gray));
            row.AddChild(inner);
            reports.AddChild(row);
        }

        HBoxContainer grid = Row();
        grid.AddChild(Stretch(Panel("NOWA MISJA", form), 7));
        grid.AddChild(Stretch(Panel("AKTYWNE MISJE", missions), 5));
        content.AddChild(grid);
        HBoxContainer lower = Row();
        lower.AddChild(Stretch(Panel("ZAKOŃCZONE RAPORTY", reports), 4));
        lower.AddChild(Stretch(Panel("RAPORT", BuildReportDetail()), 8));
        content.AddChild(lower);
    }

    private VBoxContainer BuildReportDetail()
    {
        LookScoutReport? report = CareerLookCatalog.Report(reportSelected);
        VBoxContainer box = new();
        box.AddThemeConstantOverride("separation", 8);
        if (report is null)
        {
            box.AddChild(LookChrome.Body("Brak raportu.", 13, LookChrome.Gray));
            return box;
        }

        box.AddChild(LookChrome.Display(report.Mission.ToUpperInvariant(), 18, LookChrome.Black));
        box.AddChild(LookChrome.Body("ukończono " + report.Date, 12, LookChrome.Gray, bold: true));
        foreach (LookProspect prospect in report.Prospects)
        {
            VBoxContainer card = new();
            card.AddThemeConstantOverride("separation", 4);
            HBoxContainer head = new();
            Label name = LookChrome.Body(prospect.Name, 15, LookChrome.Black, bold: true);
            name.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            head.AddChild(name);
            head.AddChild(LookChrome.Chip(prospect.Pot, "inv"));
            card.AddChild(head);
            card.AddChild(LookChrome.Body($"{prospect.Nat} · {prospect.Age} lat · {prospect.Type}", 12, LookChrome.Gray));
            card.AddChild(LookChrome.Kv("Rozpoznanie", prospect.Known + "%"));
            Label note = LookChrome.Body(prospect.Note, 13, LookChrome.Black);
            note.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            card.AddChild(note);
            box.AddChild(WrapCard(card));
        }

        return box;
    }

    private void BuildMarket()
    {
        content!.AddChild(LookBanner());
        HBoxContainer grid = Row();
        VBoxContainer table = new();
        table.AddThemeConstantOverride("separation", 4);
        HBoxContainer heads = new();
        foreach ((string Label, string Key) col in new[]
                 {
                     ("Imię", "first"), ("Nazwisko", "last"), ("Wiek", "age"), ("Profil", "role"),
                     ("OVR", "rate"), ("POT", "pot"), ("Forma", "form"), ("Wartość", "value"), ("Zainteres.", "interest"),
                 })
        {
            string key = col.Key;
            heads.AddChild(SortHead(col.Label, key, marketSort, () =>
            {
                int fresh = key is "first" or "last" or "role" ? 1 : -1;
                marketSort = CareerLookCatalog.Toggle(marketSort, key, fresh);
                RebuildContent();
            }));
        }

        table.AddChild(heads);
        foreach (LookTransfer row in CareerLookCatalog.SortedTransfers(marketSort))
        {
            LookTransfer captured = row;
            bool selected = captured.Id == marketSelected;
            Color fg = selected ? LookChrome.Paper : LookChrome.Black;
            PanelContainer line = LookChrome.ClickRow(selected, () =>
            {
                marketSelected = captured.Id;
                watchingTransfer = false;
                RebuildContent();
            });
            HBoxContainer inner = new();
            inner.AddChild(Cell(captured.First, false, fg));
            inner.AddChild(Cell(captured.Last + "\n" + captured.Nat, true, fg));
            inner.AddChild(Cell(captured.Age.ToString(CultureInfo.InvariantCulture), false, fg));
            inner.AddChild(Cell(captured.Role, false, fg));
            inner.AddChild(Cell(captured.Rate.ToString(CultureInfo.InvariantCulture), true, fg));
            inner.AddChild(Cell(captured.Pot.ToString(CultureInfo.InvariantCulture), false, fg));
            inner.AddChild(Cell(captured.Form.ToString(CultureInfo.InvariantCulture), false, fg));
            inner.AddChild(Cell(CareerLookCatalog.Zloty(captured.Value), false, fg));
            inner.AddChild(Cell(captured.Interest.ToString(CultureInfo.InvariantCulture), true, fg));
            line.AddChild(inner);
            table.AddChild(line);
        }

        grid.AddChild(Stretch(Panel("DOSTĘPNI ZAWODNICY", table), 8));
        VBoxContainer marketCard = Stretch(Panel("ZAWODNIK", BuildMarketCard()), 4);
        marketCard.CustomMinimumSize = new Vector2(340, 0);
        grid.AddChild(marketCard);
        content.AddChild(grid);
    }

    private VBoxContainer BuildMarketCard()
    {
        LookTransfer row = CareerLookCatalog.Transfer(marketSelected) ?? CareerLookCatalog.Transfers[0];
        VBoxContainer box = new();
        box.AddThemeConstantOverride("separation", 8);
        box.AddChild(ProfileHead(row.FullName, $"{row.Nat} · {row.Age} lat · {row.Role}"));
        box.AddChild(LookChrome.Kv("OVR / POT", row.Rate + " / " + row.Pot));
        box.AddChild(LookChrome.Kv("Klub", row.Team));
        box.AddChild(LookChrome.Kv("Kontrakt", row.Contract));
        box.AddChild(LookChrome.Kv("Wartość", CareerLookCatalog.Zloty(row.Value)));
        box.AddChild(LookChrome.Kv("Oczekiwana pensja", CareerLookCatalog.Zloty(row.Salary) + " / mies."));
        string interestKind = row.Interest >= 75 ? "ok" : row.Interest < 55 ? "warn" : string.Empty;
        box.AddChild(LookChrome.Kv("Zainteresowanie", row.Interest + "/100"));
        box.AddChild(LookChrome.Chip(row.Interest + "/100", interestKind));
        HBoxContainer actions = new();
        actions.AddThemeConstantOverride("separation", 8);
        actions.AddChild(LookChrome.Solid("Rozpocznij negocjacje", () => ShowToast(CareerLookCatalog.NotInWorld), LookChrome.Team, LookChrome.TeamOn, compact: true));
        actions.AddChild(LookChrome.Solid(watchingTransfer ? "Obserwowany" : "Obserwuj", () =>
        {
            watchingTransfer = !watchingTransfer;
            ShowToast(watchingTransfer ? "Obserwacja tylko na tym ekranie." : CareerLookCatalog.NotInWorld);
            RebuildContent();
        }, LookChrome.Paper, LookChrome.Black, compact: true));
        box.AddChild(actions);
        return box;
    }

    private void BuildHistory()
    {
        content!.AddChild(LookBanner());
        VBoxContainer events = new();
        events.AddThemeConstantOverride("separation", 10);
        foreach (LookHistoryEvent item in CareerLookCatalog.Chronicle)
        {
            VBoxContainer card = new();
            card.AddChild(LookChrome.Body(item.Time, 11, LookChrome.Team, bold: true));
            card.AddChild(LookChrome.Body(item.Title, 15, LookChrome.Black, bold: true));
            Label body = LookChrome.Body(item.Body, 13, LookChrome.Gray);
            body.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            card.AddChild(body);
            events.AddChild(card);
        }

        VBoxContainer records = new();
        foreach (LookKv kv in CareerLookCatalog.Records)
        {
            records.AddChild(LookChrome.Kv(kv.Label, kv.Value));
        }

        VBoxContainer archive = new();
        archive.AddChild(HeaderRow("Data", "Wyścig", "Zawodnik", "Wynik", "Punkty"));
        foreach (LookArchiveRow row in CareerLookCatalog.Archive)
        {
            archive.AddChild(HeaderRow(row.Date, row.Race, row.Rider, row.Result, row.Points));
        }

        HBoxContainer top = Row();
        top.AddChild(Stretch(Panel("KRONIKA ZESPOŁU", events), 8));
        top.AddChild(Stretch(Panel("REKORDY ZESPOŁU", records), 4));
        content.AddChild(top);
        content.AddChild(Panel("ARCHIWUM WYNIKÓW", archive));

        VBoxContainer world = new();
        bool any = false;
        foreach (CalendarEntryProjection entry in host!.Calendar)
        {
            if (entry.OfficialResult is null)
            {
                continue;
            }

            any = true;
            world.AddChild(LookChrome.Body(
                string.Create(CultureInfo.InvariantCulture, $"dzień {entry.DayNumber} · {entry.Title} · {entry.OfficialResult}"),
                14,
                LookChrome.Black,
                bold: true));
        }

        if (!any)
        {
            world.AddChild(LookChrome.Body("Brak ukończonych wyścigów w tym save.", 13, LookChrome.Gray));
        }

        if (host.Day is CareerDayProjection day)
        {
            world.AddChild(LookChrome.Body("Liczba wyścigów świata: " + day.RaceCount, 12, LookChrome.Gray));
        }

        content.AddChild(Panel("KRONIKA ŚWIATA", world));
    }

    private void BuildManager()
    {
        content!.AddChild(LookBanner());
        LookManager manager = CareerLookCatalog.Manager;
        VBoxContainer profile = new();
        profile.AddThemeConstantOverride("separation", 8);
        HBoxContainer hero = new();
        hero.AddThemeConstantOverride("separation", 12);
        hero.AddChild(LookChrome.Avatar(manager.Name));
        VBoxContainer names = new();
        names.AddChild(LookChrome.Display(manager.Name.ToUpperInvariant(), 26, LookChrome.Black));
        names.AddChild(LookChrome.Body(manager.Meta, 13, LookChrome.Gray, bold: true));
        HBoxContainer stats = new();
        stats.AddThemeConstantOverride("separation", 8);
        stats.AddChild(CareerBox(manager.Reputation.ToString(CultureInfo.InvariantCulture), "Reputacja"));
        stats.AddChild(CareerBox(manager.Seasons.ToString(CultureInfo.InvariantCulture), "Sezon w klubie"));
        stats.AddChild(CareerBox(manager.Podiums.ToString(CultureInfo.InvariantCulture), "Podia kariery"));
        names.AddChild(stats);
        hero.AddChild(names);
        profile.AddChild(hero);

        VBoxContainer contract = new();
        foreach (LookKv kv in manager.Contract)
        {
            contract.AddChild(LookChrome.Kv(kv.Label, kv.Value));
        }

        VBoxContainer career = new();
        foreach (LookHistoryEvent item in manager.Career)
        {
            career.AddChild(LookChrome.Body(item.Time, 11, LookChrome.Team, bold: true));
            career.AddChild(LookChrome.Body(item.Title, 14, LookChrome.Black, bold: true));
            career.AddChild(LookChrome.Body(item.Body, 13, LookChrome.Gray));
        }

        VBoxContainer achievements = new();
        foreach (LookKv kv in manager.Achievements)
        {
            achievements.AddChild(LookChrome.Kv(kv.Label, kv.Value));
        }

        HBoxContainer grid = Row();
        grid.AddChild(Stretch(Panel("PROFIL MANAGERA", profile), 7));
        grid.AddChild(Stretch(Panel("KONTRAKT", contract), 5));
        content.AddChild(grid);
        HBoxContainer lower = Row();
        lower.AddChild(Stretch(Panel("KARIERA", career), 6));
        lower.AddChild(Stretch(Panel("OSIĄGNIĘCIA", achievements), 6));
        content.AddChild(lower);

        CareerDayProjection? day = host!.Day;
        VBoxContainer world = new();
        world.AddThemeConstantOverride("separation", 8);
        world.AddChild(LookChrome.Display((day?.ManagerName ?? "—").ToUpperInvariant(), 22, LookChrome.Black));
        world.AddChild(LookChrome.Kv("Pracodawca świata", day?.EmployerName ?? "bez klubu"));
        content.AddChild(Panel("MANAGER ŚWIATA", world));
    }

    private void BuildHelp()
    {
        content!.AddChild(LookBanner());
        HBoxContainer cards = Row();
        foreach (LookHelpCard card in CareerLookCatalog.Help)
        {
            VBoxContainer body = new();
            body.AddThemeConstantOverride("separation", 6);
            body.AddChild(LookChrome.Display(card.Title.ToUpperInvariant(), 16, LookChrome.Black));
            Label text = LookChrome.Body(card.Body, 13, LookChrome.Black);
            text.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            body.AddChild(text);
            cards.AddChild(Stretch(Panel(card.Title.ToUpperInvariant(), body), 3));
        }

        content.AddChild(cards);
        VBoxContainer real = new();
        real.AddThemeConstantOverride("separation", 8);
        real.AddChild(LookChrome.Body("Advance Day przesuwa cały świat o jeden dzień.", 14, LookChrome.Black));
        real.AddChild(LookChrome.Body("W dzień wyścigu ten sam przycisk nazywa się Race next i wchodzi w przygotowanie.", 14, LookChrome.Black));
        real.AddChild(LookChrome.Body("Oglądanie etapu blokuje biurko. Nie ma zapisu w trakcie wyścigu.", 14, LookChrome.Black));
        real.AddChild(LookChrome.Body("Skrzynka świata nie startuje wyścigu. OVR, kasa i skauci na tych ekranach są rysunkiem.", 14, LookChrome.Black));
        content.AddChild(Panel("ŚWIAT", real));
    }

    private static Label LookBanner()
    {
        Label label = LookChrome.Body(CareerLookCatalog.Banner, 12, LookChrome.Gray, bold: true);
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        return label;
    }

    private static HBoxContainer ProfileHead(string name, string meta)
    {
        HBoxContainer head = new();
        head.AddThemeConstantOverride("separation", 12);
        head.AddChild(LookChrome.Avatar(name));
        VBoxContainer names = new();
        names.AddChild(LookChrome.Display(name.ToUpperInvariant(), 22, LookChrome.Black));
        names.AddChild(LookChrome.Body(meta, 12, LookChrome.Gray, bold: true));
        head.AddChild(names);
        return head;
    }

    private static Button SortHead(string label, string key, LookSort sort, Action onPressed)
    {
        string mark = sort.Key == key ? (sort.Dir > 0 ? " ▲" : " ▼") : string.Empty;
        Button button = LookChrome.Solid(label + mark, onPressed, LookChrome.White, LookChrome.Black, compact: true);
        button.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        return button;
    }

    private static Label Cell(string text, bool bold, Color? color = null)
    {
        Label label = LookChrome.Body(text, 12, color ?? LookChrome.Black, bold);
        label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        return label;
    }

    private static HBoxContainer Row()
    {
        HBoxContainer row = new();
        row.AddThemeConstantOverride("separation", 14);
        row.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        return row;
    }

    private static VBoxContainer Stretch(VBoxContainer panel, float ratio)
    {
        panel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        panel.SizeFlagsStretchRatio = ratio;
        return panel;
    }

    private static PanelContainer WrapCard(Control inner)
    {
        PanelContainer card = LookChrome.Card();
        card.AddChild(Pad(inner));
        return card;
    }

    private static HBoxContainer HeaderRow(params string[] cells)
    {
        HBoxContainer row = new();
        row.AddThemeConstantOverride("separation", 8);
        foreach (string cell in cells)
        {
            row.AddChild(Cell(cell, true));
        }

        return row;
    }

    private static VBoxContainer Labeled(string label, Control field)
    {
        VBoxContainer box = new();
        box.AddChild(LookChrome.Body(label, 11, LookChrome.Gray, bold: true));
        box.AddChild(field);
        return box;
    }

    private static OptionButton Combo(IReadOnlyList<string> items)
    {
        OptionButton box = new();
        foreach (string item in items)
        {
            box.AddItem(item);
        }

        box.Selected = 0;
        return box;
    }

    private static VBoxContainer CareerBox(string value, string label)
    {
        VBoxContainer box = new();
        box.AddChild(LookChrome.Display(value, 22, LookChrome.Black));
        box.AddChild(LookChrome.Body(label, 11, LookChrome.Gray, bold: true));
        return box;
    }
}
