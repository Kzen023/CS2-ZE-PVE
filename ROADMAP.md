# ROADMAP

Keep this file short. Detailed design belongs in `docs/` and component repositories.

Research-driven architecture priorities are summarized in `docs/CODE_RESEARCH_FINDINGS.md`. Migration-time execution authority is defined in `MIGRATION_AUTHORITY.md`.

## Current

### v0.1 — Integration baseline

- [x] Bot-only zombie concept
- [x] Human path recording concept
- [x] Bot respawn/recovery teleport concept
- [x] Establish ZEPVE project-family repositories
- [x] Preserve the current working plugin sources under `legacy/` as the migration/regression baseline
- [x] Register a pinned `ZEPVE-Navigation` design/integration revision in the suite
- [ ] Reproduce the legacy build and record dependency/runtime requirements
- [ ] Establish a known-good rollback package tied to exact source/config/dependency evidence
- [ ] Establish reproducible suite + component builds
- [ ] Create release staging under `game/csgo`
- [ ] Generate a release manifest with exact dependency/component revisions

The currently pinned Navigation revision is an integration/design reference. It is not considered a runtime-verified production Navigation implementation until the component baseline is implemented and tested.

## Next

### v0.2 — Safe Core/BotAI migration foundation

#### v0.2a — Baseline + authority contract

- [ ] document a reproducible build entry point and exact dependency/runtime inputs for all legacy projects
- [ ] record the known-good deployed/rollback baseline and any source-vs-deployed uncertainty
- [ ] maintain `MIGRATION_AUTHORITY.md` with current writer, migration writer, disable path and rollback rule for state-changing behavior
- [ ] distinguish active, dormant and currently-unused legacy configuration/features
- [ ] define the minimum CS2Fixes/ZombieReborn compatibility adapter boundary for round/respawn policy
- [ ] keep legacy as the only gameplay writer while this baseline is being established

Do not add a second respawn scheduler, change infection timing, introduce new AI behavior or rewrite Trail/recovery in this stage.

#### v0.2b — Core registry + identity + lifecycle invalidation

- [ ] extract/stabilize the minimum `ZEPVE.Abstractions` contracts needed by Core
- [ ] create one Core-owned `PlayerRegistry` as the identity source
- [ ] expose Human/Zombie views from the same registry rather than maintaining separate competing registries
- [ ] track map epoch, round epoch, connection identity/generation and pawn/spawn generation
- [ ] invalidate delayed work on disconnect, pawn replacement, round/map change and unload
- [ ] define hot/late-load initialization behavior
- [ ] add bounded lifecycle diagnostics / Core status queries
- [ ] add tests for delayed-action validity and slot reuse

At the end of v0.2b, legacy remains the gameplay writer. Core first proves that it can observe identity/lifecycle correctly without changing verified gameplay.

#### v0.2c — Core lifecycle authority handoff

- [ ] move existing round/lifecycle actions from legacy into Core one responsibility at a time
- [ ] migrate Bot quota/team-transition authority with exactly one active writer
- [ ] preserve current static human-count/profile behavior during the handoff
- [ ] define round-end/map-end cancellation for delayed actions
- [ ] keep respawn policy and executor explicitly separated; retain the validated ZombieReborn/compatibility executor unless a replacement is proven
- [ ] update `MIGRATION_AUTHORITY.md` in the same PR whenever a writer actually changes
- [ ] add bounded Flight Recorder events for lifecycle actions, rejection and invalidation

Do not introduce dynamic Director logic, a new respawn gameplay model or a complex BotPool merely to complete Core ownership.

#### v0.2d — BotAI ownership + observability

- [ ] create `ZEPVE.BotAI.dll` inside the main repository
- [ ] introduce authoritative `AssignedTarget` as a new ZEPVE capability
- [ ] implement target validity, rebinding and multi-human distribution
- [ ] define narrow BotAI/Navigation contracts in Abstractions
- [ ] add `AwarenessAssist` / `TargetReacquireService` baseline
- [ ] add BotAI diagnostics for AssignedTarget, Valve Enemy and reacquire state
- [ ] extend the bounded per-Bot Flight Recorder with target/awareness transitions
- [ ] add status/debug/dump commands for Core and BotAI state

Lab work:

- [ ] validate which Valve BotProfile fields materially improve ZE zombie behavior
- [ ] prove the minimum safe BotProfile adapter path and its failure behavior
- [ ] validate candidate awareness operations one at a time rather than changing all fields together

