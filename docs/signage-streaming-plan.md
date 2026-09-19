# Seaway Signage — Streaming an On-Prem Website to Amazon Signage Sticks

**Status:** Plan / pre-implementation. No code written.
**Date:** 2026-09-19
**Scope:** An internal Seaway app that drives a fleet of Amazon Signage Sticks, where sticks are
organised into groups and each group shows its own continuously-updating on-prem content.

---

## 1. The constraint that shapes everything

Two facts about the Amazon Signage Stick drive the whole design:

1. **The Amazon Signage Remote Management API manages the *device*, not the *content*.** It exposes
   power on/off, screen orientation, reboot, cache clear, OS/app update triggers, screenshot and
   video capture, and telemetry (HDMI status, memory, storage, Wi-Fi, CPU usage and temperature,
   serial number, MAC, IP). There is no "show this content next" call. Content is whatever the
   kiosk app on the stick is pointed at.
2. **We are provisioning through the Amazon Signage console only** — no third-party CMS. So the
   stick is configured **once**, at enrollment, and we should assume we cannot cheaply re-provision
   it every time content changes.

The conclusion is the central design decision:

> **Every stick is provisioned with the same URL, forever. All grouping, scheduling and content
> routing happens inside our app, after the page loads.**

The stick becomes a dumb, permanently-kiosked browser pointed at `https://signage.seaway.local/play`.
Changing what a TV shows never touches Amazon's console again.

---

## 2. Architecture

```
┌──────────────────────── On-prem ────────────────────────┐
│                                                          │
│   ┌──────────────────┐        ┌─────────────────────┐   │
│   │ Seaway.Signage   │        │ Snapshot Worker     │   │
│   │  - Admin UI      │───────▶│ (headless Chromium) │   │
│   │  - Device API    │        │  renders un-framable │   │
│   │  - SSE hub       │        │  / vendor pages      │   │
│   │  - Player shell  │        └─────────────────────┘   │
│   └────────┬─────────┘                                   │
│            │  SSE (control)  +  HTTPS (content)          │
│            │                                             │
│   ┌────────┴─────────────────────────────────────────┐   │
│   │       Signage VLAN                                │   │
│   │  [Stick A]  [Stick B]  [Stick C]  [Stick D] ...  │   │
│   │   group: Bindery       group: Front Office        │   │
│   └───────────────────────────────────────────────────┘   │
│                                                          │
│   Content sources: existing Seaway apps · new display-   │
│   only screens · vendor pages (via snapshot worker)      │
└──────────────────────────────────────────────────────────┘
                     │
                     ▼ (optional, Phase 4)
        Amazon Signage Remote Management API (cloud)
        power schedule · reboot · orientation · telemetry
```

### 2.1 Two independent planes

| Plane | Transport | Owner | Purpose |
|---|---|---|---|
| **Content plane** | HTTPS + SSE, on-prem, LAN-only | Seaway.Signage | What appears on screen; group routing; near-real-time updates |
| **Device plane** | Amazon Remote Management API, cloud | Amazon | Physical control and hardware telemetry |

They are deliberately decoupled. **The content plane must work with the device plane completely
absent** — that is what makes the Amazon API access risk (§6.1) survivable rather than fatal.

### 2.2 The player shell

A single small page served at `/play`, identical for every stick. Responsibilities:

- Identify itself (§3), then open an SSE connection to `/api/displays/{id}/stream`.
- Render the current playlist as a stack of **panes**, each of which is one of:
  - `Iframe` — an arbitrary URL rendered in an `<iframe>` (Seaway apps, framable vendor pages)
  - `Native` — a screen rendered by the display-only app itself from a data feed
  - `Snapshot` — an image produced by the snapshot worker (for pages that refuse framing)
  - `Asset` — a static image/video from Seaway.Storage (notices, fallback slides)
- Cache the last-known-good playlist in `localStorage` and keep rendering it if the server is
  unreachable. **A dead server must never blank the screens.** After a configurable staleness
  window, show a small unobtrusive stale-data indicator.
- Emit a heartbeat every 15s.

### 2.3 Near-real-time updates (< 5s)

**Server-Sent Events**, not WebSockets. SSE is one-directional (server → display), which is exactly
the shape of this problem; it auto-reconnects natively, survives reverse proxies, and needs no
keepalive protocol of our own. Heartbeats ride the same connection as a POST to a lightweight
endpoint, keeping the SSE stream pure server→client.

Control messages:

| Message | Effect |
|---|---|
| `assign` | New playlist version — player diffs and swaps panes without a full reload |
| `reload` | Hard page reload (use sparingly; it is visible) |
| `identify` | Flash the display's name full-screen for ~10s — how you physically find which TV is "Bindery 2" |
| `clear-cache` | Drop localStorage and re-fetch |
| `ping` | Liveness probe with a correlation id |

