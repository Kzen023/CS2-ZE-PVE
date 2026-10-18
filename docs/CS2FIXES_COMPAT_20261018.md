# CS2Fixes host compatibility — 2026-10-18

The user authorized updating CS2Fixes after an isolated server had no infection executor. This is an external host dependency update, not a new ZEPVE movement/respawn implementation.

## Stable release reproduced failure

Official latest release [v2.0](https://github.com/Source2ZE/CS2Fixes/releases/tag/v2.0), published 2026-09-30, is source `bb3be3449c122bebf2d74e85b61987de408ac339`. Its Windows archive SHA256 is `6ac6c29e0acb4801eb79f8abadc23fa7004a38f0b0a475a7012a2d9163e5e394`.

The preexisting DLL and actual `gamedata/cs2fixes.jsonc` already matched this release. `cs2fixes.games.txt` is an additional preexisting legacy file, not the current JSON gamedata. Reinstalling the official release preserved the custom `cfg/cs2fixes/cs2fixes.cfg` (including `zr_enable 1`) and verified the other 47 release files. An actual stopped-server install/restore/reinstall verified all 48 entries. This was a reinstall, not a native version upgrade.

Stable still crashed with `0xc0000005` at `cs2fixes.dll + 0x11e2b5`. The earlier A/B reproduced that same fault with BotController and all managed gameplay plugins absent. Setting existing `sv_kick_players_with_cooldown 2` / `cs2f_fix_game_bans 0` did not resolve startup; that attempted workaround is not applied in the current server.

## Upstream development compatibility package

[Upstream PR 471](https://github.com/Source2ZE/CS2Fixes/pull/471) explicitly updates SDK/gamedata and the whitelist implementation for the 2026-10-14 CS2 update. Its head is `68504a135fae431060992f32c58249b8b1840ce5`. We used the official successful [CI run 38845338316](https://github.com/Source2ZE/CS2Fixes/actions/runs/38845338316), Windows artifact `12022274138`.

Because this is a `pull_request` workflow, default checkout builds the synthetic merge commit **`b5a33f579743f7fa73e54648ace33746774e93d5`**, not the PR head commit. Both commits have verified identical tree `0b45ee7fc9011bb26effabdcd43afe6761a1111d`. The runtime identifies `v2.0-8-b5a33f5`. Do not attribute this DLL directly to the head commit without explaining the merge.

- Artifact archive SHA256, verified against GitHub artifact metadata: `a7f8b7738df060c13418d9a384ea509bc7edda7ff49ca2fb1c7d9311b7d70c2e`.
- DLL SHA256: `a90ced49b6f35b6888bbf89db70bdc74820ee3e33b89c446e1b0ca450b8ae53b`.
- JSON gamedata SHA256: `f2bf42a66e1a138d582e52e91fb2edabf8725f63bafc2ce706838ab5d95e604d`.
- All 47 noncustom release files verified; existing custom CS2Fixes configuration preserved.
- Actual stopped-server install/restore/reinstall of all 48 entries PASS. Original snapshots and a rollback entry point are under the same ZEPVE rollback root, `cs2fixes-dev-68504a1/` and `cs2fixes-official-v2.0/`.

This package is an upstream **development dependency**, not a published stable release or a local reproducible CS2Fixes build. Source/workflow/artifact metadata and digest establish its upstream CI origin; no claim is made that all upstream build dependencies were independently reproduced locally.

The dedicated server loads `de_dust2` with CS2Fixes/ZR, CSS 1.0.376, and the complete d540ab8 ZEPVE candidate. RCON verifies `zr_enable=true`, one Human/one ZombieBot, Navigation owner count 1, active command application and `LegacyMovementDisabled=True`. The user confirms infection and Bot generation work, but reports flat/ramp jumping, intermittent pursuit, wall sticking and almost no initial aggression. Therefore CS2Fixes load/infection is restored while Navigation gameplay acceptance is **FAIL**. Clean client restart, two-Human isolation and real-map waits remain incomplete; the client was observed still loading server modules during this test.

See [install/restore evidence](evidence/v03-20261018/dev-install-evidence.json), [stable crash](evidence/v03-20261018/official-crash.json), [failed workaround](evidence/v03-20261018/workaround-crash.json), and the separate [isolated shutdown crash](evidence/v03-20261018/isolated-shutdown-crash.json).
