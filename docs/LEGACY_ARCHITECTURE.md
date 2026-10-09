# Legacy Architecture

Audit date: 2026-10-09. This document describes the installed source snapshot inspected for the initial ZEPVE baseline import. It does not claim that every source behavior has been verified in a live server.

## Legacy Architecture

The CS2 installation is not a Git repository. The active CounterStrikeSharp plugin area contains three independently built projects and no solution file or `global.json`:

- `Kzen-ZRPVE`: one PvE lifecycle, bot quota, infection, trail and recovery implementation in `ZrPvePlugin.cs`.
- `Kzen-ZEAssist`: countdown, entity/player health HUD and learned button guidance in `KzenZeAssistPlugin.cs`.
- `Kzen-WeaponBalance`: weapon tuning, purchases, aliases and damage scaling in `WeaponBalancePlugin.cs`.

The import keeps these projects monolithic and under `legacy/`. It does not move or update the repository's Navigation submodule.

## Current Runtime Flow

1. CounterStrikeSharp loads the three plugin DLLs listed in its plugin directory.
2. ZRPVE starts map-scoped trail/watchdog/HUD timers on map start. At round start it resets round state, schedules profile application, configures CS2 and ZombieReborn cvars, and resets bots.
3. After the configured infection delay, ZRPVE changes bot team/quota and releases the zombie bots. Player spawn/death/hurt events update recovery/watch state.
4. Human CT ground positions are sampled every 0.25 seconds by default. A one-second watchdog checks live T bots for limited progress or lack of human damage. Recovery selects a recent trail point 800–1600 units from the nearest live human, reserves it, then teleports the bot there with no velocity.
5. ZEAssist observes countdown chat/SayText2 and map entity outputs, renders countdown/health HUDs, and highlights learned nearby buttons. WeaponBalance applies weapon values, handles native buy and chat commands, and applies damage scaling on pre-damage callbacks.

The source does not select a ZEPVE target. Bot pursuit is left to native CS2/CS2Fixes/ZombieReborn bot behavior and configured cvars.

## Build & Dependencies

- Projects target `net10.0`, nullable and implicit usings enabled. They contain no NuGet package references.
- Build host observed: .NET SDK 10.0.401 (also SDK 8.0.425 installed); .NET 10 runtime 10.0.12.
- Compile-time dependency: installed `CounterStrikeSharp.API.dll`, informational version `1.0.376+Branch.main.Sha.653d651f1ac09ac1ddb423d588f871b891038860.653d651`. The project files use a non-private direct assembly reference.
- Runtime dependencies: MetaMod:Source, CounterStrikeSharp, CS2Fixes with its Zombie:Reborn feature, and BotController (runtime log reported v0.7.1, commit `0ae8f18`). These are server-installed dependencies, not vendored here. Exact MetaMod and CS2Fixes build identifiers were not established from the inspected logs.
- The install tree has no solution or pinned SDK file. Each project built successfully in Release with no warnings and no errors using `dotnet build <project.csproj> --configuration Release` (restore enabled; no NuGet packages are referenced).
- In the imported projects, `CounterStrikeSharpApiPath` can be supplied to the direct reference. Example from a checkout on Windows:
  `dotnet build legacy/CounterStrikeSharp/plugins/Kzen-ZRPVE/Kzen-ZRPVE.csproj -c Release -p:CounterStrikeSharpApiPath="D:\CS2\game\csgo\addons\counterstrikesharp\api\CounterStrikeSharp.API.dll"`
  Repeat for the ZEAssist and WeaponBalance projects. No API binary is committed.
- Expected output names: `Kzen-ZRPVE.dll`, `Kzen-ZEAssist.dll`, and `Kzen-WeaponBalance.dll`, under each project's `bin/Release/net10.0/`. Generated binaries/PDBs/deps files are intentionally excluded.

