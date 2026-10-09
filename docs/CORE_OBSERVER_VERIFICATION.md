# v0.2b verification

Date: 2026-10-09. Source base: `89b5c0c0722d630ad5b9ce75b1234414d7f873d3`. Branch: `agent/core-registry-lifecycle`.

Legacy remains the only gameplay writer; no authority handoff occurs in this PR. `MIGRATION_AUTHORITY.md` and all legacy source/config files are unchanged. No component gitlink changes. Source inspection found no mismatch with the current-writer matrix; live authority observation is **NOT TESTED**. Existing ZombieReborn/CS2Fixes respawn execution is preserved.

## Local evidence

- Release Core + Abstractions build: **PASS**, zero warnings/errors.
- SDK: 10.0.401; target: net10.0.
- Installed API: CounterStrikeSharp 1.0.376, SHA256 `E81F05D960C37EE623D552733FC919E206C8C1D16157886342D06A2F4D0DAB17`.
- Deterministic registry/lifetime model tests: **PASS, 30/30** via `dotnet run --project tests/ZEPVE.Core.Tests -c Release -p:CounterStrikeSharpApiPath=<server API path>` (no native engine calls).
- Staging: `scripts/build-core.ps1` creates a Core-only install tree and exact artifact hashes; no live deployment or legacy DLL changes.

Tests exercise source re-reading at callback execution; disconnect with a still-visible controller; slot reuse even with identical handles; replacement without disconnect; missing source observations; controller serial/UserId changes; invalid entities; pawn replacement; invalid pawn; immediate/observed death; same-handle spawn/respawn; obsolete life events; round end/prestart/restart/start; map end/start; unload/reload; late-load model bootstrap; single immutable role views; current role resolution; invalid/default tokens and slots; stable observations; 100 successive slot reuses with bounded storage.

## Runtime matrix

The available CS2 process is a client (`-steam -insecure`). No dedicated-server command connection or live test session was established. No server restart, map/gameplay changes or plugin load was performed for this slice.

| Scenario | Model result | Real CS2 runtime result |
| --- | --- | --- |
| Core plugin load / dependency resolution | Release build PASS | NOT TESTED |
| Connect / disconnect | PASS | NOT TESTED |
| Slot reuse | PASS | NOT TESTED |
| Spawn / death | PASS | NOT TESTED |
| Respawn / pawn replacement | PASS | NOT TESTED |
| Round restart / round end | PASS | NOT TESTED |
| Map change / cleanup | PASS | NOT TESTED |
| Hot / late load | Registry bootstrap PASS | NOT TESTED |
| Stale delayed callback rejection | Registry guard PASS | NOT TESTED |
| Native timer / subscription cleanup | Source reviewed | NOT TESTED |
| css_zepve_status / css_zepve_probe | Compiles | NOT TESTED |
| Legacy coexistence / gameplay parity / recovery | No gameplay writer paths added | NOT TESTED |

## Server test procedure

1. Keep the accepted legacy plugins/configs enabled; load only the staged Core observer alongside them. Check load logs and `css_zepve_status`.
2. Connect a human and observe CT classification, connect/disconnect, slot reuse, CT Bot versus T Bot classifications and disconnected tombstones.
3. On a live player, queue `css_zepve_probe <slot> 5`; unchanged identity should log ACCEPT. Repeat with disconnect/reconnect, death, respawn/pawn replacement, round restart/end before execution; stale work should log REJECT. Core performs no action in either branch.
4. Queue a probe, change map/unload Core; pending timers should cancel. Load/hot reload mid-map and verify existing players appear without waiting for map start; new plugin lifetime and epochs must reject previous tokens in the model.
5. Verify quota, infection timing, ZombieReborn respawn and legacy recovery/HUD/weapons still follow the accepted baseline. Record actual logs/results before marking any runtime row PASS.

## Limitations and rollback

Event delivery/order and native handle/connected/pawn ownership semantics are build-verified only. Reconciliation latency is at most one normal 0.5-second interval for continuously observable changes; callbacks refresh synchronously. Entire unobserved same-handle life cycles cannot be inferred. Hot/late-load round phase is deliberately unknown and initial epochs provide observation validity only. Registry slots are limited to 0–63 and state is server-thread-only. Probe map/unload cases cancel timers rather than emitting a later REJECT log.

Legacy callbacks are not retrofitted; their existing slot/lifetime risks and recovery wandering remain. The historical source-vs-deployed uncertainty in the import audit is superseded for the local accepted pre-Core baseline by the user's acceptance and build/deployment records, not by this Core change. No authority handoff is justified by model-only results.

Rollback is unloading/removing just Core's plugin folder. Original legacy DLLs/configs and `ZEPVE_Backup/pre-core-2026-10-09` remain untouched.
