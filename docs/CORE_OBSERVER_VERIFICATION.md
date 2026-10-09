# v0.2b verification

Updated: 2026-10-09 (Asia/Shanghai, UTC+08:00). Source base: `89b5c0c0722d630ad5b9ce75b1234414d7f873d3`. Tested Core source: `766317fd8bef7715205c083584f8bc941e864d30`. Branch: `agent/core-registry-lifecycle`. PR #9 remains **Draft**; ready to merge: **NO**.

Legacy remains the only gameplay writer; no authority handoff occurs in this PR. `MIGRATION_AUTHORITY.md`, Core/Abstractions source, build scripts, and all legacy source/config files are unchanged by this evidence update. No v0.2c work or component gitlink changes. Source inspection found no mismatch with the current-writer matrix; basic legacy coexistence now has user-reported runtime evidence. This does not establish full gameplay parity or validate any authority handoff. Existing ZombieReborn/CS2Fixes respawn execution is preserved.

## Local evidence

- Release Core + Abstractions build: **PASS**, zero warnings/errors.
- SDK: 10.0.401; target: net10.0.
- Installed API: CounterStrikeSharp 1.0.376, SHA256 `E81F05D960C37EE623D552733FC919E206C8C1D16157886342D06A2F4D0DAB17`.
- Deterministic registry/lifetime model tests: **PASS, 30/30** via `dotnet run --project tests/ZEPVE.Core.Tests -c Release -p:CounterStrikeSharpApiPath=<server API path>` (no native engine calls).
- Staging: `scripts/build-core.ps1` creates a Core-only install tree and exact artifact hashes. Core was subsequently installed beside legacy. Installed `ZEPVE.Core.dll` SHA256 was rechecked: `D224BA9B019751BE1FBCB9956EAC1E279AF0E8B1D890A42E0BC52788F31F7AD3`, matching the tested `766317f` observer build. The installation preserved legacy DLL/config/data hashes.

Tests exercise source re-reading at callback execution; disconnect with a still-visible controller; slot reuse even with identical handles; replacement without disconnect; missing source observations; controller serial/UserId changes; invalid entities; pawn replacement; invalid pawn; immediate/observed death; same-handle spawn/respawn; obsolete life events; round end/prestart/restart/start; map end/start; unload/reload; late-load model bootstrap; single immutable role views; current role resolution; invalid/default tokens and slots; stable observations; 100 successive slot reuses with bounded storage.

## User-reported real-server evidence

These results come from the **user's actual CS2 server deployment and test sessions**, not model/unit tests and not agent-executed server tests. The agent also inspected local CounterStrikeSharp/Core logs to corroborate load, epochs/round observation, and the two ACCEPT results. No remaining acceptance commands were executed by the agent. Test-session dates are 2026-10-09; exact command-entry times were not supplied by the user.

| Real-server check | Result | Evidence / scope |
| --- | --- | --- |
| Core plugin load / dependency resolution | PASS | User test; loader logged `Finished loading plugin ZEPVE Core Observer` at 20:57:29.492 and 21:04:38.704 +08:00. |
| `css_zepve_status` | PASS | User executed command and supplied epoch/category/disconnect observations. |
| Round observation | PASS | User test; Core logged round starts before and after map change. This does not validate stale-round callback rejection. |
| Map change / reinitialization | PASS | User changed `de_dust2` to `de_mirage`; MapEpoch advanced 1 to 3; round lifecycle continued. |
| ZombieBot classification | PASS | User saw `ZombieBots=10` and `Slot=1..10 Role=ZombieBot`. |
| Bot disconnect observation | PASS | After legacy kicked Bots, user saw affected slots `Connected=False Role=Unknown`. |
| Legacy coexistence | PASS | User reported basic real-server coexistence with legacy enabled; not exhaustive infection/quota/recovery/HUD/weapon parity. |
| Probe unchanged identity ACCEPT | PASS | User executed the two probes below; local Core log records both ACCEPT results. |
| Core exceptions during test sessions | None observed | User report; inspected Core log contains no ERROR/FATAL/exception entries. Limited to those test sessions. |
| Human classification | NOT TESTED | Slot 0 probe acceptance alone does not prove `Role=Human`. |
| Probe stale identity REJECT | NOT TESTED | No real-server REJECT evidence supplied yet. |
| Death/respawn pawn invalidation | NOT TESTED | No same-round death/respawn generation and rejection sequence supplied yet. |
| Hot/late reload bootstrap | NOT TESTED | Startup load with `hotReload=false` is not a mid-map late-load/hot-reload test. |
| Player disconnect registry cleanup after disconnect | PARTIAL | Bot disconnected rows observed; role-view removal, persistence through reconciliation, and human disconnect not fully validated. |
| Slot reuse | NOT TESTED | Model PASS only; no real-server reconnect/slot-reuse sequence supplied. |
| Native timer / subscription cleanup | NOT TESTED | Requires actual unload/reload observation; source review/model results are separate. |

User's unchanged-identity probes:

