using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Seaway.Signage.Web.Streaming;

/// <summary>
/// In-process registry of live SSE connections, one entry per Display, each holding a list of
/// channels because both the browser player and the Go device agent connect to the same stream
/// endpoint as independent consumers. See IMPLEMENTATION_PLAN.md §4.1 for the full design
/// (message framing, keepalive, why this is raw streaming and not SignalR).
///
/// At 30 displays × 2 connections this is ~60 concurrent channels — trivial for one process, no
/// backplane needed. Registration/broadcast logic is Phase 1 work; this stub exists so the DI
/// registration in Program.cs and the intended shape are visible from the start.
/// </summary>
public class DisplayStreamRegistry
{
    private readonly ConcurrentDictionary<Guid, List<Channel<SseMessage>>> _channelsByDisplay = new();

    public Channel<SseMessage> Register(Guid displayId)
    {
        var channel = Channel.CreateUnbounded<SseMessage>();
        _channelsByDisplay.AddOrUpdate(
            displayId,
            _ => [channel],
            (_, existing) => { existing.Add(channel); return existing; });
        return channel;
    }

    public void Deregister(Guid displayId, Channel<SseMessage> channel)
    {
        if (_channelsByDisplay.TryGetValue(displayId, out var channels))
        {
            channels.Remove(channel);
        }
    }

    public async Task SendToDisplayAsync(Guid displayId, SseMessage message, CancellationToken ct = default)
    {
        if (_channelsByDisplay.TryGetValue(displayId, out var channels))
        {
            foreach (var channel in channels)
            {
                await channel.Writer.WriteAsync(message, ct);
            }
        }
    }

    public async Task SendToGroupAsync(IEnumerable<Guid> displayIds, SseMessage message, CancellationToken ct = default)
    {
        foreach (var displayId in displayIds)
        {
            await SendToDisplayAsync(displayId, message, ct);
        }
    }
}

/// <summary>One SSE frame. See design doc §3.3 for the message vocabulary (assign, reload,
/// identify, clear-cache, ping, and the Pi-only reboot/restart-browser).</summary>
public record SseMessage(string EventType, string DataJson, long SequenceId);
