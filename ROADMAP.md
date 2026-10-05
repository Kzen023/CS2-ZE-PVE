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

### v0.2 — Core / integration ownership

Main repository:

- [ ] Extract/stabilize `ZEPVE.Abstractions`
- [ ] Stabilize human/Bot lifecycle and round authority
- [ ] Add status/debug commands
- [ ] Define the capability boundary with Navigation
- [ ] Add integration tests for component load/unload and map changes

### v0.3 — ZEPVE-Navigation component baseline

Navigation repository:

- [ ] TrailRecorder + bounded Trail history
- [ ] TrailSegment transitions
- [ ] progress-based stuck detection
- [ ] layered Recovery pipeline
- [ ] Valve NAV / Trail mode switching
- [ ] identify the first component revision suitable for suite pinning

Integration repository:

- [ ] pin the verified Navigation revision
- [ ] validate it with the current Core
- [ ] record its commit/version in release metadata

### v0.4 — No-NAV TrailDriver

Lab first:

- [ ] BotController movement takeover PoC
- [ ] UserCmd W/A/D/JUMP injection PoC
- [ ] jump/duck/forced-movement edge-case tests

Navigation production work after evidence:

- [ ] UserCmd-driven Trail movement backend
- [ ] jump assist
- [ ] TrailSkip
- [ ] segment-transition recovery
- [ ] multi-Bot target distribution

### v0.5 — Map + CS2Fixes integration

- [ ] preserve required ZR map semantics
- [ ] prevent Bot knife infection of humans
- [ ] synchronize respawn/nuke state with ZEPVE authority
- [ ] add map signal API
- [ ] create `ZEPVE-CS2Fixes` only if a maintained fork becomes necessary
- [ ] pin any fork/compatibility component explicitly in suite releases

## Later

```text
v0.6  HUD + Weapons
v0.7  Director / difficulty
v0.8  map compatibility matrix + packaging polish
v0.9  release/update/rollback hardening
v1.0  stable ZE-first PvE suite
```

## Repository Promotion Rule

Do not add another repository just because a module exists.

A new production repository must have a clear independent responsibility, stable integration contract, separate test value, and an explicit pin/update path into `CS2-ZE-PVE`.
