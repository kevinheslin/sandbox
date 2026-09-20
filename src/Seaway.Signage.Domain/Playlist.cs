namespace Seaway.Signage.Domain;

/// <summary>The ordered rotation a group (or standalone display) cycles through. See design doc §5.</summary>
public class Playlist
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Bumped on every edit; SSE `assign` messages carry this so the player can tell a
    /// stale cached rotation from the current one. See design doc §3.3.</summary>
    public int Version { get; set; } = 1;
}

/// <summary>One entry in a Playlist's rotation.</summary>
public class PlaylistItem
{
    public Guid Id { get; set; }
    public Guid PlaylistId { get; set; }
    public Guid ContentId { get; set; }

    /// <summary>Position within the playlist; lower sorts first.</summary>
    public int Order { get; set; }

    /// <summary>How long this item stays on screen before the player advances. Drives rotation
    /// timing client-side — see design doc §5.1.</summary>
    public int DurationSeconds { get; set; }
}
