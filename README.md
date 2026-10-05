# CS2-ZE-PVE

**English | [简体中文](README.zh-CN.md)**

> **Make ZE Playable Again.**

CS2-ZE-PVE (ZEPVE) is a lightweight cooperative PvE runtime focused on keeping Counter-Strike 2 Zombie Escape maps playable with **1–6 players**.

## Goals

- ZE-first compatibility
- AI zombie opponents
- Valve NAV when available
- Trail-based fallback for no-NAV maps
- CS2Fixes / ZombieReborn compatibility
- Low server overhead
- Minimal per-map setup
- Optional HUD, weapons and Director modules
- Extensible APIs for Bot/Nav and map developers

## Status

Private early-development repository.

Current priorities:

1. Stabilize the existing PVE core.
2. Split navigation responsibilities cleanly.
3. Build a no-NAV TrailDriver.
4. Preserve mature ZE map semantics through CS2Fixes compatibility.
5. Keep user-facing documentation bilingual.

## Project Structure

```text
ZEPVE.Abstractions
├─ ZEPVE.Core
├─ ZEPVE.Navigation
├─ ZEPVE.Map
├─ ZEPVE.Hud
├─ ZEPVE.Weapons
└─ ZEPVE.Director
```

ZE remains the primary mode. Survival, Horde and standard-map PvE may be added as extensions where they improve single-player and co-op replayability.

## Development

Codex/GPT startup order:

```text
1. AGENTS.md
2. GITHUB_MANAGEMENT.md
3. ROADMAP.md
4. DECISIONS.md
```

Then read only files relevant to the current task.
