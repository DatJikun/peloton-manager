using System;
using System.Text.Json.Serialization;

namespace Peloton.Domain;

public readonly record struct WorldEntityId
{
    [JsonConstructor]
    public WorldEntityId(long value)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);

        Value = value;
    }

    public long Value { get; }
}

public sealed class WorldEntityIdAllocator
{
    public WorldEntityIdAllocator(long highWaterMark = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(highWaterMark);

        HighWaterMark = highWaterMark;
    }

    public long HighWaterMark { get; private set; }

    public WorldEntityId Allocate()
    {
        HighWaterMark = checked(HighWaterMark + 1);
        return new WorldEntityId(HighWaterMark);
    }
}

public readonly record struct WorldDate
{
    [JsonConstructor]
    public WorldDate(int dayNumber)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(dayNumber);

        DayNumber = dayNumber;
    }

    public int DayNumber { get; }

    public WorldDate NextDay() => new(checked(DayNumber + 1));
}
