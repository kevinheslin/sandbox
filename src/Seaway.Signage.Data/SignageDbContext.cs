using Microsoft.EntityFrameworkCore;
using Seaway.Signage.Domain;

namespace Seaway.Signage.Data;

/// <summary>
/// EF Core mapping of the design doc's §5 data model. No migrations exist yet — the first
/// migration should wait until the database platform (§4.4 of IMPLEMENTATION_PLAN.md) is
/// confirmed, so it doesn't need redoing against a different provider.
/// </summary>
public class SignageDbContext(DbContextOptions<SignageDbContext> options) : DbContext(options)
{
    public DbSet<Display> Displays => Set<Display>();
    public DbSet<DisplayGroup> DisplayGroups => Set<DisplayGroup>();
    public DbSet<Content> Content => Set<Content>();
    public DbSet<Playlist> Playlists => Set<Playlist>();
    public DbSet<PlaylistItem> PlaylistItems => Set<PlaylistItem>();
    public DbSet<Schedule> Schedules => Set<Schedule>();
    public DbSet<ScheduleRule> ScheduleRules => Set<ScheduleRule>();
    public DbSet<Override> Overrides => Set<Override>();
    public DbSet<DisplayEvent> DisplayEvents => Set<DisplayEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Deliberately empty for now — index/constraint tuning (e.g. a unique index on
        // Display.DeviceTokenHash, Display.PairingCode) belongs in the first real migration once
        // the schema has been exercised by Phase 1's vertical slice, not guessed at here.
    }
}
