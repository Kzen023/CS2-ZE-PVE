# v0.3 actual matched-runtime acceptance — 2026-10-10

These are actual user-server observations, separate from models. Windows x64, CSS 1.0.376, CS2Fixes/ZombieReborn, de_dust2/de_mirage, one real Human and ten ZombieBots. Lab is not a runtime dependency.

Tested executable package: suite `19c235565b54c4f720d4d79e7b8117aa6ba49d5f`, native `98cb092bb1461b4a699fb3ffb6fb51bf8e776dd1`, Navigation `8456d15d3a2adc667cf57254163ce2ed0ea64ef2`, clean source and 19-file SHA256 manifest. Later documentation revisions do not change these executable revisions.

Automated/build: native **22/22**, including 1000 worker-detach races; reverse-PInvoke/GCHandle SDK interop **14/14** on synthetic host; Navigation **30/30** models; retained Core/BotAI **127/127**. Release builds PASS; upstream native SDK warnings remain. These automated results are not CS2 runtime proof.

| Runtime item | Result and limits |
| --- | --- |
| Native/shared ABI/loading | PASS: fork0.8.0, compatibility ABI24, movement ABI1, one resident router |
| Native/Trail pursuit and renewal | PASS ordinary-map smoke; corrected far-distance pursuit confirmed by user |
| Detach/cancel/no resurrection | PASS: Owners=0, Leases=0, ActiveFrames=0; frames60531→61136 while Applied=27811/NativeAuthorized=20800 remained unchanged |
| Navigation reload | PASS: fresh GUID/lease and five repeated reloads, one owner, PendingRoots=0/LookupFailures=0 |
| BotAI/Core reload | PASS: fresh providers/bindings/routes/leases; old provider work rejected, quota/policy retained |
| Competing owner | PASS: peer Acquire=False; no peer movement submitted |
| Stale BindingVersion | PASS: deliberately stale submitted packet rejected by actual native validator. Natural isolated version race NOT TESTED |
| Bot death/external ZR respawn | PASS: same connection62, pawn337 alive→348 dead→352 alive within Released, fresh route/lease; no new executor |
| Human lifecycle/rebinding | PASS observed death/pawn replacement and fresh pursuit; isolated scheduling not inferred |
| Human disconnect/reconnect | PARTIAL: user reconnected and fresh Human identity/pursuit observed; immediate cleanup window not captured conclusively |
| Round restart | PASS: Released→Preparing, epochs advance, leases/Trail clear |
| Map change | PASS: dust2→mirage, MapEpoch1→3, Unbound/zero bindings/leases/Trails then fresh pursuit |
| Recovery placement/rejection | PASS ordinary-map smoke: placement and hazard/trigger rejection observed; complete ZE geometry NOT TESTED |
| Recovery→request→resume | PASS: slot9 event#696 t170.52 placement/invalidate/current binding/ObserveOnly request; #697 t170.63 resume. Later paired slot3/11 events. Cooldown/coalescing reported; no claim of native awareness efficacy |
| Legacy disabled/single writer | PASS matched source and runtime capability: legacy Trail/teleport/escort/recovery physically removed; LegacyMovementDisabled=True; BotAI native/movement writes0 |
| Exceptions | No Core/Navigation exception observed in these sessions; historical Lab duplicate-consumer exception predates them. Endurance NOT TESTED |
| Whole rollback | PASS stopped-server inventory/hash verification and actual whole native/Core/BotAI/Kzen/shared restore; new Navigation/SDK/helper archived |
| Multiple humans/Trail isolation | Runtime NOT TESTED; per-human models PASS |
| Real doors/elevators/platform waits | NOT TESTED; waiting/progress models PASS |
| Full ZE/no-NAV coverage/long performance | NOT TESTED |
| Native awareness efficacy | NOT TESTED; remains ObserveOnly |

## Fixed regression

Initial runtime exposed distant idle Bots. Stationary Human Waiting incorrectly stopped distant pursuit; sparse idle usercmd fields were rejected early. Navigation now limits near-target Waiting to384 units. Native final authorized mutation may use the existing read-only pawn yaw and materialize command fields only at commit. Regression models added, whole-set redeployed, user confirmed long-distance tracking normal. No new hook/perception write.

## Rollback and promotion

Actual verified rollback roots: `ZEPVE_Backup/v0.3-20261010-091805-7464417`, `v0.3-20261010-093113-5626151`, final `v0.3-20261010-124156-8955250`. Final server stopped, accepted v0.2 matched baseline restored; helper archived. Candidate authority was verified in the test package; main and installed baseline are not silently promoted.

Local evidence retained outside Git: package-2 manifest, commands.jsonl, bot-death.json, reload-cycles.json, observations-resume.json and backup inventories. Raw player names/credentials/logs not published. Helper is not a production dependency.

Native mode preserves authorized Valve commands plus clear-corridor assistance; this is not a new Valve MoveTo pathfinder or full vertical ZE certification. Recovery uses conservative geometry, not map semantics. Physical native hot unload is unsupported by design; replacement needs stopped server. Managed Navigation is hot reloadable.

Review/merge component PRs before suite promotion, then pin accepted component-main revisions. Current pins are exact candidate revisions. No automatic production merge. T tally20/HUD/Weapon/Map/Director remain out of scope.


## Subsequent behavior acceptance supersedes promotion conclusion

The ordinary-map smoke/rollback above remains historical. Latest candidacy, installation state and failure evidence are in [NAVIGATION_BEHAVIOR_ACCEPTANCE.md](NAVIGATION_BEHAVIOR_ACCEPTANCE.md). Later fixes limit near-target Waiting to96 units and normalize movement axes; user confirmed plain pursuit/low boxes. Advanced rotating jumps, barrels and stationary crouch-jumping during Human bunnyhops failed actual package14 acceptance. The current package15 run-up/cooldown correction has bounded-exit runtime evidence but no broad physical traversal acceptance. Current server is running the matched candidate with Recovery disabled; no PR was merged or accepted baseline promoted. Matched rollout merge remains blocked by actual behavior, independently of the passed backend safety architecture.
