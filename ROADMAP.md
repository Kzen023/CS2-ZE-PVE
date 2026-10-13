# ROADMAP

Keep this file short. Detailed design belongs in `docs/` and component repositories.

Research-driven architecture priorities are summarized in `docs/CODE_RESEARCH_FINDINGS.md`. Migration-time execution authority is defined in `MIGRATION_AUTHORITY.md`. The current Navigation/traversal version split is recorded in `docs/VERSION_SCOPE_RESET.md`; the 2026-10-11 full-project audit and execution handoff are recorded in `docs/PROJECT_AUDIT_20261011.md`. Persistent ZE/ZR map knowledge is defined in `docs/MAP_TRAVERSAL_GRAPH.md`; Bot-Improver research and future interoperability are recorded in `docs/CS2_BOT_IMPROVER_RESEARCH_20261014.md`.

## Completed foundations

### v0.1 — Integration baseline

- [x] Bot-only zombie concept
- [x] Human path recording concept
- [x] Bot respawn/recovery teleport concept
- [x] Establish ZEPVE project-family repositories
- [x] Preserve the current working plugin sources under `legacy/` as the migration/regression baseline
- [x] Register a pinned `ZEPVE-Navigation` design/integration revision in the suite
- [x] Reproduce the legacy source builds and record known dependency/runtime requirements, including unresolved deployed-ZRPVE provenance
- [x] Establish a known-good deployed/rollback baseline tied to recorded source/config/dependency evidence
- [x] Establish reproducible suite/component build entry points for the completed baseline components
- [x] Create release staging under `game/csgo`
- [x] Generate release-manifest foundations with source/API/file hashes and recorded revisions

v0.1 is closed as the historical integration baseline. Exact matched component/native artifact provenance for the production Navigation package is intentionally carried forward as a v0.3 release gate rather than left as unfinished v0.1 work.

### v0.2 — Safe Core/BotAI migration foundation

#### v0.2a — Baseline + authority contract

- [x] document reproducible legacy build entry points, known dependency/runtime inputs and unresolved provenance gaps
- [x] record the known-good deployed/rollback baseline and source-vs-deployed uncertainty
- [x] maintain `MIGRATION_AUTHORITY.md` with current writer, migration writer, disable path and rollback rule for state-changing behavior
- [x] distinguish active, dormant and currently-unused legacy configuration/features
- [x] define the minimum CS2Fixes/ZombieReborn compatibility adapter boundary for round/respawn policy
- [x] keep legacy as the only gameplay writer while the observer/baseline contract was being established

v0.2a is closed as the historical authority baseline. `docs/LEGACY_ARCHITECTURE.md` records the legacy build/dependency evidence and source/runtime uncertainty; `MIGRATION_AUTHORITY.md` records the writer handoff and rollback contract.

Do not add a second respawn scheduler, change infection timing, introduce new AI behavior or rewrite Trail/recovery in this stage.

#### v0.2b — Core registry + identity + lifecycle invalidation

- [x] extract/stabilize the minimum `ZEPVE.Abstractions` contracts needed by Core
- [x] create one Core-owned `PlayerRegistry` as the identity source
- [x] expose Human/Zombie views from the same registry rather than maintaining separate competing registries
- [x] track map epoch, round epoch, connection identity/generation and pawn/spawn generation
- [x] invalidate delayed work on disconnect, pawn replacement, round/map change and unload
- [x] define hot/late-load initialization behavior
- [x] add bounded lifecycle diagnostics / Core status queries
- [x] add tests for delayed-action validity and slot reuse

The observer-only v0.2b stage originally completed deterministic/model verification before authority handoff. Subsequent accepted v0.2c matched runtime exercised the same Core identity/lifecycle foundation on the real server; deferred cases remain documented separately. See `docs/CORE_OBSERVER_VERIFICATION.md` and `docs/CORE_MATCHED_RUNTIME_VERIFICATION.md`.

At the end of v0.2b, legacy remained the gameplay writer. Core first proved that it could observe identity/lifecycle correctly without changing verified gameplay.

#### v0.2c — Core lifecycle authority handoff

