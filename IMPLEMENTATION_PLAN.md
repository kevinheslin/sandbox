# Implementation Plan — Seaway Signage

## 1. Purpose

This is the engineering "how." [`docs/signage-streaming-plan.md`](./docs/signage-streaming-plan.md)
is the frozen "what/why" — read that first. This document is living: update it as decisions get
made, milestones close, and open questions get answered.

## 2. Status

**Last updated:** 2026-09-20. **Current phase:** Phase 0 (not yet started — hardware not yet
procured). **Hardware path:** confirmed — Raspberry Pi 4 or 5, 4GB, USB SSD boot. This is settled,
not a Phase 0 decision; Phase 0 validates the Pi path, it does not choose between hardware options.

| Phase | Status |
|---|---|
| 0 — Spikes | Not started |
| 1 — Thinnest vertical slice | Blocked on Phase 0 |
| 2 — The application | Blocked on Phase 1 |
| 3 — Content depth | Blocked on Phase 2 |

(Phase 4, Signage-Stick-only device plane work, does not apply — dropped with the hardware path
confirmed as Pi.)

## 3. Solution / repo layout

Two deployables, not one and not four. Admin UI + Device API + SSE hub + Player shell share one
DB/domain model, and the SSE hub needs in-process broadcast — splitting Admin from the Device API
into separate processes would force a cross-process backplane (Redis, SQL polling) for zero
benefit at 30-device scale, and would mean wiring AppSecurity/Notifications/Portal/AdminUi twice.
Auth is split *inside* one app instead: cookie auth + `IsInRole` for `/Areas/Admin`, bearer-token
middleware for `/api/**` device endpoints.

The snapshot worker is genuinely separate: resource-heavy (headless Chromium), operationally
independent (restartable without dropping 30 live SSE connections), holds vendor credentials that
don't need to be reachable from device-facing endpoints, and needs none of
AppSecurity/Portal/AdminUi.

```
Seaway.Signage.sln
/src
  Seaway.Signage.Web/                 # ASP.NET Core (net10.0)
    Areas/Admin/                      # AppSecurity-gated: Displays, DisplayGroups, Content,
                                       #   Playlists, Schedules, Overrides — Razor Pages
    Areas/Device/                     # bearer-token gated device API (Minimal API):
                                       #   POST /api/enroll
                                       #   GET  /api/displays/{id}/stream   (SSE — player + agent)
                                       #   POST /api/displays/{id}/heartbeat
                                       #   POST /api/displays/{id}/events
    Streaming/                        # DisplayStreamRegistry, SseWriter
    Services/                         # RotationEngine, ScheduleResolver, PairingService,
                                       #   DisplayHealthMonitor
    Player/wwwroot/player/            # index.html / player.js / player.css — the one page every
                                       #   Pi loads at /play
    Program.cs, appsettings.json, CHANGELOG.md
  Seaway.Signage.SnapshotWorker/      # separate Worker Service — headless Chromium via
                                       #   Playwright for .NET
  Seaway.Signage.Domain/              # POCOs only, no EF/ASP.NET refs: Display, DisplayGroup,
                                       #   Content, Playlist, PlaylistItem, Schedule,
                                       #   ScheduleRule, Override, DisplayEvent
  Seaway.Signage.Data/                # EF Core: SignageDbContext, migrations
/device-agent/                        # Go, cross-compiled linux/arm64
/ansible/                             # fleet provisioning — SSH-based, no agent needed for imaging
/tests/
  Seaway.Signage.Domain.Tests/
  Seaway.Signage.Application.Tests/   # RotationEngine, ScheduleResolver — pure unit tests
  Seaway.Signage.Web.IntegrationTests/# WebApplicationFactory — SSE contract, device API, admin CRUD
  player-shell.tests/                 # Vitest/jsdom + a handful of Playwright browser tests
/docs/signage-streaming-plan.md
README.md, IMPLEMENTATION_PLAN.md, CHANGELOG.md
```

**Why EF Core 10, Code First, in `Seaway.Signage.Data`:** the design doc's §5 data model is a
small, conventional relational schema — no case for a document store or hand-tuned Dapper at this
scale (30 displays, low write volume outside heartbeats). `Seaway.Signage.Domain` stays POCO-only
so `RotationEngine`/`ScheduleResolver`-style logic can be unit tested without EF or a database in
the loop at all.

**Why .NET 10:** current LTS as of writing; a new app should start there rather than on an
expiring version.

