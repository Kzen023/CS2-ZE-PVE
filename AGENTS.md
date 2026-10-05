# AGENTS.md

Short execution rules for GPT/Codex working on ZEPVE.

## Mission

ZEPVE is a modular CS2 Zombie Escape PvE runtime for 1–6 humans, focused on ZE compatibility, low overhead, minimal per-map setup and no-NAV fallback through recorded player trails.

## Before Changes

1. Read the existing implementation first.
2. Identify which module owns the affected state.
3. Make the smallest necessary change.
4. Do not refactor unrelated code.
5. Preserve working behavior unless the task explicitly changes it.

## Ownership

- `ZEPVE.Abstractions`: contracts only; no gameplay logic.
- `ZEPVE.Core`: lifecycle, humans, Bot pool, spawn/death/respawn, round state.
- `ZEPVE.Navigation`: Trail recording, drivers, stuck detection, recovery and movement.
- `ZEPVE.Map`: Hammer signals, map overrides, special zones/stages and Boss bindings.
- Optional modules (`Hud`, `Weapons`, `Director`, etc.) must not be required by Core.

Only one system should own a state. Do not create duplicate respawn, movement or round authorities.

## Navigation

Preferred order:

```text
Valve NAV -> TrailDriver -> Recovery
```

Teleport is recovery, not normal locomotion.

Trail rules:
- do not sample every tick
- keep bounded history
- measure distance along the trail
- split large teleports into separate segments
- validate recovery positions when needed

## Performance

Movement may run every tick. Most other work should be event-driven or lower frequency.

Avoid entity scans, JSON parsing, heavy traces, LINQ/allocation and log spam in hot paths.

## CS2Fixes

Prefer keeping ZombieReborn as a ZE compatibility layer while ZEPVE owns PvE rules.

Do not put ZEPVE gameplay logic into CS2Fixes. Prefer minimal upstream-compatible patches.

## Logging / Config

Use CounterStrikeSharp `Logger`; do not use normal `Console.WriteLine`.

Log important state changes and exceptions with context. No per-tick logging unless temporary debug mode is explicitly enabled.

Configs must be versioned and validated.

## Lifecycle

Always consider hot reload, map change, disconnect, Bot respawn and invalid entities.

Clean up timers, tasks, hooks, subscriptions and cached map state on unload/map change.

## Git

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

Preferred commits:

```text
feat(nav): add trail segment detection
fix(core): prevent bot double respawn
refactor(nav): extract stuck recovery service
```

## Verification

Distinguish:

```text
build passed
plugin loaded
feature worked
real map tested
```

If CS2 runtime testing was not possible, say so.

Uncertain engine behavior must be tested with a small PoC before production integration.

## Documentation Localization

User-facing documentation is first-class in English and Simplified Chinese.

When changing user-visible features, commands, installation, configuration or troubleshooting, update both languages in the same change when practical.

Developer-only documents do not require translation.

## Final Check

Before finishing, verify scope, ownership, lifecycle, performance, logs, config validation, build status and diff cleanliness.

Primary rule: every change should leave ZEPVE easier to debug, safer to extend and easier to roll back.
