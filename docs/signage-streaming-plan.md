# Seaway Signage — Streaming an On-Prem Website to a Display Fleet

**Status:** Plan / pre-implementation. No code written.
**Date:** 2026-09-19
**Scope:** An internal Seaway app that drives up to ~30 displays. Displays are organised into
groups — some groups hold several displays showing the same rotating content, others hold a single
display showing its own distinct rotation. Content is on-prem and continuously updating; each
display **rotates through multiple pages**, and at least one of those pages needs to keep updating
in near real time even while it isn't the one currently on screen. This is a confirmed requirement,
not a nice-to-have — the second display deployed is expected to need it.

---

## 1. Two things are settled regardless of hardware

1. **Content and device are separate concerns.** Whatever plays the content needs to be managed
   remotely — reboot, health, "what is this thing showing" — and that management should live in
   one console, not be scattered across a vendor app, an admin UI and a spreadsheet.
2. **All grouping, rotation, scheduling and content routing happens inside our own on-prem app.**
   No hardware option here supports "push new content" as a first-class device feature; every path
   below ends with the same shape: a permanently-kiosked browser pointed at one URL, and our app
   decides what appears behind that URL.

What differs by hardware is **who controls the device plane** — us, or a vendor we have to ask
nicely. That's the whole comparison in §2.

---

## 2. Hardware & Platform Comparison

Two real options were evaluated: the **Amazon Signage Stick** (the original starting point) and a
**Raspberry Pi** running Chromium in kiosk mode. Both can show a website full-screen; they differ
sharply in who holds the keys.

### 2.1 Amazon Signage Stick

| | |
|---|---|
| **Advantages** | Purpose-built signage hardware — one SKU, no component sourcing or assembly (no case/PSU/storage decisions per unit). Kiosk mode is an OS-level feature Amazon has already hardened; we don't build or maintain that hardening ourselves. A vendor support and replacement channel exists. *If* Remote Management API access is obtained, device control (power, reboot, orientation, telemetry) ships without us building or operating that layer. Simple physical install for non-technical staff — HDMI and power, nothing to configure by hand. |
| **Disadvantages** | The Remote Management API is documented as available to "approved CMS providers" — **we are an end customer, not a CMS vendor, and eligibility is genuinely unresolved.** Without it, there is no remote reboot, no telemetry, no OS-level control at all — see §9.2. The device is a locked-down webview: no root, no way to install our own CA (TLS trust risk, §9.2), no way to tune Chromium's memory behaviour for multi-page rotation, no way to add our own watchdog/restart logic. Amazon's Fire TV line is mid-migration to **Vega OS**, which does not run Android APKs — a live platform-roadmap risk sitting entirely outside our control. Third-party or open-source player software on the stick is unconfirmed; at best it's a Phase-0 experiment, not something to plan around. Unit and any signage-service pricing were not fully resolved during this research and should be treated as another open figure, not assumed. |

### 2.2 Raspberry Pi (Chromium kiosk)

| | |
|---|---|
| **Advantages** | **We hold root.** Device-plane management (reboot, restart the browser, health telemetry) is simply an extension of the same on-prem agent we're building for the content plane — one console, no vendor gate, no eligibility risk (see §7). No exposure to Amazon's Fire TV/Vega OS roadmap — this is our hardware, not theirs. Full control of the TLS trust store, so the internal-hostname certificate risk in §9.2 doesn't apply. Full control of Chromium's tuning for rotation — which pages stay warm, memory/GPU flags, a restart watchdog. Full control of boot media, so we can use a USB SSD instead of inheriting whatever storage decision a vendor made. Free, open-source fleet tooling (Ansible) fits Seaway's on-prem, no-subscription posture. Materially cheaper hardware once RAM pricing settles (§2.3), with no recurring vendor CMS or API fee. |
| **Disadvantages** | We own the whole stack — OS hardening, security patching, kiosk configuration — with no vendor support line if a board fails (though boards are cheap and swappable, which offsets this somewhat). Procurement at 30-unit scale means sourcing boards, cases, power supplies and storage ourselves rather than ordering one SKU. We have to build the OS-level agent commands (reboot, restart, health) ourselves — small in scope, but real scope that the Signage Stick path would have gotten "for free" *if* its API were obtainable. Chromium's own multi-day memory creep is real (§9.3) and needs an operational mitigation (periodic restart) regardless of RAM tier — owning root makes this manageable, but doesn't make it disappear. RAM pricing is genuinely volatile right now (§2.3) — "least expensive" is a moving target that needs a live quote before 30 units are ordered. |

