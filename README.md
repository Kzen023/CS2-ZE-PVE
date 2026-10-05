# CS2-ZE-PVE

**English | [简体中文](README.zh-CN.md)**

CS2-ZE-PVE (ZEPVE) is the **integration and release repository** for a ZE-first cooperative PvE runtime for Counter-Strike 2.

The project targets **1–6 human players** fighting Bot zombies on Zombie Escape maps, with Valve NAV where it works and Trail-based fallback where it does not.

## Project Family

| Repository | Role | Shipped in ZEPVE releases? |
| --- | --- | --- |
| **CS2-ZE-PVE** | Suite integration, Core/runtime, configs, compatibility, packaging and releases | Yes |
| **[ZEPVE-Navigation](https://github.com/kzen1023/ZEPVE-Navigation)** | Independent navigation component: Trail, Valve NAV integration, stuck detection and recovery | Yes, pinned to a specific commit/version |
| **[ZEPVE-Lab](https://github.com/kzen1023/ZEPVE-Lab)** | Reproducible Bot/UserCmd/native-engine experiments | No |
| `ZEPVE-CS2Fixes` | Future focused CS2Fixes compatibility fork if required | Only when intentionally pinned |
| `ZEPVE-MovementBridge` | Future native movement bridge only if existing APIs are insufficient | Only after it becomes a stable component |

## Integration Model

ZEPVE follows an integration-repository pattern inspired by projects such as CS2-Bot-Improver:

```text
component repository
        │
        │ tested change / release
        ▼
pinned component commit
        │
        ▼
CS2-ZE-PVE integration repository
        │
        │ suite validation
        ▼
install-ready ZEPVE release
```

A production component is **not** consumed by automatically following its moving `main`. The integration repository pins the exact component commit through Git submodules. Component updates are reviewed as explicit integration changes.

`ZEPVE-Lab` is deliberately excluded from this chain. Successful experiments are redesigned and promoted into a production repository before they can ship.

## Runtime Goals

- ZE map compatibility first
- Humans remain the PvE player team; zombies are Bots
- Valve NAV when native navigation is usable
- Player Trail fallback for no-NAV maps
- CS2Fixes / ZombieReborn compatibility without duplicate rule ownership
- Low server overhead and minimal per-map setup
- Optional HUD, Weapons and Director functionality
- Stable extension points for Bot/Nav contributors

## Repository Responsibilities

This repository owns suite-level concerns:

```text
CS2-ZE-PVE/
├─ src/                         # suite-owned runtime modules such as Core / Map
├─ components/
│  └─ ZEPVE-Navigation/         # pinned Git submodule
├─ configs/
├─ maps/
├─ integrations/
├─ release/                     # packaging/manifest definitions
├─ docs/
└─ .github/
```

Independent components should not be duplicated under `src/` after they are split into their own repository.

## Release Model

Players should eventually install **one ZEPVE release package**, not manually assemble component repositories.

A release should record:

- ZEPVE suite version and commit
- exact component commits/versions
- tested CS2 build
- Metamod version
- CounterStrikeSharp version
- CS2Fixes version/commit when used
- platform-specific native dependencies when used

Release archives should be staged so their contents can be copied into `game/csgo` with minimal manual work.

## Status

Private early development.

Current priorities:

1. Import the existing working PvE implementation into the suite repository.
2. Establish reproducible builds and release staging.
3. Develop Navigation independently and pin tested revisions here.
4. Prove risky movement/native behavior in ZEPVE-Lab before production integration.
5. Keep user-facing documentation in English and Simplified Chinese.

## Change Workflow

Documentation, repository pages and lightweight templates may be updated directly on `main` when the owner requests it.

Changes that affect source behavior, builds, dependencies, component pins or release contents should use:

```text
agent/* / feat/* / fix/*
        ↓
      PR + diff
        ↓
 tests / review
        ↓
       main
```

For coding rules, read `AGENTS.md`. For repository/component workflow, read `GITHUB_MANAGEMENT.md`.
