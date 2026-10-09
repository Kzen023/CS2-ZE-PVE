# BotAI verification

Source baseline: merged v0.2c `ed480bd`. BotAI runtime evidence must be recorded independently of earlier Core smoke. Automated/model checks: 127/127 PASS (72 retained, 55 new), Release build zero warnings/errors against installed CSS 1.0.376.

## Actual runtime acceptance — 2026-10-10, Asia/Shanghai

Windows dedicated localhost server, dust2/mirage, one actual CT human; CSS 1.0.376 / native BotController v0.7.1 @ 0ae8f18 / CS2Fixes v2.0-0-gbb3be34. Actual RCON, CSS logs and native console observations, **not model tests**. Fixed source/build `238b98cdb40a01b422c16c3bab5a18030142fad0`; final documentation commits do not change that artifact. Matched 11/11 live hashes checked; 21 current config/unrelated-plugin files preserved.

### Defect found, whole rollback and fix

Initial package `3a73439` caused server startup exit: logs stopped at Loading ZEPVE.BotAI before Core, confirmed by the user. BotAI's startup recorder read native Server.CurrentTime before engine global-vars initialization. Installed managed IL calls GetCurrentTime; exact [CSS native implementation](https://github.com/roflmuffin/CounterStrikeSharp/blob/653d651f1ac09ac1ddb423d588f871b891038860/src/scripting/natives/natives_engine.cpp#L58) dereferences global vars without a null check. No native dump stackwalk was performed; the API/load-order evidence and controlled before/after startup establish the repair.

Actual whole-set rollback from `ZEPVE_Backup/v0.2d-20261009-160908-2437293` verified hashes, restored accepted v0.2c Core hash `3CEBD7236C7D17288A63F885635F7FF3864F62D875B4FD2B542F2B18E477F585`, removed added BotAI, and baseline started normally (00:13:42 RCON: four LOADED plugins). No old monolithic writer was enabled.

Fix uses only monotonic Stopwatch module time for diagnostics/evaluation/reacquire deadlines. Rebuild 127/127 checks PASS; whole 11-file fixture install/verify/restore PASS, then stopped matched reinstallation from `238b98c`. Current previous-v0.2c backup: `ZEPVE_Backup/v0.2d-20261009-161410-5946327`. Optional unmodded-client deployment/restore exception checks visible non-dedicated client modules, never bypasses a dedicated/modded/unknown process. An updated copied restore helper was used for the initial rollback while the inspected unmodded client remained open.

### Runtime matrix (fixed package)

| Scenario | Actual evidence | Result |
| --- | --- | --- |
| Cold load/shared ABI | 00:14:37: one LOADED BotAI #4, Core #5, bridge #3, prior two unrelated plugins. BotAI discovered before Core safely; Unbound bindings=0. | PASS |
| Native ownership boundary | `bc_status`: no replay; all/aim/weapon locks=0 and buyPlans=0. BotAI NativeWrites=0/MovementWrites=0, no native API dependency. `drop=failed` is an observed external hook limitation; its prior runtime parity was not tested, BotAI does not use drop. | PASS for disjoint BotAI writer surface; external suppression/replay cases NOT TESTED |
| Assignment / Enemy independence | 00:15:35–37: all slots 1–10 target real Human slot 0; bindings 11–20, complete connection/pawn identities; Enemy=invalid while assignments remain. Bot 1 version 11 still valid at 00:15:53 despite engine perception changes. | PASS |
| Multi-human distribution | Only one real human present. 1–6-human/18-bot distribution verified in models only. | NOT TESTED |
| Unchanged delayed binding | 00:15:40.043 `Bot=1 Binding=11 ACCEPT current binding`; after reconnect 00:22:41.293 `Binding=95 ACCEPT current binding`. | PASS |
| Round restart | Actual restart 00:15:53.882; probe REJECT round epoch 00:15:55.707; Preparing bindings/pending=0, then fresh Released assignments. | PASS |
| Bot death / external respawn | Controlled slot 1 kill at about 00:16:49.57; probe REJECT pawn generation. Same connection 42, pawn 235 → 246 dead → 252 alive at +7.25 s; binding 43 → 53. Core/BotAI pending=0 after rejection; ZR delay=7 unchanged. | PASS |
| Human death / next CT spawn | User executed kill: slot 0 pawn 67 → 137/138 dead, Closed bindings=0; next CT life pawn 142, new bindings after release. Probe REJECT round epoch 00:20:02.872. Sole human death ends the round, so independent target-pawn rejection is not proven. | PARTIAL (death/respawn observed; isolated target pawn invalidation NOT TESTED) |
| BotAI hot reload / module invalidation | 00:17:32: old binding 53 probe rejected explicit cancellation; module GUID 92833f23… → 6a128ab8…, fresh bindings. Original cold GUID 9812a2a7… also changed on earlier actual reload. | PASS |
| Core hot reload | 00:18:07.383 probe REJECT plugin lifetime; Core GUID changes, same BotAI module rebinds scopes and targets (binding 21 → 31), quota 10 preserved. | PASS |
| Core manual unload/late load | 00:21:05 Core unavailable bindings=0, native quota=10. Manual load indexes existing 1+10; Unbound bindings=0 until explicit new round. Old probe REJECT plugin lifetime. | PASS |
| Map transitions | dust2 → mirage at 00:18:51; new map/gate/bootstrap and Human connection. Current probe then mirage → dust2 at 00:21:36; REJECT round epoch first during map close, fresh Core MapEpoch 1 → 3 and assignments return. Do not label this isolated map-epoch rejection. | PASS for transition/invalidation smoke; isolated reason NOT TESTED |
| Human disconnect / reconnect | 00:22:07 kick: connection 33 → tombstone 44, Humans/Bots=0, bindings/pending=0; hibernation removes bots. Reconnect CT connection 55, ten fresh bindings; old probe REJECT round epoch first. | PASS for cleanup/reconnect; isolated target-only rejection while bots remain NOT TESTED |
| Reacquire/observation | Actual auto windows start and stop with both Succeeded (assigned Enemy + visible/aim/attack) and TimedOut. Explicit diagnostic request at 00:15:38 returned cooldown. Later explicit coalescing attempt found no observing bot. No causal native repair claim. | PASS for observation lifecycle/cooldown; explicit request coalescing NOT TESTED |
| BindingVersion-only rejection | Runtime reload/life/round cases also changed lifetime or module; no second human to trigger a pure distribution rebind. Models reject forged/old versions and dispatched canceled callbacks. | NOT TESTED |
| No obvious existing gameplay regression | Core solo quota=10, T zombie roles, ZR delay=7 and actual respawn; legacy console recovery placed for no_damage; native locks/replay untouched. No source changes in Core/legacy/Navigation. | PASS for single-human smoke, not full compatibility certification |
| Bounds / exceptions | Recorder actually reaches 256/256 with Dropped increasing; pending returns zero. No Core/BotAI/CSS exception or unsolicited reload/repeated writer symptom observed after the startup fix (00:14 onward). | PASS for smoke; endurance/performance NOT TESTED |
| Rollback | Initial broken whole package actually rolled back and baseline booted. Fixed package also has whole-set backup; fixture restore passes. | PASS for actual initial whole-set rollback; restoring the final package on this server NOT TESTED |

