# Navigation version scope reset

Date: 2026-10-11

This document reconciles the roadmap with the implementation that exists in the current open Navigation, BotController and suite PRs. It is a scope/ownership correction, not a production PASS. The full audit summary and GPT-6.1 Sol handoff are in `docs/PROJECT_AUDIT_20261011.md`.

## Why this reset exists

The original v0.3 milestone was a Navigation baseline: consume BotAI `AssignedTarget`, own movement through one validated writer, maintain per-Human Trail state, degrade Native -> Trail -> Recovery, and survive lifecycle changes. During runtime work, the same PRs also accumulated timed command playback, signed subtick handling, scoped view ownership, automatic Human maneuver capture and complex traversal heuristics.

Those advanced capabilities remain useful, but they are not allowed to keep v0.3 open indefinitely.

## Version boundaries

### v0.3 — Navigation baseline

Required capabilities:

- BotAI `AssignedTarget` consumption through the narrow suite contract;
- one bounded Trail per Human identity/generation with monotonic sequence and segment validity;
- target rebind invalidates old route/recovery work;
- progress-based stuck detection with explicit legitimate waiting states;
- Native -> Trail -> Recovery layering and driver hysteresis;
- Navigation is the sole ZEPVE final movement/native-goal writer after handoff;
- leased UserCmd movement backend with application-time identity/lifecycle/revision/TTL validation;
- Recovery as a final fallback with route/target synchronization and bounded BotAI reacquire request;
- managed unload/reload drain against the process-resident movement backend;
- matched install, provenance, manifest and actual whole-package rollback.

Important correction from the full-project audit: **v0.3 already uses UserCmd movement axes/buttons.** The v0.4 boundary is not “first UserCmd movement”; it begins at richer traversal capability such as timed payloads, signed-subtick playback, scoped view/facing and MotionClip execution.

Runtime acceptance for v0.3 focuses on basic pursuit and lifecycle correctness: flat ground, normal ramps, basic no-NAV Trail following, distant/stationary targets, Native-to-Trail degradation, two-Human Trail isolation smoke, death/respawn, disconnect/reconnect, round/map changes and managed drain.

Not required for v0.3 acceptance:

- precise jump-box imitation;
- crouch-jump/rotational-jump parity;
- timed Human command playback;
- scoped traversal view replay;
- automatic MotionClip capture/selection;
- continuous complex traversal parity.

### v0.4 — Advanced traversal and awareness recovery

Movement/traversal scope:

- typed local action semantics: Entry / Execute / Exit / Abort;
- local failure/cooldown scope instead of global route suppression;
- timed/signed-subtick command payloads where a local action requires them;
- short bounded `MotionClip` execution for isolated maneuvers;
- scoped traversal view/facing through the same exclusive router/lease;
- explicit start-state conditions including relevant position/velocity/ground/support/collision/facing state;
- separate “command playback completed” from “physical obstacle/action succeeded”;
- continuous traversal handoff without stale replay or global cooldowns;
- evidence-based fixes for yaw oscillation, low-wall stalls, early/late takeoff and start-state sensitivity.

Ordinary Native/Trail pursuit does **not** own view. Backend support for scoped view is capability, not authorization.

BotAI awareness scope remains evidence-driven:

- bounded post-spawn/respawn/recovery/rebind assistance;
- preserve Valve native perception where possible;
- validate one native awareness operation at a time;
- explicit Enemy/visibility writes only if narrower approaches fail and are separately proven safe.

A `MotionClip` is an exceptional traversal executor. It is not the route planner and must not become a full gameplay replay system.

### v0.5 — Recorded routes, RouteGraph and authoring

Persistent route work becomes explicit here:

- versioned `RecordedRoute` schema and validation;
- `RouteGraph` / route-node representation;
- typed persistent traversal edges such as Walk, Jump, CrouchJump, Drop, Ladder, Door, Elevator and Wait;
- spatial Entry/Exit volumes or equivalent bounded conditions for special edges;
- authoring/recording/visualization/test tools;
- route simplification, segment preservation, safe save/load and backup;
- curated map-route data under `maps/routes/` with review/export flow.

Runtime Trail remains the default no-NAV route source. Persistent routes supplement difficult/repeatable sections; they do not replace the dynamic Trail baseline for normal map support.

## Current PR/commit classification

