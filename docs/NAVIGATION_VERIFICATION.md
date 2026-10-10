# v0.3 matched Navigation verification

Source stage: production native backend in ZEPVE-BotController, independent Navigation runtime and matched legacy disable/shared contract. Research closeout: Lab PR#1 merged2b0d2da; suite PR#12 merged236fed2. Implementation branches start at latest main. Lab is neither production build source nor runtime dependency.

Automated evidence: native21/21 (1000 deterministic worker detach races), resident SDK actual native/C# interop14/14, Navigation29/29 model checks, retained Core/BotAI127/127. Release native and managed builds PASS. Model subjects/providers/traces are explicit test doubles and are not CS2 PASS. Recovery/source tests verify old legacy teleport/scheduler/Trail writers absent and config/policy/presentation retained.

Initial actual matched runtime status (update only with observed evidence):

| Item | Status |
| --- | --- |
| Native/shared ABI/plugin loading | NOT TESTED |
| Pursuit / replace / cancel | NOT TESTED |
| Target rebind / bot death-respawn | NOT TESTED |
| Human disconnect / respawn | NOT TESTED |
| Round / map change | NOT TESTED |
| Navigation / BotAI / Core reload | NOT TESTED |
| Competing backend owner / stale BindingVersion native rejection | NOT TESTED |
| Per-human Trail isolation (two real humans) | NOT TESTED |
| Waiting vs blocked / real door-elevator wait | NOT TESTED |
| Genuine Recovery trigger / geometry rejection-placement | NOT TESTED |
| Recovery → ObserveOnly reacquire → pursuit | NOT TESTED |
| Legacy disabled / no duplicate movement writer | NOT TESTED |
| Whole matched runtime rollback | NOT TESTED |

Runtime commands: css_plugins list, meta list, css_zepve_status, css_zepve_ai, css_zepve_nav, css_zepve_nav_bot <live slot>, css_zepve_nav_events, css_zepve_nav_probe_stale <slot>. Use current LOADED IDs for unload/reload. Never swap only one shared/native/DLL. Native/shared SDK replacement requires stopped server; physical native hot unload unsupported by design. Preserve original source/package/native hashes in backup.

No full ZE/no-NAV route, multiple-human runtime isolation, endurance or awareness efficacy is inferred from ordinary-map smoke. T tally20 debt and HUD/Weapon/Map/Director remain out of scope.
