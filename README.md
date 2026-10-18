# CS2-ZE-PVE

**English | [简体中文](README.zh-CN.md)**

CS2-ZE-PVE (ZEPVE) is a cooperative PvE framework for **Counter-Strike 2 Zombie Escape**, built around **Human players versus AI-controlled Zombies**.

The project is ZE-first and targets small-group play (roughly **1–6 Human players**) while preserving map behavior and reusing Valve/native Bot behavior wherever it already works. ZEPVE adds ZE-specific ownership, persistent map knowledge and exceptional traversal only where the engine needs help.

## Core model

```text
AssignedTarget       = WHO the Zombie should pursue
MapTraversalGraph    = HOW Navigation can reach that Human legally
Valve/native AI      = normal perception, combat and ordinary movement where useful
ZEPVE Navigation     = route/branch choice, rejoin/replan and exceptional traversal
MovementBridge       = the single authorized low-level movement commit path
```

Human players remain Human in PvE. Human death changes lifecycle state; ZEPVE does not convert Human players into Zombie players.

## Features

- Bot-only Zombie PvE gameplay
- Valve/native navigation for ordinary movement when usable
- per-Human Runtime Trail fallback for unknown/unrecorded maps or no-NAV pursuit
- authoritative BotAI `AssignedTarget`
- single-owner Navigation movement pipeline with layered Recovery
- planned persistent `MapTraversalGraph` with main paths, branches, short links and typed traversal edges
- branch planning that can distribute Zombies across legal alternate routes instead of forcing every Bot to copy the Humans' path
- route deviation/rejoin/replan after physical knockback instead of snapping Bots back to recorded samples
- CS2Fixes / ZombieReborn compatibility
- optional HUD and WeaponSystem migration targets

## Project Family

| Repository | Purpose | Status |
| --- | --- | --- |
| **CS2-ZE-PVE** | Main runtime, Core/BotAI, integration, migration authority, packaging and releases | Active integration repository |
| **[ZEPVE-Navigation](https://github.com/Kzen023/ZEPVE-Navigation)** | Runtime Trails, route/branch planning, graph/traversal execution, locomotion orchestration and Recovery | v0.3 baseline + later graph/traversal work |
| **[ZEPVE-BotController](https://github.com/Kzen023/ZEPVE-BotController)** | Low-level leased movement backend, command capture/application and scoped traversal execution | Backend component; advanced traversal remains experimental |
| **[ZEPVE-Lab](https://github.com/Kzen023/ZEPVE-Lab)** | Isolated engine/Bot/movement experiments and evidence | Experimental only; never ships |
| **[ZEPVE-HUD](https://github.com/Kzen023/ZEPVE-HUD)** | Optional player-facing presentation | v0.7 migration target; not yet pinned |
| **[ZEPVE-WeaponSystem](https://github.com/Kzen023/ZEPVE-WeaponSystem)** | Optional PvE weapon/purchase policy | v0.7 migration target; not yet pinned |

Independent repositories are integrated only after their ownership boundary, build and runtime behavior are verified. Creating a repository or pinning an experimental revision does not make it release-ready.

## Current version boundaries

```text
v0.3  Navigation baseline
      AssignedTarget -> Native -> Trail -> Recovery
      leased UserCmd movement, lifecycle, matched install/rollback

v0.4  Typed local traversal
      Entry -> Execute -> Exit/Abort
      timed/signed-subtick payload only when required,
      jump/crouch-jump, scoped traversal view, short MotionClip

v0.5  Persistent MapTraversalGraph
      main path / branch / link authoring,
      Region/Route/Node/TraversalEdge,
      route rejoin/replan, branch diversity and curated map data

v0.6  MapPlan + CS2Fixes integration
      stage/mechanism legality, canonical map state and PvE respawn rules
```

The clean v0.3 cuts are valid isolation points but are **not release-ready unchanged**. Baseline pursuit defects, Recovery permission, artifact provenance and fresh real-server acceptance still have to be closed before merge.

## Route recording philosophy

Recording is an **authoring input**, not runtime full-route replay.

For ordinary ZE map support, testers should only need three recording purposes:

```text
Main path  — normal map-completion route
Branch     — a genuinely different route/entrance/floor/corridor
Link       — a short missing connection between already-known areas
```

The recorder captures rich Human movement temporarily, then simplifies it into semantic graph data. Doors, elevators, waits, teleports and ordinary jumps become graph-edge semantics rather than separate recorder systems. Advanced traversal capture is used only when normal movement or a primitive edge is proven insufficient.

Zombies may choose a legal branch even when every Human took the main path. This allows side/rear pressure through route topology and cost/congestion, without creating a separate tactical flank AI.

Weapon knockback is treated as physical route deviation: continue when legal, bounded rejoin, graph replan, alternative branch, then Recovery only as the final separately-permitted fallback.

## Recorder / replay ownership

ZEPVE does not need a separate production Recorder repository.

```text
ZEPVE-BotController -> how bounded command data is captured/applied
ZEPVE-Navigation    -> what route/traversal data means and when it is used
ZEPVE-Lab           -> experiments/evidence only
```

Persistent route authoring stays in Navigation unless future offline tooling becomes independently useful enough to justify its own tool repository.

## Legacy baseline

Working pre-split plugins are preserved under [`legacy/`](legacy/README.md) while responsibilities are migrated into the new ownership model.

Migration keeps exactly one active writer for each state-changing responsibility; handoff rules are tracked in [`MIGRATION_AUTHORITY.md`](MIGRATION_AUTHORITY.md).

## Installation

ZEPVE does not yet publish a general public release. Test and future release packages are assembled from exact matched component revisions by this repository and are intended for deployment under `game/csgo`.

Do not manually mix component DLLs/revisions from different experimental PRs.

## Status

v0.1 and v0.2 foundations are closed historical milestones. The accepted v0.2 Core/BotAI foundation remains the stable runtime baseline.

Current engineering work is Phase 1 of the audited plan: close the clean v0.3 baseline on BotController #3, Navigation #3 and Suite #15, produce a provenance-bound matched Test Preview, complete real baseline acceptance and actual whole-package rollback, then stop before v0.4 traversal work.

See [ROADMAP.md](ROADMAP.md), [docs/ZE_PATH_SYSTEM.md](docs/ZE_PATH_SYSTEM.md), [docs/MAP_TRAVERSAL_GRAPH.md](docs/MAP_TRAVERSAL_GRAPH.md), [docs/VERSION_SCOPE_RESET.md](docs/VERSION_SCOPE_RESET.md) and [docs/PROJECT_AUDIT_20261011.md](docs/PROJECT_AUDIT_20261011.md).

## Documentation

- [Simplified Chinese README](README.zh-CN.md)
- [ZE path-system policy](docs/ZE_PATH_SYSTEM.md)
- [MapTraversalGraph plan](docs/MAP_TRAVERSAL_GRAPH.md)
- [Legacy migration baseline](legacy/README.md)
- [Migration authority](MIGRATION_AUTHORITY.md)
- [Version scope reset](docs/VERSION_SCOPE_RESET.md)
- [Project audit and Phase 1 handoff](docs/PROJECT_AUDIT_20261011.md)
- [Roadmap](ROADMAP.md)
- [Contributing](CONTRIBUTING.md)
- [Repository and release management](GITHUB_MANAGEMENT.md)
- [Architecture decisions](DECISIONS.md)
