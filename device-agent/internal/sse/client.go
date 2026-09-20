// Package sse implements the device agent's SSE connection to the backend.
//
// Connects to the same GET /api/displays/{id}/stream endpoint the browser player uses, as a
// second independent consumer authenticated with the device's own bearer token — see
// IMPLEMENTATION_PLAN.md §4.3. Real connect/reconnect/parse logic is Phase 2 work
// (milestone 26); this stub establishes the package shape.
package sse

import (
	"context"
	"errors"
)

// Message is one parsed SSE frame (event/data/id), matching the shape SseWriter produces on the
// backend (Seaway.Signage.Web/Streaming/SseWriter.cs).
type Message struct {
	EventType string
	Data      string
	ID        string
}

// Client maintains a persistent SSE connection to the backend, reconnecting on drop.
type Client struct {
	BaseURL     string
	DisplayID   string
	DeviceToken string
}

// Connect opens the stream and delivers parsed messages to handler until ctx is cancelled or an
// unrecoverable error occurs. Phase 2 — see IMPLEMENTATION_PLAN.md milestone 26.
func (c *Client) Connect(ctx context.Context, handler func(Message)) error {
	return errors.New("sse.Client.Connect: not implemented, see IMPLEMENTATION_PLAN.md milestone 26")
}
