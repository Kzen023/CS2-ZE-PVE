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
- Optional HUD, Weapons and Director modules
- Extensible boundaries for navigation, map and Bot integrations

## Project Family

| Repository | Purpose |
| --- | --- |
| **CS2-ZE-PVE** | Main runtime, integration, configuration, documentation and releases |
| **[ZEPVE-Navigation](https://github.com/kzen1023/ZEPVE-Navigation)** | Zombie navigation and movement component for NAV and no-NAV maps |
| **[ZEPVE-Lab](https://github.com/kzen1023/ZEPVE-Lab)** | Isolated experiments for engine, Bot and movement behavior |

Additional repositories are created only when a component genuinely needs an independent lifecycle.

## Installation

ZEPVE is currently in private early development and does not have a public release package yet.

When releases begin, install-ready packages will be published from this repository and structured for deployment under `game/csgo`.

## Status

The current work is focused on importing and stabilizing the existing PvE prototype, establishing reproducible builds, and bringing the standalone Navigation component into the suite.

See [ROADMAP.md](ROADMAP.md) for milestones.

## Documentation

- [Simplified Chinese README](README.zh-CN.md)
- [Roadmap](ROADMAP.md)
- [Contributing](CONTRIBUTING.md)
- [Repository and release management](GITHUB_MANAGEMENT.md)
- [Architecture decisions](DECISIONS.md)

Developer and repository-management details are intentionally kept outside this README.
