namespace Seaway.Signage.Domain;

/// <summary>
/// A set of displays showing identical rotating content. A standalone display is simply a group
/// of one in practice — see design doc §5.1's deliberate-simplifications note.
/// </summary>
public class DisplayGroup
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Reserved for multi-site signage; single-site deployments can leave this null.</summary>
    public Guid? SiteId { get; set; }
}