```text
css_zepve_probe 0 5
→ Result=ACCEPT Reason=current identity

css_zepve_probe 0 10
→ Result=ACCEPT Reason=current identity
```

Matching local Core log entries:

```text
2026-10-09 21:05:23.518 +08:00 Core delayed observation probe: Slot=0 Result=ACCEPT Reason=current identity
2026-10-09 21:05:59.332 +08:00 Core delayed observation probe: Slot=0 Result=ACCEPT Reason=current identity
```

User's first map/classification/disconnect observations:

```text
de_dust2: MapEpoch=1
changelevel de_mirage
de_mirage: MapEpoch=3
ZombieBots=10
Slot=1..10 Role=ZombieBot
After legacy Bot kick: Connected=False Role=Unknown
```

Local Core log corroboration: map initialization at 20:57:32.564 had `MapEpoch=1 RoundEpoch=1`; initialization at 20:59:54.117 had `MapEpoch=3 RoundEpoch=10`; later round-start observations included `MapEpoch=3 RoundEpoch=13/15/18/21`. The advance 1 → 3 is expected: map end and map start each advance MapEpoch. Status/category snippets above are user-supplied console observations, not reconstructed from unit tests.

## Confirmed CounterStrikeSharp plugin-management commands

Commands were confirmed by read-only IL inspection of the **actually installed** `CounterStrikeSharp.API.dll` (1.0.376, full informational revision `653d651f1ac09ac1ddb423d588f871b891038860`, API SHA256 above), specifically `Application.RegisterPluginCommands`, `Application.OnCSSPluginCommand`, and `PluginContextQueryHandler.FindPluginByIdOrName`. This is a command-implementation check, not an executed reload test. Current `core.json` has `PluginHotReloadEnabled=true` and `PluginAutoLoadEnabled=true`; neither was changed.

Execute in the **server console**:

```text
css_plugins list
css_plugins reload "ZEPVE Core Observer"
css_plugins unload "ZEPVE Core Observer"
css_plugins load ZEPVE.Core
```

Execute only the commands called for by the test below. Reload/unload resolve the exact ModuleName (`ZEPVE Core Observer`) or a plugin ID expressed as plain decimal digits from `css_plugins list`. The implementation compares `PluginId.ToString()` directly: do **not** include `#`, despite the generic help example. Reload does not resolve the directory name `ZEPVE.Core`; load does resolve that directory to `plugins/ZEPVE.Core/ZEPVE.Core.dll`. Explicit reload calls unload/load with `hotReload=true`; explicit unload/load use `false`. No reload of legacy or of all plugins is required.

## Remaining minimum runtime acceptance steps

Below are exactly the five requested remaining tests. Keep legacy and the existing respawn executor enabled. Do not change quota, infection/respawn cvars or Core behavior. Run one probe at a time and retain its queued message, result and the surrounding status output. `H` and `Z` are placeholders: replace them with an actual integer slot from `css_zepve_status`; do not type the letters. A `Probe unavailable` message means the probe was not queued and the scenario has not been tested.

### 1. Human classification

- **When / action:** Connect a real human, join CT, and wait until spawned; keep the same map/round. With a single CT human, its `Role=Human` row identifies `H` unambiguously. Human T/spectator and CT Bots are intentionally `Other`.
- **Server command:** `css_zepve_status`.
- **PASS output:** That human's row shows `Role=Human Connected=True Alive=True`; `Humans` equals the actual connected non-Bot, non-HLTV CT count. Repeat while still connected to confirm stable classification.
- **FAIL:** A valid connected CT human stays `Other`/`Unknown`, is absent, or is double-counted. No human present/on CT is not a test failure; it is still NOT TESTED.

### 2. Stale probe after round transition → REJECT

- **When:** During an active round, choose live connected human slot `H` so the legacy Bot kick cannot substitute for the round invalidation test. Record the status, queue the probe, then immediately restart the round within its 10-second window. Do not change map or reload Core.
- **Server commands, in order:**

  ```text
  css_zepve_status
  css_zepve_probe H 10
  mp_restartgame 1
  css_zepve_status
  ```

- **PASS output:** RoundEpoch advances and Core observes the new round; after the original 10 seconds, log shows `Slot=H Result=REJECT Reason=round epoch`. Plugin/map lifetime remains the same.
- **FAIL:** The old probe logs ACCEPT after the observed round transition, RoundEpoch does not advance, or an exception occurs. No queued message/no round transition/no result is inconclusive; do not mark PASS.

### 3. Stale probe after death/respawn → REJECT

