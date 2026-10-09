# v0.2c matched runtime acceptance

English | [简体中文](CORE_MATCHED_RUNTIME_VERIFICATION.zh-CN.md)

## Scope and decision

**Matched single-player smoke: PASS. PR #10 can move Draft → Ready for review; no merge was performed.** This is real Windows dedicated-server evidence collected through localhost RCON, CounterStrikeSharp logs and read-only console-buffer snapshots, with the user connected as a real CT human. It is separate from the 72 model/source checks and the earlier general user report.

Test window: 2026-10-09 23:21–23:33 Asia/Shanghai. Maps: `de_dust2`, `de_mirage`. Installed artifact revision: `1cf6aa21503316ab2963c35e6e222312e14131ad`. CounterStrikeSharp 1.0.376; CS2Fixes `v2.0-0-gbb3be34`; native BotController 0.7.1 remains installed. No DLL, gameplay config or source change was required during acceptance. No BotAI/Navigation development occurred.

## Deployment and recovery path

The matched package was installed while CS2 was stopped, before this session. Before testing, all eight live files were rechecked against the clean package manifest; the current pair was snapshotted under `.codex/runtime-acceptance-v02c-20261009/before-test`. The previous accepted observer/legacy/contracts set remains in `ZEPVE_Backup/v0.2c-20261009-151119-2996277`; its backup hashes passed read-only verification. Installation remains reproducible through the paired installer.

After testing, eight live artifact hashes and all 47 preserved config/unrelated-plugin files still matched. No piecemeal installation or old legacy writer reactivation occurred. Whole-set rollback was unnecessary and **actual server rollback remains NOT TESTED**. Rollback requires stopped CS2 and the backup's whole-set restore script; the current client's process also triggers that script's stop guard.

## Observed results

| Acceptance | Result | Real evidence / limits |
| --- | --- | --- |
| Matched plugin loading / shared ABI / single provider | PASS | One LOADED Core `0.2.0-c`, one LOADED adapted ZRPVE, `Authority=Core GameplayAuthority=True`; both communicate through the shared bridge. No load/type/provider exception. UNLOADED contexts retained by CSS after explicit tests are not active writers. No artificial duplicate-provider injection was attempted. |
| Human + ZombieBot registry | PASS | One connected/live Human and ten ZombieBots; disconnected slots become Unknown/Connected=False and are excluded from views. |
| Round start/restart and stale work | PASS | Preparing → AwaitingRelease → Released; restart advances RoundEpoch. Queued human probes rejected with `Reason=round epoch` at 23:22:50.456 and 23:23:57.473. |
| Preparation / solo profile | PASS | Preparing observed; after preparation, `Profile=solo BotQuotaPolicy=10 RespawnDelayPolicy=7`. Native `bot_quota=0`, `bot_join_team=CT` during the wait. |
| Infection/release timing | PASS (smoke resolution) | External ZR 16-second countdown/start messages captured. Round start 23:23:57.493; Preparing at 23:23:57.767, AwaitingRelease at 23:23:58.777, still waiting at 23:24:14.160; join team T at 23:24:14.690; Released/ten bots at 23:24:14.971. Compatible with +1 preparation and +16 wait. Polling does not certify exact sub-frame timing or every configuration. |
| Bot quota / team transitions | PASS (solo) | Native quota reaches 10, join team T, Core lists exactly ten ZombieBots; console records actual Bot transitions to T. Pending work reaches zero. No parallel old ZRPVE executor is installed. |
| External respawn / pawn invalidation | PASS | Controlled kill of one resolved bot at 23:25:28.879; slot 1 remains connected with generation 42, changes Alive=False and pawn generation, then Alive=True about seven seconds later in the same round. Probe rejected at 23:25:28.939 with `Reason=pawn generation`. Core pending work remains zero; ZR owns respawn. `sv_cheats` was enabled only for the kill and restored false immediately. |
| Retained Recovery | PASS (placement smoke) | Console captures `recovery placed` for `stuck` and `no_damage` at distinct route points, plus expected `recovery skipped: no route point` when no point qualifies. This proves legacy placement executes through current Core validity; it does not certify all geometry, escort/post-placement behavior or the exact respawn-triggered recovery path. |
| Map transition | PASS | `changelevel de_mirage`; recorder shows map end/start, MapEpoch 1 → 3, fresh player observations and later round/policy events. The queued probe was rejected first for disconnected/invalid entity during transition, not explicitly for map epoch; no stale work crossed the boundary. |
| Human disconnect/reconnect / hibernation | PASS (observed session) | Kick at 23:28:33; status during hibernation shows Humans=0, ZombieBots=0, PendingWork=0, slot 0 Unknown/Connected=False, connection generation 44. Local reconnect reuses slot 0 with generation 55; human and solo policy resume. Probe rejects disconnected/invalid entity. General hibernation timing is not certified. |
| Core hot reload: preparing/waiting/released | PASS (observed phases) | All three phases actually reloaded using the current numeric ID. Fresh GUIDs, old probe rejection for plugin lifetime, retained phase, completion of the pending plan and final quota 10. No observed policy/countdown replay or duplicate active Core. |
| Manual Core late load / bridge loss | PASS (smoke) | Manual Core unload rejects old probe; reload bootstraps existing 1+10 players with Phase=Unbound and waits for next round. Removing the adapted bridge yields GameplayAuthority=False/PendingWork=0; next restart stays Unbound with quota zero. Reattach restores the gate but waits for a subsequent round. Only matched modules were reloaded; no binary mixing. |
| Current-identity probe | PASS | `css_zepve_probe 0 2` → ACCEPT/current identity at 23:31:05.208. |
| Bounded recorder / exceptions / duplicate symptoms | PASS (smoke) | Recorder reaches 256/256 with Dropped increasing; does not expand. Pending work returns to zero. No Core/CSS exception or spontaneous repeated load/timer/policy symptom found in the session logs. Retained legacy debug messages remain; this is not a long-duration leakage test. |
| Server overhead | PARTIAL | One `stats` sample: 64.08 FPS, 11 players. No systematic frame-time or endurance benchmark. |

