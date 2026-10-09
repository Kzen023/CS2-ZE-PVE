# CS2-ZE-PVE

**English | [简体中文](README.zh-CN.md)**

CS2-ZE-PVE (ZEPVE) is a lightweight cooperative PvE runtime for **Counter-Strike 2 Zombie Escape**, designed for **1–6 human players** fighting Bot zombies.

The project is ZE-first: it aims to keep Zombie Escape maps playable with small groups while preserving map behavior and requiring as little per-map setup as practical.

## Features

- Bot-only zombie PvE gameplay
- Valve navigation when a usable NAV mesh is available
- Player Trail navigation for maps without usable NAV
- Stuck detection and recovery for zombie Bots
- CS2Fixes / ZombieReborn compatibility
- Low server overhead and small-group operation
- Optional HUD and WeaponSystem components
- Extensible boundaries for navigation, map and Bot integrations

## Project Family

| Repository | Purpose | Status |
| --- | --- | --- |
| **CS2-ZE-PVE** | Main runtime, Core/BotAI integration, legacy migration baseline, configuration, documentation and releases | Active integration repository |
| **[ZEPVE-Navigation](https://github.com/Kzen023/ZEPVE-Navigation)** | Zombie navigation and movement for NAV and no-NAV maps | Production component under active development |
| **[ZEPVE-HUD](https://github.com/Kzen023/ZEPVE-HUD)** | Optional player-facing HUD/presentation component | Migration target; not yet pinned |
| **[ZEPVE-WeaponSystem](https://github.com/Kzen023/ZEPVE-WeponSystem)** | Optional PvE weapon balance and purchase component | Migration target; repository rename pending |
| **[ZEPVE-Lab](https://github.com/Kzen023/ZEPVE-Lab)** | Isolated experiments for engine, Bot and movement behavior | Experimental only |

Independent repositories are integrated only after their ownership boundary, build and runtime behavior are verified. Creating a repository does not automatically make it a release dependency.

## Legacy Baseline

The existing working plugins are preserved under [`legacy/`](legacy/README.md) while their responsibilities are migrated into the new ZEPVE ownership model.

The current legacy set includes the main PvE runtime, weapon-balance plugin and ZE assist/HUD plugin. Legacy code is treated as a regression baseline rather than discarded prototype code.

## Installation

ZEPVE is currently in private early development and does not have a public release package yet.

When releases begin, install-ready packages will be published from this repository and structured for deployment under `game/csgo`.

## Status

Current work focuses on reproducing and classifying the legacy baseline, extracting Core lifecycle ownership, and integrating independently developed components without changing verified gameplay unnecessarily.

See [ROADMAP.md](ROADMAP.md) for milestones.

## Documentation

- [Simplified Chinese README](README.zh-CN.md)
- [Legacy migration baseline](legacy/README.md)
- [Roadmap](ROADMAP.md)
- [Contributing](CONTRIBUTING.md)
- [Repository and release management](GITHUB_MANAGEMENT.md)
- [Architecture decisions](DECISIONS.md)

Developer and repository-management details are intentionally kept outside this README.
