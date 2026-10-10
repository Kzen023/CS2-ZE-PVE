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

Native26/26 including1000 worker-detach races, synthetic interop14/14, Navigation55/55 models and retained127/127 PASS; Release builds PASS (upstream native SDK warnings retained). Models are not CS2 runtime evidence.

Native fixes preserve the existing router/lease/validator: normalized Bot analog axes, correct button release/controlled subtick arbitration, historical bounded final command snapshots and Native-mode held-cache bookkeeping. No new hook or unowned replay.

Navigation corrections: cumulative/stable progress; avoid AssignedEnemy-only preservation; keep Trail control through intermediate nodes/flight; record dense air/ground turns and exact read-only player_jump takeoffs; observe preparation routes while denying movement until Core Released; distinguish external BaseVelocity from gravity; use validated nearby recent Human shortcuts before stuck; exclude actor bodies explicitly from terrain-only traces. All capture remains tied to Core identity and per-human sequence/generation; no second registry.

Jump execution remains a physical controller experiment: takeoff/guide/landing phases, source velocity plus positional correction, bounded jump/duck, same-segment reachable grounded skip. After package12 reproduced oscillation/zero input, package13 latches run-up, accelerates past the takeoff approach, derives launch speed from actual recorded takeoff rather than flight average, and skips old ledges only through clear reachable geometry. Core/BotAI/respawn authority unchanged; Recovery disabled throughout this behavior acceptance.

Package13 immutable19-file provenance: suite f9db854, native b27b1d6, Navigation0561b29. User reported improvement but continued blockage and stationary crouch-jumping after jumping out of CT then looping through mid back to CT. **Package13 advanced traversal remains FAIL/PARTIAL, not accepted.** Latest candidate adds IN_SPEED(1<<16) to authorized WorldDirection arbitration (SDK definition verified; observed native65536 explains walking speed near125) and a three-attempt/30-second failed-maneuver cooldown with Native fallback. That fallback is not a traversal PASS. New matched runtime retest remains pending. Later documentation commits do not change executable provenance.

## Known limitations and KZ replay boundary

No guarantee of generic longjump/rotating jump/bunnyhop/low ceiling/boost/platform traversal, full ZE/no-NAV coverage or endurance. Recorded path clearance is conservative, sampled and bounded; it is not a complete physics solver or learned global shortest-path graph. Scene/physics differences between a Human and a zombie Bot can matter. Do not force Enemy/visibility or change Core/ZR movement policy to hide these differences.

CS2KZ's [playback implementation](https://github.com/KZGlobalTeam/cs2kz-metamod/blob/master/src/kz/replays/playback.cpp) directly restores pre/post positions, velocities, angles and movement-service state. That approach can produce faithful playback but cannot be substituted for proof that a PvE Bot physically executes the route. No such state replay, parallel hook, KZ runtime dependency or disabled old BotController replay was enabled. Current movement ABI has direction/buttons and lifetime validation, not a timed subtick recording payload; perfect physical reproduction is not claimed.

Promotion remains component review/merge then suite pin promotion. Runtime behavior failures are independent of the accepted architecture safety gate. Final merge recommendation must reflect the remaining actual failure, not automated counts or noblock toggles.
