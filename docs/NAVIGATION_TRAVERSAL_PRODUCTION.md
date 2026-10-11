# v0.3 timed traversal production candidate

Owner authorized full v0.3 implementation on2026-10-11. Lab PR2 remains research, not a shipped dependency. Native implementation belongs to ZEPVE-BotController; automatic clip formation/selection belongs to ZEPVE-Navigation. Base ABI1 plus optional traversal capability1; all matched files upgrade/rollback while stopped. Existing accepted main authority is not promoted by candidate deployment.

Navigation extends its existing per-human Trail with bounded event-focused clips. Stationary prelude→actual airborne maneuver→three landing commands preserves signed fractions, movement/button edge order and scoped view. Bot normally approaches the start, revalidates pose and submits one immutable clip through the same exclusive lease. Application-time token/provider/role/permission/native identity and revision checks, atomic replace/cancel, no stack and managed detach/drain remain mandatory. Valve owns view outside active authorized clips; BotAI remains ObserveOnly. No position/velocity copy, replay bypass, perception writes, new hook or new respawn executor.

Capability is currently Windows CS2 14190 /64Hz exact-image gated. Unsupported profiles deny timed/view while retaining ordinary movement. This is a bounded traversal capability, not complete client replay or universal Human/Bot physics equivalence. Continuous run/jump without a stationary prelude, long chains, actor collisions and dynamic obstacles remain limits.

## Verification

- Native Release and managed build PASS; native upstream SDK warnings retained.
- Router26/26; traversal router26/26 including1000 worker-detach races; retained interop14/14 plus1544-byte payload/literal-negative-fraction echo PASS.
- Navigation115/115:95 retained,11 capture and9 integrated timed/lifecycle cases PASS.
- Matched19 payload + Lab3 quarantine + actual22-file install/restore fixture PASS; incomplete package rejected.
- Fresh production timed/view CS2 acceptance: **NOT TESTED**, including automatic W+jump/crouch-jump, rotation, provider/module reload/drain and broad traversal. Lab evidence is explicitly separate.
- No PR merged or production release certified.

## Install / acceptance / rollback

Build exact committed suite/component pins through scripts/build-navigation.ps1. Installer now backs up/archives only the three known Lab consumer assembly files in addition to the19 matched payload files; recordings/configs are preserved. This avoids a Lab consumer loading alongside production Navigation. Standalone restore includes those originals; failed installs restore the whole set. Installer refuses dirty/mismatched package, API, private shared ABI copies or running dedicated server. Mutable config is preserved (including current Lab quota1); configure ordinary quota intentionally after acceptance, do not silently overwrite server policy.

Start ordinary-map server; join CT and stand still before each short maneuver. Check `css_zepve_status`, `css_zepve_ai`, `css_zepve_nav`. Require Released/current roles, legacy movement disabled, capability profile available and no Lab consumer loaded. Use `css_zepve_nav_recovery 0` after every Navigation reload during traversal testing. W/jump→crouch-jump→A-site first-box/second-box rotation; confirm actual crossing, not merely commands applied. Inspect clip start/completion/failure and final input. Then cancel/rebind/provider reload/module drain, round/map/bot/human identity cases. No actual evidence means NOT TESTED.

Severe regression: stop and run the installation backup's restore.ps1 -VerifyOnly, then restore.ps1 without VerifyOnly. Do not restore legacy movement while new Navigation runs. Native physical hot unload is unsupported by design. Backup restores the exact preceding installation, which may itself be a Lab/candidate baseline, not automatically accepted v0.2 main.

## Initial real-server continuation (2026-10-11 07:38–07:54 Asia/Shanghai)

Matched suitebda0159/native1e486ee/Navigation134065d installed while stopped,19/19 live hashes verified. Current backup v0.3-20261011-073807-7690790 contains19 originals +3 Lab consumer files and standalone restore; VerifyOnly PASS. Actual rollback of this NEW production candidate remains NOT TESTED (fixture actual restore and prior Lab real rollback are separate).

Six suite/retained plugins loaded, Lab assembly absent. Traversal capability/profile enabled, Core Released/one live Human/one ZombieBot, LegacyMovementDisabled=True, one native owner/one lease, ActiveFrames0/PendingContexts0/LookupFailures0 observed. Ordinary native/world-direction command applications observed; no physical timed traversal success claimed. Human native capture showed consecutive valid rows with controller origin=tick-1, zero velocity and stationary prelude armed. CaptureLastReject reports historical failures, not necessarily the current sample.

