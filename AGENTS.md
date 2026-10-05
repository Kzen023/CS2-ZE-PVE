# AGENTS.md

Execution rules for GPT/Codex working on the **CS2-ZE-PVE integration repository**.

## Mission

ZEPVE is a ZE-first cooperative PvE runtime for 1–6 humans, focused on ZE compatibility, low overhead, minimal per-map setup and no-NAV fallback through recorded player trails.

This repository is the **suite integration/release repository**, not the owner of every component implementation.

## Before Changes

1. Read the existing implementation first.
2. Identify which repository/module owns the affected behavior.
3. Make the smallest necessary change.
4. Do not refactor unrelated code.
5. Preserve working behavior unless the task explicitly changes it.

## Repository Ownership

- `CS2-ZE-PVE`: Abstractions, Core lifecycle/round authority, Map/ZE integration, suite config, packaging, release metadata and end-user docs.
- `ZEPVE-Navigation`: Trail recording, Valve NAV integration, TrailDriver, stuck/progress detection, movement and Recovery.
- `ZEPVE-Lab`: high-risk experiments and evidence only; never a runtime dependency.
- Future `ZEPVE-CS2Fixes`: focused compatibility fork only when needed.
- Future `ZEPVE-MovementBridge`: native bridge only if existing Bot APIs are insufficient.

Do not duplicate an independently owned production component under this repository's `src/` tree.

Only one system should own a state. Do not create duplicate respawn, movement or round authorities.

## Component Integration

Production components are pinned to exact Git revisions by the suite.

A component update should be:

```text
component PR/test
→ merge component main
→ suite branch
→ bump pinned component revision
→ integration validation
→ suite PR
```

Do not make release builds silently track a moving component branch.

## Navigation

Navigation implementation belongs in `ZEPVE-Navigation`.

Preferred behavior:

```text
Valve NAV -> TrailDriver -> Recovery
```

Teleport is recovery, not normal locomotion.

If a suite change requires Navigation behavior, change the Navigation repository first, verify it, then bump the pinned component revision here.

## Experimental Work

Uncertain UserCmd/native/hook/engine behavior belongs in `ZEPVE-Lab` first.

A successful PoC provides evidence; it is not production code by default. Redesign the production implementation in the repository that owns the behavior.

## CS2Fixes

Prefer keeping ZombieReborn/CS2Fixes as a ZE compatibility layer while ZEPVE owns PvE rules.

Do not put general ZEPVE gameplay logic into CS2Fixes. Prefer minimal upstream-compatible patches and upstream contributions where practical.

## Performance

Movement may run every tick in the owning Navigation component. Most other work should be event-driven or lower frequency.

Avoid entity scans, JSON parsing, heavy traces, LINQ/allocation and log spam in hot paths.

## Logging / Config

Use CounterStrikeSharp `Logger`; do not use normal `Console.WriteLine`.

Log important state changes and exceptions with context. No per-tick logging unless temporary debug mode is explicitly enabled.

Configs must be versioned and validated.

## Lifecycle

Always consider hot reload, map change, disconnect, Bot respawn and invalid entities.

Clean up timers, tasks, hooks, subscriptions and cached map state on unload/map change.

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

If CS2 runtime testing was not possible, say so.

## Documentation Localization

User-facing documentation is first-class in English and Simplified Chinese.

When changing user-visible features, commands, installation, configuration or troubleshooting, update both languages when practical.

Developer-only documents do not require translation unless useful to contributors.

## Final Check

Before finishing, verify repository ownership, component revision impact, lifecycle, performance, logs, config validation, build status and rollback path.

Primary rule: keep **component development**, **suite integration**, and **experimentation** separate and auditable.
