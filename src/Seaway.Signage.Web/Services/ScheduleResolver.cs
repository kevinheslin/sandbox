using Seaway.Signage.Domain;

namespace Seaway.Signage.Web.Services;

/// <summary>
/// Resolves which Playlist is active for a group at a given instant: day mask / time window /
/// priority precedence among ScheduleRules, with any live Override always winning. This is the
/// "precedence algebra nobody wants to debug at 6am" the design doc's §5.1 warns about — it needs
/// to be pure logic, heavily unit tested, not something reasoned about live in production.
/// See IMPLEMENTATION_PLAN.md milestone 31 (Phase 3).
/// </summary>
public class ScheduleResolver
{
    public Guid ResolveActivePlaylistId(
        IReadOnlyList<ScheduleRule> rules,
        IReadOnlyList<Override> activeOverrides,
        DateTimeOffset atUtc)
    {
        throw new NotImplementedException("ScheduleResolver — Phase 3, see IMPLEMENTATION_PLAN.md milestone 31.");
    }
}