Important reproducibility discrepancy: ZEAssist and WeaponBalance Release outputs were byte-identical to their installed DLLs. ZRPVE compiled successfully from the inspected source, but its output SHA-256 differed from the installed `Kzen-ZRPVE.dll`. The installed folder also contains stale dependency and PDB artifacts from an older project identity. The current source snapshot therefore does not reproduce the deployed ZRPVE binary byte-for-byte; its exact source-to-runtime relationship remains unresolved. No binary is presented as a verified build of that source.

## Current Features

- ZRPVE: solo/duo/coop/group profiles; round and infection setup; CS2Fixes Zombie:Reborn cvar configuration; quota-managed bots; respawn/stuck/no-damage recovery; human trail sampling; optional bot status/debug HUD; ZE/non-ZE round timer settings.
- ZEAssist is the user's HUD/ZE-assist plugin: chat and SayText2 countdown detection, manual countdown command, map-entity and player/zombie health HUD, button output observation, per-map learned button scores and glow guidance.
- WeaponBalance: weapon clip/price/reserve settings; CT damage scaling against T players; native buy interception/settlement; prefixed aliases plus bare `sy`, `a1`, and `sq`; slot replacement and equip request in the inspected source.

Installed config locations:
- `game/csgo/addons/counterstrikesharp/configs/plugins/Kzen-ZRPVE/zrpve.cfg`
- `.../Kzen-WeaponBalance/weapons.cfg`
- `.../Kzen-ZEAssist/button-learning.json` (runtime-learned map data; intentionally not imported)
- `.../Kzen-WeaponBalance/buy_mappings.cfg` (loadout-hash keyed local runtime data; intentionally not imported)

The import includes the ZRPVE and WeaponBalance configuration snapshots; these contain gameplay settings but no credentials. An older config outside the active CounterStrikeSharp config tree is stale and was not imported. Shared CounterStrikeSharp gamedata was observed at `game/csgo/addons/counterstrikesharp/gamedata/gamedata.json` (45 entries loaded in the supplied log); it is an installed external runtime file, not a plugin-owned resource. No plugin-owned native component or bundled map asset was found.

## Known Working Behavior

Evidence is deliberately split by strength:

- **Build confirmed:** all three current source projects compiled in Release on this installation with zero warnings/errors.
- **Plugin load confirmed by supplied server logs:** the logs show CounterStrikeSharp loaded and ZRPVE/ZEAssist plugin contexts loading; WeaponBalance reported 34 config entries. This establishes startup/load only, not every feature.
- **Observed in supplied de_dust2 logs:** the infection countdown synchronized at 16 seconds; the log reports release maintaining 10 T bots; WeaponBalance intercepted native `unused 5` and `unused 3`; prefixed `!deagle` was received; ZRPVE emitted several `recovery placed` records and many `recovery skipped: no route point` records.
- The same logs report no `func_button` entities on de_dust2, so they do not verify ZE button highlighting.
- **Not confirmed in a live feature test:** bare `sy/a1/sq`, same-slot weapon replacement and auto-equip, weapon settlement/damage tuning, countdown accuracy across ZE maps, recovery safety, bot pursuit after teleport, hot reload cleanup, and any complete ZE-map run.

A recovery log line confirms that the plugin reached its placement branch; it does not establish that the bot reacquired a valid target or pursued correctly afterward.

## Known Bugs