Temporary read-only reflection inspector (no owner/hook/mutation) was loaded solely to inspect that capture; then unloaded and archived outside the plugin loader directory. It is not a package/runtime dependency. Source excerpts: Ticket5/After7856, Tick49939/Origin49938/Valid1/ground flags65665, velocity0, Idle=True/Active0. Earlier rows8046..8048 also valid. No clip formed/action observed at this checkpoint; W/jump, crouch-jump, rotational physical playback and new timed reload/drain remain NOT TESTED pending user actions. Core round/role/pawn transitions observed, not a timed-clip lifecycle PASS.

Recovery disabled throughout. Test-only sv_cheats1/bot_dont_shoot1 temporarily prevent Bot attack while sampling (original values0/0 recorded locally; restore before ordinary PvE acceptance). Config remains one-Bot profile. Recent12000 server-log lines had no exception/load-failure match; no endurance certification. Server23516 remains running for owner testing, no merge. All three production PRs returned to Draft for new capability/acceptance.

## Facing/alignment repair candidate (2026-10-11)

Owner reports backward plain steering and player-facing rotational failures. Real inspection showed clip-approach without clip-start, not proven active-clip eye takeover. Deterministic source defect fixed: the clip-search interval no longer bypasses latched start alignment. Optional traversalABI2 now includes explicit FacingDirection under the same lease/final validator; derives yaw from movement goal, outputs forward/side0, preserves pitch, rejects unowned command view-history and releases view on cancel/expiry/Native. Clip playback still owns literal source view, no new hook/Enemy/perception/flags. Core/BotAI authority unchanged. Native26+33,14+payload interop, Navigation117/117 and install/actual restore fixture PASS. Fresh facing/rotation runtime acceptance NOT TESTED at build; earlier reports remain actual failures until retest.

ABI2 retest package installed while stopped at08:21, suite9989d30/nativeaa42948/Navigationd6650d9,19 payload hashes verified by installer. Current backup v0.3-20261011-082105-7849465 restores the preceding productionABI1 candidate; VerifyOnly PASS, actual NEW rollback NOT TESTED. Server24416 started, exactly six loaded plugins/no temporary inspector, capabilityABI2 enabled and resident owner initialized, frames/contexts0 before Human. Recovery disabled; test-only cheats/dont-shoot1 restored for sampling, original0/0 remains required after acceptance. Physical forward-facing, latched entry and rotational success remain NOT TESTED pending owner retest.

## B-site entry and chained-jump report (2026-10-11)

User reports unreachable takeoff parking, no distant pursuit, single-jump phase error and first-landing oscillation during chains; reports B-site currently blocked. Snapshot showed one valid assigned target302units away, Native forward/side0 and no timed playback. No successful signed-timed clip is inferred. Repair candidate keeps nativeABI2/Core/BotAI unchanged: distance-scaled idle-entry/friction settling; moving entry refuses reversal across source arrival plane; waypoint Waiting requires8-unit arrival instead of unconditional96. Clip entry deadline retires once with fresh route/event/cooldown. Automatic second jump can use two literal consecutive moving-ground source commands, without new idle or reusing published first-clip commands. No fraction/tick retiming, velocity/position copy or Recovery masking.123/123 Navigation PASS; actual timing, B-site ascent and physical continuous-chain behavior remain NOT TESTED for repair until owner retest. Diagnostics now expose per-target bounded clip start/end/source velocity/age/sequence and completed/rejected/entry action.

Entry/chain package3 real install08:42: suite15d7b98/Navigationbebc4ff/nativeaa42948 ABI2.19 payload hashes verified, current rollback v0.3-20261011-084219-5649154 (preceding package2), VerifyOnly PASS; actual new rollback NOT TESTED. Dedicated22360 loaded six plugins/no Lab or inspector, capability2 enabled, Recovery/attack suppressed only for retest, resident owner1/contexts0 observed before Human. User must reconnect to B-site; physical outcomes still pending, no PASS added. Source and model changes are separated from gameplay acceptance.
