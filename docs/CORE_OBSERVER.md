# Core observer (v0.2b)

**English | [简体中文](CORE_OBSERVER.zh-CN.md)**

This opt-in plugin adds identity/lifecycle observations beside the accepted pre-Core runtime baseline (`CS2-ZE-PVE @ 89b5c0c0`, rollback `ZEPVE_Backup/pre-core-2026-10-09`). **Legacy remains the only gameplay writer; no authority handoff occurs in this PR.** ZombieReborn/CS2Fixes remains the existing respawn executor.

## Build and install

Requires .NET 10 SDK and the server's CounterStrikeSharp API directory, including its Logging.Abstractions assembly. No NuGet packages, native components, or component-pin changes are introduced.

```powershell
./scripts/build-core.ps1 -CounterStrikeSharpApiPath 'D:/CS2/game/csgo/addons/counterstrikesharp/api/CounterStrikeSharp.API.dll'
```

The script runs Release build and deterministic model tests, then stages an install layout and SHA256 manifest under `artifacts/core-observer`. It does not deploy to the running server. Copy the staged `game/csgo/addons/counterstrikesharp/plugins/ZEPVE.Core` directory into the server's corresponding plugins directory. Include `ZEPVE.Abstractions.dll` beside Core; the server supplies CounterStrikeSharp and logging assemblies. Load via your usual CounterStrikeSharp plugin procedure.

Keep all existing legacy plugins and configs active. Core has no reference to or replacement hook in the legacy plugins. It subscribes to engine events and reads controller/pawn state; a 0.5-second reconciliation catches pawn replacement and missed observable state changes. Nothing changes bot quota/team, infection, respawn scheduling, recovery, targets, awareness, movement, weapons, HUD or map gameplay. There is no inter-plugin provider/capability registration in this slice.

Rollback: unload **ZEPVE Core Observer** and remove only its `ZEPVE.Core` plugin directory. The accepted legacy deployment and pre-Core backup are unchanged.

## Commands

- `css_zepve_status`: reports `MapEpoch`, `RoundEpoch`, `Humans`, `ZombieBots`, and per-slot `Role`, `Connected`, `Alive`, `ConnectionGeneration`, `PawnGeneration` (including bounded disconnected tombstones). Available in server/player console. It does not display other modules' states.
- `css_zepve_probe <slot> [seconds]`: server console or `@css/root`; defaults to 5 seconds, range 0.1–60, at most 16 pending probes. Captures a live pawn token, then logs **ACCEPT/REJECT** after re-resolution. It performs no gameplay action. Disconnect, death, pawn replacement and round transitions reject stale probes. Map end/unload cancel timers; model tests separately verify tokens are invalid then too.

`Human` means non-Bot, non-HLTV CT; `ZombieBot` means non-HLTV T Bot, matching the legacy trail/recovery categories. Other teams, CT Bots and HLTV are `Other`. These are observed categories, not canonical ZombieReborn infection state. Dead players remain in their role view while connected.

## Identity and invalidation

`PlayerRegistry` is the only identity source. `Players`, `Humans` and `ZombieBots` are read-only views of the same immutable `ZepvePlayerContext` instances. The adapter supplies raw engine observations, never generations. Old context objects are snapshots and must not be used as delayed validity evidence.

`PlayerLifetime` carries plugin lifetime GUID, map epoch, round epoch, slot, connection generation and pawn generation. Registry counters never reset within a plugin lifetime. Full serial-bearing controller/pawn handles plus UserId detect replacements; SteamID and slot alone are insufficient. Connect/disconnect listeners invalidate connection lifetimes even when a native controller temporarily remains visible or its handle is reused. Spawn/death and observed alive/handle changes invalidate pawn lifetimes, including respawn on the same pawn. Pawn ownership must still match the current controller.

`TryCapture` requires a current connected, alive, bound pawn. `TryResolve` rejects obsolete plugin/map/round lifetimes, then re-reads the current controller/pawn before checking connection/pawn generations and returning the current context. Consumers must still check their required current role/state: a lifetime stamp is not gameplay permission or future target/route validity.

Round prestart, start and end each advance `RoundEpoch`; prestart/end close capture until start. Map end and start each advance map and round epochs and clear contexts. These counters are invalidation epochs, not map IDs or match round numbers. Late/hot load bootstraps the current map/players without waiting for the next map event, using a fresh plugin GUID. The initial observation epoch is open because the elapsed round phase is unknown; this supplies no canonical round permission. Empty-map load waits for map start.

Unload invalidates the registry first, kills timers, explicitly removes subscriptions and commands, and clears bounded native-identity state. No controllers/pawns are retained by delayed probes. Contexts/tokens contain values only. All entity access and registry operations run on the server thread.

## Verification and limits

Validation record: [CORE_OBSERVER_VERIFICATION.md](CORE_OBSERVER_VERIFICATION.md). Build/model results do not establish plugin loading, engine event order, suite integration or live gameplay parity. Server runtime scenarios remain **NOT TESTED**.

Reconciliation is limited to CS2 slots 0–63 and runs twice per second. Status/probe validation refreshes synchronously. An entire same-handle death/respawn cycle between samples requires the spawn/death events to be delivered; polling alone cannot reconstruct an unobserved transition. Generations are process-lifetime observation sequences, not persistent identities. Live adapter semantics, hot-load initialization and actual cleanup still require server testing before authority handoff. Legacy delayed callbacks remain unchanged and retain their documented lifecycle risks. The accepted baseline's post-recovery wandering issue is unchanged.