Content freshness *within* a pane is separate: a `Native` pane subscribes to its own data feed, and
an `Iframe` pane is the responsibility of the app inside it. The SSE control plane only governs
*which* content is shown, not the data inside it.

---

## 3. Pairing — how an identical URL becomes a named display

No keyboard, no mouse, one URL for every stick. The pairing flow:

1. Stick boots, kiosk app loads `https://signage.seaway.local/play`.
2. Player finds no device token in `localStorage`, POSTs to `/api/enroll`.
3. Server creates a `Display` in `Unclaimed` state and returns a short human-readable pairing code.
4. The TV shows, full-screen: **"Unclaimed display — code K7F2"**.
5. An admin in the Signage admin UI sees the pending display, matches the code against what is on
   the screen in front of them, names it ("Bindery TV 1"), sets its location and assigns a group.
6. Server pushes `assign` over SSE; the player stores its device token and starts playing.

This is the standard signage pairing pattern and it requires zero text entry on the device.

**Failure mode to design for:** anything that clears the kiosk app's storage — including the Remote
Management API's own "clear application cache" command — wipes the device token and drops the stick
back to `Unclaimed`. Mitigations, in order:

- **DHCP reservations** so every stick has a stable IP. On re-enrollment, the server matches the
  client IP against the previous owner and offers the admin a one-click "re-claim as Bindery TV 1"
  instead of a fresh setup.
- Never issue `clear-cache` as a routine remedy.
- Accept that re-pairing is a 20-second admin task, not an outage — the stick keeps showing its
  cached playlist until told otherwise.

---

## 4. Data model (first cut)

| Entity | Key fields |
|---|---|
| `Display` | Id, Name, Location, GroupId, DeviceTokenHash, Status, LastSeenUtc, PairingCode, ClaimedAtUtc, IpAddress, SerialNumber, MacAddress, Orientation |
| `DisplayGroup` | Id, Name, Description, SiteId |
| `Content` | Id, Kind (Iframe/Native/Snapshot/Asset), Url, NativeViewKey, AssetId, FramingMode, RefreshHintSeconds |
| `Playlist` | Id, Name, Version |
| `PlaylistItem` | PlaylistId, ContentId, Order, DurationSeconds |
| `Schedule` / `ScheduleRule` | GroupId, PlaylistId, DayMask, StartTime, EndTime, Priority |
| `Override` | Target (GroupId or DisplayId), ContentId, ExpiresAtUtc, CreatedBy |
| `DisplayEvent` | DisplayId, Kind, PayloadJson, OccurredAtUtc — heartbeats, errors, command results |

**Deliberate simplifications, recorded so nobody re-litigates them:**

- **A display belongs to exactly one group.** Many-to-many group membership is a trap: it makes
  "what is this TV showing right now?" unanswerable without a precedence algebra nobody wants to
  debug at 6am. Per-display `Override` covers the real exception case.
- **Schedules are group-level only.** Per-display scheduling is not in scope.
- `Override` exists specifically for "put the safety notice on every floor TV, now" — a single
  action with an expiry, not a playlist edit.

---

## 5. Authentication

Two separate identity planes. Do not conflate them.

### 5.1 Displays — device tokens (chosen)

- Enrollment issues an opaque 256-bit random token, **stored hashed at rest**, one per display.
- Presented as a bearer token on every device API call and on SSE connect.
- Individually **revocable** (a stolen or decommissioned stick is one click) and **rotatable** on a
  schedule without re-pairing.
- Device tokens are **not** Seaway.AppSecurity users. They carry no roles and can reach only the
  device API surface — read playlist, post heartbeat, post error. They cannot read or write anything
  else, which is the whole point of not using kiosk service accounts.

### 5.2 Displayed Seaway apps — short-lived signed handoff

For an `Iframe` pane pointing at an internal Seaway app, Seaway.Signage mints a short-lived
(~5 min), audience-scoped signed token. The target app exposes a `/display` route that accepts it
and establishes a **read-only** session. The player refreshes the token before expiry. No
long-lived credential ever sits on the device for an app other than Signage itself.

### 5.3 Admin UI — Seaway.AppSecurity, normally

Humans managing the fleet authenticate through AppSecurity as usual.

### 5.4 Vendor pages

Handled by the snapshot worker (§7), which holds the vendor credential server-side. No vendor
credential is ever delivered to a stick.

---

## 6. Risks

### 6.1 Remote Management API access — the big one

Amazon's documentation states the API is "available to approved Amazon Signage Stick CMS providers."
**We are an end customer building an internal tool, not a CMS vendor.** It is genuinely unclear
whether we can obtain credentials. This is the single largest open question and Phase 0 must resolve
it before anyone plans around it.

