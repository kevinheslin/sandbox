namespace Seaway.Signage.Domain;

/// <summary>
/// Heartbeats, health payloads, errors, and device-command acknowledgements — one append-only
/// log per display. See design doc §5, §6.1, §7.1.
/// </summary>
public class DisplayEvent
{
    public long Id { get; set; }
    public Guid DisplayId { get; set; }
    public DisplayEventKind Kind { get; set; }

    /// <summary>Raw JSON payload — shape depends on Kind (heartbeat vs. health vs. command result).</summary>
    public string PayloadJson { get; set; } = string.Empty;

    public DateTimeOffset OccurredAtUtc { get; set; }
}

public enum DisplayEventKind
{
    Heartbeat,
    Health,
    Error,
    CommandResult,
}