- [x] move existing round/lifecycle actions from legacy into Core one responsibility at a time
- [x] migrate Bot quota/team-transition authority with exactly one active writer in the paired package
- [x] preserve current static human-count/profile behavior during the handoff
- [x] define round-end/map-end cancellation for delayed actions
- [x] keep respawn policy and executor explicitly separated; retain the ZombieReborn/compatibility executor
- [x] update `MIGRATION_AUTHORITY.md` with the source handoff and explicit installed-runtime distinction
- [x] add bounded Flight Recorder events for lifecycle actions, rejection and invalidation
- [x] verify the paired v0.2c handoff on the real server (single-player dust2/mirage smoke; broader tests deferred)

Source/model validation and matched single-player runtime acceptance are complete; PR #10 merged to `main @ ed480bd` with owner authorization. See `docs/CORE_MATCHED_RUNTIME_VERIFICATION.md` for unchanged deferred acceptance. PR #9 observer evidence remains historical.

Do not introduce dynamic Director logic, a new respawn gameplay model or a complex BotPool merely to complete Core ownership.

#### v0.2d — BotAI ownership + observability

- [x] create `ZEPVE.BotAI.dll` inside the main repository
- [x] introduce authoritative `AssignedTarget` as a new ZEPVE capability
- [x] implement dual-identity validity, rebinding and stable balanced multi-human distribution
- [x] define narrow BotAI/Navigation contracts in Abstractions
- [x] add bounded ObserveOnly reacquisition baseline (native assist candidates unvalidated)
- [x] add BotAI diagnostics for AssignedTarget, Valve Enemy and reacquire state
- [x] bounded target/awareness/combat FlightRecorder
- [x] add admin status/events/reacquire/binding-probe commands

127 model/source checks pass. Matched single-human runtime smoke passed after repairing the startup native-clock defect, with actual initial whole rollback verified. Detailed PASS/PARTIAL/NOT TESTED is separate in `docs/BOTAI_VERIFICATION.md`. Native perception writes are absent and ObserveOnly is not a causal aggro repair mechanism. The v0.2d BotAI baseline was accepted and merged in PR #11; broader multiplayer/awareness work remains deferred. See bilingual `docs/BOTAI_RUNTIME.md`.

Deferred research, not a v0.2 completion gate:

- Valve BotProfile field experiments remain evidence work for later difficulty/Director integration.
- A BotProfile adapter is added only if a later phase proves a concrete missing capability and safe mutation boundary.
- Candidate awareness operations remain v0.4/Lab evidence work and must be validated one operation at a time.

`AssignedTarget` did not exist in the legacy baseline; it is a new capability, not behavior-preserving extraction. BotAI should preserve useful Valve native perception and use bounded/event-triggered awareness assistance rather than permanent per-tick AI-state forcing.

See `BOT_AI_DESIGN.md`, `MIGRATION_AUTHORITY.md` and `docs/CODE_RESEARCH_FINDINGS.md`.

The accepted v0.2 baseline is complete. Remaining broader multiplayer, BotProfile and awareness experiments are explicitly deferred and do not reopen v0.2.

## Current

### v0.3 — Navigation baseline

PR #11 establishes the accepted v0.2 complete baseline. Lab PR #1 proved the existing-dispatcher lease/validator and managed detach/drain safety model. Production implementation then advanced beyond the original milestone into timed traversal experiments; those advanced capabilities are explicitly deferred to v0.4 rather than remaining v0.3 merge blockers.

Historical clean isolation cuts are Navigation `aa50134`, BotController `df67e21`, and suite reference `b8f156a`. The Astra audit confirms these are valid **pre-timed isolation points**, not release-ready revisions. See `docs/PROJECT_AUDIT_20261011.md`.

Navigation baseline:

- [x] bounded per-Human Trail identity/generation/sequence model
- [x] TrailSegment/discontinuity semantics and important marker capture
- [x] consume BotAI-owned `AssignedTarget` through narrow suite contracts
- [x] target rebind invalidates old route/recovery state
- [x] progress-based stuck detection with legitimate waiting states implemented
- [x] Native / Trail driver switching and hysteresis implemented
- [x] layered Recovery implementation and BotAI reacquire boundary implemented
- [x] structured Navigation state/events and movement authority diagnostics
- [x] safe leased UserCmd movement backend with application-time validation and managed drain
- [x] isolate clean v0.3 review lines: BotController #3, Navigation #3, Suite #15
- [ ] correct/isolate baseline pursuit defects in the clean Navigation line: failed-action cooldown scope, near-target yield, false action-state advance and oscillation/progress cases
- [ ] unknown Recovery map permission fails closed; geometry alone is not permission
- [ ] bind suite gitlink, component checkout, source revision, ABI/build profile, produced native artifact, manifest hashes and install inventory
- [ ] basic real runtime acceptance: flat ground, normal ramps, distant/stationary target pursuit and no-NAV Trail fallback
- [ ] basic two-Human target/Trail isolation smoke
- [ ] matched death/respawn, disconnect/reconnect, round/map and reload/drain acceptance
- [ ] stopped-server install/hash validation and actual whole-package rollback
- [ ] identify the first runtime-verified v0.3 component revisions suitable for production pinning

