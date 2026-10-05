# ROADMAP

Keep this file short. Detailed design belongs in `docs/` and component repositories.

## Current

### v0.1 — Integration baseline

- [x] Bot-only zombie concept
- [x] Human path recording concept
- [x] Bot respawn/recovery teleport concept
- [x] Establish ZEPVE project-family repositories
- [ ] Import the current working PvE source into `CS2-ZE-PVE`
- [ ] Add `ZEPVE-Navigation` as a pinned production component
- [ ] Establish reproducible suite + component builds
- [ ] Create release staging under `game/csgo`
- [ ] Generate a release manifest with exact dependency/component revisions

## Next

### v0.2 — Core + BotAI ownership

Main repository:

- [ ] Extract/stabilize `ZEPVE.Abstractions`
- [ ] Stabilize human/Bot lifecycle and round authority
- [ ] Create `ZEPVE.BotAI.dll` inside the main repository
- [ ] Move authoritative `AssignedTarget` ownership into BotAI
- [ ] Implement target validity, rebinding and multi-human distribution
- [ ] Define narrow BotAI/Navigation contracts in Abstractions
- [ ] Add `AwarenessAssist` / `TargetReacquireService` baseline
- [ ] Add BotAI diagnostics for assigned target, Valve Enemy and reacquire state
- [ ] Add status/debug commands
- [ ] Add integration tests for component load/unload and map changes

BotAI should preserve useful Valve native perception where possible. Start with bounded/event-triggered awareness assistance rather than permanent per-tick AI-state forcing.

See `BOT_AI_DESIGN.md`.

### v0.3 — ZEPVE-Navigation component baseline

Navigation repository:

- [ ] TrailRecorder + bounded Trail history
- [ ] TrailSegment transitions
- [ ] consume BotAI-owned `AssignedTarget` through a narrow target-provider interface
- [ ] progress-based stuck detection
- [ ] layered Recovery pipeline
- [ ] native navigation / Trail mode switching
- [ ] post-recovery BotAI reacquisition handshake
- [ ] identify the first component revision suitable for suite pinning

Integration repository:

- [ ] pin the verified Navigation revision
- [ ] validate it with current Core + BotAI
- [ ] record its commit/version in release metadata

### v0.4 — No-NAV TrailDriver + awareness recovery

Lab first:

- [ ] movement takeover PoC
- [ ] UserCmd W/A/D/JUMP injection PoC
- [ ] jump/duck/forced-movement edge-case tests
- [ ] record Valve enemy/visibility/alert/look-around state before and after recovery teleport
- [ ] test look-around reset after teleport
- [ ] test wake/allow-active + clear IgnoreEnemies + short Alert window
- [ ] verify Valve native sound investigation remains useful
- [ ] test whether temporary follow/goal-binding to `AssignedTarget` restores normal enemy acquisition
- [ ] test explicit Enemy-state writes only if normal reacquisition remains unreliable

BotAI production work after evidence:

- [ ] event-triggered AwarenessAssist for spawn/respawn/recovery/rebind
- [ ] smallest validated look-around/ignore-enemies/alert state reset
- [ ] bounded reacquisition window with success/failure diagnostics
- [ ] avoid custom footstep/gunshot hate tables unless native hearing proves insufficient

Navigation production work after evidence:

- [ ] UserCmd-driven Trail movement backend
- [ ] jump assist
- [ ] TrailSkip
- [ ] segment-transition recovery
- [ ] Recovery TP -> route/navigation rebind -> BotAI reacquire request
- [ ] continue pursuing BotAI `AssignedTarget` while Valve Enemy is null/stale

### v0.5 — Recorded Routes and authoring tools

Navigation repository:

- [ ] persistent `RecordedRoute` schema and validation
- [ ] `RecordedRouteSource` compatible with TrailDriver
- [ ] admin-only in-game route recording commands
- [ ] route visualization and test mode
- [ ] safe save/load, schema versioning and backup behavior
- [ ] route simplification and segment preservation

Main repository:

- [ ] define curated map-route storage under `maps/routes/`
- [ ] review/export flow from local recordings to version-controlled route data
- [ ] include curated route schema/version in release validation

Runtime Trail remains the default no-NAV route source. Recorded Routes are optional supplements for difficult sections and repeatable testing.

### v0.6 — Map + CS2Fixes integration

- [ ] preserve required ZR map semantics
- [ ] prevent Bot knife infection of humans
- [ ] synchronize respawn/nuke state with ZEPVE authority
- [ ] add map signal API
- [ ] create `ZEPVE-CS2Fixes` only if a maintained fork becomes necessary
- [ ] pin any fork/compatibility component explicitly in suite releases

## Later

```text
v0.7  HUD + Weapons
v0.8  Director / difficulty
v0.9  map compatibility matrix + packaging/update/rollback hardening
v1.0  stable ZE-first PvE suite
```

## Repository Promotion Rule

Do not add another repository just because a module exists.

A new production repository must have a clear independent responsibility, stable integration contract, separate test value, and an explicit pin/update path into `CS2-ZE-PVE`.
