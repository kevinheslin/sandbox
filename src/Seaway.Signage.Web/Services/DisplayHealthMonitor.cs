namespace Seaway.Signage.Web.Services;

/// <summary>
/// Watches DisplayEvent heartbeats and flags a display Dark when it hasn't heartbeated within
/// the configured window, driving the Phase 2 dark-display alert via Seaway.Notifications. Per
/// design doc §9.1, the player heartbeat — not any vendor telemetry — is the primary health
/// signal: it proves the entire chain (network, browser, backend, page actually rendering), not
/// just that the hardware has power. See IMPLEMENTATION_PLAN.md milestones 27–28.
/// </summary>
public class DisplayHealthMonitor(/* Seaway.Signage.Data.SignageDbContext db, notification sender, once wired */)
{
    public Task CheckAndAlertAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException("DisplayHealthMonitor — Phase 2, see IMPLEMENTATION_PLAN.md milestones 27-28.");
    }
}