**Why Razor Pages, not MVC, for the Admin UI:** CRUD-heavy internal screens (Displays, Groups,
Playlists, Schedules) fit Razor Pages' page-per-workflow model better than controller/view MVC.
**Flag:** confirm against whatever Seaway's real project template (`seaway-templates`, not
reachable while this scaffold was built) actually scaffolds, and follow the template if it
disagrees with this preference.

## 4. Technical decisions (ADR-lite log)

| Decision | Chosen | Status |
|---|---|---|
| SSE hub | Raw `HttpResponse` streaming + `System.Threading.Channels`, not SignalR | Confirmed |
| Warm-pane strategy | Bounded LRU (3–4 concurrently warm iframes), crossfade via opacity/z-index | Confirmed |
| Device agent language | Go, static binary, cross-compiled `linux/arm64` | Confirmed |
| Agent ↔ backend transport | Same `/api/displays/{id}/stream` SSE endpoint as the browser player, second bearer-token consumer | Confirmed |
| Database | SQL Server (working assumption) | **Unconfirmed — needs a real answer from Seaway's platform/DBA team before Phase 2** |
| ASP.NET Core version | .NET 10 | Confirmed |
| Admin UI framework | Razor Pages | Confirmed, pending `seaway-templates` cross-check |

### 4.1 SSE hub: raw streaming, not SignalR

A native browser `EventSource` cannot talk to a SignalR endpoint — different protocol/handshake.
ASP.NET Core has no first-party "SSE hub" abstraction the way it does for WebSockets/SignalR, so
hand-rolling `text/event-stream` framing is the only way to get the auto-reconnect behavior the
design doc's §3.3 is relying on.

Shape: `GET /api/displays/{id}/stream` validates the bearer token, resolves the `DisplayId`,
registers a `Channel<SseMessage>` in a singleton `DisplayStreamRegistry`
(`ConcurrentDictionary<Guid, List<Channel<SseMessage>>>` — a *list* per display, since both the
player and the agent connect with the same token), then `await foreach`s the channel writing
`event: {type}\ndata: {json}\nid: {seq}\n\n`, flushing after each write, deregistering on
`RequestAborted`. A `:\n\n` comment-line keepalive every ~15s, independent of app messages, defeats
reverse-proxy idle timeouts. At 30 displays × 2 connections (player + agent) = 60 concurrent
connections, this is trivial for one process — no backplane needed.

### 4.2 Warm-pane strategy

`Iframe` panes are stacked, absolute-positioned elements swapped via opacity/z-index crossfade —
not `display:none`, to avoid hidden-content throttling. `Native` panes are already in-DOM, never
"cold." `Snapshot`/`Asset` panes are `<img>`/`<video>` — negligible cost to keep warm regardless of
count. The §2.3 RAM floor (3–4 warm tabs on a 4GB board) caps concurrently-warm `Iframe` panes at
3–4; longer playlists pre-fetch the next item ~10s ahead and evict the least-recently-shown one
from the warm set (client-side `RotationController`, an LRU keyed by `ContentId`).

### 4.3 Device agent

Go over Python: RAM is the single tightest, most explicitly flagged constraint in the whole design
doc (§2.3, §9.3) — a compiled Go binary has a far smaller, more predictable footprint than a
Python interpreter + venv, with no dependency-drift risk across 30 field devices.

Talks to the *same* `/api/displays/{id}/stream` endpoint as the browser, as a second independent
SSE consumer with its own bearer token — matches the design doc's literal "same SSE channel,
extended" language, avoids inventing a second protocol. Runs under a narrowly scoped `sudoers`
grant (`systemctl restart chromium-kiosk.service`, `/sbin/reboot` only) — not full root. Two
systemd units: `chromium-kiosk.service` (openbox + Chromium, the thing showing content) and
`signage-agent.service` (the Go binary, supervises the kiosk unit, reports health).

The agent, not the browser, POSTs the heartbeat, because CPU temperature/disk/uptime need OS
access the sandboxed page JS doesn't have. To keep the design doc's §9.1 point that "the heartbeat
proves the entire chain" (not just that the box has power), the agent should eventually run a
tiny `localhost`-only listener that `player.js` pings every ~5s, folding `pageAliveAgeSeconds` into
the heartbeat payload — flagged as a Phase 2 nice-to-have, not required for agent v1 (which can
heartbeat on hardware metrics alone first).

### 4.4 Database — open, not guessed silently