- **When / action:** After normal infection/release, choose a live T Bot `Z`. Record its connection/pawn generations and queue the probe. Within 30 seconds, kill only that chosen Bot through normal combat and let the existing ZombieReborn executor respawn it. Keep other zombies alive so the round continues. Do not kick the Bot or restart/change map/reload Core. No unverified `bot_kill` or new respawn command is needed.
- **Server commands:** `css_zepve_status`, then `css_zepve_probe Z 30`; run `css_zepve_status` immediately after the kill and again after its normal respawn.
- **PASS output:** Same MapEpoch/RoundEpoch and ConnectionGeneration; death status `Alive=False`; respawn `Alive=True` with PawnGeneration greater than the baseline; original probe logs `Slot=Z Result=REJECT Reason=pawn generation` at its deadline.
- **FAIL:** Same-generation respawn or old probe ACCEPT; exception during observation. `Reason=round epoch` or `connection generation` demonstrates another boundary, not pawn invalidation. If the Bot is still dead at the deadline, rejection can prove death invalidation but respawn remains unverified until the revived status is recorded. If no respawn is permitted by the existing round policy, retain NOT TESTED rather than changing that policy.

### 4. Hot reload / late load bootstrap

- **When:** Mid-map/mid-round with an existing human and Bots, avoiding simultaneous round transitions. Baseline: `css_plugins list`, `css_zepve_status`; record current slots, counts and load log PluginLifetime. This test should not need the next map/round event.
- **Hot reload commands:** Queue `css_zepve_probe H 10`, then immediately run `css_plugins reload "ZEPVE Core Observer"`. Run `css_plugins list` and `css_zepve_status`; wait past the old probe deadline before moving on.
- **Hot reload PASS:** Core logs `unloaded (hotReload=true)` and `loaded (hotReload=true, PluginLifetime=<new GUID>)`; the plugin list has one LOADED Core observer; status immediately discovers current players/categories. Old pending probe is canceled and produces no delayed result after its deadline; no duplicate observer/round logs or exceptions. MapEpoch normally restarts at 1 for the new plugin lifetime; reset counters across different GUIDs are expected.
- **Late load commands, while staying on the same map:**

  ```text
  css_plugins unload "ZEPVE Core Observer"
  css_plugins list
  css_zepve_status
  css_plugins load ZEPVE.Core
  css_plugins list
  css_zepve_status
  ```

- **Late load PASS:** While unloaded, Core is not LOADED and its status command is unavailable (a retained UNLOADED list entry is acceptable). Load logs `hotReload=false` with a fresh GUID and immediate map initialization; status immediately indexes existing players, with one LOADED Core, before any map/round restart. A fresh `css_zepve_probe H 5` after each bootstrap should log ACCEPT if identity stays unchanged.
- **FAIL:** Reload/load cannot find Core, load/unload exception, command remains active after unload, bootstrap waits for a later map/round event, duplicate loaded observer, or old pending probe fires after reload. If players genuinely change during the test, compare against the current engine population rather than demanding unchanged counts.

### 5. Registry cleanup after disconnect

- **When / action:** Use a disconnecting human client while the server stays running with an independent server console/RCON or a second client. Record `H`, role-view count and generations. Queue the probe, then that human executes `disconnect` in their **client console** within the 10-second window. Do not end the map/reload Core. Disconnecting the sole listen-server host can close the server and does not verify this scenario; keep PARTIAL in that case.
- **Server commands:** `css_zepve_status`, `css_zepve_probe H 10`; after the client disconnects, run `css_zepve_status` after at least 1 second and repeat after another 2 seconds.
- **PASS output:** Slot `H` remains only as the expected tombstone `Connected=False Alive=False Role=Unknown`, its generations have advanced, and `Humans` decreases by one; subsequent reconciliation does not resurrect it. Original probe logs REJECT (normally `Reason=disconnected/invalid entity`). For Bot disconnect, the analogous view count is ZombieBots. Physical removal of the tombstone row is **not** required by the implementation; rows are bounded by the 64 slots and cleared on map end/unload.
- **FAIL:** Disconnected player remains connected/alive/in the role-view count, reappears without a new connection, stale probe ACCEPT, or exception. The earlier Bot kick evidence already establishes disconnect observation; this step completes the cleanup/count/persistence evidence.

## Limitations and rollback

Basic loading, round/map/category observation, coexistence and unchanged-identity probe execution now have real-server evidence. Native pawn invalidation, stale rejection, hot/late-load bootstrap and complete disconnect cleanup still need the steps above. Reconciliation latency is at most one normal 0.5-second interval for continuously observable changes; callbacks refresh synchronously. Entire unobserved same-handle life cycles cannot be inferred. Hot/late-load round phase is deliberately unknown and initial epochs provide observation validity only. Registry slots are limited to 0–63 and state is server-thread-only. Probe map/unload cases cancel timers rather than emitting a later REJECT log.

Legacy callbacks are not retrofitted; their existing slot/lifetime risks and recovery wandering remain. The historical source-vs-deployed uncertainty in the import audit is superseded for the local accepted pre-Core baseline by the user's acceptance and build/deployment records, not by this Core change. No new Core code defect was established by this review or the supplied test session. No source fix was made. The supplied evidence does not justify an authority handoff or completion of the remaining acceptance tests.

Rollback is unloading/removing just Core's plugin folder. Original legacy DLLs/configs and `ZEPVE_Backup/pre-core-2026-10-09` remain untouched.

Keep PR #9 **Draft** until the five remaining tests have explicit results and any failures are resolved. Only then reassess Ready for review; this is not automatic merge approval.
