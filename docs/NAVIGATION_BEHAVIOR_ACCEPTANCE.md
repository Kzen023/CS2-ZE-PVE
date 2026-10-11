# Final behavior gate (reopened 2026-10-10)

The architecture review passed. This document supersedes any assumption that the earlier ordinary-map smoke certifies advanced traversal. No production PR is merged by this acceptance work.

## Actual evidence

| Behavior | Actual user-server result |
| --- | --- |
| Far-distance plain pursuit | PASS after normalized native command axes; user explicitly confirmed. Prior450-axis packet was renewed/applied but Bots failed to move normally |
| Low native aggro | Observed: valid AssignedTarget with empty Enemy/low visibility; pursuit can continue via movement. Native perception efficacy NOT TESTED; ObserveOnly remains |
| Low boxes | User reported successfully traversed; limited ordinary-map PASS |
| Doghole/repeated crouch jumps | FAIL on intermediate candidates; no blanket PASS |
| A-short→mid rotating jump | FAIL on intermediate candidates and package12; screenshot/user confirmed seven Bots stuck above mid doors |
| A-site→CT shortcut | User reported Native/old route detour; new validated shortcut selection implemented, broad physical completion remains unaccepted |
| Noblock experiment | CS2Fixes toggle and engine mp_solid_teammates/enemies0 confirmed. User reported no noticeable improvement. No production config changed; cvar alone does not prove pawn group change |
| Native tactical crouch versus assist | Final command observer distinguishes mode/before-after/axes/jump/duck. Source fixes release stale native duck, remove controlled subtick edges and refresh cache through Native mode. Broad crouch-regression absence NOT TESTED |
| Exit during testing | User confirmed manual server shutdown; not a crash. No Core/Navigation managed exception observed in recorded sessions |

## Corrections and verification level

Native26/26 including1000 worker-detach races, synthetic interop14/14, Navigation58/58 models and retained127/127 PASS; Release builds PASS (upstream native SDK warnings retained). Models are not CS2 runtime evidence.

Native fixes preserve the existing router/lease/validator: normalized Bot analog axes, correct button release/controlled subtick arbitration, historical bounded final command snapshots and Native-mode held-cache bookkeeping. No new hook or unowned replay.

Navigation corrections: cumulative/stable progress; avoid AssignedEnemy-only preservation; keep Trail control through intermediate nodes/flight; record dense air/ground turns and exact read-only player_jump takeoffs; observe preparation routes while denying movement until Core Released; distinguish external BaseVelocity from gravity; use validated nearby recent Human shortcuts before stuck; exclude actor bodies explicitly from terrain-only traces. All capture remains tied to Core identity and per-human sequence/generation; no second registry.

Jump execution remains a physical controller experiment: takeoff/guide/landing phases, source velocity plus positional correction, bounded jump/duck, same-segment reachable grounded skip. After package12 reproduced oscillation/zero input, package13 latches run-up, accelerates past the takeoff approach, derives launch speed from actual recorded takeoff rather than flight average, and skips old ledges only through clear reachable geometry. Core/BotAI/respawn authority unchanged; Recovery disabled throughout this behavior acceptance.

Package13 immutable19-file provenance: suite f9db854, native b27b1d6, Navigation0561b29. User reported improvement but continued blockage and stationary crouch-jumping after jumping out of CT then looping through mid back to CT. **Package13 advanced traversal remains FAIL/PARTIAL, not accepted.** Latest candidate adds IN_SPEED(1<<16) to authorized WorldDirection arbitration (SDK definition verified; observed native65536 explains walking speed near125) and a three-attempt/30-second failed-maneuver cooldown with Native fallback. That fallback is not a traversal PASS. Package14 retest FAIL: user reported continued stationary crouch-jumps/idle, Bots blocked at a barrel after the Human rotated around it, approximately half blocked above A-short/mid with repeated forward/backward motion, and stationary crouch-jumps during flat Human bunnyhops. Exact package14 executable provenance: suite1266ee6b55bf4398802d405d9f78dd2dca821057 / nativefcf3e66a11a426cb3a1d27e6ba3b0bce32cb4e35 / Navigation7900d9db8a880668dd95e8329cd1037ce39544f7. These are actual failures and remain a matched rollout merge BLOCK, not deferred items. New Navigation source ee0eb48 fixes ordinary Native-to-Trail re-entry bypassing the failed-jump cooldown and bounds grounded run-up to three seconds with cancellation/reset. Physical completion after that correction is NOT TESTED pending package15. The corrected control loop does not claim KZ reproduction. Later documentation commits do not change executable provenance.

