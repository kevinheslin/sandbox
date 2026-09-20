// signage-agent runs on each Raspberry Pi as a systemd service (see ../../systemd/). It connects
// to the backend's SSE stream as a second consumer alongside the browser player, executes the
// two commands it's trusted with (restart-browser, reboot), and posts a heartbeat with hardware
// health every ~15s. See design doc §7, §9.3 and IMPLEMENTATION_PLAN.md §4.3.
//
// This is a scaffold: wiring is real, the three packages it depends on (sse, commands, health)
// are stubs that return "not implemented" errors. Real implementation is Phase 2, see
// IMPLEMENTATION_PLAN.md milestone 26.
package main

import (
	"context"
	"flag"
	"log"
	"os/signal"
	"syscall"

	"seaway.com/signage/device-agent/internal/commands"
	"seaway.com/signage/device-agent/internal/health"
	"seaway.com/signage/device-agent/internal/sse"
)

func main() {
	backendURL := flag.String("backend-url", "", "Base URL of the Seaway.Signage.Web backend, e.g. https://signage.seaway.internal")
	displayID := flag.String("display-id", "", "This device's Display ID, assigned during pairing")
	deviceToken := flag.String("device-token", "", "This device's bearer token, assigned during pairing")
	flag.Parse()

	ctx, stop := signal.NotifyContext(context.Background(), syscall.SIGINT, syscall.SIGTERM)
	defer stop()

	client := &sse.Client{BaseURL: *backendURL, DisplayID: *displayID, DeviceToken: *deviceToken}
	executor := &commands.Executor{}
	collector := &health.Collector{}

	log.Printf("signage-agent starting (scaffold — see IMPLEMENTATION_PLAN.md milestone 26)")

	// Real event loop — SSE connect/reconnect, dispatch reboot/restart-browser, periodic
	// heartbeat with collector.Collect() — is Phase 2 work. Wiring the pieces together here so
	// the intended shape is visible; not calling client.Connect/executor.*/collector.Collect yet
	// since they all currently return "not implemented".
	_ = client
	_ = executor
	_ = collector

	<-ctx.Done()
	log.Printf("signage-agent shutting down")
}
