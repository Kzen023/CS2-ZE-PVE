# ZEPVE project audit — 2026-10-11

This document records the accepted conclusions from the full-project Astra audit. It is a planning and evidence boundary, not a production PASS.

## Executive summary

- Architecture direction: **ADJUST**, not rebuild.
- v0.2 remains the accepted Core/BotAI runtime foundation.
- The clean v0.3 cuts are **valid isolation points**, but they are **not ready to release or merge unchanged**.
- v0.4 has strong mechanism evidence for bounded timed/subtick/view playback, but traversal orchestration is still experimental.
- v0.5 RecordedRoute/RouteGraph remains design work.
- No separate Recorder/Replay repository is needed.

The main architectural correction is to stop letting automatic MotionClip capture/replay become route planning. Route/Trail decides where to go; a typed traversal action decides what local maneuver is required; MotionClip may execute only a short already-selected maneuver.

## Repository ownership

```text
CS2-ZE-PVE
  Core       -> identity / lifecycle / round / quota / team
  BotAI      -> AssignedTarget / target binding / bounded awareness observation
  Suite      -> integration / exact pins / package / release / rollback

ZEPVE-Navigation
  -> Runtime Trail / route selection / progress / traversal semantics / Recovery
  -> future RecordedRoute / RouteGraph / authoring

ZEPVE-BotController
  -> low-level bounded command capture/application
  -> single PlayerRunCommand router / lease / validation / drain
  -> timed payload and scoped view capability when explicitly authorized

ZEPVE-Lab
  -> experiments and evidence only; never a release dependency

ZEPVE-HUD / ZEPVE-WeaponSystem
  -> later migration targets; no role in v0.3-v0.5 traversal work
```

## Version truth

### v0.3 — Navigation baseline

v0.3 already uses a UserCmd-backed movement backend. The v0.4 boundary is **not** “first UserCmd movement”; it begins at richer traversal capability such as timed payloads, signed-subtick playback, scoped view/facing and MotionClip execution.

Clean historical cuts:

```text
ZEPVE-BotController#3  -> df67e21
ZEPVE-Navigation#3     -> aa50134
CS2-ZE-PVE#15          -> b8f156a
```

These cuts are valid for isolation, but the clean Navigation cut still contains baseline behavior defects and the suite cut still needs reproducible artifact provenance plus real matched acceptance.

### v0.4 — Advanced traversal

Owns:

- timed/signed-subtick traversal payloads;
- short bounded MotionClip execution;
- scoped traversal view/facing;
- typed local actions with Entry / Execute / Exit / Abort semantics;
- local retry/cancel/failure scope;
- continuous traversal handoff;
- evidence-based fixes for yaw oscillation, low-wall stalls and early/late takeoff.

Ordinary Native/Trail pursuit must not receive view ownership merely because the backend supports it.

### v0.5 — Persistent routes

Owns:

- RecordedRoute and schema/versioning;
- RouteGraph and stable node/edge identity;
- typed TraversalEdge and spatial Entry/Exit volumes;
- save/load/backup, authoring and bounded visualization;
- curated route data through suite review/export.

Runtime Trail remains the default dynamic no-NAV source.

## High-priority audit findings

### H1 — Clean Navigation cut still contains v0.3 pursuit defects

`aa50134` is a valid pre-timed cut, not a release-ready minimum.

Phase 1 must inspect and correct or isolate baseline behavior including:

- failed-jump cooldown suppressing unrelated Native->Trail fallback;
- near-target yielding to Valve without proof that Valve is actually pursuing AssignedTarget;
- traversal state advancing even when the corresponding button/action is not executed;
- progress/waiting rules that can mistake local oscillation or low-amplitude movement for useful progress.

Later commits such as `05a50f8`, `575f111`, `b3c5b1f` and `e28d230` may be used as **sources for selective baseline fixes only**. Do not pull timed/view/MotionClip capability into v0.3 just because a fix landed later.

### H2 — Package hashes do not yet prove artifact provenance

The suite must prove that the copied native binary was built from the exact recorded BotController revision and exact supported build profile. Hash verification alone proves file consistency after copying, not source/pin provenance.

Phase 1 must bind:

```text
suite gitlink
component checkout
source revision
native/shared ABI
build profile
produced artifact
manifest SHA256
installed inventory
rollback inventory
```

A mismatch must fail closed.

### H3 — Recovery geometry is not map permission