**If we cannot get access**, we lose remote power control, reboot, orientation and hardware
telemetry. Fallbacks, all of which are viable:

| Lost capability | Fallback |
|---|---|
| Liveness / health | **Player heartbeat** — strictly better anyway (§6.2) |
| Scheduled power off at night | HDMI-CEC from the stick, or the TV's own built-in power schedule |
| Hard reboot | Managed smart plug on the TV/stick circuit |
| Orientation | Set once, physically, at install |

The project is fully viable without the Amazon API. Phase 4 is genuinely optional.

### 6.2 Heartbeat beats telemetry

Worth stating plainly: the player heartbeat proves the **entire chain** — network up, kiosk app
alive, our site reachable, page actually rendering. Amazon's device telemetry proves only that the
hardware is powered and online, which is a much weaker claim. Even with full API access, the
heartbeat is the primary health signal and the telemetry is supplementary.

### 6.3 TLS on an internal hostname

The stick's kiosk webview is very unlikely to trust Seaway's internal CA, and there is no practical
way to install a root cert on a locked-down signage device. A self-signed or internal-CA cert on
`signage.seaway.local` will almost certainly produce a hard failure with no clickable "proceed"
button on a device with no pointer.

**Plan for a publicly-trusted certificate on an internal-only name**: own a real DNS name (e.g.
`signage.seawayprinting.com`), issue via ACME **DNS-01** (no inbound internet needed), and resolve
it to the on-prem IP with split-horizon DNS. Budget real time for this — it is routinely
underestimated and it blocks everything.

### 6.4 Iframe refusal

`X-Frame-Options` and CSP `frame-ancestors` will block a meaningful share of target pages.

- **Seaway-owned apps:** add `Content-Security-Policy: frame-ancestors https://signage.…` — cheap,
  but it is a change to each displayed app, so it needs to be in *their* backlog too.
- **Vendor pages:** snapshot worker (§7).
- **Reverse-proxy header rewriting:** technically possible, fragile, and it silently defeats a
  security control the vendor deliberately set. Not recommended; listed only to be explicitly ruled
  out.

### 6.5 Single point of failure

Every TV depends on one on-prem app. Mitigated by the player's local playlist cache (§2.2) — a
server outage degrades to "screens show slightly stale content" rather than "every screen in the
building goes black." This is the highest-value resilience feature in the plan and belongs in
Phase 1, not later.

### 6.6 Smaller ones

Screen burn-in on static dashboards (rotate panes, or shift content by a few pixels periodically) ·
TV auto-sleep on static HDMI input · overscan clipping the edges of the page · timezone and NTP
correctness on the stick · autoplay policy if any pane contains video.

---

## 7. Snapshot worker

For content that cannot be framed or that needs server-held credentials: a headless Chromium
service, on-prem, that logs in as needed, renders a target page on a schedule, and publishes an
image the player displays as a `Snapshot` pane.

It solves vendor auth and iframe refusal in one move. Its cost is freshness — a snapshot is as old
as its render interval, so it is unsuitable for anything needing the < 5s target. Mark snapshot
content clearly in the admin UI with its render age so nobody mistakes a 5-minute-old snapshot for
live data.

---

## 8. Shared services & standard features

Per the reuse gate, a decision is recorded for **every** row, including skips.

> **Note:** `Seaway-Printing/seaway-templates` could not be reached from this session, so these
> decisions are based on the services skill summary rather than the live service cards. Re-check
> them against `reference/services/` before scaffolding.

| Service | Decision |
|---|---|
| **Seaway.AppSecurity** | **Yes** — admin UI. Internal users only. Roles: `SignageAdmin`, `SignageEditor`, `SignageViewer`. Possible second axis by site/location if signage goes multi-site; defer until it does. Remember: AppSecurity adds no role claims — `[Authorize(Roles = …)]` does **not** work; use `HttpContext.GetSeawayUser()?.IsInRole(...)`. |
| **Seaway.Notifications** | **Yes** — required regardless (AppSecurity depends on it, and `AddSeawaySignageNotifications()`-style startup reads channel config, so the app will not start without the DB grant). Real use: "display dark for > N minutes" alerts via email + Teams. Ship the `/Admin/Notifications` UI. |
| **Seaway.Storage** | **Yes**, narrowly — static fallback/notice assets (images, short video). One entity type, short retention. Not used for snapshots, which are transient and served from the worker's own cache. |
| **Seaway.JobEngine** | **No.** This app creates no Tharstern print jobs and has no JDF path. Explicitly skipped. |
| **Seaway.Portal** | **Yes** — tile for the admin UI, Operations / Internal Tools category. Check the Access Denied page reads sensibly to someone arriving cold from the launcher. |
| **Seaway.AdminUi** | **Yes** — default-on. `/Admin` with assembly version and act-as. Requires `CHANGELOG.md` and csproj version properties, changelog copied into publish output. Impersonation targets: `SignageEditor`, `SignageViewer` — never `SignageAdmin`. Take the package; do not hand-copy an `ImpersonationHelper`. |
| **Seaway.Mailer** | **No** — deprecated, never referenced. |

