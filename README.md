# CS2-ZE-PVE

**English | [简体中文](README.zh-CN.md)**

CS2-ZE-PVE (ZEPVE) is a lightweight cooperative PvE runtime for **Counter-Strike 2 Zombie Escape**, designed for **1–6 human players** fighting Bot zombies.

The project is ZE-first: it aims to keep Zombie Escape maps playable with small groups while preserving map behavior and requiring as little per-map setup as practical.

## Features

- Bot-only zombie PvE gameplay
- Valve navigation when a usable NAV mesh is available
- per-Human Runtime Trail navigation when native navigation is unavailable or not progressing
- stuck detection and layered Recovery
- explicit BotAI `AssignedTarget` authority
- CS2Fixes / ZombieReborn compatibility
- one validated final movement writer
- optional HUD and WeaponSystem components

## Project Family

| Repository | Purpose | Status |
| --- | --- | --- |
| **CS2-ZE-PVE** | Main runtime, Core/BotAI, integration, migration authority, packaging and releases | Active integration repository |
| **[ZEPVE-Navigation](https://github.com/Kzen023/ZEPVE-Navigation)** | Runtime Trails, route/traversal selection, locomotion orchestration and Recovery | v0.3 baseline + v0.4 traversal development |
| **[ZEPVE-BotController](https://github.com/Kzen023/ZEPVE-BotController)** | Low-level leased movement backend, command capture/application and scoped traversal execution | Backend component; advanced traversal remains experimental |
| **[ZEPVE-Lab](https://github.com/Kzen023/ZEPVE-Lab)** | Isolated engine/Bot/movement experiments and evidence | Experimental only; never ships |
| **[ZEPVE-HUD](https://github.com/Kzen023/ZEPVE-HUD)** | Optional player-facing presentation | v0.7 migration target; not yet pinned |
| **[ZEPVE-WeaponSystem](https://github.com/Kzen023/ZEPVE-WeaponSystem)** | Optional PvE weapon/purchase policy | v0.7 migration target; not yet pinned |

Independent repositories are integrated only after their ownership boundary, build and runtime behavior are verified. Creating a repository or pinning an experimental revision does not make it release-ready.

## Current version boundaries

The roadmap is intentionally split so advanced movement research does not keep expanding the v0.3 merge gate:

```text
v0.3  Navigation baseline
      AssignedTarget -> Native -> Trail -> Recovery
      leased UserCmd movement, lifecycle, matched install/rollback

v0.4  Advanced traversal
      timed/signed-subtick payloads, typed local actions,
      jump/crouch-jump, scoped view, MotionClip

v0.5  Persistent routes
      RecordedRoute, RouteGraph, persistent typed TraversalEdge,
      Entry/Exit volumes and authoring
```

The clean v0.3 cuts are valid isolation points but are **not release-ready unchanged**. Baseline pursuit defects, Recovery permission, artifact provenance and fresh real-server acceptance still have to be closed before merge.

Complex jump imitation, rotational replay and exact Human physics parity are v0.4 work, not v0.3 baseline requirements.

## Recorder / replay ownership

ZEPVE does not currently need a separate production Recorder repository.

```text
ZEPVE-BotController -> how bounded command data is captured/applied
ZEPVE-Navigation    -> what maneuver/route data means and when it is used
ZEPVE-Lab           -> experiments/evidence only
```

Persistent route authoring stays in Navigation unless future offline tooling becomes independently useful enough to justify its own tool repository.

## Legacy Baseline

Working pre-split plugins are preserved under [`legacy/`](legacy/README.md) while responsibilities are migrated into the new ownership model.

Migration keeps exactly one active writer for each state-changing responsibility; handoff rules are tracked in [`MIGRATION_AUTHORITY.md`](MIGRATION_AUTHORITY.md).

## Installation

ZEPVE does not yet publish a general public release. Test and future release packages are assembled from exact matched component revisions by this repository and are intended for deployment under `game/csgo`.

Do not manually mix component DLLs/revisions from different experimental PRs.

## Status

The accepted v0.2 Core/BotAI foundation remains the stable baseline. Current engineering work is Phase 1 of the audited plan: close the clean v0.3 baseline on BotController #3, Navigation #3 and Suite #15, produce a provenance-bound matched Test Preview, complete real baseline acceptance and actual whole-package rollback, then stop before v0.4 traversal work.

See [ROADMAP.md](ROADMAP.md), [docs/VERSION_SCOPE_RESET.md](docs/VERSION_SCOPE_RESET.md) and [docs/PROJECT_AUDIT_20261011.md](docs/PROJECT_AUDIT_20261011.md).

## Documentation

- [Simplified Chinese README](README.zh-CN.md)
- [Legacy migration baseline](legacy/README.md)
- [Migration authority](MIGRATION_AUTHORITY.md)
- [Version scope reset](docs/VERSION_SCOPE_RESET.md)
- [Project audit and Phase 1 handoff](docs/PROJECT_AUDIT_20261011.md)
- [Roadmap](ROADMAP.md)
- [Contributing](CONTRIBUTING.md)
- [Repository and release management](GITHUB_MANAGEMENT.md)
- [Architecture decisions](DECISIONS.md)