The design doc names no database. Working assumption for scaffolding: **SQL Server**, matching the
"DB grant" language used elsewhere for Seaway's centrally-managed services — but **this must be
confirmed with Seaway's platform/DBA team before Phase 2 starts.** Data volume here is trivial (30
displays, low-frequency writes outside heartbeat/event logging), so SQLite or Postgres would both
work fine on pure technical merit — the deciding factor is organizational consistency, not app
requirements, which is exactly why it needs a real answer rather than an engineering guess. SQLite
is used for local dev/CI regardless of what production lands on; the EF Core provider is a
one-line swap either way.

## 5. Milestone breakdown, by design-doc phase

### Phase 0 — Spikes (gate; no app code here)

Hardware path is confirmed (Raspberry Pi 4/5, 4GB) — Phase 0 validates it, it does not select it.

1. Get a live Pi 4 vs 5 (4GB) price quote; confirm SKU before ordering.
2. Procure 2–3 Pi units, 2 screens, USB SSDs (~$15–20/unit).
3. Hand-image one Pi (Raspberry Pi OS Lite, manual — not Ansible yet), boot from USB SSD.
4. Spike: 3–4 tabs of real Seaway dashboard content kept warm in Chromium kiosk for 24–48h;
   monitor memory.
5. Spike: repeated hard power-cuts against the USB SSD boot media; check for corruption.
6. Spike: throwaway HTML/JS pane-swap prototype (no backend) to eyeball blank-flash on rotation.
7. Spike: HDMI-CEC scheduled power-off against the actual target screen models.
8. Decision checkpoint: write up findings, confirm the exact SKU, confirm results are clean.
   **Nothing in Phase 1 starts until this closes.**
9. In parallel, push on the design doc's §12 open questions (wired VLAN, DB platform, fleet
   ownership/alert recipient, site count) — non-blocking for the hardware gate, but block Phase
   1/2 scoping if left open.

### Phase 1 — Thinnest vertical slice

10. Scaffold from Seaway's real project template once reachable; wire the internal NuGet feed;
    add `Seaway.AppSecurity`, `Seaway.Notifications` (DB grant now, not later), `Seaway.AdminUi`.
11. Minimal EF model for `Display`, `DisplayGroup`, `Content`, `Playlist`, `PlaylistItem`.
12. Device enrollment: `POST /api/enroll` → `Display` in `Unclaimed`, pairing code returned; full-
    screen "Unclaimed display — code X" in the player shell.
13. Minimal claim path (crude internal page is fine here — full CRUD is Phase 2).
14. SSE hub v0: `assign` delivered on connect and on playlist change.
15. Player shell v0: single page, `EventSource`, 2–3 real Seaway pages as iframes, advances on
    `PlaylistItem.DurationSeconds`, basic warm-keeping.
16. Local rotation cache: persist last-known-good rotation to `localStorage`; render from cache
    when SSE is unreachable; staleness indicator after a configurable window.
17. Device bearer tokens: issuance on enroll, hashed at rest, bearer-auth middleware on `/api/**`.
18. Heartbeat every 15s (player-driven for this phase; agent-driven lands in Phase 2).
19. Ansible playbook v0: Chromium + openbox + autologin pointed at `/play`.
20. Deploy to Phase 0 hardware; run the exit criterion: **the display rotates correctly and
    survives a weekend, including an overnight power cut, untouched.**

### Phase 2 — The application

21. Full Admin UI: Displays, `DisplayGroup`/`Content`/`Playlist`/`PlaylistItem` CRUD,
    `SignageAdmin`/`SignageEditor`/`SignageViewer` via `HttpContext.GetSeawayUser()?.IsInRole(...)`
    — **not** `[Authorize(Roles=...)]`, which does nothing under AppSecurity.
22. `Seaway.Portal` tile registration; verify the Access Denied page reads sensibly cold.
23. `Seaway.AdminUi` `/Admin` wiring: `CHANGELOG.md`, csproj version properties, changelog copied
    into publish output, impersonation targets `SignageEditor`/`SignageViewer` only.
24. Full SSE message set: `assign`, `reload`, `identify`, `clear-cache`, `ping`, plus Pi-only
    `reboot`/`restart-browser`.
