// Package health assembles the heartbeat payload the agent POSTs every ~15s: CPU temperature,
// disk, uptime, and — once the localhost page-alive listener is built — pageAliveAgeSeconds, so
// the heartbeat proves the whole chain (design doc §9.1), not just that the hardware has power.
// See IMPLEMENTATION_PLAN.md §4.3.
package health

import (
	"errors"
	"time"
)

// Payload is the JSON body posted to POST /api/displays/{id}/heartbeat.
type Payload struct {
	CPUTemperatureCelsius float64   `json:"cpuTemperatureCelsius"`
	DiskFreeBytes         int64     `json:"diskFreeBytes"`
	UptimeSeconds         int64     `json:"uptimeSeconds"`
	PageAliveAgeSeconds   *float64  `json:"pageAliveAgeSeconds,omitempty"` // nil until the localhost listener lands
	CollectedAt           time.Time `json:"collectedAt"`
}

// Collector reads hardware health from the OS (/sys/class/thermal, df, /proc/uptime).
type Collector struct{}

// Collect assembles a Payload. Phase 2 — see IMPLEMENTATION_PLAN.md milestone 26.
func (c *Collector) Collect() (Payload, error) {
	return Payload{}, errors.New("health.Collector.Collect: not implemented, see IMPLEMENTATION_PLAN.md milestone 26")
}
