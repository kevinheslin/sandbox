# Seaway Signage

Drives Seaway's signage display fleet — up to ~30 Raspberry Pi–based displays, grouped so some
show identical rotating content and others show their own distinct rotation, all pulling from
on-prem, continuously-updating pages.

**Status:** scaffold only. No business logic has shipped yet — Phase 0 (a hardware validation
spike) gates the start of real development. See [`IMPLEMENTATION_PLAN.md`](./IMPLEMENTATION_PLAN.md)
for the current phase and milestone tracker.

## What and why

The full architecture — why Raspberry Pi over the Amazon Signage Stick, the content/device-plane
split, the data model, the SSE control protocol, auth, and the shared-services decisions — is
written up in [`docs/signage-streaming-plan.md`](./docs/signage-streaming-plan.md). Read that
first; this README doesn't restate it.

## Architecture at a glance

Two runtime deployables:

- **`Seaway.Signage.Web`** — one ASP.NET Core app hosting the Admin UI (AppSecurity-gated), the
  device API and SSE hub (bearer-token gated, under `/api/**`), and the player shell served to
  every display at `/play`.
- **`Seaway.Signage.SnapshotWorker`** — a separate Worker Service that headlessly renders pages
  that can't be framed or that need server-held vendor credentials.

Plus a non-.NET device agent (`/device-agent`, Go) running on each Pi, and Ansible playbooks
(`/ansible`) for fleet provisioning. See `IMPLEMENTATION_PLAN.md` §1 for the full solution layout
and the reasoning behind this split.

## Repo layout

```
/src            ASP.NET Core solution: Web, SnapshotWorker, Domain, Data
/device-agent   Go, cross-compiled linux/arm64 — runs on each Pi
/ansible        fleet provisioning playbooks
/tests          unit/integration/E2E test projects, matching the src layout
/docs           the design doc (source of truth for "what/why")
```

## Prerequisites

- .NET 10 SDK
- Node.js (player-shell tooling)
- Go toolchain (device agent)
- Docker (local SQL Server/SQLite for dev)
- Ansible (provisioning)
- Access to Seaway's internal NuGet feed

**Known gap:** `Seaway.AppSecurity`, `Seaway.Notifications`, `Seaway.Storage`, `Seaway.Portal`, and
`Seaway.AdminUi` are private Seaway packages on that internal feed. This scaffold was built in an
environment without access to it, so `Seaway.Signage.Web`'s `Program.cs` marks exactly where each
package wires in with comments rather than working calls, and has **not** been build-verified —
neither against that gap nor at all, since no .NET SDK was available in the environment that
created this scaffold either. Treat the whole `src/` tree as unverified until someone with the
real toolchain and feed access builds it for the first time.

## Getting started

Once the above prerequisites are in place:

```
dotnet restore
dotnet build
dotnet run --project src/Seaway.Signage.Web
```

Local device simulation without a Pi: `POST /api/enroll` to get a pairing code, claim it in
`/Areas/Admin`, then open `/play?deviceToken=<token>` in a browser tab.

## Shared services used

| Service | Decision |
|---|---|
| Seaway.AppSecurity | Yes — Admin UI auth. Roles via `HttpContext.GetSeawayUser()?.IsInRole(...)`, **not** `[Authorize(Roles=...)]`, which AppSecurity does not support. |
| Seaway.Notifications | Yes — required at startup regardless; real use is the dark-display alert. |
| Seaway.Storage | Yes, narrowly — static fallback/notice assets only. |
| Seaway.JobEngine | No — this app creates no Tharstern print jobs. |
| Seaway.Portal | Yes — launcher tile. |
| Seaway.AdminUi | Yes — default-on `/Admin` page. |
| Seaway.Mailer | No — deprecated. |

Full reasoning for each row is in the design doc's §10.

## Deploying

Not yet applicable — see `IMPLEMENTATION_PLAN.md` for the CI/CD plan and the Phase this unblocks.

## Device agent & fleet provisioning

`/device-agent` (Go) runs on each Pi as a systemd service, talking to the same SSE endpoint as the
browser player to execute reboot/restart-browser commands and report health. `/ansible` provisions
a fresh Pi: Chromium kiosk setup, the agent binary, both systemd units. Neither has real logic yet
— see the Phase 1/2 milestones in `IMPLEMENTATION_PLAN.md`.

## Contributing

Branch per change, PR against `main`. CI (once configured, see `IMPLEMENTATION_PLAN.md`) gates
merge.

## Ownership / on-call

TODO — the design doc's §12 leaves "who owns the fleet operationally" as an open question. Fill
this in once answered; don't ship Phase 2's dark-display alerting without a real recipient.

## Changelog

See [`CHANGELOG.md`](./CHANGELOG.md).
