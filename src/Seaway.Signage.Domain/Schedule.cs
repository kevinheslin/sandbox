namespace Seaway.Signage.Domain;

/// <summary>
/// A group-level schedule (a standalone display's schedule lives on its own group-of-one — see
/// design doc §5.1). Holds ScheduleRules; ScheduleResolver picks the active one at any instant.
/// </summary>
public class Schedule
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// One day/time window within a Schedule. ScheduleResolver (Seaway.Signage.Web/Services) resolves
/// which rule is active, and Override always wins over any rule — see design doc §5.1.
/// </summary>
public class ScheduleRule
{
    public Guid Id { get; set; }
    public Guid ScheduleId { get; set; }
    public Guid PlaylistId { get; set; }

    /// <summary>Bitmask, bit 0 = Sunday .. bit 6 = Saturday.</summary>
    public byte DayMask { get; set; }

    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    /// <summary>Higher wins when multiple rules overlap for the same instant.</summary>
    public int Priority { get; set; }
}