### 2.3 Which Raspberry Pi — the no-compromise floor

Rotating through multiple pages, at least one of which is still updating while off-screen, means
keeping more than one Chromium renderer process warm at once. That sets a real RAM floor, not a
preference:

- Chromium itself warns **"not recommended... on devices with less than 1GB of RAM."** Below that
  is not a judgment call.
- On an 8GB Pi 4, bare Chromium already uses **over 512MB before a single tab is open**; several
  tabs kept warm for a few days are reported to exhaust available RAM without mitigation.
- Signage-specific guidance splits consistently at the same line: **2GB is called "sufficient" for
  a simple rotating kiosk**, but **4GB is where multiple warm tabs and heavier content are
  comfortable** rather than tightly managed.

| Board | RAM | Verdict |
|---|---|---|
| Pi Zero 2 W | 512MB | **Ruled out.** Below Chromium's own supported floor. |
| Pi 3B+ | 1GB | At the edge multiple sources call marginal; weaker GPU (VideoCore IV) for smooth page transitions. Not a no-compromise pick. |
| Pi 4 Model B | 2GB | Workable for a single navigating page. Not the target here — multi-tab headroom is where sources stop crediting it. |
| **Pi 4 Model B or Pi 5** | **4GB** | **Recommended floor.** The point where rotation with warm, live-updating tabs is comfortable rather than managed. |

**Recommendation: Raspberry Pi 4 Model B (4GB) or Raspberry Pi 5 (4GB).** The Pi 5 has a
meaningfully faster CPU (Cortex-A76 vs A72) and newer GPU, further reducing stutter risk on page
switches, for similar or only slightly higher cost depending on the week purchased.

**Live pricing caveat, not hedging — this is sourced and current:** Pi RAM pricing is genuinely
volatile. A DRAM shortage has driven "memory-driven price rises" across every Pi 4/5 SKU with 2GB
or more of RAM — the Pi 5 16GB variant moved from $120 to $205 in a matter of months. The 1GB
variant is the only SKU Raspberry Pi has explicitly protected at $45, and it fails the technical
floor above regardless of price. **Get a live quote before locking the SKU across 30 units** — the
Pi 4 vs Pi 5 price gap at the 4GB tier could be a few dollars or could be inverted by purchase time.

**Boot media, while we're specifying hardware:** boot from a small USB SSD (~$15–20/unit), not
microSD. SD card corruption under 24/7 write load is the most commonly reported real-world failure
mode for always-on kiosk Pis — unrelated to the rotation requirement, but squarely in "no
compromise" territory for a 30-unit fleet that has to stay up unattended.

### 2.4 Smart TVs — Considered and Ruled Out

Worth recording explicitly so it isn't re-raised without context later. Two distinct categories
exist under "smart TV," and only one was seriously considered:

**Consumer smart TVs (Samsung/LG/Vizio/Roku off-the-shelf) — not viable.** Concrete, reported
failure modes, not hypothetical ones:

- Screens are commonly reported to freeze overnight or go dark after a power cycle, without
  automatically relaunching the kiosk page — failing the same "survives a weekend untouched"
  bar the Pi path is held to in Phase 1.
- No local caching on most models — a network drop blanks the screen rather than falling back to
  stale content, the exact failure mode the plan's local-rotation-cache (§3.2) exists to prevent.
