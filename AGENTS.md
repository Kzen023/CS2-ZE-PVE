# AGENTS.md

Execution rules for GPT/Codex working on the **CS2-ZE-PVE integration repository**.

## Mission

ZEPVE is a ZE-first cooperative PvE runtime for 1–6 humans, focused on ZE compatibility, low overhead, minimal per-map setup and no-NAV fallback through recorded player trails.

This repository is the **suite integration/release repository**, not the owner of every component implementation.

## Before Changes

1. Read the existing implementation first.
2. Identify which repository/module owns the affected behavior.
3. For migration work, read `MIGRATION_AUTHORITY.md` and identify the current active writer before changing code.
4. Make the smallest necessary change.
5. Do not refactor unrelated code.
6. Preserve working behavior unless the task explicitly changes it.

## Repository / Module Ownership

- `CS2-ZE-PVE`: Abstractions, Core lifecycle/round authority, `ZEPVE.BotAI`, Map/ZE integration, suite config, packaging, release metadata, legacy migration baseline and end-user docs.
- `ZEPVE.BotAI`: `AssignedTarget`, target selection/distribution, awareness assistance, Valve combat-perception observation and target reacquisition policy.
- `ZEPVE-Navigation`: Trail recording, Valve NAV integration, TrailDriver, stuck/progress detection, movement and Recovery.
- `ZEPVE-HUD`: player-facing presentation only; consumes suite state and must not rediscover gameplay/map authority independently.
- `ZEPVE-WeaponSystem`: PvE weapon balance, ammo/magazine rules, purchase handling, weapon aliases and weapon-specific damage policy.
- `ZEPVE-Lab`: high-risk experiments and evidence only; never a runtime dependency.
- Future `ZEPVE-CS2Fixes`: focused compatibility fork only when needed.
- Future `ZEPVE-MovementBridge`: native bridge only if existing Bot APIs are insufficient.

The existing pre-split plugins under `legacy/` are regression/migration baselines, not the target ownership model. Do not develop duplicate production implementations there.

Do not duplicate an independently owned production component under this repository's `src/` tree.

Only one system should own a state. Do not create duplicate respawn, target, movement, weapon, presentation or round authorities.

## Migration Safety

Migration is a writer handoff, not a parallel rewrite.

For every state-changing responsibility:

```text
identify current writer
→ implement replacement
→ verify replacement
→ explicitly disable old writer
→ update MIGRATION_AUTHORITY.md
```

Do not let legacy and new code execute the same gameplay action at the same time.

A new class existing in `src/` does not make it authoritative. Authority changes only when the previous execution path is disabled and the replacement is verified.

Core should begin with identity/registry/lifecycle observation before taking over gameplay actions. Preserve legacy as the gameplay writer during that first slice.

`AssignedTarget` is a new BotAI capability; legacy ZRPVE does not contain an equivalent authoritative target state. Do not describe it as behavior-preserving extraction.

## BotAI

`ZEPVE.BotAI` owns **who a zombie pursues and how awareness/reacquisition is assisted**.

`AssignedTarget` is authoritative ZEPVE state. Valve `Enemy`, visibility and attack state are transient engine perception and must not decide whether Navigation continues pursuing a valid human.

BotAI may observe and, when validated, minimally assist states such as sleep/active state, alert state, ignore-enemies state and look-around bookkeeping.

Prefer preserving Valve native hearing/sound investigation over building a custom footstep/gunshot threat table.

Do not make broad direct AI-state writes the default solution. High-risk Enemy/perception writes must be proven in `ZEPVE-Lab` first.

BotAI may request temporary navigation assistance toward `AssignedTarget`, but `ZEPVE-Navigation` remains the only final movement/native-goal writer.

Read `BOT_AI_DESIGN.md` before changing BotAI ownership, targeting or awareness behavior.

## Component Integration

A repository or gitlink may exist before a component is runtime-ready. Distinguish:

```text
registered integration revision
build-verified revision
suite-integrated revision
runtime-verified production revision
```

Production components are shipped only after the appropriate verification level has been reached.

A component update should be:

```text
component PR/test
→ merge component main
→ suite branch
→ bump pinned component revision
→ integration validation
→ suite PR
```

Do not make release builds silently track a moving component branch. Initialize the exact gitlink recorded by the suite.

