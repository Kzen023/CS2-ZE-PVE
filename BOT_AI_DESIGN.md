# ZEPVE BotAI Design

This document defines the planned responsibilities of `ZEPVE.BotAI.dll` inside the `CS2-ZE-PVE` suite.

`ZEPVE.BotAI` owns **who a zombie should pursue and how Bot awareness/combat perception is assisted**. `ZEPVE-Navigation` owns **how the Bot reaches that target**.

## 1. Ownership

```text
ZEPVE.BotAI
├─ Targeting/
│  ├─ BotTargetBinding
│  ├─ TargetSelector
│  └─ TargetDistribution
├─ Awareness/
│  ├─ AwarenessAssist
│  ├─ TargetReacquireService
│  └─ ValveBotStateAdapter
└─ Diagnostics/

ZEPVE.Navigation
├─ Native navigation
├─ Runtime Trails / Recorded Routes
├─ TrailDriver
├─ ProgressMonitor
├─ MovementIntent
└─ Recovery
```

`ZEPVE.BotAI` must not own pathfinding, Trail advancement, movement commands, stuck recovery movement or teleport placement.

`ZEPVE-Navigation` must not own target selection, Valve Enemy state, alert/sleep state or enemy reacquisition policy.

BotAI may request temporary navigation assistance toward `AssignedTarget`; Navigation remains the **only final movement/native-goal writer** and decides how that request is executed alongside its current driver/recovery state.

## 2. Authoritative Target

Each zombie has one ZEPVE-owned `AssignedTarget`.

```text
AssignedTarget
= who this zombie should pursue
= authoritative ZEPVE state

Valve Enemy / visibility / attack state
= current engine perception
= transient observation
```

Navigation continues toward a valid `AssignedTarget` even when Valve currently reports no enemy.

Target binding is re-evaluated when the assigned human dies, disconnects, becomes invalid, changes stage eligibility, or explicit redistribution is requested.

`AssignedTarget` is a new ZEPVE capability; the legacy ZRPVE baseline does not contain an equivalent authoritative target state.

## 3. Navigation Contract

Navigation should consume narrow contracts defined by the suite/shared abstractions rather than defining a second copy of them in the Navigation implementation:

```text
IBotTargetProvider
  GetAssignedTarget(bot)

IBotAwarenessApi
  RequestReacquire(bot, reason)
```

Navigation consumes the target and reports recovery events. It does not directly modify Valve combat-perception state.

A recovery teleport should conceptually be:

```text
Navigation selects recovery point for AssignedTarget
→ teleport
→ rebind route/navigation state
→ BotAI.RequestReacquire(RecoveryTeleport)
→ Navigation keeps moving toward AssignedTarget
→ BotAI observes perception recovery
```

## 4. Awareness Assist

The first production implementation should be a short, event-triggered assist rather than a permanent replacement for Valve Bot AI.

Candidate triggers:

- Bot spawn / respawn
- recovery teleport
- Trail segment teleport
- AssignedTarget rebind
- valid AssignedTarget with stale/null Valve Enemy for a bounded interval

Before applying any awareness operation, validate that the request still belongs to the current lifecycle/target binding:

```text
current map/round/player/pawn identity
→ current AssignedTarget / binding version
→ navigation/recovery state already synchronized
→ duplicate reacquire requests coalesced
→ smallest validated awareness operation
→ bounded engine observation window
→ success / timeout / invalidation
```

Candidate actions, only when runtime tests justify them:

```text
wake / allow active
clear ignore-enemies state
raise a short alert window
reset look-around bookkeeping
preserve native sound investigation
request temporary navigation assistance toward AssignedTarget
```

Stop assisting once normal engine perception recovers or the lifecycle/target binding becomes invalid.

Do not blindly force visibility. Do not continuously write broad Bot AI state every tick unless a specific field is proven to require it.

## 5. Smarter-Bot Research Conclusions

Research into `CS2-Smarter-Bot` is relevant here because it demonstrates several practical ways of keeping Valve Bot awareness responsive:

- keep Bots awake and active;
- prevent `IgnoreEnemiesTimer` from suppressing enemy response;
- keep an alert state active;
- reset look-around bookkeeping so threats can be reacquired;
- preserve native sound-investigation paths rather than replacing hearing with a custom threat table;
- repair obviously desynchronized combat state only when the engine already indicates enemy aiming/awareness.

These are **reference observations**, not code to copy directly. ZEPVE should independently implement the smallest subset needed for ZE zombie pursuit.

In particular, ZEPVE should prefer an event-triggered `AwarenessAssist` window over permanently forcing all of these states every tick.

## 6. Reacquisition Experiments

The following is an experiment sequence, not a claim that every step is required or that this exact ordering is already production-safe.

Test candidate operations independently where practical:

```text
1. confirm AssignedTarget and binding are still valid
2. establish a baseline observation window after movement/teleport synchronization
3. test look-around reset
4. test wake / allow active
5. test clear IgnoreEnemies state
6. test a short alert window
7. test temporary Navigation-owned assistance toward AssignedTarget
8. observe native Enemy/visibility recovery
9. only if still unreliable, test the smallest explicit Enemy-state write in ZEPVE-Lab
```

Do not change every candidate field at once and then attribute success to one of them.

Explicit Valve Enemy writes are a fallback, not the default design.

## 7. Sound and Hearing

Do not build a custom footstep/gunshot hate table unless real tests prove Valve hearing is insufficient.

The preferred model is:

```text
Valve native hearing / sound investigation
+
ZEPVE AssignedTarget
+
short AwarenessAssist when needed
```

This keeps useful native behavior while ensuring a zombie never loses its intended pursuit target merely because combat perception is temporarily stale.

## 8. Diagnostics

Useful debug state:

```text
AssignedTarget
TargetBindingVersion
ValveEnemy
EnemyVisible
IsAimingAtEnemy
IsAttacking
IsSleeping
AllowActive
IgnoreEnemies state
Alert state
LookAround state
Reacquire reason
Reacquire elapsed time
Target rebind count
Reacquire success/failure count
```

Logs should be transition-based, not per-tick spam.

## 9. Repository Placement

Start `ZEPVE.BotAI` as a separate DLL project inside `CS2-ZE-PVE`.

Do not create a separate `ZEPVE-BotAI` repository yet. Split it later only if the component develops an independently useful API, separate release/test lifecycle and value outside the ZEPVE suite.

## 10. V1 Goal

The first BotAI baseline should prove:

- stable per-Bot AssignedTarget ownership;
- multi-human target distribution and rebinding;
- post-spawn/post-teleport awareness recovery;
- no dependency on Valve Enemy as the navigation authority;
- Navigation remains the only final movement/native-goal writer;
- clean interaction with `ZEPVE-Navigation` through narrow contracts;
- diagnostics sufficient to explain why a Bot is or is not pursuing/attacking a human.