- The browser engine is frequently ancient and frozen at whatever shipped with that firmware
  version, with no user-facing "update browser" control — webOS TV 1.x/2.x, for example, ship
  Chromium 26 and 34 respectively (2014-era). SSE support and reliably keeping a page warm in the
  background are not guaranteed on an engine that old.
- No true kiosk/rotation control — typically one browser window, manually reopened, not several
  panes kept warm and swapped between.
- No remote management path at all for consumer sets — not even a gated one like the Signage
  Stick's API. Reboot and health both require walking up to the TV with the remote.

**Commercial signage displays (Samsung Tizen SSSP4+, LG webOS Signage) — a different product,
deliberately not chased further.** These are purpose-built signage panels with a firmware-level
URL Launcher and a real MDM/remote-management layer — not a "smart TV with a browser," a
signage-specific product line. Architecturally this is the same shape as the Amazon Signage Stick
option already in §2.1: vendor-hardened device, built-in kiosk mode, gated remote-management API.
It reimports exactly the class of risk the Pi recommendation exists to avoid (vendor API terms to
re-verify, no root, unclear whether the URL Launcher can keep multiple rotation panes warm versus
one static URL), at a real cost premium over a commercial dumb panel. Not evaluated as a third
option in §2.5 for that reason — it doesn't change the recommendation, only restates the
Stick-shaped trade-off on different hardware.

**One more point worth carrying into procurement regardless of the above:** buying an actual
"smart" TV for this is generally the wrong purchase before the software question is even asked —
it pays for a smart platform, tuner and app store that go entirely unused. A plain commercial or
signage-grade panel with no smart OS is typically cheaper at the same screen size and pairs
directly with the Pi this plan already recommends. **Buy dumb screens.**

### 2.5 Recommendation

**Raspberry Pi 4 or 5 (4GB), Chromium kiosk, USB SSD boot.** It removes the two largest risks in
this plan outright — Remote Management API eligibility (§9.2) and locked-webview TLS trust (§9.2)
— by putting the device fully under our control, at the cost of owning OS maintenance ourselves.
Given the confirmed rotation requirement and the fleet size, that trade favours the Pi. The rest of
this document is written against the Pi path, with the Signage Stick's differences called out
explicitly wherever they apply.

---

