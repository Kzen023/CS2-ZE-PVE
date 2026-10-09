# v0.2b verification

Updated: 2026-10-09 (Asia/Shanghai, UTC+08:00). Source base: `89b5c0c0722d630ad5b9ce75b1234414d7f873d3`. Tested Core source: `766317fd8bef7715205c083584f8bc941e864d30`. Branch: `agent/core-registry-lifecycle`. PR #9 closeout: the owner explicitly accepts the remaining runtime risks and authorizes **Ready for review and merge**. Remaining NOT TESTED/PARTIAL results are **deferred acceptance**, not newly passed tests.

## Owner-accepted deferred acceptance

The owner has explicitly authorized merging v0.2b before all runtime acceptance is complete. Same-round death/respawn pawn invalidation and actual hot reload remain NOT TESTED; complete human disconnect Registry cleanup and native pending-timer/subscription cleanup remain PARTIAL. Pawn/connection-specific stale rejection, hibernating-server behavior, and complete gameplay parity are not certified. These risks carry forward into later validation, especially before Bot quota/team authority handoff. This acceptance does not change Core behavior, legacy writers or `MIGRATION_AUTHORITY.md` and does not relabel any result PASS.

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
| Human classification | PASS | Subsequent user server transcript shows `Humans=1`, `Slot=0 Role=Human Connected=True Alive=True`; repeated status before/after late load confirms classification. |
| Probe stale identity REJECT (round boundary) | PASS | Latest user transcript: slot 1 probe queued, a round transition occurred, then `21:36:08 Result=REJECT Reason=round epoch`. Pawn/connection-specific REJECT is not inferred. |
| Death/respawn pawn invalidation | NOT TESTED | No same-round death/respawn generation and rejection sequence supplied yet. |
| Hot reload bootstrap | NOT TESTED | Latest session has Core #4 LOADED; `css_plugins reload 5` returned `Could not reload plugin "5"`. No hotReload=True execution occurred. |
| Late load bootstrap | PASS | Explicit mid-map unload at 21:28:01, load at 21:28:08, fresh GUID and immediate initialization; repeated status has Humans=1/ZombieBots=10, RoundEpoch=1 before the next round at 21:28:34. |
| Player disconnect registry cleanup after disconnect | PARTIAL | Bot cleanup PASS; latest transcript confirms human slot 0 disconnect and a still-running hibernating dedicated server, but no post-disconnect Core status snapshot or completed disconnect probe result. |
| Bot registry cleanup | PASS | Transcript shows ZombieBots=0 with all Bot rows disconnected/Unknown; older disconnected rows remain excluded across successive status/reconciliation. |
| Slot reuse observation (Bots) | PASS | Slots 1–5 reused with increased ConnectionGeneration after legacy kicks; stale connection callback rejection is still NOT TESTED. |
| Native timer / subscription cleanup | PARTIAL | Explicit unload completed; no Core status output while unloaded and only one LOADED observer after load. Pending-probe cancellation/hot-reload duplication checks remain NOT TESTED. |

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

### Subsequent user transcript audit (21:24–21:28 +08:00)

Source: user's pasted server transcript supplied after the initial evidence update (attachment SHA256 `3D80D3B225575F917F2A68906669EB55EC0DA1122526E990BE32D344B9246EAA`). It starts a dedicated server on de_dust2 with maxplayers=16. Evidence line numbers below refer to that attachment, not this Markdown file. No raw player names, Steam identifiers or full server log are imported into the repository.