25. Per-display command buttons in Admin with command-result tracking via `DisplayEvent`.
26. Device agent v1 (Go): systemd service, own SSE connection, executes commands, posts health.
27. Display health dashboard (last heartbeat, status, health metrics).
28. Dark-display alerting via `Seaway.Notifications` (email + Teams) + `/Admin/Notifications` UI.
29. Ansible kiosk role productionized: USB SSD automation, agent binary deploy, both systemd
    units, OS update playbook.
30. Re-claim-by-IP flow: DHCP reservation matching, one-click re-claim instead of fresh pairing.

### Phase 3 — Content depth

31. `Schedule`/`ScheduleRule` model + admin UI + `ScheduleResolver` — pure logic, thoroughly unit
    tested; this is exactly the precedence algebra that needs tests, not code review, to trust at
    6am.
32. `Override` model + admin UI + expiry handling.
33. Snapshot worker: Playwright renderer, per-`Content` render cadence, render-age metadata badge.
34. Native display-only panes.
35. Signage type-scale variant of `seaway-app-style` for the player shell.
36. Cross-repo backlog items (file against the *other* Seaway apps): `frame-ancestors` header and
    a `/display` read-only route for each app that ends up in the content mix.

## 6. Testing / CI approach

**CI-testable without hardware:** rotation/scheduling logic as pure unit tests (no EF/ASP.NET in
the dependency graph — this is where the "precedence algebra nobody wants to debug at 6am" risk
actually gets retired); SSE contract tests via `WebApplicationFactory` (framing, message shapes,
disconnect deregisters cleanly, group broadcast reaches every display); device API tests
(enrollment → claim → token) against SQLite; Admin UI Playwright E2E tests using AppSecurity's test
auth scheme (confirm it exists before committing to this approach); player-shell state-machine
logic as Vitest/jsdom unit tests; snapshot worker render-cadence logic against a mocked renderer,
plus a real-headless-Chromium integration test against a local fixture (doesn't need a Pi — the
worker runs on-prem server hardware); device agent command dispatch and health-payload assembly
with the actual `systemctl`/`sudo`/`/sys/class/thermal` calls mocked behind an interface.

**Genuinely needs Phase 0/real hardware:** multi-day Chromium memory creep on a real 4GB board;
USB SSD survival under power cuts; blank-flash judgment on a physical display at viewing distance;
HDMI-CEC against real panel models; the agent's actual privileged execution path on real Raspberry
Pi OS (CI can cross-compile-check the `arm64` binary but can't run it natively without QEMU).

**CI pipeline (GitHub Actions):** `.NET` job (`dotnet build`/`test`, `dotnet format
--verify-no-changes`); JS job (`npm test` via Vitest, eslint); E2E job (docker-compose + throwaway
DB + Playwright); Go job (`build`/`vet`/`test` plus a `GOARCH=arm64 GOOS=linux go build`
compile-only smoke test). All gate PR merge. Phase-0 hardware checks are a manual checklist in the
Phase 0 milestone, not a CI job.

## 7. Open questions (carried from design doc §12)

| # | Question | Owner | Status |
|---|---|---|---|
| 1 | Database platform (§4.4 above) | Seaway platform/DBA | Open |
| 2 | Which existing internal apps are in the content mix (need `frame-ancestors` + `/display` route)? | — | Open |
| 3 | Which vendor pages (snapshot worker credential handling, render cadence)? | — | Open |
| 4 | Wired vs Wi-Fi, which VLAN? | — | Open |
| 5 | Who owns the fleet operationally (dark-display alert recipient)? | — | Open |
| 6 | How many sites, beyond the ~30-display count? | — | Open |

## 8. Risk register

See design doc §9 for the full breakdown (common risks, Pi-specific risks — Signage Stick risks no
longer apply, hardware path is confirmed). Log any *new* risk discovered during implementation
that the design doc didn't anticipate here as it comes up.

## 9. Glossary

- **Display** — one physical Pi + screen.
- **DisplayGroup** — a set of displays showing identical rotating content; a standalone display is
  a group of one.
- **Playlist / PlaylistItem** — the ordered rotation a group cycles through; `DurationSeconds`
  drives timing.
- **Pane** — one on-screen item within a rotation: `Iframe`, `Native`, `Snapshot`, or `Asset`.
- **Content** — a reusable, addressable thing that can appear in a playlist (a URL, a native view,
  a snapshot source, a static asset).
- **Schedule / ScheduleRule** — group-level day/time windows selecting which playlist is active.
- **Override** — a single-action, expiring content push that takes precedence over the schedule
  (e.g. "safety notice on every floor screen, now").