`ZEPVE-HUD` and `ZEPVE-WeaponSystem` currently exist as migration targets and are not automatically release dependencies merely because their repositories exist.

## Navigation

Navigation implementation belongs in `ZEPVE-Navigation`.

Preferred behavior:

```text
Valve NAV -> TrailDriver -> Recovery
```

Navigation consumes the BotAI-owned `AssignedTarget` through a narrow suite-defined contract. It does not select combat targets or own Valve Enemy/awareness state.

Teleport is recovery, not normal locomotion. After a recovery teleport, Navigation rebinds its route state and requests BotAI reacquisition assistance rather than modifying combat-perception state itself.

Runtime Trail data must be per-human identity/generation. Do not migrate the legacy shared point pool as the final Trail model.

If a suite change requires Navigation behavior, change the Navigation repository first, verify it, then bump the pinned component revision here.

## HUD / Weapon migration

Legacy migration sources are:

```text
legacy/CounterStrikeSharp/plugins/Kzen-ZEAssist
  → presentation parts to ZEPVE-HUD
  → map/entity semantics to ZEPVE.Map / owning suite service

legacy/CounterStrikeSharp/plugins/Kzen-WeaponBalance
  → ZEPVE-WeaponSystem
```

Do not copy mixed legacy plugins wholesale into new repositories and call the migration complete. First preserve verified behavior, then move responsibilities according to ownership.

## Experimental Work

Uncertain UserCmd/native/hook/engine behavior belongs in `ZEPVE-Lab` first.

A successful PoC provides evidence; it is not production code by default. Redesign the production implementation in the repository that owns the behavior.

## CS2Fixes / ZombieReborn

Prefer keeping ZombieReborn/CS2Fixes as a ZE compatibility layer while ZEPVE owns PvE rules.

Respawn policy ownership and respawn execution are separate concerns. If ZombieReborn remains the validated executor, Core must not add a second respawn scheduler merely to claim ownership.

Do not put general ZEPVE gameplay logic into CS2Fixes. Prefer minimal upstream-compatible patches and upstream contributions where practical.

## Performance

Movement may run every tick in the owning Navigation component. Most other work should be event-driven or lower frequency.

Avoid entity scans, JSON parsing, heavy traces, LINQ/allocation and log spam in hot paths.

## Logging / Config

Use CounterStrikeSharp `Logger`; do not use normal `Console.WriteLine`.

Log important state changes and exceptions with context. No per-tick logging unless temporary debug mode is explicitly enabled.

Configs must be versioned and validated.

## Lifecycle

Always consider hot reload, late load, map change, round end/restart, disconnect, Bot respawn, pawn replacement and invalid entities.

Delayed work must not trust slot identity alone. Validate the lifetimes relevant to the action, normally including:

```text
plugin lifetime
map epoch
round epoch
connection identity/generation
pawn/spawn generation
```

Target- or route-specific work may also require a binding version.

At callback execution time, re-resolve current handles and reject stale work.

Clean up timers, tasks, hooks, subscriptions and cached map state on unload/map change. Round-ending actions must stop at round end even when the map does not change.

## Git / Change Safety

Documentation, README pages and lightweight repository templates may be committed directly to `main` when the owner allows it.

Use branch + PR for source/runtime changes, build behavior, dependencies, component pins, release contents and public API/config behavior.

Before major work:

```bash
git status
```

Before commit:

```bash
git diff
git diff --cached
```

Do not overwrite unknown user changes. Do not use destructive Git commands unless explicitly requested.

Prefer revert commits/PRs over rewriting shared history when a merged change breaks something.

## Verification

Distinguish:

```text
build passed
component tests passed
plugin loaded
feature worked
suite integration worked
real map tested
```

Do not call a design pin or build-only component production-verified.

If CS2 runtime testing was not possible, say so.

## Documentation Localization

User-facing documentation is first-class in English and Simplified Chinese.

When changing user-visible features, commands, installation, configuration or troubleshooting, update both languages when practical.

Developer-only documents do not require translation unless useful to contributors.

## Final Check

Before finishing, verify repository ownership, current/next writer, component revision impact, lifecycle validity, performance, logs, config validation, build status and rollback path.

Primary rule: keep **component development**, **suite integration**, **legacy migration**, and **experimentation** separate and auditable.