Engine T tally 20 vs actual ten bots recurred at disconnect; unchanged independent diagnostic debt, no speculative fix or AssignedTarget reliance. Native field-write efficacy, automatic Recovery→reacquire synchronization, movement following AssignedTarget, multiple real humans, full ZE maps, native schema failure injection and long-duration load remain NOT TESTED/unimplemented where stated.

Raw evidence stays local under `.codex/runtime-botai-v02d-20261010` (names/network identifiers excluded from Git). Final transcript SHA256 `10E7CA2DE8AEC3BB83B1CD60314E2720C68DB4E4BF49B911075E8B57B502DB5A`. Temporary cheats restored false, event logging off, ephemeral RCON disabled and credential file removed. Fixed matched test server left running. PR #11 is ready for review with these explicit limits; no merge and no Navigation progression.

## Remaining acceptance procedure

The following procedure uses existing authority; no old writer is enabled. Statuses above are authoritative; commands below are instructions, not additional PASS evidence.

| Remaining case | Minimal future procedure / expected evidence |
| --- | --- |
| Real multi-human distribution | 2–6 actual CT humans within Released; query all live bots with `css_zepve_bot`; counts differ by at most one and unchanged evaluations preserve versions. |
| Independent human lifetime | At least two CT humans keep the round alive; queue `css_zepve_ai_probe <bot assigned to test human> 30`, kill/disconnect that human. Confirm Released persists, other bots/identities unchanged, affected bindings removed/rebound and old probe rejected. |
| BindingVersion-only change | With one real human and live bots, queue probe for highest live bot slot; second real human joins CT in the same Released round. Balancing moves excess high slots; compare unchanged bot/old-human lifetime fields, new target/version and rejection of old queued binding. |
| Explicit reacquire coalescing | When status says Observing, call `css_zepve_reacquire <bot>` twice, confirm accepted/coalesced and unchanged Until; success/timeout stops the same window. Cooldown should reject a new completed-window request before ten seconds. |
| Native awareness efficacy | No writable native mode ships. Validate a single candidate operation and ownership in Lab before adding any field writer. Current ObserveOnly outcomes are never causal repair evidence. |
| Sustained performance | Repeat natural map/round/player transitions over an extended session; monitor frame time, memory/timer counts, bounded recorder and absence of unsolicited writer/log duplication. |

Native awareness field writes / efficacy: **NOT TESTED / not implemented**. ObserveOnly success is observation, not a perception repair claim. Legacy Recovery notification integration is not implemented; Navigation remains a future consumer. Full maps, endurance, multiple human physical layouts and native team-tally parity remain NOT TESTED.