- **Human PASS:** lines 715–717 show MapEpoch=1/RoundEpoch=4/Humans=1 and slot 0 Human/connected/alive, ConnectionGeneration=1/PawnGeneration=4. Lines 917–919 repeat that human state.
- **Bot cleanup PASS / overall player cleanup PARTIAL:** lines 1166–1183 show old slots 6–10 disconnected/Unknown while ten currently connected ZombieBots use other/reused slots. Lines 1316–1333 show ZombieBots=0, all Bot rows disconnected/Unknown, including those same older tombstones; Human remains connected. Thus role-count exclusion and persistence through status reconciliation are verified for Bots, not a human disconnect.
- **Bot slot reuse observation PASS:** slot 1 changes from ConnectionGeneration=2 (line 920) to 22 (line 1169), and other reused slots similarly advance. This does not prove stale-probe rejection on slot reuse.
- **Round stale-probe NOT TESTED:** `css_zepve_probe H 10` returned Usage (lines 1436–1437); the later `mp_restartgame 1`/new RoundEpoch=12 (lines 1466/1490) had no valid pending probe. The two actual Slot=0 probes had already ACCEPTed at 21:26:20 and 21:26:23 (lines 1227–1241). Round restart observation is PASS; rejection is not.
- **Pawn stale-probe NOT TESTED:** `css_zepve_probe Z 30` returned Usage (lines 1531–1532). Legacy respawn/recovery lines alone do not supply same-round Core pawn-generation/rejection evidence.
- **Late load PASS:** lines 1762–1775 show unload `hotReload=False`, no Core response to status while unloaded, then explicit load with immediate map initialization and new PluginLifetime `cc3eec52-b9ab-44cf-9290-0133f4e4755e` (initial lifetime was `3b83033d-b6dc-4e70-9e35-dca6401e5485`). Lines 1865–1913 show one UNLOADED context (#4), one LOADED context (#5), and two immediate status snapshots at MapEpoch=1/RoundEpoch=1/Humans=1/ZombieBots=10 before the next round at 21:28:34 (line 1950).
- **Hot reload NOT TESTED:** no `css_plugins reload` or `hotReload=True` sequence is present. **Human disconnect NOT TESTED:** no slot 0 disconnected snapshot. **Stale rejection NOT TESTED:** no `Result=REJECT` anywhere in the attachment. Pending-probe cancellation was not demonstrated around unload/load.
- No Core exception is present in the supplied transcript. Engine/other-plugin warnings, malformed command input and frame spikes are not attributed to Core without further evidence; Core overhead has not been profiled.

Literal `H`/`Z` input and concatenated commands produced Usage/Unknown command, so those attempts never queued a probe. For the next test, copy **one command at a time and press Enter**; use the actual numeric slot and require the `Observation probe queued` response before triggering invalidation. There is no confirmed new Core defect in this transcript.

### Latest user transcript audit (21:34–21:36 +08:00)

Source: second supplied server attachment, SHA256 `30D503BDABA4724534F4B7D0FBC4EF9243E36934FF055BC9816CF44B2A02DEC6`. Line numbers refer to this attachment. The dedicated server was restarted; its initial Core PluginLifetime is `8d8a6d14-8148-4d64-8b76-bf8eb3259dc6`. Final engine `status` identifies CS2 `1.41.9.0/14190 10924`, Windows dedicated on de_dust2; no server addresses/player identifiers are copied here.

- **Round stale REJECT PASS:** lines 1003–1004 queue `css_zepve_probe 1 30`; line 1085 records round start MapEpoch=1/RoundEpoch=9 at 21:35:59; line 1125 records `21:36:08 Core delayed observation probe: Slot=1 Result=REJECT Reason=round epoch`. This is a real pending callback rejected across a round transition even though it was originally queued for the Bot pawn test.
- **Human-specific restart probe did not test rejection:** slot 0's first 10-second probe queued at lines 694–695 and ACCEPTed at 21:35:08 (line 827), before `mp_restartgame 1` (line 828) and the new round at 21:35:14. There is no defect in accepting unchanged identity before the transition. The Bot probe above independently supplies the round-boundary PASS.
- **Pawn invalidation remains NOT TESTED:** the Bot probe rejected for round epoch, which has validation priority over pawn generation. No same-round Core death/respawn status/generation sequence or `Reason=pawn generation` is supplied.
- **Hot reload remains NOT TESTED:** `css_plugins list` at lines 1127–1136 shows Core #4 LOADED, not #5. `css_plugins reload 5` at lines 1230–1231 returns `Could not reload plugin "5"`. The target did not exist after this server restart; no Core hot reload ran and no Core load/unload failure is established.
- **Human cleanup remains PARTIAL:** lines 1295–1296 queue a slot 0 probe; lines 1297–1307 record the human's user-requested disconnect; line 1308 enters server hibernation. Final engine status shows the dedicated server still running with zero humans/Bots, but the attachment contains no `css_zepve_status` after disconnect and no pending-probe result. Engine population is not evidence of Registry count/tombstone cleanup. Hibernation can pause timer progress; do not treat the missing result as a proven Core defect. Use an active second client for a timed disconnect test and record Core status independently.
- No Core exception is present in this attachment. The native PreloadLibrary access-violation warnings and frame spikes are separate observed server diagnostics; no causal link to Core is established by this transcript.

Remaining acceptance is now **three scenarios**: same-round pawn invalidation, actual hot reload, and human disconnect Registry cleanup. No source/build/config changes were made for this evidence update. These remain deferred acceptance under the subsequent explicit owner risk acceptance above.

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

**After the observed unload/load, use the current LOADED numeric ID for reload/unload.** The user's list has #4 UNLOADED and #5 LOADED with the same ModuleName; the installed name resolver selects the first matching context, without filtering LOADED state. Thus name-only selection is ambiguous now. Run `css_plugins list` again; if #5 remains the LOADED Core, use `css_plugins reload 5` (or `css_plugins unload 5` when required). If the ID has changed after server restart/load, use the new LOADED row's plain numeric ID. This is command-selection guidance, not a Core source change or evidence of two active writers.

**Latest session supersedes the earlier ID:** after the server restart, the new transcript shows #4 LOADED and no #5. Run `css_plugins list`; if that remains the current state, the exact hot reload command is `css_plugins reload 4`. IDs are runtime allocations, not permanent installation identifiers. For unload in that same state use `css_plugins unload 4`; explicit load still uses `css_plugins load ZEPVE.Core`.

## Remaining minimum runtime acceptance steps

Below are the original five requested tests, with completed portions marked above. Human classification, round-boundary REJECT and late load need not be repeated; remaining work is pawn REJECT, hot reload and human disconnect cleanup (deferred acceptance). Keep legacy and the existing respawn executor enabled. Do not change quota, infection/respawn cvars or Core behavior. Run one command/probe at a time and retain its queued message, result and the surrounding status output. `H` and `Z` below are notation only, not literal commands: this transcript identifies H=0; choose Z from the current live ZombieBot rows (slot 1 is an example only when currently connected/alive/ZombieBot). Usage/Probe unavailable means no test was queued.

### 1. Human classification

- **When / action:** Connect a real human, join CT, and wait until spawned; keep the same map/round. With a single CT human, its `Role=Human` row identifies `H` unambiguously. Human T/spectator and CT Bots are intentionally `Other`.
- **Server command:** `css_zepve_status`.
- **PASS output:** That human's row shows `Role=Human Connected=True Alive=True`; `Humans` equals the actual connected non-Bot, non-HLTV CT count. Repeat while still connected to confirm stable classification.
- **FAIL:** A valid connected CT human stays `Other`/`Unknown`, is absent, or is double-counted. No human present/on CT is not a test failure; it is still NOT TESTED.

### 2. Stale probe after round transition → REJECT (PASS; no repeat required)

- **When:** During an active round, choose live connected human slot `H` so the legacy Bot kick cannot substitute for the round invalidation test. Record the status, queue the probe, then immediately restart the round within its 10-second window. Do not change map or reload Core.
- **Server commands, in order:**

  ```text
  css_zepve_status
  css_zepve_probe 0 10
  mp_restartgame 1
  css_zepve_status
  ```

- **PASS output:** RoundEpoch advances and Core observes the new round; after the original 10 seconds, log shows `Slot=H Result=REJECT Reason=round epoch`. Plugin/map lifetime remains the same.
- **FAIL:** The old probe logs ACCEPT after the observed round transition, RoundEpoch does not advance, or an exception occurs. No queued message/no round transition/no result is inconclusive; do not mark PASS.

### 3. Stale probe after death/respawn → REJECT

- **When / action:** After normal infection/release, choose a live T Bot `Z`. Record its connection/pawn generations and queue the probe. Within 30 seconds, kill only that chosen Bot through normal combat and let the existing ZombieReborn executor respawn it. Keep other zombies alive so the round continues. Do not kick the Bot or restart/change map/reload Core. No unverified `bot_kill` or new respawn command is needed.
- **Server commands:** `css_zepve_status`; if slot 1 is currently a live ZombieBot, execute `css_zepve_probe 1 30` (otherwise substitute the actual live Bot slot). Run `css_zepve_status` immediately after the kill and again after its normal respawn.
- **PASS output:** Same MapEpoch/RoundEpoch and ConnectionGeneration; death status `Alive=False`; respawn `Alive=True` with PawnGeneration greater than the baseline; original probe logs `Slot=Z Result=REJECT Reason=pawn generation` at its deadline.
- **FAIL:** Same-generation respawn or old probe ACCEPT; exception during observation. `Reason=round epoch` or `connection generation` demonstrates another boundary, not pawn invalidation. If the Bot is still dead at the deadline, rejection can prove death invalidation but respawn remains unverified until the revived status is recorded. If no respawn is permitted by the existing round policy, retain NOT TESTED rather than changing that policy.

### 4. Hot reload / late load bootstrap

- **When:** Mid-map/mid-round with an existing human and Bots, avoiding simultaneous round transitions. Baseline: `css_plugins list`, `css_zepve_status`; record current slots, counts and load log PluginLifetime. This test should not need the next map/round event.
- **Hot reload commands:** Run `css_plugins list`; if #4 is the current LOADED Core as in the latest transcript, queue `css_zepve_probe 0 10`, then immediately run `css_plugins reload 4`. Use the actual LOADED numeric ID if different. Run `css_plugins list` and `css_zepve_status`; wait past the old probe deadline before moving on. Do not use a previous session's ID or a same-name UNLOADED context.
- **Hot reload PASS:** Core logs `unloaded (hotReload=true)` and `loaded (hotReload=true, PluginLifetime=<new GUID>)`; the plugin list has one LOADED Core observer; status immediately discovers current players/categories. Old pending probe is canceled and produces no delayed result after its deadline; no duplicate observer/round logs or exceptions. MapEpoch normally restarts at 1 for the new plugin lifetime; reset counters across different GUIDs are expected.
- **Late load commands (already PASS; no repeat required), while staying on the same map:**

  ```text
  css_plugins unload 4
  css_plugins list
  css_zepve_status
  css_plugins load ZEPVE.Core
  css_plugins list
  css_zepve_status
  ```

- **Late load PASS:** While unloaded, Core is not LOADED and its status command is unavailable (a retained UNLOADED list entry is acceptable). Load logs `hotReload=false` with a fresh GUID and immediate map initialization; status immediately indexes existing players, with one LOADED Core, before any map/round restart. A fresh `css_zepve_probe H 5` after each bootstrap should log ACCEPT if identity stays unchanged.
- **FAIL:** Reload/load cannot find Core, load/unload exception, command remains active after unload, bootstrap waits for a later map/round event, duplicate loaded observer, or old pending probe fires after reload. If players genuinely change during the test, compare against the current engine population rather than demanding unchanged counts.

### 5. Registry cleanup after disconnect

- **When / action:** Use a disconnecting human client while the server stays running with an independent server console/RCON. Keep a second human client connected/alive so the server remains active and the round continues. Record `H`, role-view count and generations. Queue the probe, then that human executes `disconnect` in their **client console** within the 10-second window. Do not end the map/reload Core. The latest dedicated-server test remains running after the last human disconnects but enters hibernation; an engine `status` and no timer result in that state do not complete Registry/timed-callback acceptance.
- **Server commands:** For the observed human slot 0: `css_zepve_status`, then `css_zepve_probe 0 10`; after that client's disconnect, run `css_zepve_status` after at least 1 second and repeat after another 2 seconds. Use the actual human slot if it changes.
- **PASS output:** Slot `H` remains only as the expected tombstone `Connected=False Alive=False Role=Unknown`, its generations have advanced, and `Humans` decreases by one; subsequent reconciliation does not resurrect it. Original probe logs REJECT (normally `Reason=disconnected/invalid entity`). For Bot disconnect, the analogous view count is ZombieBots. Physical removal of the tombstone row is **not** required by the implementation; rows are bounded by the 64 slots and cleared on map end/unload.
- **FAIL:** Disconnected player remains connected/alive/in the role-view count, reappears without a new connection, stale probe ACCEPT, or exception. The earlier Bot kick evidence already establishes disconnect observation; this step completes the cleanup/count/persistence evidence.

## Limitations and rollback

Basic loading, round/map/category observation (including Human), coexistence, unchanged-identity probes, real round-boundary REJECT, Bot cleanup/slot reuse observation, and late-load bootstrap now have real-server evidence. Native pawn invalidation, hot-reload bootstrap/pending-timer cancellation and complete human disconnect cleanup still need the steps above. Reconciliation latency is at most one normal 0.5-second interval while the server is actively ticking for continuously observable changes; callbacks/status refresh synchronously. Hibernation is not certified by the active-server timing tests. Entire unobserved same-handle life cycles cannot be inferred. Hot/late-load round phase is deliberately unknown and initial epochs provide observation validity only. Registry slots are limited to 0–63 and state is server-thread-only. Probe map/unload cases cancel timers rather than emitting a later REJECT log.

Legacy callbacks are not retrofitted; their existing slot/lifetime risks and recovery wandering remain. The historical source-vs-deployed uncertainty in the import audit is superseded for the local accepted pre-Core baseline by the user's acceptance and build/deployment records, not by this Core change. No new Core code defect was established by this review or the supplied test session. No source fix was made. The supplied evidence does not justify an authority handoff or completion of the remaining acceptance tests.

Rollback is unloading/removing just Core's plugin folder. Original legacy DLLs/configs and `ZEPVE_Backup/pre-core-2026-10-09` remain untouched.

The owner explicitly authorizes PR #9 **Draft → Ready → merge** with the remaining runtime risks accepted as deferred acceptance. Merge does not certify the untested scenarios and transfers no gameplay authority. v0.2c1 must separately document its lifecycle-only authority boundary and validation.
