using Seaway.Signage.Domain;

namespace Seaway.Signage.Web.Services;

/// <summary>
/// Pure rotation-advance and warm-set-selection logic — deliberately free of EF/ASP.NET
/// dependencies so it can be unit tested without a database. See IMPLEMENTATION_PLAN.md §3
/// (why Domain/RotationEngine stay dependency-free) and §4.2 (the warm-pane strategy this
/// implements: a bounded LRU of 3–4 concurrently-warm panes, not "keep everything warm").
///
/// Logic itself is Phase 1/2 work — see IMPLEMENTATION_PLAN.md milestones 15, 21.
/// </summary>
public class RotationEngine
{
    /// <summary>Given the current position in a playlist and how long it's been shown, returns
    /// the next item to advance to (or the same item, if not yet due).</summary>
    public PlaylistItem? GetCurrentItem(IReadOnlyList<PlaylistItem> items, TimeSpan elapsedInCurrentItem, int currentIndex)
    {
        throw new NotImplementedException("RotationEngine — Phase 1, see IMPLEMENTATION_PLAN.md milestone 15.");
    }

    /// <summary>Selects which ContentIds should be kept "warm" (pre-rendered) given the current
    /// position, capped at the design doc's §2.3 RAM-driven limit.</summary>
    public IReadOnlyList<Guid> GetWarmSet(IReadOnlyList<PlaylistItem> items, int currentIndex, int maxWarm = 4)
    {
        throw new NotImplementedException("RotationEngine — Phase 1, see IMPLEMENTATION_PLAN.md milestone 15.");
    }
}
