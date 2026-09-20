using Seaway.Signage.Web.Services;
using Xunit;

namespace Seaway.Signage.Application.Tests;

// Real test cases land with the RotationEngine implementation — see IMPLEMENTATION_PLAN.md
// milestone 15 (Phase 1). Listed here as Skip-marked placeholders so the checklist of what
// "done" needs to cover is visible before any of it is written.
public class RotationEngineTests
{
    [Fact(Skip = "Phase 1 — RotationEngine not implemented yet, see IMPLEMENTATION_PLAN.md milestone 15.")]
    public void Advances_to_next_item_once_DurationSeconds_elapses()
    {
    }

    [Fact(Skip = "Phase 1 — RotationEngine not implemented yet, see IMPLEMENTATION_PLAN.md milestone 15.")]
    public void Warm_set_is_capped_at_the_configured_maximum()
    {
    }

    [Fact(Skip = "Phase 1 — RotationEngine not implemented yet, see IMPLEMENTATION_PLAN.md milestone 15.")]
    public void Warm_set_evicts_least_recently_shown_item_first()
    {
    }
}
