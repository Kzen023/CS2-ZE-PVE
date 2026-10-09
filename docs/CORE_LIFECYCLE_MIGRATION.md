# v0.2c lifecycle / authority migration

English | [简体中文](CORE_LIFECYCLE_MIGRATION.zh-CN.md)

PR #9 was closed out and merged at `745fa4a82bb7745f9ef1648c0473e0496c532efa`, with the owner's remaining runtime risks explicitly accepted as deferred acceptance. This implementation starts from that main revision on `agent/core-lifecycle-authority`. PR #9's results describe the observer artifact, not this gameplay handoff.

## Architecture and exact handoff

One PlayerRegistry owns plugin GUID, map/round epochs, connection and pawn generations. Immutable player contexts and role views implement the Abstractions read-only `ICoreLifecycle`/`IPlayerContext` API. `SuiteRuntime.Current` is the single published provider reference. Publish refuses another live provider or an unloaded provider. An internal migration bridge exposes legacy config and the existing countdown/recovery services; it cannot execute quota/team/infection policy.

Core coordinates map/round/spawn events and forwards recovery-local reset/spawn notifications after identity advancement. `PveRoundController` owns the original static profiles, round preparation, release deadline, quota and global Bot team transition commands. The native adapter re-resolves controller handles/UserId and validates current connection/pawn generations before team actions, including dead controllers; it never trusts a retained slot. Recovery geometry, point selection, teleport, shared Trail data and presentation remain in legacy. Their algorithm bodies and config parser/defaults are checked against the accepted baseline Git object.

Adapted legacy removes all old lifecycle/quota/team/policy executors and `_roundToken`. Core issues gameplay commands only with the adapted bridge present. Missing Core/bridge fails closed; there is no dynamic fallback to old legacy gameplay. Duplicate bridge/provider registration is rejected. A partial upgrade therefore cannot create a second migrated writer, but can leave gameplay policy unavailable. Install the matched pair.

## Preserved behavior / deliberate validity changes

- Countdown cvars and existing legacy display run immediately at round start.
- Profile application stays at round start +1 second; Core then waits the configured InfectionDelay. With the existing 16-second config, release is at +17 seconds, quota follows by BotAddDelay=0.2 and the Bot team pass by ReleaseMoveDelay=1. No timing “correction” is introduced.
- Solo/duo/coop/group quota rules, infection/compatibility values and respawn delay 7/5/4/3 seconds remain the baseline values. Profile counts include CT and T humans, excluding Bots/HLTV/spectators. No Director, target selection or new AI policy exists.
- Core supplies respawn policy through existing ZR settings. **ZombieReborn / existing compatible runtime remains the sole respawn executor.** No respawn scheduler/API call was added.
- Work from an ended/restarted round, old map, disconnected connection, replaced/dead pawn or unloaded provider/consumer is canceled/rejected. This intentionally stops unsafe stale execution that legacy allowed. Recovery's destination/teleport algorithm is unchanged; its delayed callback now uses Core tokens.
- Repeating legacy map services capture Core map/plugin validity and re-read current players on every sample. Map-local sampling may continue across rounds; reset is coordinated by Core. No per-tick scans or new disk writes were introduced.

## Tokens, scopes and bounded diagnostics

PlayerLifetime carries plugin lifetime, MapEpoch, RoundEpoch, slot, ConnectionGeneration and PawnGeneration. Capture/resolve defaults to a live bound pawn; explicitly controller-only work can require Connected and still checks all token generations while re-reading the current pawn/controller. `ICoreWorkScope` cancels work on consumer disposal and relevant Core invalidation; a dispatched native callback after cancellation cannot resurrect the job. Capacity is 256 pending jobs globally, with a 16-probe command limit. Callbacks execute on the server thread and must still validate their own current role/permission/binding.

ServerLifetime covers plugin/map/round for global actions with no actor. Round work validates all three; explicitly map-only settings/sampling intentionally survive round transitions and validate plugin/map. Player-specific work always uses PlayerLifetime; a server token is not an excuse to capture a slot. Future target/route bindings remain outside this stage.

FlightRecorder is an in-memory 256-entry ring with sequence order, dropped count and details limited to 160 characters. It records plugin/map/round, connect/disconnect, role/pawn changes, stale rejections, cancellations, authority and policy transitions. There is no automatic dump or periodic disk/log spam.

Commands:

- `css_zepve_status`: lifecycle authority, provider GUID/epochs, counts/contexts, pending work, recorder counts, Core phase/profile/quota policy and external respawn executor. No BotAI/Navigation/Trail state.
- `css_zepve_probe <numeric-slot> [seconds]`: same limits as the observer; invalidation now logs REJECT eagerly rather than waiting for the original timer deadline.
- `css_zepve_events [1..64]`: server console / @css/root, bounded recorder snapshot (default 32).

## Hot / late load

Existing players are indexed immediately. Atomic Core hot reload retains a value-only Core-authored policy plan in the shared ABI: profile, status and absolute pending deadlines. It publishes a fresh GUID, cancels old player work, restarts map services without clearing same-map legacy Trail/recovery data, and schedules only unfinished policy steps. Preparation, countdown or completed quota/release actions are not replayed. Carry-over is accepted only for hot reload on the same map.

Manual unload discards carry-over and stops dependent services. Mid-map manual load establishes observations and map settings but Phase=Unbound until the next round start; it does not infer infection state/deadlines from teams or reset existing Bots. This may defer Core-dependent recovery until the next round. Legacy bridge reload suspends policy work, then resumes the still-valid Core-owned plan. Upgrade of the shared ABI itself requires server shutdown, not hot reload.

## Build, matched installation and rollback

