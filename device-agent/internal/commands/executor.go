// Package commands executes the two device commands the agent is trusted with: restarting the
// kiosk browser and rebooting the box. Runs under a narrowly scoped sudoers grant — exactly
// these two commands, not full root. See IMPLEMENTATION_PLAN.md §4.3.
package commands

import (
	"context"
	"errors"
)

// Executor runs the privileged commands the agent is allowed to issue.
type Executor struct{}

// RestartBrowser runs `systemctl restart chromium-kiosk.service`. This is the routine remedy for
// Chromium's own multi-day memory creep — see design doc §9.3. Phase 2 — milestone 26.
func (e *Executor) RestartBrowser(ctx context.Context) error {
	return errors.New("commands.Executor.RestartBrowser: not implemented, see IMPLEMENTATION_PLAN.md milestone 26")
}

// Reboot runs `/sbin/reboot`. Phase 2 — milestone 26.
func (e *Executor) Reboot(ctx context.Context) error {
	return errors.New("commands.Executor.Reboot: not implemented, see IMPLEMENTATION_PLAN.md milestone 26")
}