v0.3 already uses UserCmd movement axes/buttons through the leased backend. It does **not** require timed/signed-subtick traversal payloads, precise box/crouch/rotational jumps, scoped traversal view or automatic MotionClip capture. Those belong to v0.4 even where experimental code already exists.

Integration repository:

- [x] choose clean pre-timed component PRs instead of a release-time advanced-traversal feature gate
- [ ] synchronize clean candidate ADR/documentation with current main without changing authority ownership
- [ ] validate the matched v0.3 package with current Core + BotAI
- [ ] bump component gitlinks only through reviewed component/integration PRs
- [ ] record exact revisions, build provenance, acceptance evidence and rollback in release metadata

## Next

### v0.4 — Advanced traversal + awareness recovery

Lab / evidence already established:

- [x] movement takeover PoC in the existing dispatcher/router
- [x] bounded Human command capture and W/A/D/JUMP/duck replay PoC
- [x] signed subtick timing preservation for the tested Windows/64Hz profile
- [x] scoped leased traversal-view PoC through the same owner/router
- [x] cancel/replace/lifecycle/drain safety for bounded playback experiments

Production traversal work:

- [ ] keep ordinary Native/Trail steering separate from exceptional traversal execution
- [ ] typed local traversal action with Entry / Execute / Exit / Abort and local failure scope
- [ ] timed/signed-subtick command playback only for actions that require it
- [ ] jump and crouch-jump assistance with explicit geometry/entry conditions
- [ ] short bounded `MotionClip` executor for isolated maneuvers; never a route planner or general replay system
- [ ] scoped view/facing ownership only while an explicitly authorized traversal action needs it; ordinary pursuit must not acquire view
- [ ] entry conditions include relevant position/velocity/ground/support/collision/facing state instead of one exact recorded point
- [ ] playback completion and obstacle/action success are separate results
- [ ] continuous traversal handoff without stale replay or global cooldowns
- [ ] expose bounded movement/view ownership state so optional cooperating enhancers can yield rather than fight traversal
- [ ] resolve current experimental failures: 180-degree yaw oscillation, low-wall stalls, early/late takeoff and start-state sensitivity
- [ ] real complex-terrain acceptance on representative ZE obstacles with Recovery disabled during traversal proof

BotAI awareness work after evidence:

- [ ] event-triggered AwarenessAssist for spawn/respawn/recovery/rebind
- [ ] smallest validated look-around/ignore-enemies/alert state reset
- [ ] bounded reacquisition window with success/failure diagnostics
- [ ] preserve Valve native hearing/perception where useful
- [ ] explicit Enemy/visibility writes only if narrower validated approaches fail

Bot-Improver source review may supply candidate mechanisms, but broad process-wide AI patches/direct velocity writes/permanent visibility forcing are not implicitly authorized. Validate one operation/domain at a time.

Advanced traversal PRs remain Draft until their own production acceptance; failures here do not reopen v0.3 baseline scope.

### v0.5 — Persistent Routes & Traversal Graph

Canonical design: `docs/MAP_TRAVERSAL_GRAPH.md`; Navigation implementation plan: `ZEPVE-Navigation/RECORDED_ROUTES.md` and `BOT_IMPROVER_INTEGRATION.md`.

Shared graph/runtime:

- [ ] versioned `MapTraversalGraph` with stable Region / Route / RouteNode / TraversalEdge identity
- [ ] typed persistent traversal edges such as Walk, Jump, CrouchJump, Drop, Ladder, Door, Elevator, Wait and exceptional MotionClip
- [ ] spatial Entry/Exit volumes with direction/availability/cost and stage/branch identity
- [ ] bounded spatial lookup for nearby Regions/EntryVolumes rather than global per-Bot edge scans
- [ ] entry latch/hysteresis and edge-local failure/cooldown
- [ ] safe save/load, schema versioning and backup behavior

ZE:

- [ ] curated `RecordedRoute` / stage / branch progress as persistent map knowledge
- [ ] bounded local backtracking; never reverse forbidden boundaries or `ForwardOnly` edges
- [ ] report route-unavailable to BotAI when AssignedTarget leaves the legal graph

ZR:

- [ ] target localization into reachability Regions
- [ ] dynamic graph path from Bot Region to target Region
- [ ] persistent movement-skill links for camp/high-ground access
- [ ] Native NAV for ordinary sections where useful; Runtime Trail fallback for unknown/unrecorded sections

Authoring / visualization:

- [ ] chat-first admin route/section recording (`record/stop/cancel/show/test/save/list` equivalents)
- [ ] temporary rich capture -> simplification/classification -> semantic graph draft
- [ ] optional bounded recent authoring buffer for `save last maneuver` style capture
- [ ] in-world Regions/nodes/edge arrows/types/Entry/Exit volumes/test state
- [ ] single-edge and route test
- [ ] safe local drafts plus explicit export/review into curated suite data
- [ ] runtime Bot graph/action diagnostics explaining current edge/state/failure

Main repository:

- [ ] define curated map-route storage under `maps/routes/`
- [ ] review/export flow from local recordings to version-controlled route data
- [ ] include curated graph schema/version in release validation

A minimal versioned read-only graph/runtime ownership context capability may be designed if it does not delay core graph acceptance. Generic Bot-Improver compatibility is **not** part of the v0.5 release gate.

Runtime Trail remains useful for unknown/unrecorded maps and dynamic fallback. Persistent graph data does not create a second locomotion authority. Route selection and traversal execution remain separate responsibilities.

### v0.6 — MapPlan + CS2Fixes integration

- [ ] introduce the minimum useful `MapConfig -> MapDefinition -> MapPlan -> MapRuntime` pipeline incrementally
- [ ] add one reusable Map entity selector model for targetname / HammerID / class / relevant Entity I/O matching
- [ ] define selector AND/OR semantics, zero/multiple matches and entity rebind behavior
- [ ] add public map/Boss state contracts so HUD never owns entity discovery
- [ ] preserve required ZR map semantics
- [ ] feed canonical stage/entity state into graph edge availability rather than generic Navigator hard-codes
- [ ] prevent Bot knife infection of humans
- [ ] synchronize respawn/nuke state with ZEPVE authority
- [ ] add the minimum trustworthy round/nuke signals needed by Core before broader map automation
- [ ] add compatibility tests using real-world ZE configuration corpus data
- [ ] create `ZEPVE-CS2Fixes` only if a maintained fork becomes necessary
- [ ] pin any fork/compatibility component explicitly in suite releases

### v0.7 — HUD + WeaponSystem migration

`ZEPVE-HUD`:

- [ ] extract presentation behavior from legacy `Kzen-ZEAssist`
- [ ] consume stable Core/Map/Boss/Navigation read-only state APIs instead of independently discovering map state
- [ ] establish reproducible build and component tests
- [ ] verify HUD lifecycle, center-channel arbitration and update cadence on map change/hot reload

`ZEPVE-WeaponSystem`:

- [ ] extract verified behavior from legacy `Kzen-WeaponBalance`
- [ ] separate config/catalog, purchase policy, runtime weapon application and damage policy
- [ ] preserve CS2Fixes compatibility boundaries
- [ ] allow later read-only Region/Stage context to inform policy without gaining Navigation authority
- [ ] verify legacy pricing/settlement, delayed-action identity and zombie-only damage semantics before claiming behavioral equivalence
- [ ] establish reproducible build and component tests

Suite integration:

- [ ] pin verified HUD/WeaponSystem revisions only after their component contracts are stable
- [ ] validate combined package behavior and rollback targets

## Later

```text
v0.8  Director / difficulty through Core spawn pressure + BotAI stable profile/perception policy
v0.9  map compatibility matrix + optional Bot-Improver adapters + offline MapAnalyzer + packaging/update/rollback hardening
v1.0  stable ZE-first PvE suite
```

Bot-Improver interoperability remains optional. Supported combinations require a mutation-domain matrix proving no competing movement/view/perception authority. `MapTraversalGraph` may provide read-only knowledge and Navigation-owned traversal requests; external plugins do not receive a bypass around MovementBridge.

## Repository Promotion Rule

Do not add another repository just because a module exists.

A new production repository must have a clear independent responsibility, stable integration contract, separate test value, and an explicit pin/update path into `CS2-ZE-PVE`.
