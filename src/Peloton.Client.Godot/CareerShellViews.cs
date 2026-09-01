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
    private void BuildNewGame()
    {
        VBoxContainer inner = new();
        inner.AddThemeConstantOverride("separation", 6);
        inner.AddChild(LookChrome.Body(
            "Wybierz zespół. Paczka 2026 pokazuje WorldTour; ProTeam i Continental wejdą tym samym oknem później.",
            13,
            LookChrome.Gray,
            bold: true));
        foreach (NewGameClubProjection club in host!.ListNewGameClubs(CareerShellHost.WorldTourScenarioId))
        {
            NewGameClubProjection captured = club;
            string label = string.Create(
                CultureInfo.InvariantCulture,
                $"{captured.Name} · {captured.Country} · {captured.TitleSponsor}");
            inner.AddChild(LookChrome.Solid(
                label,
                () =>
                {
                    CommandResult created = host.OpenWorldTour(captured.OriginId);
                    if (!created.Succeeded)
                    {
                        ShowToast(Reason(created.ReasonCode));
                        Refresh();
                        return;
                    }

                    CommandResult planning = host.BeginPreSeasonPlanning();
                    Apply(planning);
                },
                LookChrome.Paper,
                LookChrome.Black,
                compact: true));
        }

        content!.AddChild(Panel("NOWA GRA — WYBIERZ ZESPÓŁ", inner));
    }

    private void BuildSeasonPlan()
    {
        PreSeasonPlanningProjection? plan = host!.PreSeasonPlanning;
        if (plan is null)
        {
            content!.AddChild(Panel("PLAN SEZONU", LookChrome.Body("Brak planu sezonu.", 13, LookChrome.Gray)));
            return;
        }

        VBoxContainer list = new();
        list.AddThemeConstantOverride("separation", 8);
        IReadOnlyList<ClubRosterEntry> roster = host.ClubRoster?.Riders ?? Array.Empty<ClubRosterEntry>();
        foreach (PreSeasonRaceEntryProjection race in plan.Races)
        {
            PreSeasonRaceEntryProjection captured = race;
            VBoxContainer row = new();
            row.AddThemeConstantOverride("separation", 4);
            row.AddChild(LookChrome.Body(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"{CareerCalendarDates.FormatLong(captured.DayNumber)} · {captured.Title}"),
                14,
                LookChrome.Black,
                bold: true));
            HBoxContainer actions = new();
            actions.AddThemeConstantOverride("separation", 8);
            actions.AddChild(LookChrome.Solid(
                captured.Entered ? "Jedziemy" : "Pomijamy",
                () =>
                {
                    Apply(host.SetSeasonRaceEntry(captured.RaceContentId, !captured.Entered));
                },
                captured.Entered ? LookChrome.Team : LookChrome.Paper,
                captured.Entered ? LookChrome.TeamOn : LookChrome.Black,
                compact: true));
            OptionButton leader = new();
            leader.AddItem("— lider —", 0);
            int selectedIndex = 0;
            for (int index = 0; index < roster.Count; index++)
            {
                ClubRosterEntry rider = roster[index];
                leader.AddItem(rider.Name, (int)rider.RiderCareerId.Value);
                if (captured.DesignatedLeaderId == rider.RiderCareerId)
                {
                    selectedIndex = index + 1;
                }
            }

            leader.Selected = selectedIndex;
            leader.ItemSelected += index =>
            {
                if (index <= 0)
                {
                    return;
                }

                int riderId = leader.GetItemId((int)index);
                Apply(host.SetSeasonRaceLeader(
                    captured.RaceContentId,
                    new WorldEntityId(riderId)));
            };
            actions.AddChild(leader);
            row.AddChild(actions);
            list.AddChild(WrapCard(row));
        }

        content!.AddChild(Panel("PLAN SEZONU", list));
    }

    private void BuildDesk()
    {
        HBoxContainer top = Row();
        VBoxContainer list = Panel("NADCHODZĄCE WYŚCIGI", BuildUpcomingList());
        list.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        list.SizeFlagsStretchRatio = 5;
        VBoxContainer raceCol = new();
        raceCol.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        raceCol.SizeFlagsStretchRatio = 7;
        raceCol.AddThemeConstantOverride("separation", 14);
        raceCol.AddChild(Panel("WYŚCIG", BuildUpcomingDetail()));
        if (host!.Preparation is not null)
        {
            raceCol.AddChild(Panel("PRZYGOTOWANIE", BuildPrepSeats()));
        }

        raceCol.AddChild(Panel("INBOX", BuildDeskInbox()));
        top.AddChild(list);
        top.AddChild(raceCol);
        content!.AddChild(top);

        HBoxContainer mid = Row();
        VBoxContainer squad = Panel("SKŁAD — OCENA", BuildDeskSquad());
        squad.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        squad.SizeFlagsStretchRatio = 7;
        VBoxContainer results = Panel("OSTATNIE WYNIKI", BuildRecentResults());
        results.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        results.SizeFlagsStretchRatio = 5;
        mid.AddChild(squad);
        mid.AddChild(results);
        content!.AddChild(mid);

        HBoxContainer bottom = Row();
        bottom.AddChild(Stretch(Panel("RANKING", BuildRanking()), 4));
        bottom.AddChild(Stretch(Panel("FINANSE · TYDZIEŃ", BuildWeekFinance()), 4));
        bottom.AddChild(Stretch(Panel("NOTATKI SZTABU", BuildStaffNotes()), 4));
        content!.AddChild(bottom);
    }

    private VBoxContainer BuildUpcomingList()
    {
        VBoxContainer list = new();
        list.AddThemeConstantOverride("separation", 6);
        Button more = LookChrome.Solid("pełny kalendarz ›", () => Show(View.Calendar), LookChrome.Paper, LookChrome.Black, compact: true);
        list.AddChild(more);
        foreach (SeasonEventProjection item in host!.UpcomingEvents)
        {
            SeasonEventProjection captured = item;
            bool active = selectedEventId == captured.RaceContentId;
            Color fg = active ? LookChrome.Paper : LookChrome.Black;
            PanelContainer row = LookChrome.ClickRow(active, () =>
            {
                selectedEventId = captured.RaceContentId;
                RebuildContent();
            });
            HBoxContainer inner = new();
            inner.AddThemeConstantOverride("separation", 10);
            Label date = LookChrome.Body(
                CareerCalendarDates.FormatLong(captured.StartDay),
                12,
                fg,
                bold: true);
            date.CustomMinimumSize = new Vector2(140, 0);
            VBoxContainer names = new();
            names.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            names.AddChild(LookChrome.Body(captured.Name, 14, fg, bold: true));
            string subtitle = captured.StageCount > 1
                ? string.Create(CultureInfo.InvariantCulture, $"{captured.StageCount} etapów")
                : "jednodniowy";
            names.AddChild(LookChrome.Body(subtitle, 11, active ? LookChrome.Hair : LookChrome.Gray));
            inner.AddChild(date);
            inner.AddChild(names);
            row.AddChild(inner);
            list.AddChild(row);
        }

        if (host.UpcomingEvents.Count == 0)
        {
            list.AddChild(LookChrome.Body("Brak nadchodzących wyścigów.", 13, LookChrome.Gray));
        }

        return list;
    }

    private VBoxContainer BuildUpcomingDetail()
    {
        VBoxContainer box = new();
        box.AddThemeConstantOverride("separation", 8);
        SeasonEventProjection? item = FindSelectedEvent();
        if (item is null)
        {
            box.AddChild(LookChrome.Body("Kalendarz świata jest pusty.", 13, LookChrome.Gray));
            return box;
        }

        HBoxContainer head = new();
        head.AddThemeConstantOverride("separation", 10);
        Label name = LookChrome.Display(item.Name.ToUpperInvariant(), 26, LookChrome.Black);
        name.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        head.AddChild(name);
        head.AddChild(LookChrome.Chip(item.Status, "inv"));
        box.AddChild(head);
        box.AddChild(LookChrome.Body(
            CareerCalendarDates.FormatRange(item.StartDay, item.EndDay),
            13,
            LookChrome.Gray,
            bold: true));
        string stageLabel = item.StageCount > 1
            ? string.Create(CultureInfo.InvariantCulture, $"{item.StageCount} etapów")
            : "jednodniowy";
        box.AddChild(LookChrome.Kv("Format", stageLabel));
        box.AddChild(LookChrome.Solid(
            "otwórz wyścig ›",
            () => OpenRaceEvent(View.Desk),
            LookChrome.Team,
            LookChrome.TeamOn,
            compact: true));

        RacePreparationProjection? prep = host!.Preparation;
        if (prep is not null)
        {
            box.AddChild(LookChrome.Kv("Cel", prep.Objective));
            box.AddChild(LookChrome.Body(
                "Skład klubu. Kliknij kolarza, żeby ustawić lidera. Support idzie z domyślnej strategii.",
                12,
                LookChrome.Gray));
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
        foreach (WorldEntityId riderId in prep.Squad)
        {
            WorldEntityId captured = riderId;
            string role = captured == prep.LeaderId
                ? "Leader"
                : captured == prep.SupportId
                    ? "Support"
                    : "Skład";
            box.AddChild(LookChrome.Solid(
                $"{host.RiderDisplayName(captured)} · {role}",
                () => Apply(host.SetLeader(captured)),
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
        if (host!.Inbox.Count == 0)
        {
            box.AddChild(LookChrome.Body("Brak spraw.", 14, LookChrome.Gray, bold: true));
            return box;
        }

        foreach (InboxItemProjection item in host.Inbox)
        {
            InboxItemProjection captured = item;
            VBoxContainer mail = new();
            mail.AddThemeConstantOverride("separation", 4);
            mail.AddChild(LookChrome.Chip("świat · " + captured.Category, "inv"));
            mail.AddChild(LookChrome.Body(captured.Body, 14, LookChrome.Black, bold: true));
            if (captured.Category == "race-due")
            {
                mail.AddChild(LookChrome.Solid("Jedź wyścig", () => Apply(host.FollowPrimary()), LookChrome.Team, LookChrome.TeamOn, compact: true));
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

        return box;
    }

    private VBoxContainer BuildDeskSquad()
    {
        VBoxContainer box = new();
        box.AddThemeConstantOverride("separation", 4);
        IReadOnlyList<ClubRosterEntry>? roster = host!.ClubRoster?.Riders;
        if (roster is { Count: > 0 })
        {
            HBoxContainer heads = new();
            heads.AddThemeConstantOverride("separation", 6);
            foreach (string col in new[] { "Zawodnik", "OVR", "POT", "Góry", "Pagórki", "Płaskie", "TT", "Sprint", "Bruk" })
            {
                heads.AddChild(LookChrome.Body(col, 11, LookChrome.Gray, bold: true));
            }

            box.AddChild(heads);
            foreach (ClubRosterEntry rider in roster)
            {
                HBoxContainer row = new();
                row.AddThemeConstantOverride("separation", 8);
                row.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                row.AddChild(Cell(rider.Name, true));
                row.AddChild(Cell(rider.Ovr.ToString(CultureInfo.InvariantCulture), true, numeric: true));
                row.AddChild(Cell(rider.PotentialOvr.ToString(CultureInfo.InvariantCulture), false, numeric: true));
                row.AddChild(Cell(rider.Climb.ToString(CultureInfo.InvariantCulture), false, numeric: true));
                row.AddChild(Cell(rider.Hills.ToString(CultureInfo.InvariantCulture), false, numeric: true));
                row.AddChild(Cell(rider.Flat.ToString(CultureInfo.InvariantCulture), false, numeric: true));
                row.AddChild(Cell(rider.TimeTrial.ToString(CultureInfo.InvariantCulture), false, numeric: true));
                row.AddChild(Cell(rider.Sprint.ToString(CultureInfo.InvariantCulture), false, numeric: true));
                row.AddChild(Cell(rider.Cobbles.ToString(CultureInfo.InvariantCulture), false, numeric: true));
                box.AddChild(row);
            }
        }
        else
        {
            box.AddChild(LookChrome.Body("Brak składu ze świata.", 13, LookChrome.Gray));
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
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"{result.Title} · wygrał {host.RiderDisplayName(result.WinnerId)}"),
                13,
                LookChrome.Black,
                bold: true));
            if (host.Classifications is { IsStageRace: true } classifications)
            {
                box.AddChild(LookChrome.Body($"Żółta {JerseyLabel(classifications.GcLeader)}", 12, LookChrome.Gray));
                box.AddChild(LookChrome.Body($"Zielona {JerseyLabel(classifications.PointsLeader)}", 12, LookChrome.Gray));
                box.AddChild(LookChrome.Body($"Góry {JerseyLabel(classifications.KomLeader)}", 12, LookChrome.Gray));
                box.AddChild(LookChrome.Body($"Biała {JerseyLabel(classifications.YouthLeader)}", 12, LookChrome.Gray));
                box.AddChild(LookChrome.Body($"Drużynowa {JerseyLabel(classifications.TeamLeader)}", 12, LookChrome.Gray));
            }

            HBoxContainer filters = new();
            filters.AddThemeConstantOverride("separation", 6);
            filters.AddChild(FilterChip("Wszyscy", null, host.ResultTeamFilter is null));
            foreach (OrganizationNameProjection team in host.ResultTeams)
            {
                OrganizationNameProjection captured = team;
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
                meta.AddChild(LookChrome.Body(host.RiderDisplayName(row.RiderId), 14, LookChrome.Black, bold: true));
                meta.AddChild(LookChrome.Body(row.OrganizationName, 11, LookChrome.Gray));
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

    private static string JerseyLabel(ClassificationStanding? standing)
    {
        if (standing is null || string.IsNullOrWhiteSpace(standing.Label))
        {
            return "—";
        }

        return standing.OrganizationName.Length == 0
            ? standing.Label
            : $"{standing.Label} ({standing.OrganizationName})";
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
        ClubFinanceProjection? finance = host!.ClubFinance;
        if (finance is null)
        {
            box.AddChild(LookChrome.Body("Brak danych finansowych ze świata.", 13, LookChrome.Gray));
            box.AddChild(LookChrome.Solid("księga ›", () => Show(View.Finance), LookChrome.Paper, LookChrome.Black, compact: true));
            return box;
        }

        box.AddChild(LookChrome.Kv("Kasa", CareerLookCatalog.Euro(finance.CashEur)));
        box.AddChild(LookChrome.Kv(
            "Sponsor tytularny",
            string.Create(
                CultureInfo.InvariantCulture,
                $"{finance.TitleSponsorName} · {CareerLookCatalog.Euro(finance.TitleSponsorAnnualFeeEur)} / rok")));
        box.AddChild(LookChrome.Kv("Sponsor / dzień", CareerLookCatalog.SignedEuro(finance.DailySponsor)));
        box.AddChild(LookChrome.Kv("Płace / dzień", CareerLookCatalog.SignedEuro(-finance.DailyWages)));
        box.AddChild(LookChrome.Hairline());
        box.AddChild(LookChrome.Kv("Bilans dnia", CareerLookCatalog.SignedEuro(finance.DailyNet)));
        box.AddChild(LookChrome.Kv("Pensja składu / rok", CareerLookCatalog.Euro(finance.WageBillAnnual)));
        if (finance.Overdrawn)
        {
            box.AddChild(LookChrome.Body("Klub jest na debecie", 13, LookChrome.Red, bold: true));
        }

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
        HBoxContainer grid = Row();
        VBoxContainer table = Panel("KADRA", BuildSquadTable());
        table.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        table.SizeFlagsStretchRatio = 7;
        VBoxContainer card = Panel("KARTA ZAWODNIKA", BuildWorldRiderCard());
        card.CustomMinimumSize = new Vector2(340, 0);
        card.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        card.SizeFlagsStretchRatio = 5;
        grid.AddChild(table);
        grid.AddChild(card);
        content!.AddChild(grid);
    }

    private VBoxContainer BuildWorldRiderCard()
    {
        VBoxContainer box = new();
        box.AddThemeConstantOverride("separation", 8);
        IReadOnlyList<ClubRosterEntry>? roster = host!.ClubRoster?.Riders;
        if (roster is not { Count: > 0 })
        {
            box.AddChild(LookChrome.Body("Brak składu ze świata.", 13, LookChrome.Gray));
            return box;
        }

        if (selectedRiderId == 0 || roster.All(entry => entry.RiderCareerId.Value != selectedRiderId))
        {
            selectedRiderId = roster[0].RiderCareerId.Value;
        }

        ClubRosterEntry rider = roster.First(entry => entry.RiderCareerId.Value == selectedRiderId);
        bool isNegotiating = negotiating &&
            host.ContractNegotiation?.RiderCareerId == rider.RiderCareerId;
        int today = host.Day?.DayNumber ?? 0;
        int prefillWage = rider.AnnualWage > 0 ? rider.AnnualWage : 100_000;
        int prefillEndDay = rider.ContractEndDay > today ? rider.ContractEndDay : today + 365;
        if (host.ContractNegotiation?.OfferAnnualWage is int draftWage)
        {
            prefillWage = draftWage;
        }

        if (host.ContractNegotiation?.OfferContractEndDay is int draftEndDay)
        {
            prefillEndDay = draftEndDay;
        }

        box.AddChild(ProfileHead(rider.Name, string.Create(
            CultureInfo.InvariantCulture,
            $"OVR {rider.Ovr} · POT {rider.PotentialOvr}")));
        box.AddChild(LookChrome.Kv("Pensja / rok", CareerLookCatalog.Euro(rider.AnnualWage)));
        box.AddChild(LookChrome.Kv(
            "Koniec kontraktu",
            CareerCalendarDates.FormatLong(rider.ContractEndDay)));
        foreach ((string statLabel, int statValue) in new[]
                 {
                     ("Góry", rider.Climb), ("Pagórki", rider.Hills), ("Płaskie", rider.Flat), ("TT", rider.TimeTrial),
                     ("Sprint", rider.Sprint), ("Bruk", rider.Cobbles),
                 })
        {
            box.AddChild(LookChrome.Kv(statLabel, statValue.ToString(CultureInfo.InvariantCulture)));
        }

        if (isNegotiating)
        {
            box.AddChild(LookChrome.Display("OFERTA KONTRAKTOWA", 12, LookChrome.Team));
            SpinBox wageBox = new();
            wageBox.MinValue = 1;
            wageBox.MaxValue = 50_000_000;
            wageBox.Value = prefillWage;
            box.AddChild(Labeled("Pensja / rok", wageBox));
            SpinBox endDayBox = new();
            endDayBox.MinValue = today + 1;
            endDayBox.MaxValue = 50_000;
            endDayBox.Value = Math.Max(prefillEndDay, today + 1);
            box.AddChild(Labeled("Koniec kontraktu (dzień)", endDayBox));
            box.AddChild(LookChrome.Solid("Złóż ofertę", () =>
            {
                CommandResult set = host.SetContractOffer((int)wageBox.Value, (int)endDayBox.Value);
                if (!set.Succeeded)
                {
                    ShowToast(Reason(set.ReasonCode));
                    Refresh();
                    return;
                }

                CommandResult confirm = host.ConfirmContractOffer();
                negotiating = false;
                ShowToast(confirm.Succeeded ? "Kontrakt przyjęty." : Reason(confirm.ReasonCode));
                Refresh();
            }, LookChrome.Team, LookChrome.TeamOn, compact: true));
        }

        HBoxContainer actions = new();
        actions.AddThemeConstantOverride("separation", 8);
        actions.AddChild(LookChrome.Solid(isNegotiating ? "Anuluj" : "Negocjuj kontrakt", () =>
        {
            if (isNegotiating)
            {
                host.CancelContractNegotiation();
                negotiating = false;
            }
            else
            {
                CommandResult begin = host.BeginContractNegotiation(rider.RiderCareerId);
                if (!begin.Succeeded)
                {
                    ShowToast(Reason(begin.ReasonCode));
                }
                else
                {
                    negotiating = true;
                }
            }

            Refresh();
        }, LookChrome.Team, LookChrome.TeamOn, compact: true));
        actions.AddChild(LookChrome.Solid("Zwolnij z zespołu", () => ShowToast(CareerLookCatalog.NotInWorld), LookChrome.Red, LookChrome.Paper, compact: true));
        box.AddChild(actions);
        return box;
    }

    private VBoxContainer BuildSquadTable()
    {
        VBoxContainer box = new();
        box.AddThemeConstantOverride("separation", 4);
        IReadOnlyList<ClubRosterEntry>? roster = host!.ClubRoster?.Riders;
        if (roster is not { Count: > 0 })
        {
            box.AddChild(LookChrome.Body("Brak składu ze świata.", 13, LookChrome.Gray));
            return box;
        }

        ScrollContainer scroll = new();
        scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Auto;
        scroll.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        scroll.SizeFlagsVertical = SizeFlags.ExpandFill;
        VBoxContainer table = new();
        table.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        HBoxContainer heads = new();
        foreach ((string Label, string Key) col in new[]
                 {
                     ("Zawodnik", "last"), ("OVR", "ovr"), ("POT", "pot"), ("Góry", "climb"), ("Pagórki", "hills"),
                     ("Płaskie", "flat"), ("TT", "tt"), ("Sprint", "sprint"), ("Bruk", "cobbles"),
                     ("Pensja", "wage"), ("Koniec", "end"),
                 })
        {
            string key = col.Key;
            heads.AddChild(SortHead(col.Label, key, squadSort, () =>
            {
                int fresh = key is "last" ? 1 : -1;
                squadSort = CareerLookCatalog.Toggle(squadSort, key, fresh);
                RebuildContent();
            }, numeric: key is not "last"));
        }

        table.AddChild(heads);
        foreach (ClubRosterEntry rider in SortSquad(roster))
        {
            ClubRosterEntry captured = rider;
            bool selected = captured.RiderCareerId.Value == selectedRiderId;
            Color fg = selected ? LookChrome.Paper : LookChrome.Black;
            PanelContainer row = LookChrome.ClickRow(selected, () =>
            {
                if (negotiating || host!.ContractNegotiation is not null)
                {
                    host.CancelContractNegotiation();
                    negotiating = false;
                }

                selectedRiderId = captured.RiderCareerId.Value;
                RebuildContent();
            });
            row.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            HBoxContainer inner = new();
            inner.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            inner.AddChild(LookChrome.Avatar(captured.Name, mini: true));
            inner.AddChild(Cell(captured.Name, true, fg));
            inner.AddChild(Cell(captured.Ovr.ToString(CultureInfo.InvariantCulture), true, fg, numeric: true));
            inner.AddChild(Cell(captured.PotentialOvr.ToString(CultureInfo.InvariantCulture), false, fg, numeric: true));
            inner.AddChild(Cell(captured.Climb.ToString(CultureInfo.InvariantCulture), false, fg, numeric: true));
            inner.AddChild(Cell(captured.Hills.ToString(CultureInfo.InvariantCulture), false, fg, numeric: true));
            inner.AddChild(Cell(captured.Flat.ToString(CultureInfo.InvariantCulture), false, fg, numeric: true));
            inner.AddChild(Cell(captured.TimeTrial.ToString(CultureInfo.InvariantCulture), false, fg, numeric: true));
            inner.AddChild(Cell(captured.Sprint.ToString(CultureInfo.InvariantCulture), false, fg, numeric: true));
            inner.AddChild(Cell(captured.Cobbles.ToString(CultureInfo.InvariantCulture), false, fg, numeric: true));
            inner.AddChild(Cell(CareerLookCatalog.Euro(captured.AnnualWage), false, fg, numeric: true));
            inner.AddChild(Cell(CareerCalendarDates.FormatLong(captured.ContractEndDay), false, fg, numeric: false));
            row.AddChild(inner);
            table.AddChild(row);
        }

        scroll.AddChild(table);
        box.AddChild(scroll);
        return box;
    }

    private VBoxContainer BuildRiderCard()
    {
        LookRider? rider = CareerLookCatalog.Rider((int)selectedRiderId) ?? CareerLookCatalog.Riders[0];
        VBoxContainer box = new();
        box.AddThemeConstantOverride("separation", 8);
        box.AddChild(ProfileHead(rider.FullName, $"{rider.Nat} · {rider.Age} lat · {rider.Role}"));
        box.AddChild(LookChrome.Kv("OVR / POT", rider.Rate + " / " + rider.Pot));
        box.AddChild(LookChrome.Kv("Forma / morale", rider.Form + " / " + rider.Morale));
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
        content!.AddChild(grid);
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
        HBoxContainer grid = Row();
        grid.AddChild(Stretch(Panel("KALENDARZ WYŚCIGÓW", BuildWorldMonthGrid()), 8));
        grid.AddChild(Stretch(Panel("WYŚCIG", BuildCalendarEventDetail()), 4));
        content!.AddChild(grid);
    }

    private VBoxContainer BuildWorldMonthGrid()
    {
        VBoxContainer box = new();
        box.AddThemeConstantOverride("separation", 8);
        HBoxContainer nav = new();
        nav.AddThemeConstantOverride("separation", 12);
        Button prev = LookChrome.Solid("‹", () =>
        {
            if (calendarMonth > 1)
            {
                calendarMonth--;
            }
            else
            {
                calendarYear--;
                calendarMonth = 12;
            }

            RebuildContent();
        }, LookChrome.Paper, LookChrome.Black, compact: true);
        Button next = LookChrome.Solid("›", () =>
        {
            if (calendarMonth < 12)
            {
                calendarMonth++;
            }
            else
            {
                calendarYear++;
                calendarMonth = 1;
            }

            RebuildContent();
        }, LookChrome.Paper, LookChrome.Black, compact: true);
        Label title = LookChrome.Display(
            CareerCalendarDates.FormatMonthNav(calendarYear, calendarMonth),
            22,
            LookChrome.Black);
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

        DateOnly firstOfMonth = new(calendarYear, calendarMonth, 1);
        int offset = ((int)firstOfMonth.DayOfWeek + 6) % 7;
        DateOnly gridStart = firstOfMonth.AddDays(-offset);
        int todayDay = host!.Day?.DayNumber ?? 0;
        Dictionary<int, SeasonEventProjection> startsByDay = host.SeasonEvents
            .Where(item => CareerCalendarDates.ToDate(item.StartDay).Year == calendarYear &&
                           CareerCalendarDates.ToDate(item.StartDay).Month == calendarMonth)
            .GroupBy(item => item.StartDay)
            .ToDictionary(group => group.Key, group => group.First());

        VBoxContainer weeks = new();
        weeks.AddThemeConstantOverride("separation", 4);
        weeks.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        for (int week = 0; week < 6; week++)
        {
            HBoxContainer row = LookEqualCell.Strip();
            for (int col = 0; col < LookEqualCell.CalendarColumns; col++)
            {
                DateOnly cellDate = gridStart.AddDays((week * LookEqualCell.CalendarColumns) + col);
                int dayNumber = CareerCalendarDates.DayNumberFromDate(cellDate);
                bool outsideMonth = cellDate.Month != calendarMonth;
                bool isToday = dayNumber == todayDay;
                startsByDay.TryGetValue(dayNumber, out SeasonEventProjection? eventStart);
                row.AddChild(BuildWorldDayCell(cellDate.Day, outsideMonth, isToday, eventStart));
            }

            weeks.AddChild(row);
        }

        box.AddChild(weeks);
        return box;
    }

    private LookEqualCell BuildWorldDayCell(
        int day,
        bool outsideMonth,
        bool isToday,
        SeasonEventProjection? eventStart)
    {
        LookEqualCell slot = new(LookEqualCell.DayHeight);
        bool selected = eventStart is not null && selectedEventId == eventStart.RaceContentId;
        PanelContainer panel = new();
        panel.AddThemeStyleboxOverride(
            "panel",
            LookChrome.ChipBox(isToday ? LookChrome.Paper : selected ? LookChrome.Black : LookChrome.White));
        if (outsideMonth)
        {
            panel.Modulate = new Color(1, 1, 1, 0.45f);
        }

        if (eventStart is not null)
        {
            SeasonEventProjection captured = eventStart;
            panel.MouseDefaultCursorShape = CursorShape.PointingHand;
            panel.GuiInput += e =>
            {
                if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
                {
                    selectedEventId = captured.RaceContentId;
                    RebuildContent();
                    panel.AcceptEvent();
                }
            };
        }

        VBoxContainer inner = new();
        inner.AddThemeConstantOverride("separation", 2);
        Color numColor = selected ? LookChrome.Paper : LookChrome.Gray;
        Label number = LookChrome.Body(day.ToString(CultureInfo.InvariantCulture), 11, numColor, bold: true);
        number.ClipText = true;
        inner.AddChild(number);
        if (eventStart is not null)
        {
            Color eventFg = selected ? LookChrome.Black : LookChrome.Paper;
            Label name = LookChrome.Body(eventStart.Name, 10, eventFg, bold: true);
            name.AutowrapMode = TextServer.AutowrapMode.Arbitrary;
            name.ClipText = true;
            name.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            name.SizeFlagsVertical = SizeFlags.ExpandFill;
            name.MouseFilter = MouseFilterEnum.Ignore;
            ColorRect chip = LookChrome.Block(LookChrome.Team);
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

    private VBoxContainer BuildCalendarEventDetail()
    {
        VBoxContainer box = new();
        box.AddThemeConstantOverride("separation", 8);
        SeasonEventProjection? item = FindSelectedEvent();
        if (item is null)
        {
            box.AddChild(LookChrome.Body("Wybierz wyścig z kalendarza.", 13, LookChrome.Gray));
            return box;
        }

        box.AddChild(LookChrome.Display(item.Name.ToUpperInvariant(), 22, LookChrome.Black));
        box.AddChild(LookChrome.Body(CareerCalendarDates.FormatRange(item.StartDay, item.EndDay), 13, LookChrome.Gray, bold: true));
        string stageLabel = item.StageCount > 1
            ? string.Create(CultureInfo.InvariantCulture, $"{item.StageCount} etapów")
            : "jednodniowy";
        box.AddChild(LookChrome.Kv("Format", stageLabel));
        box.AddChild(LookChrome.Solid(
            "otwórz wyścig ›",
            () => OpenRaceEvent(View.Calendar),
            LookChrome.Team,
            LookChrome.TeamOn,
            compact: true));
        return box;
    }

    private void BuildRaceEvent()
    {
        SeasonEventProjection? item = FindSelectedEvent();
        if (item is null)
        {
            content!.AddChild(Panel("WYŚCIG", LookChrome.Body("Brak wybranego wyścigu.", 13, LookChrome.Gray)));
            return;
        }

        content!.AddChild(LookChrome.Solid(
            "‹ wróć",
            () => Show(raceEventBackView),
            LookChrome.Paper,
            LookChrome.Black,
            compact: true));
        VBoxContainer header = new();
        header.AddThemeConstantOverride("separation", 6);
        header.AddChild(LookChrome.Display(item.Name.ToUpperInvariant(), 28, LookChrome.Black));
        header.AddChild(LookChrome.Body(CareerCalendarDates.FormatRange(item.StartDay, item.EndDay), 14, LookChrome.Gray, bold: true));
        content!.AddChild(Panel("WYŚCIG", header));

        VBoxContainer stages = new();
        stages.AddThemeConstantOverride("separation", 6);
        IReadOnlyList<CalendarEntryProjection> stageRows = host!.Calendar
            .Where(entry => string.Equals(entry.RaceContentId, item.RaceContentId, StringComparison.Ordinal))
            .OrderBy(entry => entry.DayNumber)
            .ToArray();
        foreach (CalendarEntryProjection stage in stageRows)
        {
            CalendarEntryProjection captured = stage;
            VBoxContainer row = new();
            row.AddThemeConstantOverride("separation", 2);
            row.AddChild(LookChrome.Body(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"{CareerCalendarDates.FormatLong(captured.DayNumber)} · {captured.Title}"),
                14,
                LookChrome.Black,
                bold: true));
            if (!string.IsNullOrWhiteSpace(captured.OfficialResult))
            {
                row.AddChild(LookChrome.Body(captured.OfficialResult, 12, LookChrome.Gray));
            }

            stages.AddChild(WrapCard(row));
        }

        content!.AddChild(Panel("ETAPY", stages));
    }

    private void BuildSponsors()
    {
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
        detail.AddChild(LookChrome.Kv("Kwota umowy", current.Value));
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
        content!.AddChild(grid);
    }

    private void BuildFinance()
    {
        ClubFinanceProjection? finance = host!.ClubFinance;
        if (finance is null)
        {
            content!.AddChild(Panel("FINANSE", LookChrome.Body("Brak danych finansowych ze świata.", 13, LookChrome.Gray)));
            return;
        }

        HBoxContainer top = Row();
        VBoxContainer summary = new();
        summary.AddThemeConstantOverride("separation", 8);
        summary.AddChild(LookChrome.Display(CareerLookCatalog.Euro(finance.CashEur), 32, LookChrome.Black));
        summary.AddChild(LookChrome.Body("kasa klubu · euro", 12, LookChrome.Gray, bold: true));
        summary.AddChild(LookChrome.Kv(
            "Sponsor tytularny",
            string.Create(
                CultureInfo.InvariantCulture,
                $"{finance.TitleSponsorName} · {CareerLookCatalog.Euro(finance.TitleSponsorAnnualFeeEur)} / rok")));
        summary.AddChild(LookChrome.Kv("Sponsor / dzień", CareerLookCatalog.SignedEuro(finance.DailySponsor)));
        summary.AddChild(LookChrome.Kv("Płace / dzień", CareerLookCatalog.SignedEuro(-finance.DailyWages)));
        summary.AddChild(LookChrome.Kv("Bilans dnia", CareerLookCatalog.SignedEuro(finance.DailyNet)));
        summary.AddChild(LookChrome.Kv("Pensja składu / rok", CareerLookCatalog.Euro(finance.WageBillAnnual)));
        if (finance.Overdrawn)
        {
            summary.AddChild(LookChrome.Body("Klub jest na debecie", 13, LookChrome.Red, bold: true));
        }

        top.AddChild(Stretch(Panel("FINANSE", summary), 12));
        content!.AddChild(top);
    }

    private void BuildScouting()
    {
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
            ShowToast(CareerLookCatalog.NotInWorld);
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
        content!.AddChild(grid);
        HBoxContainer lower = Row();
        lower.AddChild(Stretch(Panel("ZAKOŃCZONE RAPORTY", reports), 4));
        lower.AddChild(Stretch(Panel("RAPORT", BuildReportDetail()), 8));
        content!.AddChild(lower);
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
        IReadOnlyList<MarketRiderProjection> riders = FilteredMarketRiders();
        if (selectedMarketRiderId == 0 ||
            riders.All(rider => rider.RiderCareerId.Value != selectedMarketRiderId))
        {
            selectedMarketRiderId = riders.Count > 0 ? riders[0].RiderCareerId.Value : 0;
        }

        HBoxContainer grid = Row();
        VBoxContainer tableWrap = new();
        tableWrap.AddThemeConstantOverride("separation", 8);
        OptionButton clubFilter = new();
        clubFilter.AddItem("Wszystkie", 0);
        int filterIndex = 0;
        int selectedFilterIndex = 0;
        foreach (string club in host!.MarketRiders
                     .Select(rider => rider.OrganizationName)
                     .Where(name => !string.IsNullOrWhiteSpace(name))
                     .Distinct(StringComparer.Ordinal)
                     .OrderBy(name => name, StringComparer.Ordinal))
        {
            filterIndex++;
            clubFilter.AddItem(club, filterIndex);
            if (string.Equals(club, marketClubFilter, StringComparison.Ordinal))
            {
                selectedFilterIndex = filterIndex;
            }
        }

        clubFilter.Selected = selectedFilterIndex;
        clubFilter.ItemSelected += index =>
        {
            marketClubFilter = index <= 0 ? string.Empty : clubFilter.GetItemText((int)index);
            RebuildContent();
        };
        tableWrap.AddChild(Labeled("Klub", clubFilter));

        VBoxContainer table = new();
        table.AddThemeConstantOverride("separation", 4);
        HBoxContainer heads = new();
        foreach ((string Label, string Key) col in new[]
                 {
                     ("Zawodnik", "name"), ("Klub", "club"), ("OVR", "ovr"), ("POT", "pot"),
                     ("Góry", "climb"), ("Pensja", "wage"), ("Koniec", "end"),
                 })
        {
            string key = col.Key;
            heads.AddChild(SortHead(col.Label, key, marketSort, () =>
            {
                int fresh = key is "name" or "club" ? 1 : -1;
                marketSort = CareerLookCatalog.Toggle(marketSort, key, fresh);
                RebuildContent();
            }, numeric: key is not ("name" or "club")));
        }

        table.AddChild(heads);
        foreach (MarketRiderProjection row in SortMarket(riders))
        {
            MarketRiderProjection captured = row;
            bool selected = captured.RiderCareerId.Value == selectedMarketRiderId;
            Color fg = selected ? LookChrome.Paper : LookChrome.Black;
            PanelContainer line = LookChrome.ClickRow(selected, () =>
            {
                selectedMarketRiderId = captured.RiderCareerId.Value;
                negotiating = false;
                RebuildContent();
            });
            HBoxContainer inner = new();
            inner.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            inner.AddChild(LookChrome.Avatar(captured.Name, mini: true));
            inner.AddChild(Cell(captured.Name, true, fg));
            inner.AddChild(Cell(
                string.IsNullOrWhiteSpace(captured.OrganizationName) ? "—" : captured.OrganizationName,
                false,
                fg));
            inner.AddChild(Cell(captured.Ovr.ToString(CultureInfo.InvariantCulture), true, fg, numeric: true));
            inner.AddChild(Cell(captured.PotentialOvr.ToString(CultureInfo.InvariantCulture), false, fg, numeric: true));
            inner.AddChild(Cell(captured.Climb.ToString(CultureInfo.InvariantCulture), false, fg, numeric: true));
            inner.AddChild(Cell(CareerLookCatalog.Euro(captured.AnnualWage), false, fg, numeric: true));
            inner.AddChild(Cell(
                captured.ContractEndDay > 0
                    ? CareerCalendarDates.FormatLong(captured.ContractEndDay)
                    : "—",
                false,
                fg));
            line.AddChild(inner);
            table.AddChild(line);
        }

        if (riders.Count == 0)
        {
            table.AddChild(LookChrome.Body("Brak zawodników na rynku.", 13, LookChrome.Gray));
        }

        tableWrap.AddChild(table);
        grid.AddChild(Stretch(Panel("DOSTĘPNI ZAWODNICY", tableWrap), 8));
        VBoxContainer marketCard = Stretch(Panel("ZAWODNIK", BuildMarketCard()), 4);
        marketCard.CustomMinimumSize = new Vector2(340, 0);
        grid.AddChild(marketCard);
        content!.AddChild(grid);
    }

    private VBoxContainer BuildMarketCard()
    {
        MarketRiderProjection? row = host!.MarketRiders
            .FirstOrDefault(rider => rider.RiderCareerId.Value == selectedMarketRiderId);
        VBoxContainer box = new();
        box.AddThemeConstantOverride("separation", 8);
        if (row is null)
        {
            box.AddChild(LookChrome.Body("Wybierz zawodnika z listy.", 13, LookChrome.Gray));
            return box;
        }

        bool isNegotiating = negotiating &&
            host.ContractNegotiation?.RiderCareerId == row.RiderCareerId;
        int today = host.Day?.DayNumber ?? 0;
        int prefillWage = row.AnnualWage > 0 ? row.AnnualWage : 100_000;
        int prefillEndDay = row.ContractEndDay > today ? row.ContractEndDay : today + 365;
        if (host.ContractNegotiation?.OfferAnnualWage is int draftWage)
        {
            prefillWage = draftWage;
        }

        if (host.ContractNegotiation?.OfferContractEndDay is int draftEndDay)
        {
            prefillEndDay = draftEndDay;
        }

        box.AddChild(ProfileHead(
            row.Name,
            string.Create(CultureInfo.InvariantCulture, $"OVR {row.Ovr} · POT {row.PotentialOvr}")));
        box.AddChild(LookChrome.Kv(
            "Klub",
            string.IsNullOrWhiteSpace(row.OrganizationName) ? "—" : row.OrganizationName));
        box.AddChild(LookChrome.Kv("Pensja / rok", CareerLookCatalog.Euro(row.AnnualWage)));
        box.AddChild(LookChrome.Kv(
            "Koniec kontraktu",
            row.ContractEndDay > 0 ? CareerCalendarDates.FormatLong(row.ContractEndDay) : "—"));
        foreach ((string statLabel, int statValue) in new[]
                 {
                     ("Góry", row.Climb), ("Pagórki", row.Hills), ("Płaskie", row.Flat), ("TT", row.TimeTrial),
                     ("Sprint", row.Sprint), ("Bruk", row.Cobbles),
                 })
        {
            box.AddChild(LookChrome.Kv(statLabel, statValue.ToString(CultureInfo.InvariantCulture)));
        }

        if (isNegotiating)
        {
            box.AddChild(LookChrome.Display("OFERTA KONTRAKTOWA", 12, LookChrome.Team));
            SpinBox wageBox = new();
            wageBox.MinValue = 1;
            wageBox.MaxValue = 50_000_000;
            wageBox.Value = prefillWage;
            box.AddChild(Labeled("Pensja / rok", wageBox));
            SpinBox endDayBox = new();
            endDayBox.MinValue = today + 1;
            endDayBox.MaxValue = 50_000;
            endDayBox.Value = Math.Max(prefillEndDay, today + 1);
            box.AddChild(Labeled("Koniec kontraktu (dzień)", endDayBox));
            box.AddChild(LookChrome.Solid("Złóż ofertę", () =>
            {
                CommandResult set = host.SetContractOffer((int)wageBox.Value, (int)endDayBox.Value);
                if (!set.Succeeded)
                {
                    ShowToast(Reason(set.ReasonCode));
                    Refresh();
                    return;
                }

                CommandResult confirm = host.ConfirmContractOffer();
                negotiating = false;
                ShowToast(confirm.Succeeded ? "Kontrakt przyjęty." : Reason(confirm.ReasonCode));
                Refresh();
            }, LookChrome.Team, LookChrome.TeamOn, compact: true));
        }

        HBoxContainer actions = new();
        actions.AddThemeConstantOverride("separation", 8);
        actions.AddChild(LookChrome.Solid(isNegotiating ? "Anuluj" : "Negocjuj kontrakt", () =>
        {
            if (isNegotiating)
            {
                host.CancelContractNegotiation();
                negotiating = false;
            }
            else
            {
                CommandResult begin = host.BeginContractNegotiation(row.RiderCareerId);
                if (!begin.Succeeded)
                {
                    ShowToast(Reason(begin.ReasonCode));
                }
                else
                {
                    negotiating = true;
                }
            }

            Refresh();
        }, LookChrome.Team, LookChrome.TeamOn, compact: true));
        box.AddChild(actions);
        return box;
    }

    private void BuildHistory()
    {
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
        content!.AddChild(top);
        content!.AddChild(Panel("ARCHIWUM WYNIKÓW", archive));

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
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"{CareerCalendarDates.FormatLong(entry.DayNumber)} · {entry.Title} · {entry.OfficialResult}"),
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

        content!.AddChild(Panel("KRONIKA ŚWIATA", world));
    }

    private void BuildManager()
    {
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
        content!.AddChild(grid);
        HBoxContainer lower = Row();
        lower.AddChild(Stretch(Panel("KARIERA", career), 6));
        lower.AddChild(Stretch(Panel("OSIĄGNIĘCIA", achievements), 6));
        content!.AddChild(lower);

        CareerDayProjection? day = host!.Day;
        VBoxContainer world = new();
        world.AddThemeConstantOverride("separation", 8);
        world.AddChild(LookChrome.Display((day?.ManagerName ?? "—").ToUpperInvariant(), 22, LookChrome.Black));
        world.AddChild(LookChrome.Kv("Pracodawca świata", day?.EmployerName ?? "bez klubu"));
        content!.AddChild(Panel("MANAGER ŚWIATA", world));
    }

    private void BuildHelp()
    {
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

        content!.AddChild(cards);
        VBoxContainer real = new();
        real.AddThemeConstantOverride("separation", 8);
        real.AddChild(LookChrome.Body("Advance Day przesuwa cały świat o jeden dzień.", 14, LookChrome.Black));
        real.AddChild(LookChrome.Body("W dzień wyścigu ten sam przycisk nazywa się Race next i wchodzi w przygotowanie.", 14, LookChrome.Black));
        real.AddChild(LookChrome.Body("Oglądanie etapu jest opcją w Ustawieniach. Domyślnie dostajesz wynik i tabelę.", 14, LookChrome.Black));
        real.AddChild(LookChrome.Body("Skrzynka świata nie startuje wyścigu. Kasa na biurku i w Finansach to świat (euro). Sztab, sponsorzy i skauting to jeszcze nie w tej wersji.", 14, LookChrome.Black));
        content!.AddChild(Panel("ŚWIAT", real));
    }

    private void EnsureSelectedEvent()
    {
        if (host is null)
        {
            return;
        }

        IReadOnlyList<SeasonEventProjection> upcoming = host.UpcomingEvents;
        if (upcoming.Count > 0)
        {
            if (selectedEventId is null ||
                upcoming.All(item => item.RaceContentId != selectedEventId))
            {
                selectedEventId = upcoming[0].RaceContentId;
            }

            return;
        }

        if (selectedEventId is null && host.SeasonEvents.Count > 0)
        {
            selectedEventId = host.SeasonEvents[0].RaceContentId;
        }
    }

    private SeasonEventProjection? FindSelectedEvent()
    {
        if (host is null || selectedEventId is null)
        {
            return null;
        }

        return host.SeasonEvents.FirstOrDefault(item => item.RaceContentId == selectedEventId)
            ?? host.UpcomingEvents.FirstOrDefault(item => item.RaceContentId == selectedEventId);
    }

    private void OpenRaceEvent(View backView)
    {
        raceEventBackView = backView;
        Show(View.RaceEvent);
    }

    private ClubRosterEntry[] SortSquad(IReadOnlyList<ClubRosterEntry> roster)
    {
        IEnumerable<ClubRosterEntry> ordered = squadSort.Key switch
        {
            "ovr" => roster.OrderBy(rider => rider.Ovr),
            "pot" => roster.OrderBy(rider => rider.PotentialOvr),
            "climb" => roster.OrderBy(rider => rider.Climb),
            "hills" => roster.OrderBy(rider => rider.Hills),
            "flat" => roster.OrderBy(rider => rider.Flat),
            "tt" => roster.OrderBy(rider => rider.TimeTrial),
            "sprint" => roster.OrderBy(rider => rider.Sprint),
            "cobbles" => roster.OrderBy(rider => rider.Cobbles),
            "wage" => roster.OrderBy(rider => rider.AnnualWage),
            "end" => roster.OrderBy(rider => rider.ContractEndDay),
            _ => roster.OrderBy(rider => rider.Name, StringComparer.Ordinal),
        };

        return squadSort.Dir > 0
            ? ordered.ToArray()
            : ordered.Reverse().ToArray();
    }

    private IReadOnlyList<MarketRiderProjection> FilteredMarketRiders()
    {
        if (host is null)
        {
            return Array.Empty<MarketRiderProjection>();
        }

        if (string.IsNullOrWhiteSpace(marketClubFilter))
        {
            return host.MarketRiders;
        }

        return host.MarketRiders
            .Where(rider => string.Equals(rider.OrganizationName, marketClubFilter, StringComparison.Ordinal))
            .ToArray();
    }

    private MarketRiderProjection[] SortMarket(IReadOnlyList<MarketRiderProjection> riders)
    {
        IEnumerable<MarketRiderProjection> ordered = marketSort.Key switch
        {
            "club" => riders.OrderBy(rider => rider.OrganizationName, StringComparer.Ordinal),
            "ovr" => riders.OrderBy(rider => rider.Ovr),
            "pot" => riders.OrderBy(rider => rider.PotentialOvr),
            "climb" => riders.OrderBy(rider => rider.Climb),
            "wage" => riders.OrderBy(rider => rider.AnnualWage),
            "end" => riders.OrderBy(rider => rider.ContractEndDay),
            _ => riders.OrderBy(rider => rider.Name, StringComparer.Ordinal),
        };

        return marketSort.Dir > 0
            ? ordered.ToArray()
            : ordered.Reverse().ToArray();
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

    private static Button SortHead(string label, string key, LookSort sort, Action onPressed, bool numeric = false)
    {
        string mark = sort.Key == key ? (sort.Dir > 0 ? " ▲" : " ▼") : string.Empty;
        Button button = LookChrome.Solid(label + mark, onPressed, LookChrome.White, LookChrome.Black, compact: true);
        button.CustomMinimumSize = new Vector2(numeric ? 52 : 80, 36);
        button.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        button.SizeFlagsStretchRatio = numeric ? 0.7f : 1.4f;
        button.Alignment = numeric ? HorizontalAlignment.Center : HorizontalAlignment.Left;
        return button;
    }

    private static Label Cell(string text, bool bold, Color? color = null, bool numeric = false)
    {
        Label label = LookChrome.Body(text, 13, color ?? LookChrome.Black, bold);
        label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        label.SizeFlagsStretchRatio = numeric ? 0.7f : 1.4f;
        label.HorizontalAlignment = numeric ? HorizontalAlignment.Center : HorizontalAlignment.Left;
        label.VerticalAlignment = VerticalAlignment.Center;
        return label;
    }

    private static bool IsNumericColumn(string key)
    {
        return key is "age" or "rate" or "pot" or "form" or "fatigue" or "interest" or "salary" or "trend";
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
