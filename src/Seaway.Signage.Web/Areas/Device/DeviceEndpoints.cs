namespace Seaway.Signage.Web.Areas.Device;

/// <summary>
/// Bearer-token gated device API — everything a display or its agent calls. See design doc §3.3
/// for the message set and §6.1 for the auth model. Route bodies are Phase 1 work
/// (IMPLEMENTATION_PLAN.md milestones 12–18); this reserves the shape so it's visible from the
/// start rather than invented ad hoc once coding starts.
/// </summary>
public static class DeviceEndpoints
{
    public static IEndpointRouteBuilder MapDeviceEndpoints(this IEndpointRouteBuilder app)
    {
        // POST /api/enroll
        //   -> creates a Display in Unclaimed state, returns a pairing code. See §4.
        app.MapPost("/enroll", () => Results.StatusCode(StatusCodes.Status501NotImplemented));

        // GET /api/displays/{id}/stream
        //   -> SSE endpoint. Both the browser player and the Go device agent connect here with
        //      the device bearer token, as two independent consumers of the same channel. See
        //      IMPLEMENTATION_PLAN.md §4.1 for the framing/registry design.
        app.MapGet("/displays/{id:guid}/stream", (Guid id) =>
            Results.StatusCode(StatusCodes.Status501NotImplemented));

        // POST /api/displays/{id}/heartbeat
        app.MapPost("/displays/{id:guid}/heartbeat", (Guid id) =>
            Results.StatusCode(StatusCodes.Status501NotImplemented));

        // POST /api/displays/{id}/events
        //   -> errors, command-result acknowledgements (reboot/restart-browser).
        app.MapPost("/displays/{id:guid}/events", (Guid id) =>
            Results.StatusCode(StatusCodes.Status501NotImplemented));

        return app;
    }
}
