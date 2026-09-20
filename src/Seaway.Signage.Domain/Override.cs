namespace Seaway.Signage.Domain;

/// <summary>
/// A single-action, expiring content push that takes precedence over the schedule — e.g. "put the
/// safety notice on every floor screen, now." Not a rotation edit. See design doc §5.1.
/// </summary>
public class Override
{
    public Guid Id { get; set; }

    /// <summary>Exactly one of TargetGroupId / TargetDisplayId is set.</summary>
    public Guid? TargetGroupId { get; set; }
    public Guid? TargetDisplayId { get; set; }

    public Guid ContentId { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}
