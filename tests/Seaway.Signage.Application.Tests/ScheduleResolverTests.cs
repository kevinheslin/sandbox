using Xunit;

namespace Seaway.Signage.Application.Tests;

// This is the "precedence algebra nobody wants to debug at 6am" class — design doc §5.1. Real
// test cases land with the implementation, Phase 3 — see IMPLEMENTATION_PLAN.md milestone 31.
public class ScheduleResolverTests
{
    [Fact(Skip = "Phase 3 — ScheduleResolver not implemented yet, see IMPLEMENTATION_PLAN.md milestone 31.")]
    public void Higher_priority_rule_wins_when_two_rules_overlap()
    {
    }

    [Fact(Skip = "Phase 3 — ScheduleResolver not implemented yet, see IMPLEMENTATION_PLAN.md milestone 31.")]
    public void Active_override_always_wins_over_any_schedule_rule()
    {
    }

    [Fact(Skip = "Phase 3 — ScheduleResolver not implemented yet, see IMPLEMENTATION_PLAN.md milestone 31.")]
    public void Expired_override_no_longer_applies()
    {
    }

    [Fact(Skip = "Phase 3 — ScheduleResolver not implemented yet, see IMPLEMENTATION_PLAN.md milestone 31.")]
    public void No_matching_rule_falls_back_to_the_groups_default_playlist()
    {
    }
}