### 8.1 Not in either register

Two capabilities here have **no card**:

- **Device identity / kiosk display tokens** (§5.1)
- **On-prem headless page rendering** (§7)

Per the promotion rule this is the *first* app to need either, so no card is required yet. Flagging
it now so it is not a surprise: **if a second Seaway app needs display device tokens, that is the
moment it needs a card, a conformance definition and a named owner.**

### 8.2 Styling

- Admin UI and the display-only screens: **seaway-app-style** (Direction 1 — Bricolage Grotesque /
  Inter, orange/navy/teal/mustard/cream, warm borders, no drop shadows).
- Display screens need a deliberate variant of it: viewing distance is 3–10 metres, not 50cm.
  Expect to define larger type scale, heavier weights and higher-contrast pairings as an explicit
  "signage" extension rather than reusing app-screen sizing.
- Any install/runbook documentation produced: **seaway-document-style**. If it becomes a floor
  procedure, **seaway-sop-builder** for numbering.

---

## 9. Phasing

### Phase 0 — Spikes (1 stick, ~1 week)
Buy one stick, one TV, answer the questions that invalidate the plan:

- [ ] Can the Amazon Signage console point a stick at an **arbitrary URL**? *(If no, the whole
      approach changes and we revisit third-party CMS.)*
- [ ] Does that URL **persist across reboot and power loss** without human intervention?
- [ ] What webview engine/version does the kiosk app use? Does it support **SSE**, ES2020,
      `localStorage`?
- [ ] Can we obtain **Remote Management API credentials** as an end customer? (§6.1)
- [ ] Does the target TV honour **HDMI-CEC** power control?
- [ ] Does a **publicly-trusted cert on a split-horizon internal name** work on the stick? (§6.3)

**Phase 0 is a gate.** The first two questions can invalidate the architecture; nothing else starts
until they are answered.

### Phase 1 — Thinnest vertical slice (~1 week)
One group, one hard-coded URL, pairing flow, localStorage playlist cache, heartbeat.
**Exit criterion: a TV shows the page and survives a weekend, including an overnight power cut,
with nobody touching it.** That single test retires most of the operational risk.

### Phase 2 — The actual app (~3 weeks)
Admin UI on AppSecurity, Portal tile, `/Admin` page, groups, playlists, SSE control plane, display
health dashboard, dark-display alerts via Notifications.

### Phase 3 — Content depth (~2 weeks)
Schedules (shift changes), overrides, snapshot worker, native display-only screens, signage type
scale.

### Phase 4 — Device plane (~1 week + procurement lead time, **optional**)
Remote Management API: nightly power schedule, remote reboot, hardware telemetry merged into the
health dashboard. Only if Phase 0 confirmed access.

---

## 10. Open questions for Seaway

1. **How many sticks, and at how many sites?** Drives whether `DisplayGroup` needs a site axis and
   whether one on-prem instance is enough.
2. **Which existing internal apps are in the "mix"?** Each needs a `frame-ancestors` change and a
   `/display` read-only route — work in *their* backlogs, not this one.
3. **Which vendor pages?** Determines snapshot worker credential handling and render cadence.
4. **Wired or Wi-Fi, and which VLAN?** Wired is strongly preferred for always-on displays.
5. **Who owns the fleet operationally** — who gets the dark-display alert at 6am?

---

## Sources

- [Why the Amazon Signage Remote Management API is a game changer](https://signage.amazon.com/blog/why-the-amazon-signage-remote-management-api-is-a-game-changer-for-you-and-your-customers)
- [Remote Management for Digital Signage — Amazon Signage Stick](https://signage.amazon.com/remote-management)
- [Amazon Signage Stick API Documentation](https://signage.amazon.com/api-documentation)
- [Amazon Signage Stick support / FAQ](https://signage.amazon.com/support/faq)
- [Amazon launches Vega OS for Fire TV — AFTVnews](https://www.aftvnews.com/amazon-launches-vega-os-for-fire-tv-heres-how-it-affects-new-old-fire-tvs-apps-and-sideloading/)

> Note: `signage.amazon.com` is blocked by this environment's network egress proxy, so the Amazon
> pages above were summarised from search results rather than read directly. Verify the Remote
> Management API details against the live documentation during Phase 0.