## Known limitations and KZ replay boundary

No guarantee of generic longjump/rotating jump/bunnyhop/low ceiling/boost/platform traversal, full ZE/no-NAV coverage or endurance. Recorded path clearance is conservative, sampled and bounded; it is not a complete physics solver or learned global shortest-path graph. Scene/physics differences between a Human and a zombie Bot can matter. Do not force Enemy/visibility or change Core/ZR movement policy to hide these differences.

CS2KZ's [playback implementation](https://github.com/KZGlobalTeam/cs2kz-metamod/blob/master/src/kz/replays/playback.cpp) directly restores pre/post positions, velocities, angles and movement-service state. That approach can produce faithful playback but cannot be substituted for proof that a PvE Bot physically executes the route. No such state replay, parallel hook, KZ runtime dependency or disabled old BotController replay was enabled. Current movement ABI has direction/buttons and lifetime validation, not a timed subtick recording payload; perfect physical reproduction is not claimed.

Promotion remains component review/merge then suite pin promotion. Runtime behavior failures are independent of the accepted architecture safety gate. Final merge recommendation must reflect the remaining actual failure, not automated counts or noblock toggles.


## Package15 control-loop retest

Matched package15 was installed while stopped on 2026-10-10, all19 hashes verified, current backup `ZEPVE_Backup/v0.3-20261010-145634-1925240`. Executable provenance: suitebe90d23 / nativefcf3e66 / Navigationee0eb48. Native documentation-only follow-up df67e21 is not a new executable. Core/BotAI/legacy/shared were deployed together; no helper or private shared ABI was added. Server started, one Human and10 Bots observed, RecoveryEnabled=False, LegacyMovementDisabled=True, PerceptionWrites=0.

Actual control observation: bot1 exhausted its grounded run-up, retired lease12 and then held fresh lease19; historical LastCommit must not be treated as a live old intent. Native mode later preserved forward0/side0/duckTrue with empty/transient Enemy. This separates an observed tactical Native hold from dropped Navigation input; ObserveOnly does not restore native aggro. The bounded timeout actually executed. No broad physical traversal PASS is inferred; user feedback below supersedes pending package15 result. Previous package14 failures remain the acceptance blocker until verified resolved.

The backup restores the preceding matched candidate, not automatically the accepted v0.2 baseline. For full baseline rollback, restore complete verified chain in reverse deployment order while stopped; never mix native, Core/BotAI/legacy/shared/Navigation files from different packages. Earlier actual baseline rollback remains historical evidence. Current server is running package15, not restored v0.2.


## Package15 user result and package16 candidate

User-reported package15 result is PARTIAL/FAIL: fewer repeated actions, a few Bots passed but others fell behind; stationary Human hopping left Bots crouch-jumping sideways at the hop location, sometimes backing away from the Human. Required box→A-short crouch-jump remains FAIL. Bounded failure exit did not establish physical traversal acceptance. No deferred test was promoted to blocker; these are real observed failures.

Navigation251b019 stops inheriting live target Jump/Duck markers during Native pursuit and avoids jumping solely toward an elevated airborne target when standing clearance exists. Optional recorded jumps advance only over a complete bounded standing hull/stable-support corridor; barriers/gaps/high steps/special motion reject the walking conversion. Newer current-segment grounded sequences must improve AssignedTarget distance and pass that same ground check. Exact-position entry ties prefer newer sequence. Cancel/new route version retires old intent when advancing. Model67/67 PASS; retained suite and Release matched build rerun when packaging. Real geometry/traversal for package16 remains NOT TESTED until observed. No native backend, Core/BotAI authority, Enemy/perception or Recovery change.


## Package16 user result and raised/drop retest candidate

User actually confirmed flat-ground behavior improved; complex crouch-jump still FAIL. Explicit additional failures: A-long ramp→A-site ascent and A-short→CT descent. No broad PASS is claimed. Live slot5 had valid target/route but recorded-curve hull rejection (`worldent`, fraction0.738), cancelled lease/RouteInvalid; this is geometry/route execution rejection, not missing identity or a competing writer.