`AssignedTarget` did not exist in the legacy baseline; it is a new capability, not behavior-preserving extraction. BotAI should preserve useful Valve native perception and use bounded/event-triggered awareness assistance rather than permanent per-tick AI-state forcing.

See `BOT_AI_DESIGN.md`, `MIGRATION_AUTHORITY.md` and `docs/CODE_RESEARCH_FINDINGS.md`.

### v0.3 — ZEPVE-Navigation component baseline

Navigation repository:

- [ ] TrailRecorder with one bounded Trail per human identity/generation
- [ ] monotonic Trail node sequence semantics suitable for ring-buffer eviction
- [ ] TrailSegment transitions using explicit discontinuity signals plus movement-context heuristics
- [ ] capture important movement markers even when normal positional sampling would skip the node
- [ ] consume BotAI-owned `AssignedTarget` through suite-defined narrow contracts
- [ ] invalidate old route/recovery/reacquire state after target rebinding
- [ ] progress-based stuck detection with legitimate waiting states
- [ ] driver-switch hysteresis / observation windows to avoid Native/Trail/Recovery oscillation
- [ ] layered Recovery pipeline
- [ ] separate recovery permission/map semantics from geometric destination validation
- [ ] native navigation / Trail mode switching
- [ ] post-recovery BotAI reacquisition handshake
- [ ] publish structured navigation state/events for suite diagnostics
- [ ] identify the first component revision suitable for runtime-verified production pinning

Integration repository:

- [ ] validate the first runtime-ready Navigation revision with current Core + BotAI
- [ ] bump the suite gitlink only through a component integration PR when needed
- [ ] record its commit/version and verification evidence in release metadata

### v0.4 — No-NAV TrailDriver + awareness recovery

Lab first:

- [ ] movement takeover PoC
- [ ] UserCmd W/A/D/JUMP injection PoC
- [ ] jump/duck/forced-movement edge-case tests
- [ ] record Valve enemy/visibility/alert/look-around state before and after recovery teleport
- [ ] test look-around reset after teleport
- [ ] test wake/allow-active + clear IgnoreEnemies + short Alert window
- [ ] verify Valve native sound investigation remains useful
- [ ] test whether temporary navigation assistance toward `AssignedTarget` restores normal enemy acquisition
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

### v0.6 — MapPlan + CS2Fixes integration

- [ ] introduce the minimum useful `MapConfig -> MapDefinition -> MapPlan -> MapRuntime` pipeline incrementally
- [ ] add one reusable Map entity selector model for targetname / HammerID / class / relevant Entity I/O matching
- [ ] define selector AND/OR semantics, zero/multiple matches and entity rebind behavior
- [ ] add public map/Boss state contracts so HUD never owns entity discovery
- [ ] preserve required ZR map semantics
- [ ] prevent Bot knife infection of humans
- [ ] synchronize respawn/nuke state with ZEPVE authority
- [ ] add the minimum trustworthy round/nuke signals needed by Core before broader map automation
- [ ] add compatibility tests using real-world ZE configuration corpus data
- [ ] create `ZEPVE-CS2Fixes` only if a maintained fork becomes necessary
- [ ] pin any fork/compatibility component explicitly in suite releases

### v0.7 — HUD + WeaponSystem migration

`ZEPVE-HUD`:

- [ ] extract presentation behavior from legacy `Kzen-ZEAssist`
- [ ] consume stable Core/Map/Boss state APIs instead of independently discovering map state
- [ ] establish reproducible build and component tests
- [ ] verify HUD lifecycle, center-channel arbitration and update cadence on map change/hot reload

`ZEPVE-WeaponSystem`:

- [ ] extract verified behavior from legacy `Kzen-WeaponBalance`
- [ ] separate config/catalog, purchase policy, runtime weapon application and damage policy
- [ ] preserve CS2Fixes compatibility boundaries
- [ ] verify legacy pricing/settlement, delayed-action identity and zombie-only damage semantics before claiming behavioral equivalence
- [ ] establish reproducible build and component tests

Suite integration:

- [ ] pin verified HUD/WeaponSystem revisions only after their component contracts are stable
- [ ] validate combined package behavior and rollback targets

## Later

```text
v0.8  Director / difficulty through Core spawn pressure + BotAI profile policy
v0.9  map compatibility matrix + offline MapAnalyzer + packaging/update/rollback hardening
v1.0  stable ZE-first PvE suite
```

## Repository Promotion Rule

Do not add another repository just because a module exists.

A new production repository must have a clear independent responsibility, stable integration contract, separate test value, and an explicit pin/update path into `CS2-ZE-PVE`.