## Diagnostic limits and deferred tests

An engine Round_End line reported a T team tally of 20 while Core and native `status` enumerated ten bots. This does **not** establish twenty active bots or two suite writers. The former ChangeTeam→SwitchTeam sequence is retained in the single Core adapter, and native infection/team processing emits bot death/pawn transitions. The cause and pre-migration runtime tally parity were not proved; **native team-tally parity remains NOT TESTED**. No gameplay failure attributable to that diagnostic mismatch was observed, and no speculative fix was made.

Also **NOT TESTED**: real duo/coop/group populations, arbitrary-config or exact sub-frame timing, duplicate-provider injection, the pending quota/team sub-second hot-reload windows, multiple simultaneous humans disconnecting, exhaustive recovery/respawn permissions, long-duration timer/leak/performance checks, actual whole-set server rollback and full ZE-map compatibility. Prior PR #9 deferred items are historical; matching v0.2c scenarios above have new evidence rather than retroactive observer PASS.

## Evidence retention and final state

Raw transcripts stay local because they include player/network identifiers; repository records are sanitized. Local evidence directory: `.codex/runtime-acceptance-v02c-20261009`. `commands.jsonl` SHA256: `F118B302C3C07A78E0E578326C0B1FF9534E9CA62E7AE25B3F88E96626D8EDB1`; console snapshot `console-232906.txt` SHA256: `F7DA76B18A16202D345B0A1D65B21164FF9750561E794501370E12E654B2F149`. Core/CSS log copies and `acceptance-summary.json` are retained there.

The matched server was left running on localhost with the final normal solo policy. Temporary game-event logging was restored off, cheats restored false, temporary RCON disabled and its local credential file removed. No severe regression or Core/bridge code defect was found; no rollback or source rebuild was needed. Recommend Ready for review with the explicit deferred tests above; PR remains Draft until the owner requests the state change. Stop here; no automatic merge or next-phase development.
