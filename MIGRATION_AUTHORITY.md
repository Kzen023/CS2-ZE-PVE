# Migration Authority

This document defines who is allowed to execute state-changing behavior while ZEPVE migrates from the working legacy plugins to the new ownership model.

The target architecture is already defined elsewhere. This file exists to prevent **two active writers during migration**.

## Primary rule

For every state-changing behavior:

```text
one current writer
→ replacement implemented behind a narrow boundary
→ replacement verified
→ old writer explicitly disabled
→ replacement becomes authoritative
```

Do not run the legacy and replacement executors in parallel for the same responsibility.

Keeping legacy source as a regression baseline does **not** mean loading two production authorities at once.

## Current authority matrix

The current-writer column describes the audited legacy baseline. Update this table when an ownership handoff is merged.

| Behavior / state | Current writer / executor | Migration rule | Final owner |
| --- | --- | --- | --- |
| Player/Bot identity observation | legacy plugins + engine state | Core may first observe/index without changing gameplay | `ZEPVE.Core` |
| PvE round preparation / infection timing | `Kzen-ZRPVE` with CS2Fixes/ZR interaction | preserve existing timing until Core handoff is verified; then disable the matching legacy path | `ZEPVE.Core` policy + compatibility adapter |
| Bot quota / global Bot team transitions | `Kzen-ZRPVE` | do not add a second quota/team writer; migrate one action path at a time | `ZEPVE.Core` |
| Zombie respawn execution | ZombieReborn / CS2Fixes-compatible runtime; legacy config influences policy | Core must not add a second scheduler merely to claim ownership; first model policy/permission and keep one executor | `ZEPVE.Core` policy with one explicit executor/adapter |
| Recovery teleport | `Kzen-ZRPVE` legacy recovery | legacy remains writer until Navigation recovery is implemented and validated | `ZEPVE-Navigation` |
| Pursuit target (`AssignedTarget`) | no explicit ZEPVE state in legacy | this is a new BotAI capability, not a legacy extraction; introduce only when BotAI becomes the target authority | `ZEPVE.BotAI` |
| Valve `Enemy` / visibility | Valve engine state; legacy has no authoritative ZEPVE write path | BotAI observes and may use only validated bounded assistance; never make Navigation the writer | Valve engine state + `ZEPVE.BotAI` observation/assist policy |
| Native/trail movement goal | Valve native behavior plus limited legacy recovery teleport | only Navigation may become the final movement/native-goal writer; BotAI may request assistance but not issue movement itself | `ZEPVE-Navigation` |
| Runtime Trail | `Kzen-ZRPVE` legacy shared point history | treat as baseline behavior only; new per-human Trail replaces it after Navigation validation | `ZEPVE-Navigation` |
| Map stage / boss / signal semantics | no canonical suite state; `Kzen-ZEAssist` contains heuristics/observation | heuristics may inform migration but must not silently become gameplay authority | `ZEPVE.Map` |
| HUD presentation | `Kzen-ZEAssist` plus legacy status/debug output | migrate presentation only after stable Core/Map state contracts exist | `ZEPVE-HUD` |
| Weapon purchase / ammo / damage policy | `Kzen-WeaponBalance` | preserve legacy writer until WeaponSystem replacement is validated; do not run both purchase/damage paths | `ZEPVE-WeaponSystem` |
| Difficulty policy | legacy static human-count/profile policy | keep static behavior during migration; do not introduce Director as part of Core extraction | future `ZEPVE.Director` policy with execution delegated to owning modules |

## Handoff checklist

Before changing a row from the legacy/current writer to a new owner, the PR must state:

1. **Current writer** — exact legacy/plugin/engine path that changes the state today.
2. **Replacement writer** — exact new service/adapter that will execute it.
3. **Disable path** — how the previous writer is prevented from executing the same behavior.
4. **Validation** — build, plugin-load and runtime evidence appropriate to the behavior.
5. **Rollback** — commit/release/config path that restores the previous known-good writer.

A handoff is incomplete if the new implementation exists but the old writer can still execute the same action.

## Lifecycle validity

Delayed work must not rely on player slot alone.

The minimum validity context should cover the lifetimes that can invalidate an action:

```text
Plugin lifetime
Map epoch
Round epoch
Connection identity / generation
Pawn / spawn generation
```

Target- or route-specific delayed work should additionally carry a target/binding version when needed.

At callback execution time:

- re-resolve current entities/handles;
- reject work from an old map or round;
- reject work from a disconnected/replaced connection;
- reject work for an old pawn generation;
- reject work for an obsolete target/route binding;
- stop cleanly if the owning component/provider has unloaded.

A single `SpawnGeneration` value is not sufficient for every delayed action.

## Core migration stages

Core should gain authority gradually.

### Stage A — Observe

Core creates reliable identity, registry views and lifecycle invalidation while legacy remains the gameplay writer.

Suggested first slice:

```text
PlayerRegistry
Human/Zombie read-only views
MapEpoch
RoundEpoch
ConnectionGeneration
PawnGeneration
Core diagnostics / bounded lifecycle events
```

### Stage B — Handoff

Move one lifecycle responsibility at a time from legacy into Core, with the authority matrix updated in the same change.

Examples:

```text
round gating
bot quota/team transitions
compatibility policy
```

Do not introduce new gameplay while performing the handoff.

### Stage C — New capabilities

Capabilities that did not exist in the legacy baseline, such as BotAI-owned `AssignedTarget`, are introduced only after the lifecycle foundation is stable. They must not be described as behavior-preserving extraction.

## Respawn note

`RespawnAuthority` means ZEPVE owns the policy and has exactly one known execution path. It does not require Core to directly call a respawn API if ZombieReborn/CS2Fixes remains the validated executor.

If the compatibility layer cannot provide a safe handoff, document the state honestly as:

```text
Core policy / observation
+ external respawn executor
```

rather than adding a second scheduler.

## Recovery note

Legacy recovery teleport is a behavioral baseline, not proof of navigation-safe recovery. The production Navigation implementation still needs route ownership, target synchronization, geometry validation and map/round permission checks.

## Updating this file

Any PR that transfers a state-changing responsibility should update this matrix when the handoff is complete.

If runtime evidence contradicts the table, runtime evidence wins and the documentation must be corrected before the next migration step.
