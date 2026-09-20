using Seaway.Signage.Domain;
using Xunit;

namespace Seaway.Signage.Domain.Tests;

// Domain is POCO-only, so it doesn't have much to unit test on its own yet — real coverage
// belongs to RotationEngine/ScheduleResolver in Seaway.Signage.Application.Tests, once those are
// implemented (Phase 1/3). This smoke test just proves the test project itself is wired up.
public class PlaceholderTests
{
    [Fact]
    public void Display_can_be_constructed_with_defaults()
    {
        var display = new Display { Id = Guid.NewGuid(), Name = "Test Display" };

        Assert.Equal(DisplayStatus.Unclaimed, display.Status);
        Assert.Equal(DisplayOrientation.Landscape, display.Orientation);
    }
}
