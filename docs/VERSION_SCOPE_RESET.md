# Navigation version scope reset

Date: 2026-10-11

This document reconciles the roadmap with the implementation that exists in the current open Navigation, BotController and suite PRs. It is a scope/ownership correction, not a claim that any experimental traversal branch is production-ready.

## Why this reset exists

The original v0.3 milestone was a Navigation baseline: consume BotAI `AssignedTarget`, own movement through one validated writer, maintain per-Human Trail state, degrade Native -> Trail -> Recovery, and survive lifecycle changes. During real runtime work, the same PRs also accumulated timed UserCmd playback, signed subtick handling, scoped view ownership, automatic Human maneuver capture, jump/crouch-jump experiments and continuous traversal heuristics.

Those advanced capabilities are useful, but keeping them as v0.3 merge blockers makes the baseline milestone open-ended. From this point forward, roadmap acceptance and PR review use the version boundaries below.

## Version boundaries

### v0.3 — Navigation baseline

Required capabilities:

- BotAI `AssignedTarget` consumption through the narrow suite contract.
- One bounded Trail per Human identity/generation with monotonic sequence and segment validity.
- Target rebind invalidates old route/recovery work.
- Progress-based stuck detection with explicit legitimate waiting states.
- Native -> Trail -> Recovery layering and driver hysteresis.
- Navigation is the sole ZEPVE final movement/native-goal writer after handoff.
- Recovery remains a final fallback and performs route/target synchronization plus bounded BotAI reacquire request.
- Managed unload/reload drains safely against the process-resident movement backend.
- Bounded status/events sufficient to diagnose the active driver and lifecycle rejection.
- Matched install and actual whole-package rollback.

Runtime acceptance for v0.3 focuses on basic pursuit and lifecycle correctness: flat ground, normal ramps, basic no-NAV Trail following, distant/stationary targets, Native-to-Trail degradation, death/respawn, disconnect/reconnect, round/map changes and at least basic two-Human Trail isolation.

Not required for v0.3 acceptance:

- precise jump-box imitation;
- crouch-jump fidelity;
- rotational jumps;
- timed Human command playback;
- scoped traversal view replay;
- automatic MotionClip capture/selection;
- continuous complex traversal parity.

These are v0.4 work even when code already exists on the current mixed-history PRs.

### v0.4 — Advanced traversal and awareness recovery

Movement/traversal scope:

- UserCmd-driven movement where ordinary Native/Trail steering is insufficient.
- Jump and crouch-jump assistance.
- Signed subtick/timed movement payloads.
- Short bounded `MotionClip` execution for isolated maneuvers.
- Scoped traversal view/facing ownership through the same exclusive router/lease.
- Entry/exit conditions, retry/cancel semantics and continuous traversal handoff.
- Evidence-based fixes for yaw oscillation, low-wall stalls, early/late takeoff and other complex terrain failures.

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
- typed traversal edges such as Walk, Jump, CrouchJump, Drop, Ladder, Door, Elevator and Wait;
- spatial Entry/Exit volumes or equivalent bounded conditions for special edges;
- authoring/recording/visualization/test tools;
- route simplification, segment preservation, safe save/load and backup;
- curated map-route data under `maps/routes/` with review/export flow.

Runtime Trail remains the default no-NAV route source. Persistent routes supplement difficult/repeatable sections; they do not replace the dynamic Trail baseline for normal map support.

## Current PR/commit classification

| Repository / PR | Historical v0.3 cut | Start of v0.4 advanced traversal | Current classification |
| --- | --- | --- | --- |
| `ZEPVE-BotController#1` | `df67e21` | `1e486ee` `feat(movement): add scoped timed traversal capability` | mixed history; current `aa42948` is v0.4 experimental on top of baseline |
| `ZEPVE-Navigation#1` | `aa50134` | `134065d` `feat(nav): record and execute bounded human maneuvers` | mixed history; current `e28d230` is v0.4 experimental on top of baseline |
| `CS2-ZE-PVE#13` | historical matched reference `b8f156a` pinning Navigation `aa50134` and the pre-timed backend line | later suite pins for `1e486ee` / `134065d` and successors | mixed integration history; keep Draft |
| `ZEPVE-Lab#2` | n/a | research evidence for timed/subtick/view behavior | Lab evidence only; never ships |

The historical cut points above are audit references, not automatic production pins. A v0.3 release still requires a clean matched build, acceptance and actual rollback.

## Baseline isolation decision

Decision: **use clean v0.3 baseline PRs instead of carrying the advanced traversal layer behind a release-time feature gate.**

Reasoning:

- the pre-timed cut points are already known and auditable;
- a clean branch makes it impossible for ABI2/timed/view traversal code to activate accidentally in a v0.3 package;
- existing mixed-history PRs remain valuable as v0.4 development history and do not need force-push/rewrite;
- component/suite review stays aligned with the repository promotion rule.

Clean baseline review lines created:

```text
ZEPVE-BotController#3  -> df67e21
ZEPVE-Navigation#3     -> aa50134
CS2-ZE-PVE#15          -> b8f156a matched suite reference
```

These PRs are Draft and are **not** automatically accepted just because they are cleanly split. They still require refreshed matched v0.3 acceptance and actual rollback.

The advanced traversal implementation remains in:

```text
ZEPVE-BotController#1
ZEPVE-Navigation#1
CS2-ZE-PVE#13
```

No force-push or history rewrite is used.

## PR disposition

- `ZEPVE-BotController#3`, `ZEPVE-Navigation#3`, `CS2-ZE-PVE#15`: clean v0.3 Draft baseline review lines; next work is refreshed baseline acceptance.
- `ZEPVE-BotController#1`, `ZEPVE-Navigation#1`, `CS2-ZE-PVE#13`: keep Draft/unmerged as v0.4 experimental/mixed history.
- `ZEPVE-Lab#2`: keep Draft/evidence-only.
- documentation reconciliation lives in `CS2-ZE-PVE#14` plus component documentation PRs.
- do not relabel old FAIL/PARTIAL runtime evidence as PASS.
- do not make white-car/B-box/doghole/rotational-jump tuning a v0.3 completion condition.

## v0.3 acceptance gate

A v0.3 candidate is reviewable only when the matched package records at least:

- one movement writer / one active lease authority;
- AssignedTarget-driven pursuit independent of transient Valve Enemy state;
- flat/ramp pursuit without persistent idle or unnecessary jump spam;
- basic no-NAV Trail fallback;
- Native -> Trail degradation when Native makes no meaningful progress;
- two-Human Trail/target isolation smoke;
- bot death/external respawn;
- target disconnect/reconnect and rebind;
- round/map transition;
- managed consumer reload/drain;
- legacy movement/Trail/Recovery writer disabled in the matched candidate;
- stopped-server install/hash verification;
- actual whole-package rollback and hash verification.

Complex traversal remains useful v0.4 evidence, but it does not block this baseline gate.
