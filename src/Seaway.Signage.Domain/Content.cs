namespace Seaway.Signage.Domain;

/// <summary>
/// One addressable, reusable thing that can appear in a rotation. See design doc §3.2 for the
/// four pane kinds and §5 for the field list.
/// </summary>
public class Content
{
    public Guid Id { get; set; }
    public ContentKind Kind { get; set; }

    /// <summary>The iframe/snapshot source URL, when applicable.</summary>
    public string? Url { get; set; }

    /// <summary>Identifies which first-party view renders this content, for Kind == Native.</summary>
    public string? NativeViewKey { get; set; }

    /// <summary>References a Seaway.Storage asset, for Kind == Asset.</summary>
    public Guid? AssetId { get; set; }

    public FramingMode FramingMode { get; set; } = FramingMode.Iframe;

    /// <summary>For Snapshot content: how often the snapshot worker should re-render this. Null
    /// for other kinds.</summary>
    public int? RefreshHintSeconds { get; set; }
}

public enum ContentKind
{
    Iframe,
    Native,
    Snapshot,
    Asset,
}

/// <summary>
/// How this content's pane is rendered client-side. See IMPLEMENTATION_PLAN.md §4.2 — the warm-
/// pane strategy differs by framing mode (stacked iframe crossfade vs. always-warm img/video).
/// </summary>
public enum FramingMode
{
    Iframe,
    Image,
    Video,
}