Next source correction: a demonstrated, fully validated recorded jump is no longer denied by the generic56-unit obstacle heuristic; generic unsupported high jumps remain denied. If the current grounded pose cannot execute the recorded curve, the intent is retired once and the maneuver receives bounded cooldown/Native fallback instead of permanent RouteInvalid parking. Read-only diagnostics now expose recorded takeoff/landing and LandingDeltaZ; no new traces in the status command. Native fallback does not mean traversal completed. Model70/70 PASS including raised recorded landing versus generic jump rejection, required curve/support gate and single retirement without repeated invalid-route parking. Actual ascent/descent efficacy is NOT TESTED pending next matched retest. No backend/Core/BotAI/perception/Recovery authority changed.


## Current matched package17 deployment

Stopped-server replacement on 2026-10-10: previous whole-set backup/inventory verified, current state backed up and all19 matched files installed/hash verified. Executable revisions suite93494299ffcaa8e96c58fe72d23d10faa05e43e7 / Navigation0dd1d3b16b1da295ba06953d6bba437053c60986 / nativefcf3e66 executable (df67e21 docs-only pin). Backup `ZEPVE_Backup/v0.3-20261010-180220-4733607/restore.ps1` restores preceding package16. Release build, retained127/127 and Navigation70/70 PASS. No new native source/binary change.

Server launched, shared ABI/provider/gates observed with RecoveryEnabled=False, LegacyMovementDisabled=True and PerceptionWrites=0. Initial no-Human Unbound state had zero leases/intents; not a gameplay failure. Await actual human ascent/descent retest. Native remains process resident; previous server process exited before replacement. No partial-DLL installation, old writer re-enable or baseline promotion occurred.


## Package17 A-short failure and launch posture candidate

User explicitly reported A-short→CT still failed and Bots did not appear to run forward then jump. This remains FAIL. Package17 current-curve retirement was observed with worldent rejection, but that control exit is not passage completion.

Source diagnosis: ProbeRecordedJump derived standing takeoff clearance from whether the Human ever ducked during flight. That conflated airborne duck with grounded launch posture. It now checks the actual standing hull at the Bot's launch position. With standing clearance, a running launch presses jump only and preserves that posture for its bounded grounded pulse; airborne flight can then duck. A real low ceiling still permits the existing crouch launch. Pulse cancellation/reset and all native lifetime/owner gates are unchanged. Model73/73 PASS, including grounded jump-only pulse, airborne duck transition, low ceiling and bounded retirement. Real launch/route efficacy for this new source remains NOT TESTED. No view/Enemy/position/velocity writes or authority changes.


## Matched package18 launch posture deployment

2026-10-10 stopped-server whole-set installation/hash verification PASS, current-state backup `ZEPVE_Backup/v0.3-20261010-181108-0027450/restore.ps1` restores package17. Package18 executable provenance: suitec6ff4c2de09484ea2d00103a40fa6de5a82d7a0a / Navigationf6115764e3c67f14ab3572d33865fb97fd1ec5fd / unchanged nativefcf3e66 executable (df67e21 documentation pin). Release build, retained127/127 and Navigation73/73 PASS. Core/BotAI/legacy/shared/Navigation/native deployed as19 verified files after previous dedicated process exited. No helper or private shared ABI.

Server load and shared gates observed, RecoveryEnabled=False, LegacyMovementDisabled=True, PerceptionWrites=0. Physical A-short descent and A-long ascent remain NOT TESTED for package18 until actual observation; prior failures are not automatically cleared by build/deployment. Matched rollout remains merge NO. Native/backend/Core/BotAI/respawn ownership unchanged; no physical state replay.


## Package18 failure and slope/staged launch candidate

Actual user evidence: A-short descent still blocked/standing jumps; some mid Bots crouched/held. Additional FAIL: repeated jumping on a ramp and CT→A-short staged crate jumps at the two marked takeoff/landing positions. No exact map coordinates are hardcoded. Live capture with Core Released confirmed both Native zero input and Native nonzero forward input with zero speed; these are different observations. Earlier Closed/hibernating snapshots were cached, not current wall input. Router Leases counts reserved records, not currently authorized applications; steady-clock expiry and final Core/Binding checks still fail closed.

