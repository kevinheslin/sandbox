namespace Seaway.Signage.Web.Streaming;

/// <summary>
/// Writes a single SseMessage as standard `text/event-stream` framing. Kept separate from
/// DisplayStreamRegistry so the wire format can be unit tested independently of the connection
/// registry. See IMPLEMENTATION_PLAN.md §4.1.
/// </summary>
public static class SseWriter
{
    public static async Task WriteAsync(HttpResponse response, SseMessage message, CancellationToken ct)
    {
        await response.WriteAsync($"event: {message.EventType}\n", ct);
        await response.WriteAsync($"data: {message.DataJson}\n", ct);
        await response.WriteAsync($"id: {message.SequenceId}\n\n", ct);
        await response.Body.FlushAsync(ct);
    }

    /// <summary>A `:\n\n` comment-line keepalive, sent independently of app messages every ~15s
    /// to defeat reverse-proxy idle timeouts. See design doc §3.3.</summary>
    public static async Task WriteKeepAliveAsync(HttpResponse response, CancellationToken ct)
    {
        await response.WriteAsync(":\n\n", ct);
        await response.Body.FlushAsync(ct);
    }
}