- Reported test issue: a bot may wander after recovery teleport because Valve Enemy/aggro may not recover. The inspected ZRPVE source contains no `Enemy`, `AssignedTarget`, `Alert`, `IgnoreEnemies`, or `LookAround` reads/writes and performs no explicit target reacquisition. The post-teleport engine state is therefore unknown. Preserve this as a BotAI/Lab follow-up; do not change it in this baseline.
- `UpdateEscorts()`, `QueueInitialRecoveries()`, and `AddBots()` have no call sites in the inspected source; escort stepping and those code paths are dormant. Several escort/initial-recovery config values are correspondingly ineffective.
- `_cancelledRecoveries` is cleared but not otherwise used. `StopEscort` currently only removes the dictionary entry.
- WeaponBalance's current source emits DEBUG console lines for frequent buy, settlement and weapon-entity paths; potential log volume is unbounded by a debug setting.
- WeaponBalance changes global `cs2f_weapons_enable` and optional reserve-ammo cvars at load; no unload restoration is implemented.
- ZEAssist's map-learning JSON path is assembled relative to `AppContext.BaseDirectory`; the effective resolved path under the live loader was not independently verified. Its map/button indices may become stale after map entity changes.
- ZRPVE uses `Server.PrintToConsole` rather than CounterStrikeSharp Logger. This is baseline behavior, not corrected in the import.
- The deployed ZRPVE DLL/source mismatch and stale artifacts make the exact running code version uncertain.

## Lifecycle/Cleanup Risks

- ZRPVE clears state on map/round start and uses map-stopping timers. It handles bot spawn/death/hurt, but has no player-disconnect handler, no round-end handler and no explicit unload/hot-reload cleanup override.
- ZEAssist resets map state at map start and its map timers stop on map change. Per-slot health is removed on HUD expiry, not explicitly on disconnect; hot-reload/unload cleanup is not explicit.
- WeaponBalance uses map-stopping timers for deferred work but has no explicit unload restoration for global cvars or per-slot pending purchase state cleanup. Slot reuse during delayed settlement warrants testing.
- These are source-level lifecycle risks; their impact during live hot reload/map transitions is unverified.

## Performance Risks

- ZRPVE scans players for trail recording every 0.25 seconds and for bot watchdog/status updates at configured intervals. Recovery searches recent path points against live humans; path history is capped at 2048.
- ZEAssist enumerates map buttons and sorts candidates every second; countdown HUD work is gated to 10 Hz from an OnTick listener. Health HUD refresh is 2 Hz.
- WeaponBalance scans owned weapons for spawn/buy operations and emits console diagnostics on hot entity/purchase paths.
- No profiler or server frame-time measurement was supplied. These are code-path observations, not measured performance regressions.

## Future ZEPVE Ownership Mapping

- **ZEPVE.Core:** profile and round lifecycle, bot quota, infection/release, spawn/death/respawn coordination. Current code mixes these with path/recovery state in ZRPVE.
- **ZEPVE.BotAI:** future authoritative `AssignedTarget`, target selection/distribution and post-recovery reacquisition. Legacy source has no such state; Valve Enemy is not managed here.
- **ZEPVE.Navigation:** human Trail sampling, progress/stuck checks, recovery point selection/reservation and direct recovery teleport. Dormant escort code is not an active movement system.
- **ZEPVE.Map:** map-name round-time override; ZEAssist button/countdown/map entity integration and learned map button state.
- **ZEPVE.Hud:** ZRPVE status/debug HUD and ZEAssist countdown/health/button-guidance display.
- **ZEPVE.Weapons:** all WeaponBalance configuration, buy handlers, weapon properties and damage scaling.
- **ZEPVE.Director:** profile thresholds and human-count-based quota/difficulty scaling.

Mixed responsibilities should be separated only in later behavior-preserving PRs with comparison against this baseline.

## Migration Notes

- Imported only the three active Kzen plugin sources, project files and two non-secret gameplay config snapshots. No borrowed reference-plugin source or binary was included. Project references were parameterized for the external CounterStrikeSharp API assembly; gameplay source was not refactored.
- Excluded DLL/PDB/deps outputs, server logs, server-wide/private configs, button-learning state, buy mappings, disabled plugin binaries, historical research trees, and third-party CS2Fixes source/binaries.
- This is a source/build baseline import, not a server deployment and not proof of full feature parity. Compare all future gameplay changes against this snapshot and use ZEPVE-Lab for uncertain engine AI behavior.
