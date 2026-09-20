// Seaway Signage player shell.
//
// Intended structure, per design doc §3.2 and IMPLEMENTATION_PLAN.md §4.2:
//   - PairingController: on first load, no device token in localStorage -> POST /api/enroll,
//     show the pairing code full-screen, poll or wait for an `assign` SSE message once claimed.
//   - RotationController: an EventSource connection to /api/displays/{id}/stream, advancing
//     through the current playlist on PlaylistItem.DurationSeconds, keeping a bounded LRU
//     warm-set of Iframe panes (cap 3-4, per the design doc's §2.3 RAM floor) rather than
//     keeping every item warm.
//   - PaneController: renders one pane (Iframe / Native / Snapshot / Asset) and handles the
//     opacity/z-index crossfade swap (not display:none, to avoid hidden-content throttling).
//   - Local cache: persist the last-known-good rotation to localStorage; if the SSE connection
//     is unreachable, keep rendering from cache and show a small staleness indicator after a
//     configurable window. A dead server must never blank the screens.
//
// None of this is implemented yet — see IMPLEMENTATION_PLAN.md milestones 12-16 (Phase 1).

console.info("Seaway Signage player shell — scaffold only, see IMPLEMENTATION_PLAN.md Phase 1.");
