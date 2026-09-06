using System;
using System.Collections.Generic;

namespace Peloton.Domain;

public enum ScoutingKnowledgeLevel
{
    Unknown = 0,
    Observed = 1,
    Discovered = 2,
}

public sealed class ScoutingMission
{
    public ScoutingMission(
        int id,
        WorldEntityId organizationId,
        WorldEntityId targetRiderCareerId,
        string scoutName,
        int durationDays,
        int daysRemaining,
        bool isCompleted = false)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(scoutName);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(durationDays);
        ArgumentOutOfRangeException.ThrowIfNegative(daysRemaining);

        Id = id;
        OrganizationId = organizationId;
        TargetRiderCareerId = targetRiderCareerId;
        ScoutName = scoutName;
        DurationDays = durationDays;
        DaysRemaining = daysRemaining;
        IsCompleted = isCompleted;
    }

    public int Id { get; }
    public WorldEntityId OrganizationId { get; }
    public WorldEntityId TargetRiderCareerId { get; }
    public string ScoutName { get; }
    public int DurationDays { get; }
    public int DaysRemaining { get; private set; }
    public bool IsCompleted { get; private set; }

    public bool AdvanceOneDay()
    {
        if (IsCompleted)
        {
            return false;
        }

        DaysRemaining = Math.Max(0, DaysRemaining - 1);
        if (DaysRemaining == 0)
        {
            IsCompleted = true;
            return true;
        }

        return false;
    }
}

public sealed class OrganizationScoutingStore
{
    private readonly Dictionary<WorldEntityId, ScoutingKnowledgeLevel> levels = new();
    private readonly List<ScoutingMission> missions = new();

    public ScoutingKnowledgeLevel GetLevel(WorldEntityId riderCareerId) =>
        levels.TryGetValue(riderCareerId, out ScoutingKnowledgeLevel level) ? level : ScoutingKnowledgeLevel.Unknown;

    public void SetLevel(WorldEntityId riderCareerId, ScoutingKnowledgeLevel level) =>
        levels[riderCareerId] = level;

    public IReadOnlyList<ScoutingMission> Missions => missions;

    public void AddMission(ScoutingMission mission)
    {
        ArgumentNullException.ThrowIfNull(mission);
        missions.Add(mission);
    }

    public void AdvanceOneDay(Action<ScoutingMission>? onMissionCompleted = null)
    {
        foreach (ScoutingMission mission in missions)
        {
            if (mission.AdvanceOneDay())
            {
                ScoutingKnowledgeLevel current = GetLevel(mission.TargetRiderCareerId);
                ScoutingKnowledgeLevel next = current switch
                {
                    ScoutingKnowledgeLevel.Unknown => ScoutingKnowledgeLevel.Observed,
                    _ => ScoutingKnowledgeLevel.Discovered,
                };
                SetLevel(mission.TargetRiderCareerId, next);
                onMissionCompleted?.Invoke(mission);
            }
        }
    }
}