## 3. Architecture

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
│            │  SSE (content + device commands)             │
│            │  + HTTPS                                     │
│   ┌────────┴─────────────────────────────────────────┐   │
│   │       Signage VLAN                                │   │
│   │  [Pi A]  [Pi B]  [Pi C]  [Pi D] ...              │   │
│   │   group: Bindery      standalone: Loading Dock 3  │   │
│   └───────────────────────────────────────────────────┘   │
│                                                          │
│   Content sources: existing Seaway apps · new display-   │
│   only screens · vendor pages (via snapshot worker)      │
└──────────────────────────────────────────────────────────┘
```

### 3.1 Two independent planes, one console

| Plane | Transport | Purpose |
|---|---|---|
| **Content plane** | HTTPS + SSE, on-prem, LAN-only | Which pages a display rotates through; group routing; near-real-time updates |
| **Device plane** | Same SSE channel, extended (Pi path) | Reboot, restart the browser, health telemetry |

On the Pi path these are genuinely one console — the same admin UI, the same connection to the
device, no separate vendor dashboard. §7 covers this in detail. **On the Signage Stick path they
would have stayed split**, with the device plane gated behind Amazon's API approval — this is the
structural reason the Pi path was recommended in §2.5.

### 3.2 The player shell

A single small page, identical for every display. Responsibilities:

- Identify itself (§4), then open an SSE connection to `/api/displays/{id}/stream`.
- Render the current rotation as a sequence of **panes**, each of which is one of:
  - `Iframe` — an arbitrary URL rendered in an `<iframe>` (Seaway apps, framable vendor pages)
  - `Native` — a screen rendered by the display-only app itself from a data feed
  - `Snapshot` — an image produced by the snapshot worker (for pages that refuse framing)
  - `Asset` — a static image/video from Seaway.Storage (notices, fallback slides)
- **Keep the next pane(s) in the rotation warm** rather than navigating fresh on every rotation —
  this is what avoids a blank flash and a stalled reconnect on pages that are still receiving live
  updates while off-screen. This is the direct consequence of the confirmed rotation requirement
  and is the reason the hardware floor in §2.3 is what it is.
- Cache the last-known-good rotation in `localStorage` and keep rendering it if the server is
  unreachable. **A dead server must never blank the screens.** After a configurable staleness
  window, show a small unobtrusive stale-data indicator.
- Emit a heartbeat every 15s, including basic device health on the Pi path (CPU temperature, disk,
  uptime) — see §7.

### 3.3 Near-real-time updates (< 5s)

**Server-Sent Events**, not WebSockets. SSE is one-directional (server → display), which is exactly
the shape of this problem; it auto-reconnects natively, survives reverse proxies, and needs no
keepalive protocol of our own. Heartbeats ride separately as a lightweight POST, keeping the SSE
stream pure server→client.

Control messages:

| Message | Effect |
|---|---|
| `assign` | New rotation/playlist version — player diffs and swaps panes without a full reload |
| `reload` | Hard page reload (use sparingly; it is visible) |
| `identify` | Flash the display's name full-screen for ~10s — how you physically find which screen is "Bindery 2" |
| `clear-cache` | Drop localStorage and re-fetch |
| `ping` | Liveness probe with a correlation id |
| `reboot` *(Pi only)* | Reboot the device — see §7 |
| `restart-browser` *(Pi only)* | Restart Chromium without a full OS reboot — the routine remedy for memory creep, §9.3 |

Content freshness *within* a pane is separate: a `Native` pane subscribes to its own data feed, and
an `Iframe` pane is the responsibility of the app inside it. The SSE control plane only governs
*which* content is shown, not the data inside it.

---

## 4. Pairing — how an identical URL becomes a named display

No keyboard, no mouse, one URL for every device. The pairing flow:

1. Device boots, Chromium loads `https://signage.seaway.local/play`.
2. Player finds no device token in `localStorage`, POSTs to `/api/enroll`.
3. Server creates a `Display` in `Unclaimed` state and returns a short human-readable pairing code.
4. The screen shows, full-screen: **"Unclaimed display — code K7F2"**.
5. An admin in the Signage admin UI sees the pending display, matches the code against what is on
   the screen in front of them, names it ("Bindery TV 1"), sets its location, and assigns it to a
   group (or leaves it standalone).
6. Server pushes `assign` over SSE; the player stores its device token and starts playing.

This is the standard signage pairing pattern and it requires zero text entry on the device.

**Failure mode to design for:** anything that clears local storage — a factory reset, a manual
cache clear — wipes the device token and drops the display back to `Unclaimed`. Mitigations, in
order:

- **DHCP reservations** so every device has a stable IP. On re-enrollment the server matches the
  client IP against the previous owner and offers the admin a one-click "re-claim as Bindery TV 1"
  instead of a fresh setup.
- Never issue `clear-cache` as a routine remedy.
- Accept that re-pairing is a 20-second admin task, not an outage — the display keeps showing its
  cached rotation until told otherwise.

---

## 5. Data Model (first cut)

Rotation is a confirmed requirement, so the rotation/scheduling entities below are load-bearing,
not optional extras.

| Entity | Key fields |
|---|---|
| `Display` | Id, Name, Location, GroupId, DeviceTokenHash, Status, LastSeenUtc, PairingCode, ClaimedAtUtc, IpAddress, MacAddress, Orientation |
| `DisplayGroup` | Id, Name, Description, SiteId |
| `Content` | Id, Kind (Iframe/Native/Snapshot/Asset), Url, NativeViewKey, AssetId, FramingMode, RefreshHintSeconds |
| `Playlist` | Id, Name, Version |
| `PlaylistItem` | PlaylistId, ContentId, Order, DurationSeconds |
| `Schedule` / `ScheduleRule` | GroupId, PlaylistId, DayMask, StartTime, EndTime, Priority |
| `Override` | Target (GroupId or DisplayId), ContentId, ExpiresAtUtc, CreatedBy |
| `DisplayEvent` | DisplayId, Kind, PayloadJson, OccurredAtUtc — heartbeats, health, errors, command results |