New source holds recorded forward direction throughout grounded run-up instead of zeroing/reversing at a fixed goal point. Supported walking follows floor planes/height profile and standing hull segments; total ramp rise is not a vertical-step jump trigger. Unknown/steep/discontinuous/moving/gapped paths reject; local supported ramp can receive controlled forward walking without inherited Human jumps. Stage entry prefers grounded nodes at the Bot's current height. Recorded landings require12-unit horizontal/6-unit vertical proximity before jump progression; nearby incomplete landings are corrected without skipping to the next jump, with bounded attempts. Initial takeoff aligns lateral position/launch window and support height. Full geometry/identity/permission/lease checks remain. Status suppresses invalid cached bindings before the next tick and exposes gameplay permission; no movement write in diagnostics.

Model85/85 PASS (ramp versus vertical crate, steep/nonfinite rejection, lower-stage entry, landing correction, lateral alignment, finite-goal forward wish, closed/disconnected/rebound status). Real profile/compound traversal for this source NOT TESTED until matched deployment. Earlier failures remain merge BLOCK. No Native/Core/BotAI/perception/respawn authority changes; Recovery off, no KZ state replay.


## Package19 slope PASS / low-obstacle regression

User real-server result: ramp now walkable (limited slope PASS), low obstacles now block Bots without jump (new FAIL/regression), complex jump points still stuck (FAIL). No overall traversal acceptance.

Next correction scopes generic jump validation to a bounded local supported landing48/80/112/144 units toward AssignedTarget instead of treating the distant pursuit target as the jump endpoint. Four candidates/four bounded rises, full hull arc/support and moving-ground rejection; a local proof may enable controlled obstacle input without changing AssignedTarget. Unknown landing rejects. A missed landing cancels/version-invalidates the old attempt and resets approach/guide; grounded launch alignment resumes after the bounded pulse. Native combat yielding requires visible close same-level attack/contact (96 distance/24 height), not an unreachable elevated knife attempt; near-goal Native preservation also needs clear direct same-level corridor. BotAI only observed, no native perception/view writes. Local landing is Navigation routing, not target assignment.

91/91 models PASS including distant target with local obstacle, unsupported landing rejection, visible close combat preservation, elevated/invisible attack not becoming movement hold and stale route after missed landing. Release matched build/retained suite rerun on package creation. Real low-obstacle/compound efficacy for this correction remains NOT TESTED until new matched retest. Native/Core/BotAI/respawn authority unchanged, Recovery off, no KZ state replay. Package19 slope PASS does not automatically transfer to a modified package.


## Package20 current deployment

Whole19-file stopped-server backup/installation/hash verification PASS on 2026-10-10. Current rollback `ZEPVE_Backup/v0.3-20261010-191556-1240612/restore.ps1` restores prior package19. Executables suite12a6a1c60d679f7a24952d8ec56656e3b2e3db92 / Navigationf22d987c286a8f10eb4587b6d68b8b74b42d6146 / unchanged nativefcf3e66 (df67e21 docs pin). Release build, Navigation91/91 and retained127/127 PASS. Startup/shared gate observed, RecoveryEnabled=False, LegacyMovementDisabled=True, PerceptionWrites=0. Local-obstacle/slope/staged physical efficacy remains NOT TESTED until actual observation. Earlier package19 slope PASS and low-obstacle regression remain historical. No baseline promotion or merge.


## Package20 user results / ordered stage and target proximity

Actual user feedback: slope normal PASS; extremely few Bots reach A-short (PARTIAL/FAIL), collider/approach snagging, weak attack/pursuit while a Human stands nearby, low-obstacle standstill crouch-jump about three times before moving forward (PARTIAL), complex jumps still FAIL. User also reports out-of-order jumping and scraping walls while finding takeoff. No complete acceptance.

Next correction: periodically validate a nearby current AssignedTarget's full supported walk corridor; if reachable, retire old Trail/box traversal, cancel/version-invalid old intent, reset launch state and request bounded ObserveOnly NavigationRequest from BotAI. Revalidate nearby reachability to suppress re-entry into obsolete jumps while the Human remains directly reachable; unreachable nearby target cannot bypass a stage. Entry chooses the nearest recorded takeoff at current support height, with a full supported approach rather than truncated ClearPath. Mandatory detour stages are no longer discarded by a direct target-distance improvement heuristic. Human sequence order remains authoritative within the selected route; no map coordinates hardcoded and no target selection moved to Navigation.

95/95 models PASS, including proximity interruption/stale packet, no inaccessible-stage bypass, lower-ground versus later upper takeoff, necessary detour entry and reentry suppression/coalescing. Real proximity/order efficacy NOT TESTED for new source until matched retest. Collider bounds/physical timing fidelity remain unproven; no perfect-reproduction claim.