| Repository / PR | Historical v0.3 cut | Start of v0.4 advanced traversal | Current classification |
| --- | --- | --- | --- |
| `ZEPVE-BotController#3` | `df67e21` | n/a | clean v0.3 backend review line; valid cut, still Draft |
| `ZEPVE-Navigation#3` | `aa50134` | n/a | clean v0.3 Navigation review line; valid cut, still contains baseline defects to fix/isolate |
| `CS2-ZE-PVE#15` | `b8f156a` matched reference | n/a | clean v0.3 suite line; packaging/provenance/runtime/rollback still required |
| `ZEPVE-BotController#1` | `df67e21` | `1e486ee` | mixed history; current `aa42948` is v0.4 experimental on top of baseline |
| `ZEPVE-Navigation#1` | `aa50134` | `134065d` | mixed history; current `e28d230` is v0.4 experimental on top of baseline |
| `CS2-ZE-PVE#13` | `b8f156a` historical matched reference | later timed/view pins | mixed integration history; keep Draft |
| `ZEPVE-Lab#2` | n/a | research evidence for timed/subtick/view behavior | Lab evidence only; never ships |

## Baseline isolation decision

Decision: **use clean v0.3 baseline PRs instead of carrying the advanced traversal layer behind a release-time feature gate.**

Clean review lines:

```text
ZEPVE-BotController#3  -> df67e21
ZEPVE-Navigation#3     -> aa50134
CS2-ZE-PVE#15          -> b8f156a matched suite reference
```

The Astra audit validates these as isolation points, not as release-ready revisions.

### What still blocks v0.3

1. **Baseline pursuit behavior:** fix/isolate failed-action cooldown scope, near-target yield without real pursuit ownership, action-state advance when the action is not executed, and progress/waiting oscillation cases.
2. **Complex jump isolation:** later recorded-jump heuristics must not interfere with flat/ramp/basic Trail behavior. Later commits may be selectively mined for baseline fixes without importing timed/view/MotionClip capability.
3. **Recovery permission:** geometry/hull safety is not map-stage permission. Unknown map permission must fail closed.
4. **Artifact provenance:** suite gitlink, component checkout, source revision, native/shared ABI, supported build profile, produced binary, manifest hashes, installed inventory and rollback inventory must be bound and mismatch-rejected.
5. **Documentation/ADR sync:** the clean suite branch must carry current version-scope ADRs without reusing the same ADR numbers for different historical decisions.
6. **Real matched acceptance:** automated PASS does not replace real CS2 runtime/gameplay evidence or actual whole-package rollback.

## Advanced traversal audit conclusions

- The safe router/lease/drain/timing kernel remains worth keeping.
- Ordinary pursuit must not automatically request scoped facing/view merely because the backend can provide it.
- Automatic MotionClip capture/replay must not act as route selection.
- The intended v0.4 chain is:

```text
Route / Runtime Trail decides where
        ↓
typed local action decides what
        ↓
MotionClip optionally executes the short action
```

Persistent RouteGraph/authoring remains v0.5.

## PR disposition

- `ZEPVE-BotController#3`, `ZEPVE-Navigation#3`, `CS2-ZE-PVE#15`: **KEEP DRAFT** until Phase 1 baseline remediation, matched package acceptance and actual rollback are complete.
- `ZEPVE-BotController#1`, `ZEPVE-Navigation#1`, `CS2-ZE-PVE#13`: **KEEP DRAFT** as v0.4 experimental/mixed history.
- `ZEPVE-Lab#2`: **KEEP DRAFT** / evidence-only.
- do not relabel old FAIL/PARTIAL runtime evidence as PASS;
- do not make white-car/B-box/doghole/rotational-jump tuning a v0.3 completion condition;
- do not merge production PRs merely because they are mergeable.

## v0.3 acceptance gate

A v0.3 candidate is reviewable only when the matched package records at least:

- one movement writer / one active lease authority;
- AssignedTarget-driven pursuit independent of transient Valve Enemy state;
- flat/ramp pursuit without persistent idle or unnecessary jump spam;
- basic no-NAV Trail fallback;
- Native -> Trail degradation when Native makes no meaningful progress;
- a failed local action does not suppress unrelated valid route/fallback;
- two-Human Trail isolation smoke;
- bot death/external respawn;
- target disconnect/reconnect and rebind;
- round/map transition;
- managed consumer reload/drain;
- legacy movement/Trail/Recovery writer disabled in the matched candidate;
- unknown Recovery map permission does not teleport;
- exact source/pin/build/artifact/ABI/manifest provenance;
- stopped-server install/hash verification;
- actual whole-package rollback and old-hash verification.

Complex traversal remains useful v0.4 evidence, but it does not block this baseline gate.
