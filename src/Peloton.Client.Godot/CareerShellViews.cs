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
        VBoxContainer list = Panel("NADCHODZĄCE WYŚCIGI", BuildUpcomingList(), "PEŁNY KALENDARZ ›", () => Show(View.Calendar));
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

        string inboxTitle = host.Inbox.Count > 0
            ? string.Create(CultureInfo.InvariantCulture, $"INBOX · {host.Inbox.Count} SPRAWY")
            : "INBOX";
        raceCol.AddChild(Panel(inboxTitle, BuildDeskInbox()));
        top.AddChild(list);
        top.AddChild(raceCol);
        content!.AddChild(top);

        HBoxContainer mid = Row();
        VBoxContainer squad = Panel("SKŁAD — OCENA", BuildDeskSquad(), "PEŁNY SKŁAD ›", () => Show(View.Squad));
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
        int today = host!.Day?.DayNumber ?? 0;
        bool worldTour = host.IsWorldTourWorld;
        string? firstId = host.UpcomingEvents.Count > 0 ? host.UpcomingEvents[0].RaceContentId : null;
        foreach (SeasonEventProjection item in host.UpcomingEvents)
        {
            SeasonEventProjection captured = item;
            bool active = captured.RaceContentId == firstId ||
                captured.RaceContentId == selectedEventId;
            Color fg = active ? LookChrome.Paper : LookChrome.Black;
            Color micro = active ? LookChrome.Hair : LookChrome.Gray;
            PanelContainer row = LookChrome.ClickRow(active, () =>
            {
                selectedEventId = captured.RaceContentId;
                RebuildContent();
            });
            HBoxContainer inner = new();
            inner.AddThemeConstantOverride("separation", 10);
            inner.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            inner.AddChild(LookChrome.DateChip(LookFormat.DateChipLabel(captured.StartDay), inverted: active));
            VBoxContainer names = new();
            names.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            names.AddThemeConstantOverride("separation", 2);
            names.AddChild(LookChrome.Body(captured.Name, 14, fg, bold: true));
            names.AddChild(LookChrome.Meta(
                LookFormat.EventMetaLine(captured, today, worldTour),
                10,
                micro));
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
        return BuildEventDetailPanel(View.Desk);
    }

    private VBoxContainer BuildEventDetailPanel(View backView)
    {
        VBoxContainer box = new();
        box.AddThemeConstantOverride("separation", 8);
        SeasonEventProjection? item = FindSelectedEvent();
        if (item is null)
        {
            box.AddChild(LookChrome.Body("Kalendarz świata jest pusty.", 13, LookChrome.Gray));
            return box;
        }

        bool worldTour = host!.IsWorldTourWorld;
        HBoxContainer head = new();
        head.AddThemeConstantOverride("separation", 10);
        Label name = LookChrome.Title(item.Name);
        name.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        head.AddChild(name);
        head.AddChild(LookChrome.Body(
            LookFormat.EventCategoryLabel(item, worldTour),
            14,
            LookChrome.Team,
            bold: true));
        box.AddChild(head);
        string format = item.StageCount > 1
            ? string.Create(CultureInfo.InvariantCulture, $"{item.StageCount} ETAPÓW")
            : "JEDNODNIOWY";
        HBoxContainer metaRow = new();
        metaRow.AddThemeConstantOverride("separation", 12);
        metaRow.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        metaRow.AddChild(LookChrome.Meta("DATA", 10, LookChrome.Gray));
        metaRow.AddChild(LookChrome.Meta(
            CareerCalendarDates.FormatRange(item.StartDay, item.EndDay).ToUpperInvariant(),
            10,
            LookChrome.Black));
        metaRow.AddChild(LookChrome.Meta("FORMAT", 10, LookChrome.Gray));
        metaRow.AddChild(LookChrome.Meta(format, 10, LookChrome.Black));
        box.AddChild(metaRow);
        HBoxContainer chips = new();
        chips.AddThemeConstantOverride("separation", 6);
        chips.AddChild(LookChrome.Chip(LookFormat.EventStatusLabel(item.Status), "inv"));
        box.AddChild(chips);
        if (item.ElevationSparkline is { Count: >= 2 })
        {
            LookSparkline sparkline = new();
            sparkline.SetHeights(item.ElevationSparkline.ToArray());
            box.AddChild(sparkline);
        }
        if (item.LengthKm > 0 || item.ElevationGainM > 0 || !string.IsNullOrWhiteSpace(item.ClassifiedStageType))
        {
            HBoxContainer courseMeta = new();
            courseMeta.AddThemeConstantOverride("separation", 12);
            if (item.LengthKm > 0)
            {
                courseMeta.AddChild(LookChrome.Meta("DYSTANS", 10, LookChrome.Gray));
                courseMeta.AddChild(LookChrome.Meta(string.Create(CultureInfo.InvariantCulture, $"{item.LengthKm:F0} KM"), 10, LookChrome.Black));
            }
            if (item.ElevationGainM > 0)
            {
                courseMeta.AddChild(LookChrome.Meta("PRZEWYŻSZENIE", 10, LookChrome.Gray));
                courseMeta.AddChild(LookChrome.Meta(string.Create(CultureInfo.InvariantCulture, $"+{item.ElevationGainM:F0} M"), 10, LookChrome.Black));
            }
            if (!string.IsNullOrWhiteSpace(item.ClassifiedStageType))
            {
                courseMeta.AddChild(LookChrome.Meta("PROFIL", 10, LookChrome.Gray));
                courseMeta.AddChild(LookChrome.Meta(item.ClassifiedStageType.ToUpperInvariant(), 10, LookChrome.Team));
            }
            box.AddChild(courseMeta);
        }
        box.AddChild(LookChrome.Solid(
            "otwórz wyścig ›",
            () => OpenRaceEvent(backView),
            LookChrome.Team,
            LookChrome.TeamOn,
            compact: true));

        RacePreparationProjection? prep = host.Preparation;
        if (prep is not null)
        {
            box.AddChild(LookChrome.Kv("Cel", prep.Objective));
            box.AddChild(LookChrome.Body(
                "Kliknij kolarza, aby powołać lub odwołać ze składu. Przycisk LIDER wyznacza kapitana.",
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

        int requiredCount = prep.RequiredStartersCount;
        IReadOnlyList<WorldEntityId> starters = prep.SelectedRiderIds ??
            prep.Squad.Take(requiredCount).ToArray();
        HashSet<WorldEntityId> starterSet = starters.ToHashSet();

        box.AddChild(LookChrome.Body(
            string.Create(CultureInfo.InvariantCulture, $"{prep.Title} · {prep.Objective}"),
            13,
            LookChrome.Black,
            bold: true));

        box.AddChild(LookChrome.Meta(
            string.Create(CultureInfo.InvariantCulture, $"SKŁAD WYJŚCIOWY ({starters.Count}/{requiredCount}):"),
            11,
            LookChrome.Black));

        foreach (WorldEntityId riderId in starters)
        {
            WorldEntityId captured = riderId;
            HBoxContainer row = new();
            row.AddThemeConstantOverride("separation", 6);

            string role = captured == prep.LeaderId
                ? "Lider"
                : captured == prep.SupportId
                    ? "Pomocnik"
                    : "Skład";

            Button riderBtn = LookChrome.Solid(
                $"✓ {host.RiderDisplayName(captured)} · {role}",
                () => Apply(host.ToggleStarter(captured)),
                LookChrome.Paper,
                LookChrome.Black,
                compact: true);
            riderBtn.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            row.AddChild(riderBtn);

            if (captured == prep.LeaderId)
            {
                Button leadBadge = LookChrome.Solid("★ LIDER", () => { }, LookChrome.Team, LookChrome.TeamOn, compact: true);
                leadBadge.CustomMinimumSize = new Vector2(80, 36);
                row.AddChild(leadBadge);
            }
            else
            {
                Button leadBtn = LookChrome.Solid("LIDER", () => Apply(host.SetLeader(captured)), LookChrome.Hair, LookChrome.Black, compact: true);
                leadBtn.CustomMinimumSize = new Vector2(80, 36);
                row.AddChild(leadBtn);
            }

            box.AddChild(row);
        }

        List<WorldEntityId> reserves = prep.Squad.Where(id => !starterSet.Contains(id)).ToList();
        if (reserves.Count > 0)
        {
            box.AddChild(LookChrome.Meta("REZERWA (KLIKNIJ, ABY POWOŁAĆ):", 11, LookChrome.Gray));

            ScrollContainer scroll = new()
            {
                CustomMinimumSize = new Vector2(0, 160),
                HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            };
            VBoxContainer reserveBox = new();
            reserveBox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            reserveBox.AddThemeConstantOverride("separation", 4);

            foreach (WorldEntityId riderId in reserves)
            {
                WorldEntityId captured = riderId;
                Button reserveBtn = LookChrome.Solid(
                    $"+ {host.RiderDisplayName(captured)}",
                    () => Apply(host.ToggleStarter(captured)),
                    LookChrome.Hair,
                    LookChrome.Black,
                    compact: true);
                reserveBtn.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                reserveBox.AddChild(reserveBtn);
            }

            scroll.AddChild(reserveBox);
            box.AddChild(scroll);
        }

        return box;
    }

    private VBoxContainer BuildDeskInbox()
    {
        VBoxContainer box = new();
        box.AddThemeConstantOverride("separation", 0);
        if (host!.Inbox.Count == 0)
        {
            MarginContainer pad = new();
            pad.AddThemeConstantOverride("margin_left", 14);
            pad.AddThemeConstantOverride("margin_top", 14);
            pad.AddThemeConstantOverride("margin_right", 14);
            pad.AddThemeConstantOverride("margin_bottom", 14);
            pad.AddChild(LookChrome.Body("Brak spraw.", 14, LookChrome.Gray, bold: true));
            box.AddChild(pad);
            return box;
        }

        int index = 1;
        foreach (InboxItemProjection item in host.Inbox)
        {
            InboxItemProjection captured = item;
            string number = index.ToString("00", CultureInfo.InvariantCulture);
            string when = captured.DayNumber is int day
                ? CareerCalendarDates.FormatLong(day)
                : "—";
            box.AddChild(LookChrome.InboxRow(number, captured.Body, when, urgent: false));
            if (captured.Category == "race-due")
            {
                MarginContainer actionPad = new();
                actionPad.AddThemeConstantOverride("margin_left", 14);
                actionPad.AddThemeConstantOverride("margin_top", 6);
                actionPad.AddThemeConstantOverride("margin_bottom", 6);
                actionPad.AddChild(LookChrome.Solid(
                    "Jedź wyścig",
                    () => Apply(host.FollowPrimary()),
                    LookChrome.Team,
                    LookChrome.TeamOn,
                    compact: true));
                box.AddChild(actionPad);
            }
            else
            {
                MarginContainer actionPad = new();
                actionPad.AddThemeConstantOverride("margin_left", 14);
                actionPad.AddThemeConstantOverride("margin_top", 6);
                actionPad.AddThemeConstantOverride("margin_bottom", 6);
                actionPad.AddChild(LookChrome.Solid("Archiwizuj", () =>
                {
                    CommandResult result = host.ArchiveInbox(captured.Identity);
                    ShowToast(result.Succeeded ? "Zarchiwizowano." : Reason(result.ReasonCode));
                    Refresh();
                }, LookChrome.Paper, LookChrome.Black, compact: true));
                box.AddChild(actionPad);
            }

            index++;
        }

        return box;
    }

    private VBoxContainer BuildDeskSquad()
    {
        VBoxContainer box = new();
        box.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        IReadOnlyList<ClubRosterEntry>? roster = host!.ClubRoster?.Riders;
        if (roster is not { Count: > 0 })
        {
            box.AddChild(LookChrome.Body("Brak składu ze świata.", 13, LookChrome.Gray));
            return box;
        }

        ClubRosterEntry[] sorted = SortSquad(roster);
        TableColumn[] columns =
        [
            new("Zawodnik", "last", TableAlign.Left, false, 0, true),
            new("OVR", "ovr", TableAlign.Center, true, 56),
            new("POT", "pot", TableAlign.Center, false, 56),
            new("Góry", "climb", TableAlign.Center, false, 56),
            new("Pagórki", "hills", TableAlign.Center, false, 56),
            new("Płaskie", "flat", TableAlign.Center, false, 56),
            new("TT", "tt", TableAlign.Center, false, 56),
            new("Sprint", "sprint", TableAlign.Center, false, 56),
            new("Bruk", "cobbles", TableAlign.Center, false, 56),
        ];
        List<TableRow> rows = new(sorted.Length);
        foreach (ClubRosterEntry rider in sorted)
        {
            rows.Add(new TableRow(
            [
                new TableCell(rider.Name),
                new TableCell(rider.Ovr.ToString(CultureInfo.InvariantCulture)),
                new TableCell(rider.PotentialOvr.ToString(CultureInfo.InvariantCulture)),
                new TableCell(rider.Climb.ToString(CultureInfo.InvariantCulture)),
                new TableCell(rider.Hills.ToString(CultureInfo.InvariantCulture)),
                new TableCell(rider.Flat.ToString(CultureInfo.InvariantCulture)),
                new TableCell(rider.TimeTrial.ToString(CultureInfo.InvariantCulture)),
                new TableCell(rider.Sprint.ToString(CultureInfo.InvariantCulture)),
                new TableCell(rider.Cobbles.ToString(CultureInfo.InvariantCulture)),
            ]));
        }

        box.AddChild(LookChrome.Table(
            columns,
            rows,
            -1,
            squadSort.Key,
            squadSort.Dir,
            key =>
            {
                int fresh = key is "last" ? 1 : -1;
                squadSort = CareerLookCatalog.Toggle(squadSort, key, fresh);
                RebuildContent();
            },
            null));
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
            line.AddThemeConstantOverride("separation", 10);
            line.AddChild(LookChrome.RankChip(
                row.Place.ToString(CultureInfo.InvariantCulture),
                row.Mine));
            Label team = LookChrome.Body(row.Team, 13, LookChrome.Black, bold: row.Mine);
            team.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            Label pts = LookChrome.Display(
                row.Points.ToString(CultureInfo.InvariantCulture),
                14,
                LookChrome.Black);
            pts.HorizontalAlignment = HorizontalAlignment.Right;
            line.AddChild(team);
            line.AddChild(pts);
            box.AddChild(line);
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

        box.AddChild(LookChrome.SignedKv("Sponsor / dzień", finance.DailySponsor));
        box.AddChild(LookChrome.SignedKv("Płace / dzień", -finance.DailyWages));
        box.AddChild(LookChrome.Hairline());
        box.AddChild(LookChrome.Kv("Bilans dnia", CareerLookCatalog.SignedEuro(finance.DailyNet)));
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
            card.AddChild(LookChrome.Meta(note.Who, 10, note.Urgent ? LookChrome.Red : LookChrome.Gray));
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
        grid.SizeFlagsVertical = SizeFlags.ExpandFill;
        VBoxContainer table = Panel("KADRA", BuildSquadTable(), expandVertical: true);
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
        box.AddThemeConstantOverride("separation", 10);
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

        HBoxContainer head = new();
        head.AddThemeConstantOverride("separation", 12);
        head.AddChild(LookChrome.Avatar(rider.Name));
        VBoxContainer names = new();
        names.AddChild(LookChrome.Title(rider.Name));
        string natPart = !string.IsNullOrWhiteSpace(rider.Nationality) ? $"{rider.Nationality.ToUpperInvariant()} · " : "";
        string agePart = rider.Age.HasValue ? $"{rider.Age} LAT · " : "";
        names.AddChild(LookChrome.Body(
            string.Create(CultureInfo.InvariantCulture, $"{natPart}{agePart}OVR {rider.Ovr} · POT {rider.PotentialOvr}"),
            12,
            LookChrome.Gray,
            bold: true));
        names.AddChild(LookChrome.Kv("Styl kolarza", $"{rider.StyleLabel} ({rider.StarsDisplay})"));
        names.AddChild(LookChrome.Kv("Zmęczenie sezonowe", $"{rider.SeasonalFatiguePercent}% ({rider.SeasonRaceDaysCount} dni startowych)"));
        head.AddChild(names);
        box.AddChild(head);

        GridContainer stats = new() { Columns = 2 };
        stats.AddThemeConstantOverride("h_separation", 12);
        stats.AddThemeConstantOverride("v_separation", 7);
        stats.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        VBoxContainer leftStats = new();
        leftStats.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        leftStats.AddThemeConstantOverride("separation", 7);
        leftStats.AddChild(LookChrome.Stat("Góry", rider.Climb));
        leftStats.AddChild(LookChrome.Stat("Sprint", rider.Sprint));
        leftStats.AddChild(LookChrome.Stat("Bruk", rider.Cobbles));
        VBoxContainer rightStats = new();
        rightStats.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        rightStats.AddThemeConstantOverride("separation", 7);
        rightStats.AddChild(LookChrome.Stat("Pagórki", rider.Hills));
        rightStats.AddChild(LookChrome.Stat("TT", rider.TimeTrial));
        rightStats.AddChild(LookChrome.Stat("Płaskie", rider.Flat));
        stats.AddChild(leftStats);
        stats.AddChild(rightStats);
        box.AddChild(stats);

        VBoxContainer contractBody = new();
        contractBody.AddThemeConstantOverride("separation", 6);
        contractBody.AddChild(LookChrome.Kv("Pensja / rok", CareerLookCatalog.Euro(rider.AnnualWage)));
        contractBody.AddChild(LookChrome.Kv(
            "Koniec kontraktu",
            CareerCalendarDates.FormatLong(rider.ContractEndDay)));
        box.AddChild(LookChrome.ContractFrame("KONTRAKT", contractBody));

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
            Label endPreview = LookChrome.Body(
                CareerCalendarDates.FormatLong((int)endDayBox.Value),
                12,
                LookChrome.Gray,
                bold: true);
            endDayBox.ValueChanged += number =>
            {
                endPreview.Text = CareerCalendarDates.FormatLong((int)number);
            };
            box.AddChild(Labeled("Koniec kontraktu", endDayBox));
            box.AddChild(endPreview);
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
        box.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        IReadOnlyList<ClubRosterEntry>? roster = host!.ClubRoster?.Riders;
        if (roster is not { Count: > 0 })
        {
            box.AddChild(LookChrome.Body("Brak składu ze świata.", 13, LookChrome.Gray));
            return box;
        }

        ClubRosterEntry[] sorted = SortSquad(roster);
        int selectedIndex = Array.FindIndex(
            sorted,
            entry => entry.RiderCareerId.Value == selectedRiderId);
        if (selectedIndex < 0)
        {
            selectedIndex = 0;
        }

        TableColumn[] columns =
        [
            new("Zawodnik", "last", TableAlign.Left, false, 0, true),
            new("Styl", "style", TableAlign.Center, false, 74),
            new("Gwiazdki", "stars", TableAlign.Center, false, 82),
            new("OVR", "ovr", TableAlign.Center, true, 46),
            new("POT", "pot", TableAlign.Center, false, 46),
            new("Zmęcz.", "fatigue", TableAlign.Center, false, 56),
            new("Dni", "days", TableAlign.Center, false, 42),
            new("Góry", "climb", TableAlign.Center, false, 46),
            new("Sprint", "sprint", TableAlign.Center, false, 46),
            new("TT", "tt", TableAlign.Center, false, 44),
            new("Pensja", "wage", TableAlign.Right, false, 86),
        ];
        List<TableRow> rows = new(sorted.Length);
        foreach (ClubRosterEntry rider in sorted)
        {
            string natAge = "";
            if (!string.IsNullOrWhiteSpace(rider.Nationality) || rider.Age.HasValue)
            {
                string n = rider.Nationality?.ToUpperInvariant() ?? "";
                string a = rider.Age.HasValue ? $"{rider.Age}L" : "";
                natAge = (!string.IsNullOrEmpty(n) && !string.IsNullOrEmpty(a)) ? $"{n} · {a}" : $"{n}{a}";
            }
            rows.Add(new TableRow(
            [
                new TableCell(rider.Name, natAge),
                new TableCell(rider.StyleLabel),
                new TableCell(rider.StarsDisplay),
                new TableCell(rider.Ovr.ToString(CultureInfo.InvariantCulture)),
                new TableCell(rider.PotentialOvr.ToString(CultureInfo.InvariantCulture)),
                new TableCell($"{rider.SeasonalFatiguePercent}%"),
                new TableCell(rider.SeasonRaceDaysCount.ToString(CultureInfo.InvariantCulture)),
                new TableCell(rider.Climb.ToString(CultureInfo.InvariantCulture)),
                new TableCell(rider.Sprint.ToString(CultureInfo.InvariantCulture)),
                new TableCell(rider.TimeTrial.ToString(CultureInfo.InvariantCulture)),
                new TableCell(CareerLookCatalog.Euro(rider.AnnualWage)),
            ]));
        }

        ScrollContainer table = LookChrome.Table(
            columns,
            rows,
            selectedIndex,
            squadSort.Key,
            squadSort.Dir,
            key =>
            {
                int fresh = key is "last" ? 1 : -1;
                squadSort = CareerLookCatalog.Toggle(squadSort, key, fresh);
                RebuildContent();
            },
            index =>
            {
                ClubRosterEntry captured = sorted[index];
                if (negotiating || host!.ContractNegotiation is not null)
                {
                    host.CancelContractNegotiation();
                    negotiating = false;
                }

                selectedRiderId = captured.RiderCareerId.Value;
                RebuildContent();
            });
        box.AddChild(table);
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
        foreach (string dow in new[] { "PON", "WT", "ŚR", "CZW", "PT", "SOB", "NIE" })
        {
            LookEqualCell slot = new(LookEqualCell.HeadHeight);
            PanelContainer bar = new();
            bar.AddThemeStyleboxOverride("panel", new StyleBoxFlat
            {
                BgColor = LookChrome.Black,
                ContentMarginTop = 6,
                ContentMarginBottom = 6,
            });
            Label label = LookChrome.Meta(dow, 10, LookChrome.Paper);
            label.HorizontalAlignment = HorizontalAlignment.Center;
            label.VerticalAlignment = VerticalAlignment.Center;
            label.SetAnchorsPreset(LayoutPreset.FullRect);
            bar.AddChild(label);
            slot.AddChild(bar);
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
        bool worldTour = host!.IsWorldTourWorld;
        StyleBoxFlat cellStyle = new()
        {
            BgColor = outsideMonth ? LookChrome.Hair : LookChrome.Paper,
            BorderColor = isToday ? LookChrome.Team : LookChrome.Black,
            BorderWidthLeft = isToday ? 3 : 1,
            BorderWidthTop = isToday ? 3 : 1,
            BorderWidthRight = isToday ? 3 : 1,
            BorderWidthBottom = isToday ? 3 : 1,
            ContentMarginLeft = 0,
            ContentMarginRight = 0,
            ContentMarginTop = 0,
            ContentMarginBottom = 0,
        };
        PanelContainer panel = new();
        panel.AddThemeStyleboxOverride("panel", cellStyle);
        if (outsideMonth)
        {
            panel.Modulate = new Color(1, 1, 1, 0.55f);
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
        Color numColor = outsideMonth ? LookChrome.Gray : LookChrome.Black;
        Label number = LookChrome.Display(day.ToString(CultureInfo.InvariantCulture), 12, numColor);
        number.ClipText = true;
        inner.AddChild(number);
        if (eventStart is not null)
        {
            string category = worldTour ? "WORLDTOUR" : eventStart.StageCount > 1 ? $"{eventStart.StageCount} ETAPÓW" : "JEDNODNIOWY";
            ColorRect chip = LookChrome.Block(LookChrome.Team);
            chip.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            chip.SizeFlagsVertical = SizeFlags.ExpandFill;
            chip.MouseFilter = MouseFilterEnum.Ignore;
            VBoxContainer ev = new();
            ev.SetAnchorsPreset(LayoutPreset.FullRect);
            ev.OffsetLeft = 4;
            ev.OffsetTop = 3;
            ev.OffsetRight = -4;
            ev.OffsetBottom = -3;
            ev.AddThemeConstantOverride("separation", 1);
            ev.MouseFilter = MouseFilterEnum.Ignore;
            Label name = LookChrome.Body(eventStart.Name, 10, LookChrome.Paper, bold: true);
            name.AutowrapMode = TextServer.AutowrapMode.Arbitrary;
            name.ClipText = true;
            name.MouseFilter = MouseFilterEnum.Ignore;
            Label cat = LookChrome.Meta(category, 9, LookChrome.Paper);
            cat.ClipText = true;
            cat.MouseFilter = MouseFilterEnum.Ignore;
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
        _ = selected;
        return slot;
    }

    private VBoxContainer BuildCalendarEventDetail()
    {
        return BuildEventDetailPanel(View.Calendar);
    }

    private void BuildRaceResults()
    {
        RaceResultProjection? result = host!.Result;
        if (result is null)
        {
            content!.AddChild(Panel("WYNIK", LookChrome.Body("Brak wyniku wyścigu.", 13, LookChrome.Gray)));
            return;
        }

        string? employer = host.EmployerName;
        TableColumn[] columns =
        [
            new("#", "place", TableAlign.Center, true, 40, DisplayFont: true),
            new("Zawodnik", "rider", TableAlign.Left, false, 0, true),
            new("Czas / Strata", "time", TableAlign.Right, false, 110),
        ];
        List<TableRow> rows = new(result.FinishOrder.Count);
        foreach (RaceResultPlacement row in result.FinishOrder)
        {
            string timeText = "—";
            if (row.Place == 1 && row.FinishTimeSeconds.HasValue)
            {
                timeText = RaceOutcomeQueries.FormatCyclingClock(row.FinishTimeSeconds.Value);
            }
            else if (row.GapSeconds.HasValue)
            {
                string gapText = RaceOutcomeQueries.FormatCyclingGap(row.GapSeconds);
                if (!string.IsNullOrEmpty(gapText))
                {
                    timeText = gapText;
                }
            }

            rows.Add(new TableRow(
            [
                new TableCell(row.Place.ToString(CultureInfo.InvariantCulture)),
                new TableCell(host.RiderDisplayName(row.RiderId), row.OrganizationName),
                new TableCell(timeText),
            ]));
        }

        ScrollContainer table = LookChrome.Table(
            columns,
            rows,
            -1,
            "place",
            1,
            null,
            null,
            index =>
            {
                RaceResultPlacement row = result.FinishOrder[index];
                return !string.IsNullOrWhiteSpace(employer) &&
                    string.Equals(row.OrganizationName, employer, StringComparison.OrdinalIgnoreCase);
            });

        VBoxContainer body = new();
        body.SizeFlagsVertical = SizeFlags.ExpandFill;
        body.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        table.SizeFlagsVertical = SizeFlags.ExpandFill;
        body.AddChild(table);
        string title = string.Create(CultureInfo.InvariantCulture, $"WYNIK · {result.Title}");
        VBoxContainer panel = Panel(title, body, "ZAMKNIJ ›", () => Apply(host.ContinueOutcome()), expandVertical: true);
        panel.SizeFlagsVertical = SizeFlags.ExpandFill;
        content!.AddChild(panel);
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
        if (item.ElevationSparkline is { Count: >= 2 })
        {
            LookSparkline sparkline = new();
            sparkline.SetHeights(item.ElevationSparkline.ToArray());
            header.AddChild(sparkline);
        }
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
        SponsorOverviewProjection? sponsor = host!.Sponsors;
        HBoxContainer grid = Row();
        VBoxContainer agreementPanel = new();
        agreementPanel.AddThemeConstantOverride("separation", 8);

        if (sponsor is not null && !string.IsNullOrWhiteSpace(sponsor.SponsorName))
        {
            agreementPanel.AddChild(LookChrome.Display(sponsor.SponsorName.ToUpperInvariant(), 22, LookChrome.Black));
            agreementPanel.AddChild(LookChrome.Body(sponsor.Tier, 13, LookChrome.Gray, bold: true));
            agreementPanel.AddChild(LookChrome.Kv("Wkład roczny", CareerLookCatalog.Euro(sponsor.AnnualFeeEur) + " / rok"));
            agreementPanel.AddChild(LookChrome.Kv("Ważność umowy", $"do końca sezonu {sponsor.EndSeasonYear}"));
            agreementPanel.AddChild(LookChrome.Hairline());

            Color trustColor = sponsor.TrustPercent >= 70
                ? LookChrome.Team
                : (sponsor.TrustPercent >= 40 ? LookChrome.Black : LookChrome.Red);
            agreementPanel.AddChild(LookChrome.Kv("Zaufanie zarządu", $"{sponsor.TrustPercent}%"));
            agreementPanel.AddChild(LookChrome.Body(sponsor.TrustDescription, 12, trustColor, bold: true));
            agreementPanel.AddChild(LookChrome.Hairline());

            agreementPanel.AddChild(LookChrome.Solid("Przedłuż umowę (+2 lata)", () =>
            {
                CommandResult res = host.ExtendSponsorAgreement(2);
                ShowToast(res.Succeeded ? "Przedłużono umowę sponsorską o 2 lata!" : Reason(res.ReasonCode));
                Refresh();
            }, LookChrome.Team, LookChrome.TeamOn, compact: true));
        }
        else
        {
            agreementPanel.AddChild(LookChrome.Body("Brak aktywnego sponsora tytularnego.", 13, LookChrome.Gray));
        }

        VBoxContainer goals = new();
        goals.AddThemeConstantOverride("separation", 8);
        goals.AddChild(LookChrome.Body("Cele sezonowe wyznaczone przez zarząd. Realizacja podnosi zaufanie i przynosi premie finansowe.", 11, LookChrome.Gray));

        if (sponsor?.Objectives is { Count: > 0 } objectives)
        {
            foreach (BoardObjectiveProjection obj in objectives)
            {
                VBoxContainer card = new();
                card.AddThemeConstantOverride("separation", 4);
                HBoxContainer row = new();
                Label title = LookChrome.Body(obj.Title, 13, LookChrome.Black, bold: true);
                title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                title.AutowrapMode = TextServer.AutowrapMode.WordSmart;
                row.AddChild(title);
                row.AddChild(LookChrome.Chip(obj.IsCompleted ? "ZREALIZOWANY" : "W TOKU", obj.IsCompleted ? "ok" : string.Empty));
                card.AddChild(row);
                card.AddChild(LookChrome.Body(
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"Postęp: {obj.ProgressDisplay} · Premia: +{CareerLookCatalog.Euro(obj.BonusRewardEur)} · Zaufanie: +{obj.TrustImpactPercent}%"),
                    11,
                    obj.IsCompleted ? LookChrome.Team : LookChrome.Gray));
                goals.AddChild(WrapCard(card));
            }
        }
        else
        {
            goals.AddChild(LookChrome.Body("Brak zdefiniowanych celów zarządu na bieżący sezon.", 13, LookChrome.Gray));
        }

        VBoxContainer marketPanel = new();
        marketPanel.AddThemeConstantOverride("separation", 8);
        marketPanel.AddChild(LookChrome.Body("Oferty sponsorów zainteresowanych wejściem do kolarstwa.", 11, LookChrome.Gray));

        if (sponsor?.MarketOffers is { Count: > 0 } offers)
        {
            foreach (SponsorMarketOfferProjection offer in offers)
            {
                VBoxContainer card = new();
                card.AddThemeConstantOverride("separation", 3);
                card.AddChild(LookChrome.Body(offer.SponsorName, 14, LookChrome.Black, bold: true));
                card.AddChild(LookChrome.Body($"{offer.Tier} · {CareerLookCatalog.Euro(offer.ProposedFeeEur)} / rok · umowa {offer.ContractYears} lata", 12, LookChrome.Gray));
                if (offer.ProposedGoals.Count > 0)
                {
                    card.AddChild(LookChrome.Body("Wymagania: " + string.Join(", ", offer.ProposedGoals), 11, LookChrome.Gray));
                }
                marketPanel.AddChild(WrapCard(card));
            }
        }
        else
        {
            marketPanel.AddChild(LookChrome.Body("Brak alternatywnych ofert na rynku sponsorskim.", 13, LookChrome.Gray));
        }

        grid.AddChild(Stretch(Panel("SPONSOR TYTULARNY", agreementPanel), 4));
        grid.AddChild(Stretch(Panel("CELE ZARZĄDU", goals), 5));
        grid.AddChild(Stretch(Panel("RYNEK SPONSORSKI", marketPanel), 3));
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
        VBoxContainer budget = new();
        budget.AddThemeConstantOverride("separation", 8);
        budget.AddChild(LookChrome.Display(CareerLookCatalog.SignedEuro(finance.CashEur), 34, LookChrome.Black));
        budget.AddChild(LookChrome.Meta("KASA KLUBU · 2026", 10, LookChrome.Gray));
        budget.AddChild(LookChrome.Hairline());
        budget.AddChild(LookChrome.Kv(
            "Sponsor tytularny",
            string.Create(
                CultureInfo.InvariantCulture,
                $"{finance.TitleSponsorName} · {CareerLookCatalog.Euro(finance.TitleSponsorAnnualFeeEur)} / rok")));
        budget.AddChild(LookChrome.Kv("Pensje składu / rok", CareerLookCatalog.Euro(finance.WageBillAnnual)));
        if (finance.Overdrawn)
        {
            budget.AddChild(LookChrome.Body("Klub jest na debecie", 13, LookChrome.Red, bold: true));
        }

        VBoxContainer daily = new();
        daily.AddThemeConstantOverride("separation", 6);
        daily.AddChild(LookChrome.SignedKv("Sponsor / dzień", finance.DailySponsor));
        daily.AddChild(LookChrome.SignedKv("Płace / dzień", -finance.DailyWages));
        daily.AddChild(LookChrome.Hairline());
        Label balance = LookChrome.Body(CareerLookCatalog.SignedEuro(finance.DailyNet), 14, LookChrome.Black, bold: true);
        HBoxContainer balanceRow = new();
        balanceRow.AddThemeConstantOverride("separation", 10);
        Label balanceLabel = LookChrome.Meta("Bilans dnia", 10, LookChrome.Gray);
        balanceLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        balance.HorizontalAlignment = HorizontalAlignment.Right;
        balanceRow.AddChild(balanceLabel);
        balanceRow.AddChild(balance);
        daily.AddChild(balanceRow);

        top.AddChild(Stretch(Panel("BUDŻET", budget), 6));
        top.AddChild(Stretch(Panel("KASA DNIA", daily), 6));
        content!.AddChild(top);

        VBoxContainer ledger = new();
        ledger.AddChild(LookChrome.Body(
            "Księga operacji pojawi się po pierwszych dniach sezonu.",
            13,
            LookChrome.Gray,
            bold: true));
        content!.AddChild(Panel("KSIĘGA OPERACJI", ledger));
    }

    private void BuildScouting()
    {
        VBoxContainer form = new();
        form.AddThemeConstantOverride("separation", 8);

        List<MarketRiderProjection> candidates = host!.MarketRiders
            .Where(r => r.ScoutingLevel != "Pełny")
            .Take(40)
            .ToList();

        OptionButton riderSelect = new();
        if (candidates.Count > 0)
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                MarketRiderProjection r = candidates[i];
                riderSelect.AddItem($"{r.Name} ({r.OrganizationName} · {r.StyleLabel})", i);
            }
        }
        else
        {
            riderSelect.AddItem("Wszyscy kolarze zbadani", 0);
        }

        OptionButton scout = new();
        scout.AddItem("Główny Skaut (ocena 88)", 1);
        scout.AddItem("Zwiadowca Regionalny (ocena 78)", 2);
        scout.AddItem("Trener Młodzieży (ocena 72)", 3);

        OptionButton days = new();
        days.AddItem("7 dni (zwiad wstępny)", 7);
        days.AddItem("14 dni (obserwacja głęboka)", 14);
        days.AddItem("21 dni (pełna analiza)", 21);
        days.Selected = 0;

        form.AddChild(Labeled("Kolarz", riderSelect));
        form.AddChild(Labeled("Zwiadowca", scout));
        form.AddChild(Labeled("Długość", days));
        form.AddChild(LookChrome.Solid("Wyślij skauta", () =>
        {
            if (candidates.Count == 0)
            {
                ShowToast("Brak kolarzy do zbadania.");
                return;
            }

            int selectedIdx = riderSelect.Selected;
            if (selectedIdx >= 0 && selectedIdx < candidates.Count)
            {
                MarketRiderProjection target = candidates[selectedIdx];
                string scoutName = scout.GetItemText(scout.Selected).Split('(')[0].Trim();
                int dur = days.GetSelectedId();
                CommandResult res = host.StartScoutingMission(target.RiderCareerId, scoutName, dur);
                ShowToast(res.Succeeded ? $"Wysłano zwiadowcę ({scoutName}) na kolarza {target.Name}!" : Reason(res.ReasonCode));
                Refresh();
            }
        }, LookChrome.Team, LookChrome.TeamOn, compact: true));

        VBoxContainer missions = new();
        missions.AddThemeConstantOverride("separation", 8);
        IReadOnlyList<ScoutMissionProjection>? activeMissions = host.Scouting?.ActiveMissions;
        if (activeMissions is { Count: > 0 })
        {
            foreach (ScoutMissionProjection mission in activeMissions)
            {
                int done = (int)Math.Round((1.0 - (mission.DaysRemaining / (double)Math.Max(1, mission.DurationDays))) * 100);
                VBoxContainer card = new();
                card.AddChild(LookChrome.Body($"{mission.TargetRiderName} · {mission.ScoutName}", 14, LookChrome.Black, bold: true));
                card.AddChild(LookChrome.Body(
                    string.Create(CultureInfo.InvariantCulture, $"Pozostało {mission.DaysRemaining} dni z {mission.DurationDays} ({done}% zaawansowania) · {mission.StatusLabel}"),
                    12,
                    LookChrome.Gray));
                missions.AddChild(WrapCard(card));
            }
        }
        else
        {
            missions.AddChild(LookChrome.Body("Brak aktywnych misji skautowych w terenie.", 13, LookChrome.Gray));
        }

        VBoxContainer reports = new();
        reports.AddThemeConstantOverride("separation", 8);
        IReadOnlyList<ScoutingReportProjection>? discovered = host.Scouting?.DiscoveredRiders;
        if (discovered is { Count: > 0 })
        {
            if (selectedScoutReportRiderId == 0 || discovered.All(r => r.RiderCareerId.Value != selectedScoutReportRiderId))
            {
                selectedScoutReportRiderId = discovered[0].RiderCareerId.Value;
            }

            foreach (ScoutingReportProjection report in discovered)
            {
                ScoutingReportProjection captured = report;
                bool selected = captured.RiderCareerId.Value == selectedScoutReportRiderId;
                Color fg = selected ? LookChrome.Paper : LookChrome.Black;
                PanelContainer row = LookChrome.ClickRow(selected, () =>
                {
                    selectedScoutReportRiderId = captured.RiderCareerId.Value;
                    RebuildContent();
                });
                VBoxContainer inner = new();
                inner.AddChild(LookChrome.Body(captured.RiderName, 14, fg, bold: true));
                string agePart = captured.Age.HasValue ? $"{captured.Age} lat · " : "";
                inner.AddChild(LookChrome.Body($"{captured.Country} · {agePart}{captured.Style} ({captured.StarsDisplay})", 12, selected ? LookChrome.Hair : LookChrome.Gray));
                row.AddChild(inner);
                reports.AddChild(row);
            }
        }
        else
        {
            reports.AddChild(LookChrome.Body("Brak zbadanych zawodników. Wyślij skauta, aby odblokować raporty.", 13, LookChrome.Gray));
        }

        HBoxContainer grid = Row();
        grid.AddChild(Stretch(Panel("NOWA MISJA", form), 6));
        grid.AddChild(Stretch(Panel("AKTYWNE MISJE SKAUTÓW", missions), 6));
        content!.AddChild(grid);

        HBoxContainer lower = Row();
        lower.AddChild(Stretch(Panel("ZBADANI ZAWODNICY", reports), 5));
        lower.AddChild(Stretch(Panel("SZCZEGÓŁY RAPORTU", BuildReportDetail()), 7));
        content!.AddChild(lower);
    }

    private VBoxContainer BuildReportDetail()
    {
        VBoxContainer box = new();
        box.AddThemeConstantOverride("separation", 8);

        IReadOnlyList<ScoutingReportProjection>? discovered = host!.Scouting?.DiscoveredRiders;
        ScoutingReportProjection? report = discovered?.FirstOrDefault(r => r.RiderCareerId.Value == selectedScoutReportRiderId);
        if (report is null)
        {
            box.AddChild(LookChrome.Body("Wybierz zbadanego kolarza z listy po lewej.", 13, LookChrome.Gray));
            return box;
        }

        box.AddChild(LookChrome.Display(report.RiderName.ToUpperInvariant(), 20, LookChrome.Black));
        string agePart = report.Age.HasValue ? $"{report.Age} lat · " : "";
        box.AddChild(LookChrome.Body($"{report.Country} · {agePart}Klub: {report.ClubName}", 12, LookChrome.Gray, bold: true));
        box.AddChild(LookChrome.Kv("Styl kolarza", report.Style));
        box.AddChild(LookChrome.Kv("Gwiazdki", report.StarsDisplay));
        box.AddChild(LookChrome.Kv("Ocena OVR", report.Ovr.ToString(CultureInfo.InvariantCulture)));
        box.AddChild(LookChrome.Kv("Poziom rozpoznania", report.LevelLabel));

        HBoxContainer actions = new();
        actions.AddThemeConstantOverride("separation", 8);
        actions.AddChild(LookChrome.Solid("Zobacz na rynku transferowym", () =>
        {
            selectedMarketRiderId = report.RiderCareerId.Value;
            Show(View.Market);
        }, LookChrome.Team, LookChrome.TeamOn, compact: true));

        if (report.LevelLabel != "Pełny")
        {
            actions.AddChild(LookChrome.Solid("Pogłęb zwiad (+7 dni)", () =>
            {
                CommandResult res = host.StartScoutingMission(report.RiderCareerId, "Główny Skaut", 7);
                ShowToast(res.Succeeded ? "Wysłano zwiadowcę na pogłębienie obserwacji!" : Reason(res.ReasonCode));
                Refresh();
            }, LookChrome.Paper, LookChrome.Black, compact: true));
        }

        box.AddChild(actions);
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

        List<string> clubs = new() { "Wszystkie kluby", "Wolni agenci" };
        clubs.AddRange(host!.MarketRiders
            .Select(rider => rider.OrganizationName)
            .Where(name => !string.IsNullOrWhiteSpace(name) && !string.Equals(name, "Wolny agent", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal));
        int selectedClubIndex = 0;
        if (!string.IsNullOrWhiteSpace(marketClubFilter))
        {
            selectedClubIndex = clubs.FindIndex(club => string.Equals(club, marketClubFilter, StringComparison.Ordinal));
            if (selectedClubIndex < 0)
            {
                selectedClubIndex = 0;
            }
        }

        List<string> styles = new() { "Wszystkie style", "GÓRY", "SPRINT", "KLASYKI", "CZASOWIEC", "POMOCNIK" };
        int selectedStyleIndex = 0;
        if (!string.IsNullOrWhiteSpace(marketStyleFilter))
        {
            selectedStyleIndex = styles.FindIndex(s => string.Equals(s, marketStyleFilter, StringComparison.OrdinalIgnoreCase));
            if (selectedStyleIndex < 0)
            {
                selectedStyleIndex = 0;
            }
        }

        List<string> contracts = new() { "Wszystkie umowy", "Wygasające", "Wolni agenci" };
        int selectedContractIndex = 0;
        if (!string.IsNullOrWhiteSpace(marketContractFilter))
        {
            selectedContractIndex = contracts.FindIndex(c => string.Equals(c, marketContractFilter, StringComparison.OrdinalIgnoreCase));
            if (selectedContractIndex < 0)
            {
                selectedContractIndex = 0;
            }
        }

        HBoxContainer filterWrap = new();
        filterWrap.AddThemeConstantOverride("separation", 6);
        filterWrap.AddChild(LookChrome.Meta("Klub", 9, LookChrome.TeamOn));
        OptionButton clubFilter = LookChrome.CompactSelect(
            clubs,
            selectedClubIndex,
            index =>
            {
                marketClubFilter = index <= 0 ? string.Empty : clubs[index];
                RebuildContent();
            });
        filterWrap.AddChild(clubFilter);

        filterWrap.AddChild(LookChrome.Meta("Styl", 9, LookChrome.TeamOn));
        OptionButton styleFilter = LookChrome.CompactSelect(
            styles,
            selectedStyleIndex,
            index =>
            {
                marketStyleFilter = index <= 0 ? string.Empty : styles[index];
                RebuildContent();
            });
        filterWrap.AddChild(styleFilter);

        filterWrap.AddChild(LookChrome.Meta("Umowa", 9, LookChrome.TeamOn));
        OptionButton contractFilter = LookChrome.CompactSelect(
            contracts,
            selectedContractIndex,
            index =>
            {
                marketContractFilter = index <= 0 ? string.Empty : contracts[index];
                RebuildContent();
            });
        filterWrap.AddChild(contractFilter);

        HBoxContainer grid = Row();
        grid.SizeFlagsVertical = SizeFlags.ExpandFill;
        VBoxContainer table = Panel("RYNEK TRANSFEROWY", BuildMarketTable(riders), rightAccessory: filterWrap, expandVertical: true);
        table.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        table.SizeFlagsStretchRatio = 8;
        VBoxContainer marketCard = Panel("ZAWODNIK", BuildMarketCard());
        marketCard.CustomMinimumSize = new Vector2(340, 0);
        marketCard.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        marketCard.SizeFlagsStretchRatio = 4;
        grid.AddChild(table);
        grid.AddChild(marketCard);
        content!.AddChild(grid);
    }

    private VBoxContainer BuildMarketTable(IReadOnlyList<MarketRiderProjection> riders)
    {
        VBoxContainer box = new();
        box.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        if (riders.Count == 0)
        {
            box.AddChild(LookChrome.Body("Brak zawodników na rynku.", 13, LookChrome.Gray));
            return box;
        }

        MarketRiderProjection[] sorted = SortMarket(riders);
        int selectedIndex = Array.FindIndex(
            sorted,
            rider => rider.RiderCareerId.Value == selectedMarketRiderId);
        if (selectedIndex < 0)
        {
            selectedIndex = 0;
        }

        TableColumn[] columns =
        [
            new("Zawodnik", "name", TableAlign.Left, false, 0, true),
            new("Kraj", "nat", TableAlign.Center, false, 46),
            new("Wiek", "age", TableAlign.Center, false, 46),
            new("Styl", "style", TableAlign.Center, false, 82),
            new("Gwiazdki", "stars", TableAlign.Center, true, 105),
            new("OVR", "ovr", TableAlign.Center, false, 46),
            new("Pensja", "wage", TableAlign.Right, false, 86),
            new("Koniec", "end", TableAlign.Right, false, 86),
            new("Skaut", "scout", TableAlign.Center, false, 74),
        ];
        List<TableRow> rows = new(sorted.Length);
        foreach (MarketRiderProjection row in sorted)
        {
            string club = string.IsNullOrWhiteSpace(row.OrganizationName) ? "Wolny agent" : row.OrganizationName;
            string nat = !string.IsNullOrWhiteSpace(row.Nationality) ? row.Nationality.ToUpperInvariant() : "—";
            string age = row.Age.HasValue ? $"{row.Age}" : "—";
            string style = !string.IsNullOrWhiteSpace(row.StyleLabel) ? row.StyleLabel : (!string.IsNullOrWhiteSpace(row.RoleLabel) ? row.RoleLabel : "—");
            string stars = !string.IsNullOrWhiteSpace(row.StarsDisplay) ? row.StarsDisplay : CareerLookCatalog.Stars(3);
            string ovr = row.ScoutingLevel == "Nieznany" ? "—" : row.Ovr.ToString(CultureInfo.InvariantCulture);
            string wage = CareerLookCatalog.Euro(row.AnnualWage);
            string end = row.IsFreeAgent ? "Wolny" : (row.ContractEndDay > 0 ? CareerCalendarDates.FormatLong(row.ContractEndDay) : "—");
            string scout = !string.IsNullOrWhiteSpace(row.ScoutingLevel) ? row.ScoutingLevel : "Nieznany";

            rows.Add(new TableRow(
            [
                new TableCell(row.Name, club),
                new TableCell(nat),
                new TableCell(age),
                new TableCell(style),
                new TableCell(stars),
                new TableCell(ovr),
                new TableCell(wage),
                new TableCell(end),
                new TableCell(scout),
            ]));
        }

        ScrollContainer table = LookChrome.Table(
            columns,
            rows,
            selectedIndex,
            marketSort.Key,
            marketSort.Dir,
            key =>
            {
                int fresh = key is "name" ? 1 : -1;
                marketSort = CareerLookCatalog.Toggle(marketSort, key, fresh);
                RebuildContent();
            },
            index =>
            {
                selectedMarketRiderId = sorted[index].RiderCareerId.Value;
                negotiating = false;
                RebuildContent();
            });
        box.AddChild(table);
        return box;
    }

    private VBoxContainer BuildMarketCard()
    {
        MarketRiderProjection? row = host!.MarketRiders
            .FirstOrDefault(rider => rider.RiderCareerId.Value == selectedMarketRiderId);
        VBoxContainer box = new();
        box.AddThemeConstantOverride("separation", 10);
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

        HBoxContainer head = new();
        head.AddThemeConstantOverride("separation", 12);
        head.AddChild(LookChrome.Avatar(row.Name));
        VBoxContainer names = new();
        names.AddChild(LookChrome.Title(row.Name));
        string natPart = !string.IsNullOrWhiteSpace(row.Nationality) ? $"{row.Nationality.ToUpperInvariant()} · " : "";
        string agePart = row.Age.HasValue ? $"{row.Age} LAT · " : "";
        string ovrDisplay = row.ScoutingLevel == "Nieznany" ? "—" : row.Ovr.ToString(CultureInfo.InvariantCulture);
        string potDisplay = row.ScoutingLevel == "Pełny" ? row.PotentialOvr.ToString(CultureInfo.InvariantCulture) : "—";
        names.AddChild(LookChrome.Body(
            string.Create(CultureInfo.InvariantCulture, $"{natPart}{agePart}OVR {ovrDisplay} · POT {potDisplay}"),
            12,
            LookChrome.Gray,
            bold: true));
        names.AddChild(LookChrome.Kv("Styl / Klasa", $"{row.StyleLabel} ({row.StarsDisplay})"));
        names.AddChild(LookChrome.Kv("Stan zwiadu", row.ScoutingLevel));
        head.AddChild(names);
        box.AddChild(head);

        if (row.ScoutingLevel == "Nieznany")
        {
            VBoxContainer fogBox = new();
            fogBox.AddThemeConstantOverride("separation", 4);
            fogBox.AddChild(LookChrome.Body("Parametry ukryte (mgła wojny).", 12, LookChrome.Black, bold: true));
            fogBox.AddChild(LookChrome.Body("Wyślij skauta, aby poznać dokładne atrybuty fizjologiczne kolarza.", 11, LookChrome.Gray));
            box.AddChild(WrapCard(fogBox));
        }
        else
        {
            GridContainer stats = new() { Columns = 2 };
            stats.AddThemeConstantOverride("h_separation", 12);
            stats.AddThemeConstantOverride("v_separation", 7);
            stats.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            VBoxContainer leftStats = new();
            leftStats.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            leftStats.AddThemeConstantOverride("separation", 7);
            leftStats.AddChild(LookChrome.Stat("Góry", row.Climb));
            leftStats.AddChild(LookChrome.Stat("Sprint", row.Sprint));
            leftStats.AddChild(LookChrome.Stat("Bruk", row.Cobbles));
            VBoxContainer rightStats = new();
            rightStats.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            rightStats.AddThemeConstantOverride("separation", 7);
            rightStats.AddChild(LookChrome.Stat("Pagórki", row.Hills));
            rightStats.AddChild(LookChrome.Stat("TT", row.TimeTrial));
            rightStats.AddChild(LookChrome.Stat("Płaskie", row.Flat));
            stats.AddChild(leftStats);
            stats.AddChild(rightStats);
            box.AddChild(stats);
        }

        VBoxContainer transferBody = new();
        transferBody.AddThemeConstantOverride("separation", 6);
        transferBody.AddChild(LookChrome.Kv(
            "Klub",
            string.IsNullOrWhiteSpace(row.OrganizationName) ? "—" : row.OrganizationName));
        transferBody.AddChild(LookChrome.Kv("Pensja / rok", CareerLookCatalog.Euro(row.AnnualWage)));
        transferBody.AddChild(LookChrome.Kv(
            "Koniec kontraktu",
            row.ContractEndDay > 0 ? CareerCalendarDates.FormatLong(row.ContractEndDay) : (row.IsFreeAgent ? "Wolny agent" : "—")));
        box.AddChild(LookChrome.ContractFrame("SYTUACJA TRANSFEROWA", transferBody));

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
            Label endPreview = LookChrome.Body(
                CareerCalendarDates.FormatLong((int)endDayBox.Value),
                12,
                LookChrome.Gray,
                bold: true);
            endDayBox.ValueChanged += number =>
            {
                endPreview.Text = CareerCalendarDates.FormatLong((int)number);
            };
            box.AddChild(Labeled("Koniec kontraktu", endDayBox));
            box.AddChild(endPreview);
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

        if (row.ScoutingLevel != "Pełny")
        {
            actions.AddChild(LookChrome.Solid("Wyślij skauta (7 dni)", () =>
            {
                CommandResult scoutRes = host.StartScoutingMission(row.RiderCareerId, "Główny Skaut", 7);
                ShowToast(scoutRes.Succeeded ? "Wysłano skauta na zwiad!" : Reason(scoutRes.ReasonCode));
                Refresh();
            }, LookChrome.Paper, LookChrome.Black, compact: true));
        }

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

        if (selectedEventId is not null &&
            host.SeasonEvents.Any(item => item.RaceContentId == selectedEventId))
        {
            return;
        }

        if (host.UpcomingEvents.Count > 0)
        {
            selectedEventId = host.UpcomingEvents[0].RaceContentId;
            return;
        }

        if (host.SeasonEvents.Count > 0)
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
            "style" => roster.OrderBy(rider => rider.StyleLabel, StringComparer.Ordinal),
            "stars" => roster.OrderBy(rider => rider.Stars),
            "fatigue" => roster.OrderBy(rider => rider.SeasonalFatiguePercent),
            "days" => roster.OrderBy(rider => rider.SeasonRaceDaysCount),
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

        IEnumerable<MarketRiderProjection> query = host.MarketRiders;

        if (!string.IsNullOrWhiteSpace(marketClubFilter))
        {
            if (string.Equals(marketClubFilter, "Wolni agenci", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(rider => rider.IsFreeAgent);
            }
            else
            {
                query = query.Where(rider => string.Equals(rider.OrganizationName, marketClubFilter, StringComparison.Ordinal));
            }
        }

        if (!string.IsNullOrWhiteSpace(marketStyleFilter) && !string.Equals(marketStyleFilter, "Wszystkie style", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(rider => string.Equals(rider.StyleLabel, marketStyleFilter, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(marketContractFilter) && !string.Equals(marketContractFilter, "Wszystkie kontrakty", StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(marketContractFilter, "Wygasające", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(rider => rider.IsExpiringThisYear);
            }
            else if (string.Equals(marketContractFilter, "Wolni agenci", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(rider => rider.IsFreeAgent);
            }
        }

        return query.ToArray();
    }

    private MarketRiderProjection[] SortMarket(IReadOnlyList<MarketRiderProjection> riders)
    {
        IEnumerable<MarketRiderProjection> ordered = marketSort.Key switch
        {
            "club" => riders.OrderBy(rider => rider.OrganizationName, StringComparer.Ordinal),
            "nat" => riders.OrderBy(rider => rider.Nationality ?? string.Empty, StringComparer.Ordinal),
            "age" => riders.OrderBy(rider => rider.Age ?? 99),
            "style" => riders.OrderBy(rider => rider.StyleLabel, StringComparer.Ordinal),
            "stars" => riders.OrderBy(rider => rider.Stars),
            "ovr" => riders.OrderBy(rider => rider.Ovr),
            "pot" => riders.OrderBy(rider => rider.PotentialOvr),
            "climb" => riders.OrderBy(rider => rider.Climb),
            "wage" => riders.OrderBy(rider => rider.AnnualWage),
            "end" => riders.OrderBy(rider => rider.ContractEndDay),
            "scout" => riders.OrderBy(rider => rider.ScoutingLevel, StringComparer.Ordinal),
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
