# v0.3 Navigation execution safety gate

## 2026-10-10 Lab follow-up: mechanism gate cleared

**Backend production safety: PASS under process-resident native + hot-reloadable managed consumer. v0.3 implementation may resume; no Navigation runtime or authority handoff occurs here.** [Lab PR#1](https://github.com/Kzen023/ZEPVE-Lab/pull/1) now contains the actual existing PlayerRunCommand hook adapter and sanitized runtime drain evidence (`experiments/movement-hook/README.md` / `DETACH_RUNTIME_EVIDENCE.json`). Original audit below is historical.

The owner authorized using their real server. One dedicated process stayed running throughout the accepted20/20 detach records: actual CSS context unload during validator, after complete validation/before commit, actively applying reload, replace/cancel-before-unload, worker detach, immediate reload, real Core/BotAI replacement while a conservative pending-root release fence held, fresh lease/lifetime rejection and10 repeated reload cycles. Retired owners release only at nativeDrained=true/frame0/callback0; no old callback/application/resurrection or context lookup failure. Separate native44/44 and interop35/35 regression PASS.

The native router/hook and shared callback adapter are process resident. Physical native hot unload is **unsupported by design**, not a NOT TESTED blocker; replacement requires stopped server. Actual managed CSS Unload/Dispose/Load is verified; CSS loader/assembly-memory retention remains host-owned and is not claimed to be immediate physical ALC reclamation. The patch uses the existing dispatcher, with prior real owner/old-API arbitration evidence retained rather than repeated.

This is evidence for a patched candidate, not a declaration that installed ABI22 became safe. Implement the production backend independently in its owning native repository, then the clean Navigation consumer and matched handoff. Lab is never a release dependency. Test package was rolled back while stopped: original native restored, temporary Lab modules archived,15 baseline hashes verified. Production Core/BotAI/legacy source, shared ABI, native/component pins and MIGRATION_AUTHORITY current writers remain unchanged; existing deferred acceptance remains deferred. No Trail/Recovery/stuck/Navigation runtime begins in this follow-up.

## Baseline and disposition

PR #11 was merged with owner authorization. New accepted v0.2 complete source baseline: `main @ 7cf389157f7f09f08c399ba5ca6a33b5ccbf4d46`. Its installed matched package remains source `238b98c`; no DLL or native module was changed during this audit. Existing v0.2 deferred acceptance remains unchanged. Baseline model/source checks rerun: **127/127 PASS**.

**v0.3 is NOT implemented or ready for review.** Runtime writer handoff stops at the movement execution prerequisite. This report is a source/ABI audit, not a Native/Trail/Recovery runtime PASS. No legacy writer is disabled, no new movement writer is installed, and the Navigation gitlink remains `14cda1f42fdaab749facae2ca35ca21b7dadd770`.

The requested callback safety includes bot/target Core lifetime, BotAI module GUID/BindingVersion, Navigation lifetime and current gameplay permission at execution. The available persistent movement override cannot independently satisfy that boundary. Timer-time validation must not be presented as native execution-time validation.

## Inspected inputs

- Core PlayerRegistry/lifecycle validator/scheduler and gated round policy; BotAI's sole target provider, dual Core identities, permission/version validator and ObserveOnly reacquisition.
- Adapted legacy `ZrPvePlugin.cs`: RecordHumanPath shared history, CheckZombieBots watchdog, ObserveSpawn/QueueBotRecovery, TryRecoverZombieBot/TryFindRecoveryPoint, teleport paths and map-scoped services. They remain active owners until a real replacement/disable path is ready.
- Navigation pinned design `14cda1f` and current component main `49e65f55dc0e147d7d73d42059d0960602dc0cc7`, including audited per-human sequence/segment, hysteresis/waiting, permission/geometry and Lab-before-native requirements. Current main has design documents, no runtime implementation.
- Installed CSS API SHA256 `E81F05D960C37EE623D552733FC919E206C8C1D16157886342D06A2F4D0DAB17`: existing TraceHullShape/TraceEndShape/PointContents APIs available for later geometry validation. Availability alone is not proof of safe recovery placement.
- Installed BotController DLL SHA256 `C957E7CDEC12A0E8EE20CB45C18BD1F16D2DFEEBF5B5F2324A3ADBFABB314756`; logs report v0.7.1 @ `0ae8f18`. Read-only PE export inspection confirms Start/Update/CancelUsercmdMovement and GetVersion machine code `mov eax,22; ret`. No native DLL was loaded into an audit process and no movement command was executed.
- Exact [BotController source](https://github.com/XBribo/CS2-Bot-Controller/tree/0ae8f18fd6872a369cb984e0e95e5a352be092fe), especially InputInjector.cpp, PawnBinding.cpp, MotionRecorder.cpp and exports.cpp. Source/log/exports correspond; binary reproducibility was not established.
- Optional [BotNav reference](https://github.com/ed0ard/CS2-Bot-Nav/tree/b80d6a687a7e8fef530fd4ef9b9aab073466ab67): native MoveTo signature, repeated goal executor and raw field access reviewed only. BotNav is not installed or a suite dependency. No third-party implementation/signature was copied into production.

## Concrete gap in BotController ABI 22

| Source path | Observed behavior | Required distinction |
| --- | --- | --- |
| `src/features/recorder/InputInjector.cpp:171` StartUsercmdMovement | Appends persistent `{id, forwardMove, leftMove}` to a vector indexed by slot. Checks slot, finite inputs, hook availability and replay; captures no controller/pawn serial or suite lifetime. | A cancellation ID identifies an override, not an identity/permission token. |
| `InputInjector.cpp:187` UpdateUsercmdMovement | Finds matching ID in that slot vector, replaces analog values. | No Core/binding/module validity gate is exposed at execution. |
| `InputInjector.cpp:318` ApplyUsercmdMovement | Selects `movements.back()`, overwrites final directional buttons and analog/subtick values. | Last override wins; no exclusive owner acquisition/arbitration contract. |
| `InputInjector.cpp:674` HookedPlayerRunCommand | Resolves current services to a current slot and applies that slot's stored movement. | Current slot resolution is not comparison against the identity/target binding that authorized the stored intent. |
| `PawnBinding.cpp` ServicesToSlot | Validates current pawn/services ownership for lookup. | It does not bind the saved override to the original pawn or BotAI target generation. |
| `MotionRecorder.cpp:733` StartReplay | Clears that slot's usercmd work. | External replay can erase another caller's intent; replay and exclusive Navigation movement require an explicit boundary. |

This is **not evidence of two active movement callers today**. The managed BotControllerImpl is disabled and prior v0.2 smoke reported no replay/locks. The gap is that the proposed durable execution mechanism has no enforceable lifetime/ownership boundary once work is handed to it.

Two source-level counterexamples matter before adding a production writer:

1. A slot-indexed persistent override remains stored while its pawn/occupant or pursuit binding changes. The next native command can select it using the new current slot mapping unless cancellation has already reached native state. A managed periodic validator cannot certify every intervening native command.
2. A second override can be appended for the same slot and silently wins, while the old one remains underneath. Canceling the newest can expose the previous stale override. A suite-owned provider reference alone does not arbitrate this native state.

Normal unload cleanup could cancel known IDs and event handlers could narrow these windows. That is useful cleanup, but does not create the required native application-time identity check or exclusive backend owner. A public export's presence is not enough to call this capability safe.

## Why not work around it

- Do not install BotNav as a second autonomous goal scheduler. Its slot-based repeating executor and unverified signature are research inputs, not the suite's lifecycle-safe authority.
- Do not silently replace the backend with CBot speed/button/goal field writes whose ordering against Valve/BotController is unproven.
- Do not use All/Aim locks as a blanket solution; freezing Valve Update/Upkeep also changes combat/perception behavior.
- Do not keep legacy recovery and a new recovery executor active together, or call teleport frequently to simulate pursuit.
- Do not change AssignedTarget/Core gameplay authority or add Enemy/visibility writes to bypass the missing locomotion boundary.

The user's stop conditions for an unguaranteed movement writer/native ownership boundary apply. No active handoff occurs in this documentation PR.

## Prerequisite for resuming full v0.3

Establish a movement backend with an explicit exclusive-owner lease and a rejectable application-time gate (or an equally verifiable command mechanism): current bot/pawn serial plus complete current suite identity/binding/permission validation, before final native state mutation. Release/invalidation must cancel only the owning generation's work and must not expose an old intent. Define replay/other-callers conflict rejection and map/round/disconnect/respawn/module-unload semantics.

Prove the mechanism in ZEPVE-Lab before production promotion: one-step application, competing owner rejection, target rebind between submission/application, same-slot reuse/pawn change, map/round/provider/module invalidation, cancellation and native cleanup. Then implement the production backend cleanly in its owning repository. This is a prerequisite specification, not a request to extend perception scope or an unverified replacement already shipped.

Once that gate is proven, finish Navigation in its component repository, with per-human bounded sequence/segment Trails, multi-signal waiting/progress and Native→Trail→Recovery escalation. Switch legacy writers only in the matched suite integration. Recovery must separately validate permission and hull/ground safety, rebind the current route and call BotAI ObserveOnly reacquisition before normal pursuit resumes. Future Navigation stays the sole final movement/native-goal writer.

## Verification status

| Check | Result |
| --- | --- |
| PR #11 merge / latest main baseline | PASS, merge `7cf3891` |
| v0.2 retained automated/model/source checks | 127/127 PASS |
| Installed API hash, native exports / ABI and source safety audit | Completed, read-only |
| v0.3 build / automated/model tests | NOT TESTED / no implementation |
| v0.3 Native movement / Trail / Recovery runtime | NOT TESTED |
| No new writer / no deployment / no writer disable / pin unchanged | Confirmed by source/Git audit |

All previously deferred tests remain deferred. T tally 20, awareness efficacy, full maps and long-duration performance were not investigated or promoted to blockers. The blocker here is the proposed execution mechanism's missing lifecycle/ownership guarantee, not absent certification of a deferred feature.
