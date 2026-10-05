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

## 3. Navigation Contract

Navigation should depend on a narrow contract, preferably from `ZEPVE.Abstractions`:

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

Candidate actions, only when runtime tests justify them:

```text
wake / allow active
clear ignore-enemies state
raise a short alert window
reset look-around bookkeeping
preserve native sound investigation
optionally request temporary navigation assistance toward AssignedTarget
```

Stop assisting once normal engine perception recovers.

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

## 6. Reacquisition Escalation

Test in this order:

```text
1. AssignedTarget remains valid
2. reset look-around bookkeeping
3. wake / allow active
4. clear IgnoreEnemies state
5. short alert window
6. temporary navigation assist toward AssignedTarget
7. observe native Enemy/visibility recovery
8. only if still unreliable, test the smallest explicit Enemy-state write in ZEPVE-Lab
```

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
- clean interaction with `ZEPVE-Navigation` through narrow contracts;
- diagnostics sufficient to explain why a Bot is or is not pursuing/attacking a human.
