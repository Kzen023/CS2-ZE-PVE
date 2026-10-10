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

### v0.3 candidate source / deployment distinction

The v0.3 PR candidate physically removes legacy RecordHumanPath/CheckZombieBots/QueueBotRecovery/TryRecoverZombieBot/TryStartEscort/UpdateEscorts and every legacy Teleport call. Matched Navigation becomes the only ZEPVE movement/Trail/Recovery writer, gated by the adapter's MovementWriterDisabled capability and one shared Navigation provider. Production native execution is ZEPVE-BotController ABI1 in the existing dispatcher, process resident, with explicit rejection of old anonymous movement/replay/locks. Lab is evidence only.

**At initial source implementation the installed accepted baseline is still v0.2 and legacy retains runtime Trail/Recovery authority.** Candidate deployment/testing must be the entire stopped-server matched package; source presence alone does not claim runtime acceptance or main authority handoff. Actual PASS/NOT TESTED and rollback are recorded in `docs/NAVIGATION_VERIFICATION.md`. The matrix below describes the accepted v0.2 baseline until candidate acceptance is recorded.

Core identity/lifecycle/round/quota/team, BotAI AssignedTarget/ObserveOnly and external ZR/CS2Fixes respawn execution remain their exact writers. No authority handoff for those responsibilities. Candidate Recovery cancels intent and reserves exclusive ownership before geometric placement/route synchronization/reacquire. Missing Navigation/backend does not re-enable old legacy writer; whole-set rollback restores the accepted baseline only while stopped.

