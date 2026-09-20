namespace Seaway.Signage.SnapshotWorker;

/// <summary>
/// Per-Content render cadence (Content.RefreshHintSeconds). Renders on a schedule, publishes an
/// image plus render-age metadata for the player to show a staleness badge. See design doc §8 and
/// IMPLEMENTATION_PLAN.md milestone 33 (Phase 3).
/// </summary>
public class SnapshotScheduler(ChromiumRenderer renderer) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Phase 3 work — see IMPLEMENTATION_PLAN.md milestone 33. Left unimplemented rather than
        // looping on nothing, so an accidental deploy of this scaffold fails loudly instead of
        // running a worker that silently does nothing.
        throw new NotImplementedException("SnapshotScheduler — Phase 3, see IMPLEMENTATION_PLAN.md milestone 33.");
    }
}
