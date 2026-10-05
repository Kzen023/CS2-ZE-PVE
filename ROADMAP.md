# ROADMAP

Keep this file short. Detailed design belongs in `docs/`.

## Current

### v0.1 — Existing ZEPVE baseline

- [x] Bot-only zombie concept
- [x] Human path recording
- [x] Bot respawn/recovery teleport concept
- [ ] Import current working source into repository
- [ ] Establish reproducible build

## Next

### v0.2 — Core + Abstractions

- [ ] Extract shared contracts
- [ ] Define module ownership
- [ ] Stabilize Bot lifecycle
- [ ] Add status/debug commands

### v0.3 — Navigation extraction

- [ ] TrailRecorder
- [ ] TrailSegment
- [ ] progress-based stuck detection
- [ ] Recovery pipeline
- [ ] Valve NAV / Trail mode switching

### v0.4 — No-NAV TrailDriver

- [ ] BotController movement PoC
- [ ] UserCmd-driven Trail movement
- [ ] jump assist
- [ ] TrailSkip
- [ ] segment transition recovery

### v0.5 — Map + CS2Fixes integration

- [ ] preserve ZR map semantics
- [ ] prevent Bot knife infection of humans
- [ ] synchronize respawn/nuke state
- [ ] map signal API

## Later

```text
v0.6  HUD + Weapons
v0.7  Director / difficulty
v0.8+ map compatibility and polish
v1.0  stable ZE-first PvE runtime
```