The Core/legacy rows preserve the **accepted paired v0.2c authority**, originally installed from `1cf6aa2` and now merged at baseline `ed480bd` (PR #10). The matched v0.2d package `238b98c` adds only suite-owned AssignedTarget/reacquire policy; fixed-package single-human smoke passed, with limits in `docs/BOTAI_VERIFICATION.md`. Core gameplay authority and the v0.2c deferred acceptance remain unchanged. Never mix Core with the old monolithic legacy writer. Historical Core evidence/limits are in `docs/CORE_MATCHED_RUNTIME_VERIFICATION.md`; matched installation/rollback is in `docs/BOTAI_RUNTIME.md`.

| Behavior / state | Current writer / executor | Migration rule | Final owner |
| --- | --- | --- | --- |
| Player/Bot identity / lifecycle validity | `ZEPVE.Core` PlayerRegistry, read-only shared API and validator | one provider owns plugin/map/round/connection/pawn generations; adapted legacy consumes tokens and does not maintain `_roundToken` | `ZEPVE.Core` |
| PvE round preparation / infection timing policy | `ZEPVE.Core` PveRoundController + native compatibility adapter | old ApplyProfile/ReleaseInfectionBots execution removed; preserve countdown, 1-second preparation offset and configured release delays; ZR still performs infection | `ZEPVE.Core` policy + compatibility adapter |
| Bot quota / global Bot team transitions | `ZEPVE.Core` PveRoundController / NativePveGameAdapter | Core writes only with the adapted legacy bridge present; no fallback/parallel legacy quota/team executor | `ZEPVE.Core` |
| Zombie respawn policy | `ZEPVE.Core` migrated static profiles / `zr_respawn_delay` compatibility setting | preserve solo/duo/coop/group delay values; no new respawn timer; external runtime retains map/permission/execution rules | `ZEPVE.Core` policy + compatibility adapter |
| Zombie respawn execution | ZombieReborn / existing CS2Fixes-compatible runtime | unchanged sole executor; Core does not call respawn APIs or schedule respawns | `ZEPVE.Core` policy with one explicit executor/adapter |
| Recovery teleport | `Kzen-ZRPVE` legacy recovery | legacy remains writer until Navigation recovery is implemented and validated | `ZEPVE-Navigation` |
| Pursuit target (`AssignedTarget`) | `ZEPVE.BotAI` in the matched v0.2d package; absent in the v0.2c baseline | new capability, not legacy extraction; single published provider, dual Core tokens + module GUID/BindingVersion; Released/live-role gate | `ZEPVE.BotAI` |
| Valve `Enemy` / visibility | Valve engine state; BotAI reads only | v0.2d ObserveOnly / zero native writes; BotController lock/replay/input/weapon ownership untouched; future field assist needs evidence/ownership review | Valve engine state + `ZEPVE.BotAI` observation/assist policy |
| Native/trail movement goal | Valve native behavior plus limited legacy recovery teleport | only Navigation may become the final movement/native-goal writer; BotAI may request assistance but not issue movement itself | `ZEPVE-Navigation` |
| Runtime Trail | `Kzen-ZRPVE` legacy shared point history | treat as baseline behavior only; new per-human Trail replaces it after Navigation validation | `ZEPVE-Navigation` |
| Map stage / boss / signal semantics | no canonical suite state; `Kzen-ZEAssist` contains heuristics/observation | heuristics may inform migration but must not silently become gameplay authority | `ZEPVE.Map` |
| HUD presentation | `Kzen-ZEAssist` plus legacy status/debug output | migrate presentation only after stable Core/Map state contracts exist | `ZEPVE-HUD` |
| Weapon purchase / ammo / damage policy | `Kzen-WeaponBalance` | preserve legacy writer until WeaponSystem replacement is validated; do not run both purchase/damage paths | `ZEPVE-WeaponSystem` |
| Difficulty policy | Core's migrated legacy static human-count/profile table (paired v0.2c) | identical static profile/quota/compatibility values; no dynamic Director | future `ZEPVE.Director` policy with execution delegated to owning modules |

## v0.2c handoff boundary and deployment state

- Previous writer: accepted `Kzen-ZRPVE @ 89b5c0c0`, installed with the user-tested `766317f` Core observer. The matched `1cf6aa2` package replaced that installation while stopped on 2026-10-09; the whole previous artifact set is backed up at `ZEPVE_Backup/v0.2c-20261009-151119-2996277`.
- Replacement: `ZEPVE.Core` lifecycle coordinator, scheduler, PveRoundController and native command/team adapter. The engine/ZR executes its normal native infection/respawn behavior; Core owns only the migrated ZEPVE policy/commands.
- Disable path: adapted `ZrPvePlugin.cs` physically removes QueueApply, ApplyProfile, ReleaseInfectionBots, MoveExistingBots, AddBots, ApplyMapRoundTime, PickProfile and `_roundToken`; map/round/spawn coordination is forwarded from Core. Its sole remaining Server.ExecuteCommand is the existing presentation countdown command. No automatic fallback reactivates old writers.
- Gate: shared `SuiteRuntime` permits one Core provider and one adapted legacy bridge. New Core without the bridge issues no gameplay commands; adapted legacy without Core pauses services/recovery instead of writing quota/team. Shared ABI must be installed once under `shared/ZEPVE.Abstractions`.
- Validation: Release builds and 72 model/source checks PASS; paired installer/restore fixture PASS; actual stopped-server installation (8/8 hashes, 47 preserved files) and backup verification PASS. Matched real-server single-player smoke PASS: shared ABI/gate, registry, round/map/connection/pawn invalidation, solo release/quota/team, external ZR respawn, retained Recovery placement, hot/late load and bridge loss. Multi-human profiles, precise timing/native team-tally parity, endurance and actual server rollback remain NOT TESTED/PARTIAL. PR #9's deferred observer evidence remains historical; current proof is recorded separately.
- Switch/rollback: stop the server and deploy/restore the whole Core + adapted ZRPVE + shared contract set using the paired scripts. Keep configs, WeaponBalance, ZEAssist and the pre-Core backup. The deployed handoff has single-player smoke verification, not broad production certification. No rollback was needed; no automatic merge or BotAI/Navigation progression is authorized by this evidence.

Recovery point selection/teleport, legacy Trail recording, HUD presentation and WeaponBalance remain their existing writers. Recovery callbacks now use Core validity/cancellation, but their recovery algorithm remains byte-equivalent to the accepted baseline.

## v0.2d new-capability boundary

PR #11 is now merged at `main @ 7cf389157f7f09f08c399ba5ca6a33b5ccbf4d46`, the owner-accepted v0.2 complete baseline. The v0.3 movement execution audit found no application-time suite-lifetime/exclusive-owner gate in the installed persistent BotController movement API. No Navigation runtime handoff or deployment occurs: current writer rows remain unchanged, legacy Trail/Recovery stays active and the Navigation pin remains a design reference. See `docs/NAVIGATION_BACKEND_SAFETY_GATE.md` for exact evidence and the prerequisite for resuming.

PR #10 merged to `main @ ed480bd500ca01e5ed4da6a61a7391944d38905c`, the new matched-runtime baseline. v0.2c remaining deferred acceptance is unchanged. No Core/legacy gameplay authority is moved by BotAI. Existing bot quota/team, infection, external respawn execution, recovery, Trail, weapons and HUD writers remain unchanged. The only new writer is BotAI's suite-owned AssignedTarget/reacquisition policy.

BotController v0.7.1 source/log revision `0ae8f18` was audited: lock/replay/usercmd/view/weapon/buy/profile controls are separate from BotAI's read-only schema observation. BotAI calls none of them and writes no native perception/movement field. Native locks/replay may impair observed reacquisition; BotAI never overrides them. Future Navigation remains the only final movement-intent writer; its current design pin is not an active runtime.

Matched v0.2d installation/rollback includes Core + adapted ZRPVE + BotAI + one shared ABI, offline with hashes/current backup. See `docs/BOTAI_RUNTIME.md` and `docs/BOTAI_VERIFICATION.md` for implemented contracts and separate model/runtime evidence. AssignedTarget does not claim to change Valve pursuit before Navigation consumes it; Recovery's missing automatic reacquire notification remains explicit debt. `Round_End T tally 20 vs actual 10` remains independent diagnostic debt.

Actual v0.2d matched single-human smoke passed after fixing the startup native-time defect (`238b98c` installed; initial broken whole set was actually rolled back and v0.2c baseline booted). Assignment independent of null Enemy, round/pawn/provider/module invalidation, map/reconnect cleanup and ObserveOnly success/timeout have real evidence. Multiple real humans, isolated target-pawn/BindingVersion-only rejection and native assist efficacy are still NOT TESTED/PARTIAL. Current rollback backup is `ZEPVE_Backup/v0.2d-20261009-161410-5946327`; no writer besides suite-owned target/reacquire policy moved.

### Existing handoff checklist

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