Hull/ground/hazard validation cannot prove that a teleport is allowed by ZE stage/door/map semantics. Until canonical map permission exists, unknown permission must fail closed. Recovery must not be used to hide baseline pursuit/traversal failures.

### H4 — v0.4 ordinary pursuit view scope is too broad

The advanced branch may expose scoped facing/view through the same safe router, but ordinary Native/Trail pursuit must not automatically acquire it. View is an exceptional traversal permission and must end on completion/cancel/invalidation.

### H5 — MotionClip must not become route selection

Keep the proven capture/timing/lease/execution mechanisms. Change the orchestration boundary:

```text
Route / Runtime Trail decides where
        ↓
typed local traversal action decides what
        ↓
MotionClip optionally executes the short action
```

Do not use “nearby clip exists” as a substitute for a valid route/action decision.

## PR disposition

All current production/runtime PRs remain unmerged until their own gates are satisfied.

| PR | Disposition |
| --- | --- |
| `ZEPVE-BotController#3` | KEEP DRAFT — valid v0.3 backend cut; refresh provenance/matched acceptance |
| `ZEPVE-Navigation#3` | KEEP DRAFT — valid cut; baseline pursuit defects must be fixed/isolated |
| `CS2-ZE-PVE#15` | KEEP DRAFT — valid matched reference; packaging, ADR sync, runtime acceptance and actual rollback remain |
| `ZEPVE-BotController#1` | KEEP DRAFT — v0.4 capability/history |
| `ZEPVE-Navigation#1` | KEEP DRAFT — v0.4 capability/history; route/action/view boundaries need adjustment |
| `CS2-ZE-PVE#13` | KEEP DRAFT — mixed v0.3/v0.4 package history only |
| `ZEPVE-Lab#2` | KEEP DRAFT — evidence only; never a runtime dependency |

## Phase 1 — GPT-6.1 Sol handoff

Goal: produce a clean, long-running, matched and fully rollbackable v0.3 Test Preview, then stop.

Allowed repositories:

```text
CS2-ZE-PVE
ZEPVE-Navigation
ZEPVE-BotController
```

Read-only reference:

```text
ZEPVE-Lab
advanced #1/#1/#13 history
```

Starting points:

```text
BotController #3  agent/v03-movement-baseline @ df67e21
Navigation #3     agent/v03-navigation-baseline @ aa50134
Suite #15         agent/v03-navigation-baseline @ b8f156a
```

Phase 1 scope:

1. fix/isolate only baseline pursuit defects that affect flat/ramp/basic Trail behavior;
2. keep complex jump/replay behavior out of the default v0.3 path;
3. make unknown Recovery map permission fail closed;
4. bind source/pins/build profile/artifacts/ABI/manifest/install/restore into one reproducible matched package;
5. synchronize branch ADR/documentation with current main without changing authority boundaries;
6. run automated checks and the required real CS2 acceptance;
7. perform an **actual whole-package rollback**, not VerifyOnly.

Hard boundaries:

- no timed traversal;
- no MotionClip;
- no scoped view writer;
- no complex box/doghole/rotational-jump tuning;
- no Core quota/team redesign;
- no BotAI target-authority redesign;
- no native Enemy/visibility writes;
- no second movement writer or PlayerRunCommand hook;
- no Lab runtime dependency;
- do not merge automatically.

Required real-server acceptance:

- flat ground and normal ramps;
- distant/stationary Human pursuit;
- basic no-NAV Trail follow;
- Native->Trail degradation;
- failed local action does not block unrelated valid route/fallback;
- two-Human Trail isolation smoke;
- target rebind, death/respawn, disconnect/reconnect, round/map;
- managed Core/BotAI/Navigation reload/drain as applicable;
- legacy movement/Trail/Recovery writer disabled in candidate;
- stopped-server matched install;
- actual whole-package rollback with old hashes restored.

Not required in Phase 1:

- precise crouch-jump/rotational-jump parity;
- full ZE certification;
- native awareness efficacy;
- persistent routes;
- full performance/endurance program.

Stop condition: once the Test Preview is reproducible, the required v0.3 acceptance is recorded, actual rollback passes, and #3/#3/#15 have an evidence-backed Ready/Keep-Draft recommendation, stop. Do not continue into v0.4 in the same task.

## Later phases

Phase 2 is v0.4 typed local traversal and MotionClip as an executor. Phase 3 is v0.5 persistent RecordedRoute/RouteGraph/authoring. Each begins only after the prior phase is explicitly accepted.