**Deliberate simplifications, recorded so nobody re-litigates them:**

- **A display belongs to exactly one group, or none (standalone).** Many-to-many group membership
  is a trap: it makes "what is this screen showing right now?" unanswerable without a precedence
  algebra nobody wants to debug at 6am. A standalone display is simply a group of one in practice —
  it still gets its own `Playlist` and `ScheduleRule`.
- **Schedules are group-level (including groups of one).** Per-display scheduling below that isn't
  needed — a standalone display's schedule already lives on its own group.
- `Override` exists specifically for "put the safety notice on every floor screen, now" — a single
  action with an expiry, not a rotation edit.
- **`PlaylistItem.DurationSeconds` drives rotation timing.** The player advances through a group's
  active playlist on that cadence, keeping the next item's pane warm (§3.2) rather than navigating
  cold.

---

## 6. Authentication

Two separate identity planes. Do not conflate them.

### 6.1 Displays — device tokens

- Enrollment issues an opaque 256-bit random token, **stored hashed at rest**, one per display.
- Presented as a bearer token on every device API call and on SSE connect.
- Individually **revocable** (a stolen or decommissioned device is one click) and **rotatable** on a
  schedule without re-pairing.
- Device tokens are **not** Seaway.AppSecurity users. They carry no roles and can reach only the
  device API surface — read rotation, post heartbeat, post health, post error, accept device
  commands. They cannot read or write anything else, which is the whole point of not using kiosk
  service accounts.

### 6.2 Displayed Seaway apps — short-lived signed handoff

For an `Iframe` pane pointing at an internal Seaway app, Seaway.Signage mints a short-lived
(~5 min), audience-scoped signed token. The target app exposes a `/display` route that accepts it
and establishes a **read-only** session. The player refreshes the token before expiry. No
long-lived credential ever sits on the device for an app other than Signage itself.

### 6.3 Admin UI and vendor pages

Administrators managing the fleet authenticate through Seaway.AppSecurity in the normal way. Vendor
pages are handled by the snapshot worker (§8), which holds the vendor credential server-side; no
vendor credential is ever delivered to a display.

---

## 7. Device & OS Management — one console for the fleet

This section is specific to the Pi path (§2.5). On the Signage Stick path, everything below would
instead depend entirely on obtaining Remote Management API access — see §9.2.

### 7.1 Why one console is achievable here

Because we hold root on every device, the OS-level commands (`reboot`, `restart-browser` from the
table in §3.3, and a health payload of CPU temperature/disk/uptime folded into the regular
heartbeat) are just a small extension of the same agent and the same SSE channel already built for
content. **The Seaway admin UI is the single console** for both "what is this screen showing" and
"is this box healthy, and can I reboot it" — no second dashboard, no external subscription.

### 7.2 Provisioning and OS updates — Ansible

Day-to-day operation goes through the console above. **Initial imaging and occasional OS-level
updates** (security patches, a new base image) are a different, infrequent kind of task, better
suited to a purpose-built provisioning tool than to the live console:

- **Ansible** — open source, SSH-based, no agent installed on the device. A ready-made
  [kiosk role pattern](https://github.com/stevewoolley/pi-fleet) installs Chromium, openbox and
  autologin, and the same playbooks push OS updates across the fleet in one run. Free, self-hosted,
  fits Seaway's on-prem posture, and needs nothing beyond SSH access on the signage VLAN.

### 7.3 Alternative: buy instead of build — balenaCloud

Worth naming honestly as the road not taken. **balenaCloud** gives a genuinely good single console
out of the box — device fleets and groups, remote terminal, over-the-air container deploys,
per-device variables that map cleanly onto "assign this device its group and URL." But the free
tier caps at **10 devices total**, and 30 devices lands on the **Prototype plan at $159/month**
($3/device beyond that) — a real recurring cost for what the rest of this plan treats as on-prem
and subscription-free. **Not recommended** at this fleet size, given the content-plane console is
being built regardless and extending it (§7.1) costs little by comparison — but documented here so
the trade-off is explicit rather than assumed away.

---

## 8. Snapshot Worker

For content that cannot be framed or that needs server-held credentials: a headless Chromium
service, on-prem, that logs in as needed, renders a target page on a schedule, and publishes an
image the player displays as a `Snapshot` pane.

It solves vendor auth and iframe refusal in one move. Its cost is freshness — a snapshot is as old
as its render interval, so it is unsuitable for anything needing the < 5s target. Mark snapshot
content clearly in the admin UI with its render age so nobody mistakes a 5-minute-old snapshot for
live data.

---

## 9. Risks

### 9.1 Common to both hardware paths

**Iframe refusal.** `X-Frame-Options` and CSP `frame-ancestors` will block a meaningful share of
target pages, regardless of what plays them.

- **Seaway-owned apps:** add `Content-Security-Policy: frame-ancestors https://signage.…` — cheap,
  but it is a change to each displayed app, so it belongs in *their* backlog too.
- **Vendor pages:** snapshot worker (§8).
- **Reverse-proxy header rewriting:** technically possible, fragile, and it silently defeats a
  security control the vendor deliberately set. Not recommended; listed only to be explicitly ruled
  out.

**Single point of failure.** Every screen depends on one on-prem app. Mitigated by the player's
local rotation cache (§3.2) — a server outage degrades to "screens show slightly stale content"
rather than "every screen in the building goes black." This is the highest-value resilience feature
in the plan and belongs in Phase 1, not later.

**Heartbeat beats vendor telemetry.** Worth stating plainly regardless of hardware: the player
heartbeat proves the entire chain — network up, browser alive, our site reachable, page actually
rendering. Any vendor-supplied device telemetry proves only that the hardware is powered and
online, a much weaker claim. The heartbeat should be treated as the primary health signal even on a
path where richer telemetry is also available.

**Smaller, genuinely hardware-agnostic risks.** Screen burn-in on static content (rotation itself
substantially mitigates this); display auto-sleep on a static HDMI input; overscan clipping the
edges of the page; timezone and NTP correctness; autoplay policy if any pane contains video.

### 9.2 Specific to the Amazon Signage Stick path (not applicable if the Pi path is chosen)

**Remote Management API eligibility.** Documented as available to "approved CMS providers." We are
an end customer, not a CMS vendor, and eligibility is genuinely unresolved — this was the single
largest open risk in the original version of this plan, and it's the main reason §2.5 recommends
the Pi instead.

**TLS on a locked-down webview.** The kiosk webview is very unlikely to trust an internal
certificate authority, and there is no practical way to install a root certificate on a locked-down
device with no pointer and no "proceed anyway" button. A publicly-trusted certificate on a
split-horizon internal name would be required, adding real setup time.

**Vega OS platform risk.** Amazon's Fire TV line is migrating to Vega OS, which does not run
Android APKs. Whatever is true about the stick's software flexibility today is not guaranteed to
remain true.

### 9.3 Specific to the Raspberry Pi path

**Chromium's own memory creep.** Real and reported independent of RAM tier — several tabs kept open
for a few days can exhaust available memory without mitigation. The `restart-browser` command
(§3.3) exists specifically for this; a nightly scheduled restart is cheap insurance regardless of
how comfortable the RAM headroom is.

**We own OS patching and hardening.** No vendor support line if something goes wrong — offset
somewhat by boards being cheap and quickly swappable, but real ongoing operational ownership that
the Signage Stick path would not have carried.

**RAM pricing volatility.** Covered in detail in §2.3 — get a live quote before ordering 30 units.

---

## 10. Shared Services & Standard Features

Per the reuse gate, a decision is recorded for **every** row, including skips.

> **Note:** `Seaway-Printing/seaway-templates` could not be reached from this session, so these
> decisions are based on the services skill summary rather than the live service cards. Re-check
> them against `reference/services/` before scaffolding.

| Service | Decision |
|---|---|
| **Seaway.AppSecurity** | **Yes** — admin UI. Internal users only. Roles: `SignageAdmin`, `SignageEditor`, `SignageViewer`. Possible second axis by site/location if signage goes multi-site; defer until it does. Remember: AppSecurity adds no role claims — `[Authorize(Roles = …)]` does **not** work; use `HttpContext.GetSeawayUser()?.IsInRole(...)`. |
| **Seaway.Notifications** | **Yes** — required regardless (AppSecurity depends on it, and startup reads channel config, so the app will not start without the DB grant). Real use: "display dark for > N minutes" alerts via email + Teams. Ship the `/Admin/Notifications` UI. |
| **Seaway.Storage** | **Yes**, narrowly — static fallback/notice assets (images, short video). One entity type, short retention. Not used for snapshots, which are transient and served from the worker's own cache. |
| **Seaway.JobEngine** | **No.** This app creates no Tharstern print jobs and has no JDF path. Explicitly skipped. |
| **Seaway.Portal** | **Yes** — tile for the admin UI, Operations / Internal Tools category. Check the Access Denied page reads sensibly to someone arriving cold from the launcher. |
| **Seaway.AdminUi** | **Yes** — default-on. `/Admin` with assembly version and act-as. Requires `CHANGELOG.md` and csproj version properties, changelog copied into publish output. Impersonation targets: `SignageEditor`, `SignageViewer` — never `SignageAdmin`. Take the package; do not hand-copy an `ImpersonationHelper`. |
| **Seaway.Mailer** | **No** — deprecated, never referenced. |

### 10.1 Not in either register

Two capabilities here have **no card**:

- **Device identity / kiosk display tokens** (§6.1)
- **On-prem headless page rendering** (§8)

Per the promotion rule this is the *first* app to need either, so no card is required yet. Flagging
it now so it is not a surprise: **if a second Seaway app needs display device tokens, that is the
moment it needs a card, a conformance definition and a named owner.**

### 10.2 Styling

- Admin UI and the display-only screens: **seaway-app-style** (Direction 1 — Bricolage Grotesque /
  Inter, orange/navy/teal/mustard/cream, warm borders, no drop shadows).
- Display screens need a deliberate variant of it: viewing distance is 3–10 metres, not 50cm.
  Expect to define larger type scale, heavier weights and higher-contrast pairings as an explicit
  "signage" extension rather than reusing app-screen sizing.
- Any install/runbook documentation produced: **seaway-document-style**. If it becomes a floor
  procedure, **seaway-sop-builder** for numbering.

---

## 11. Phasing

### Phase 0 — Spikes (2–3 units, ~1 week)

Buy a small batch — two or three Pi 4/5 (4GB) units and a couple of screens — and answer the
questions that would change the plan:

- [ ] Does Chromium keeping 3–4 tabs warm hold up on a 4GB board under real Seaway content
      (dashboards, live-updating pages) without hitting the memory creep in §9.3 within a day?
- [ ] Does a USB SSD boot cleanly and survive a hard power cut without corruption?
- [ ] Does the rotation pane-swap avoid a visible blank flash on the pages that stay warm?
- [ ] Does HDMI-CEC work against the actual target screens, for a scheduled power-off?
- [ ] Confirm current Pi 4 vs Pi 5 (4GB) pricing before committing to a SKU across 30 units (§2.3).

*If the Signage Stick path is chosen instead*, run this checklist against one stick and one screen:

- [ ] Can the Amazon Signage console point a stick at an **arbitrary URL**, and does it persist
      across reboot and power loss?
- [ ] Can Remote Management API credentials be obtained as an end customer (§9.2)?
- [ ] Does a publicly-trusted certificate on a split-horizon internal name work on the stick's
      webview (§9.2)?
- [ ] Does the kiosk webview support SSE, keeping multiple pages warm, and `localStorage`?

**Phase 0 is a gate.** Nothing else starts until it answers cleanly.

### Phase 1 — Thinnest vertical slice (~1 week)

One group, one rotation of 2–3 real pages, the pairing flow, the local rotation cache, the
heartbeat. **Exit criterion: a screen rotates correctly and survives a weekend, including an
overnight power cut, with nobody touching it.** That single test retires most of the operational
risk in this plan.

### Phase 2 — The actual app (~3–4 weeks)

Admin UI on AppSecurity, Portal tile, `/Admin` page, groups, playlists/rotation, SSE control plane
(including the device commands from §3.3 and §7.1 on the Pi path), display health dashboard,
dark-display alerts via Notifications.

### Phase 3 — Content depth (~2 weeks)

Schedules (shift changes), overrides, snapshot worker, native display-only screens, signage type
scale.

### Phase 4 — Device plane, Signage Stick path only (~1 week + procurement lead time, **conditional**)

Only relevant if the Signage Stick path was chosen instead of the Pi: Remote Management API,
nightly power schedule, remote reboot, hardware telemetry merged into the health dashboard. On the
Pi path this work is already folded into Phase 2 (§7.1) and there is no separate Phase 4.

---

## 12. Open Questions for Seaway

1. **Confirm the hardware path** — this plan recommends the Pi (§2.5); if the Signage Stick is
   still preferred, Phase 0 changes accordingly and the risks in §9.2 need direct answers first.
2. **Which existing internal applications are in the content mix?** Each needs a `frame-ancestors`
   change and a `/display` read-only route — work that belongs in their backlogs, not this one.
3. **Which vendor pages?** Determines snapshot worker credential handling and render cadence.
4. **Wired or Wi-Fi, and which VLAN?** Wired is strongly preferred for always-on displays.
5. **Who owns the fleet operationally** — specifically, who receives the dark-display alert at six
   in the morning?
6. **How many sites, beyond the ~30-display count already confirmed?** Drives whether
   `DisplayGroup` needs a site axis and whether one on-prem instance is enough.

---

## Sources

- [Why the Amazon Signage Remote Management API is a game changer](https://signage.amazon.com/blog/why-the-amazon-signage-remote-management-api-is-a-game-changer-for-you-and-your-customers)
- [Remote Management for Digital Signage — Amazon Signage Stick](https://signage.amazon.com/remote-management)
- [Amazon Signage Stick API Documentation](https://signage.amazon.com/api-documentation)
- [Amazon launches Vega OS for Fire TV — AFTVnews](https://www.aftvnews.com/amazon-launches-vega-os-for-fire-tv-heres-how-it-affects-new-old-fire-tvs-apps-and-sideloading/)
- [Raspberry Pi 5 price increases — Tom's Hardware](https://www.tomshardware.com/raspberry-pi/raspberry-pi-5-price-increases-drastically-as-ai-shortage-bites-16gb-version-now-usd205-second-price-increase-in-three-months-over-70-percent-more-expensive-than-original-msrp)
- [1GB Raspberry Pi 5 pricing — Raspberry Pi Foundation](https://www.raspberrypi.com/news/1gb-raspberry-pi-5-now-available-at-45-and-memory-driven-price-rises/)
- [Chromium RAM behaviour on Raspberry Pi — Raspberry Pi Forums](https://forums.raspberrypi.com/viewtopic.php?t=384545)
- [Chromium minimum RAM warning — GitHub](https://github.com/Manawyrm/AnotterKiosk/issues/32)
- [Ansible for Raspberry Pi fleets — Opensource.com](https://opensource.com/article/20/9/raspberry-pi-ansible)
- [Ansible Pi kiosk role example](https://github.com/stevewoolley/pi-fleet)
- [balenaCloud pricing](https://www.balena.io/pricing)

> Note: `signage.amazon.com` is blocked by this environment's network egress proxy, so the Amazon
> pages above were summarised from search results rather than read directly. Verify the Remote
> Management API details against the live documentation if the Signage Stick path is pursued.
