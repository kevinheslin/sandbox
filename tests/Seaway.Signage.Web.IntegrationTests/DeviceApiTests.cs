using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Seaway.Signage.Web.IntegrationTests;

// WebApplicationFactory<Program>-based tests: SSE framing/contract, enrollment/claim/token flow,
// Admin CRUD. Real cases land with each Phase 1/2 milestone (IMPLEMENTATION_PLAN.md §6). Listed
// as skip-marked placeholders so the intended coverage is visible from the start.
public class DeviceApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact(Skip = "Phase 1 — device endpoints return 501 until milestone 12, see IMPLEMENTATION_PLAN.md.")]
    public async Task Enroll_creates_an_unclaimed_display_and_returns_a_pairing_code()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsync("/api/enroll", content: null);
        response.EnsureSuccessStatusCode();
    }

    [Fact(Skip = "Phase 1 — SSE stream endpoint not implemented yet, see IMPLEMENTATION_PLAN.md milestone 14.")]
    public void Stream_endpoint_emits_valid_text_event_stream_framing()
    {
    }

    [Fact(Skip = "Phase 1 — SSE stream endpoint not implemented yet, see IMPLEMENTATION_PLAN.md milestone 14.")]
    public void Client_disconnect_deregisters_its_channel_with_no_leak()
    {
    }

    [Fact(Skip = "Phase 2 — group broadcast not implemented yet, see IMPLEMENTATION_PLAN.md milestone 24.")]
    public void Group_broadcast_reaches_every_registered_display_in_that_group()
    {
    }
}