```powershell
./scripts/build-core.ps1 -CounterStrikeSharpApiPath '<server>/game/csgo/addons/counterstrikesharp/api/CounterStrikeSharp.API.dll' -OutputDirectory '<fresh staging directory>'
```

Build requires a Git checkout containing the accepted baseline object for the recovery/config source audit, .NET 10 SDK and the installed CounterStrikeSharp API/logging assemblies. It builds Core and adapted ZRPVE, runs tests and stages eight DLL/PDB/deps artifacts plus hashes/revision/dirty-tree status. Abstractions is installed once at `shared/ZEPVE.Abstractions/ZEPVE.Abstractions.dll`; no private copies ship in either plugin directory. The installed API's PluginManager shared-library resolver and PreferSharedTypes behavior were inspected to confirm this layout.

Stop this installation's CS2 processes. From a **clean committed** build, run:

```powershell
./scripts/install-core-lifecycle.ps1 -ServerRoot '<CS2 installation>' -PackageDirectory '<staging directory>'
```

The installer rejects running CS2, dirty/mismatched manifests and API/hash mismatches, backs up the exact previous artifact set, installs Core + adapted ZRPVE + shared ABI together, removes recorded private contract copies, and verifies hashes. No configs, mutable data, ZEAssist or WeaponBalance are replaced. Do not separately copy only the new Core or only adapted ZRPVE. Keep old monolithic ZRPVE copies out of active plugin paths; source/runtime modes must not be mixed.

Rollback while stopped: run the backup's `restore.ps1 -VerifyOnly`, then `restore.ps1`. It restores all previous files and removes only new recorded artifacts, checking paths/hashes and refusing later-changed files. The accepted `ZEPVE_Backup/pre-core-2026-10-09` stays untouched. Installer/restore were exercised only against disposable filesystem fixtures; actual live upgrade/rollback is NOT TESTED.

## Verification and runtime acceptance

Release Core, Abstractions and adapted ZRPVE builds: **PASS**, zero warnings/errors, SDK 10.0.401 / CounterStrikeSharp API 1.0.376. Tests: **72/72 PASS** (30 identity +42 authority/scheduler/recorder/migration checks). Installer/restore syntax and offline filesystem roundtrip: **PASS**. Tests cover token invalidation, same-slot respawn, raced callbacks, consumer unload, job/recorder bounds, exact static profile/command/timing parity, reentrant round end, three hot-resume stages, absent/duplicate providers and source disable/recovery/config parity.

**v0.2c runtime: NOT TESTED.** No new artifacts were deployed to the live server, and no live writer ownership handoff is claimed. The currently installed accepted observer/legacy artifacts remain unchanged. Model/file checks do not prove loader/ABI identity, native team API order, real infection/respawn timing, recovery usability or performance. All new runtime rows below remain NOT TESTED; PR #9's deferred observer acceptance remains deferred.

| Runtime acceptance | Status | Expected evidence |
| --- | --- | --- |
| Matched load / one shared ABI / one writer | NOT TESTED | One LOADED Core and adapted ZRPVE; status GameplayAuthority=True; no old quota/team writer; provider missing/duplicate fails closed. |
| Static solo/duo/coop/group profiles | NOT TESTED | Configured quotas, preparation at +1, unchanged countdown/release offsets and no duplicate kicks/cvar writes. |
| Round restart/end / map change | NOT TESTED | Old work cancels, no delayed quota/team writes after boundary; recorder epochs advance and map services rebind. |
| Connection/pawn/death/respawn | NOT TESTED | Correct generation increments, stale probe/recovery rejected; legitimate post-spawn recovery still works. |
| External respawn / recovery regression | NOT TESTED | ZR performs one respawn at existing policy delay; normal recovery/Trail/HUD behavior preserved. |
| Hot reload in preparing / waiting / released phase | NOT TESTED | Fresh GUID, no old callback, no repeated preparation/release, only pending quota/team deadlines resume, same-map Trail data retained. |
| Manual late load / dependency loss | NOT TESTED | Immediate player bootstrap, Unbound until next round without premature release; no legacy fallback on Core/bridge loss. |
| Human disconnect / hibernation | NOT TESTED | Core status removes human from view and keeps disconnected tombstone; no stale action on wake/reuse; timing under hibernation recorded separately. |
| FlightRecorder / overhead / actual rollback | NOT TESTED | Ring <=256, pending <=256, no duplicate logs/services, acceptable server frame time; stopped-server rollback restores previous writer set. |

Use `css_plugins list` for current plugin IDs; the new ModuleName is `ZEPVE Core` (observer was `ZEPVE Core Observer`). Use the current plain numeric LOADED ID for reload/unload, never an earlier session's ID or `#` prefix. Keep another human connected for timed disconnect tests so hibernation does not obscure results. Record status/recorder before and after each boundary, and retain every probe's queued/result messages.

## Remaining debt and completion boundary

Legacy still has shared per-map Trail history, per-slot watch/pending/display collections, dormant escort code and mixed recovery/HUD/config concerns. ZEAssist/WeaponBalance's independent delayed work and known source-level risks are not retrofitted here. Existing post-recovery wandering is not fixed. Core respawn permission remains governed by the compatible external executor/map rules rather than a new canonical Map system. The temporary bridge and config parser can be narrowed in later ownership-specific PRs.

The v0.2c **source implementation** is complete and build/model verified; production deployment/runtime handoff acceptance remains pending. Do not declare the live handoff complete, silently merge deferred results into PASS, or automatically proceed to BotAI/Navigation. The next work is matched runtime acceptance and fixing any observed regressions; only afterward consider the next architecture phase.
