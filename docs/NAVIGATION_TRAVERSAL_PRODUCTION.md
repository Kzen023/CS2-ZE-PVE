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
