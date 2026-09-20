namespace Seaway.Signage.Domain;

/// <summary>
/// One physical device (Raspberry Pi + screen). See design doc §5, §6.1.
/// </summary>
public class Display
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }

    /// <summary>Null while the display is standalone (not assigned to a group).</summary>
    public Guid? GroupId { get; set; }

    /// <summary>256-bit device token, hashed at rest. Never store the raw token. See §6.1.</summary>
    public string DeviceTokenHash { get; set; } = string.Empty;

    public DisplayStatus Status { get; set; } = DisplayStatus.Unclaimed;
    public DateTimeOffset? LastSeenUtc { get; set; }

    /// <summary>Short human-readable code shown on an unclaimed display's screen. See §4.</summary>
    public string? PairingCode { get; set; }
    public DateTimeOffset? ClaimedAtUtc { get; set; }

    /// <summary>Used for the re-claim-by-IP flow after a token loss. See §4.1.</summary>
    public string? IpAddress { get; set; }
    public string? MacAddress { get; set; }

    public DisplayOrientation Orientation { get; set; } = DisplayOrientation.Landscape;
}

public enum DisplayStatus
{
    Unclaimed,
    Active,
    Dark, // hasn't heartbeated within the configured window — see Services/DisplayHealthMonitor
}

public enum DisplayOrientation
{
    Landscape,
    Portrait,
}
