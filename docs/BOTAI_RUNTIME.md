# v0.2d BotAI

Baseline: PR #10 merged at `ed480bd500ca01e5ed4da6a61a7391944d38905c` (v0.2c matched runtime). Its deferred acceptance remains deferred. BotAI introduces a new pursuit authority; it does not migrate a legacy target selector.

## Architecture and ownership

`ZEPVE.BotAI.dll` consumes `SuiteRuntime.Current`. Core remains the only identity, lifecycle, round, quota/team and static respawn-policy source. ZombieReborn/CS2Fixes remains the only respawn executor. Legacy Recovery, Trail, HUD and weapons are unchanged.

`IBotAi` / `BotAiRuntime.Current` is a single target-provider reference, not a player registry. `BotTargetBinding` contains BotAI module GUID, full Core bot token, full Core target token and monotonically increasing BindingVersion. Both tokens include Core plugin lifetime, map/round epochs, slot, connection and pawn generations. Hot reload creates a fresh module GUID and fresh assignments; it never resumes old requests. Core reload recreates the work scope and all bindings. Missing/unloaded Core, missing bridge, Unbound/Disabled/Preparing/AwaitingRelease/Closed, dead/changed roles or invalid entities deny pursuit permission. Getters/validation re-resolve both identities immediately; no stale binding is returned while periodic reconciliation catches up.

The only per-slot cache is bounded BotAI target state (64 slots). It stores tokens, observations and deadlines, not controller/pawn objects. Live eligibility comes from Core views. Event hints plus 0.5-second reevaluation run on the server thread; a single owned 0.25-second dispatch timer persists across map changes and is killed on unload. No per-tick entity scan or distance matrix.

## Distribution

Valid assignments stay stable. New bots choose the live CT human with the fewest assigned live zombie bots, with a rotating slot tie-break. Human join/leave or pawn/role changes trigger minimal redistribution until maximum minus minimum assignments is at most one. Excess bindings move deterministically from the highest bot slot. This works for 1–6 humans and does not use Enemy, visibility, engine team tally, distance or hate/threat scores as target authority. It is intentionally geometry-blind; future Navigation decides whether/how to reach the chosen human.

Every removal/rebind invalidates queued binding probes. At execution Core refreshes the bot, and BotAI checks its own module/version, gameplay gate, bot and target tokens/roles. Per-target scopes are consumer-owned; module unload disposes all work. Maximum 16 concurrent probes and Core's existing global work limit apply.

## Native BotController audit

Installed logs identify BotController v0.7.1 / `0ae8f18`. Audited exact source: [0ae8f18fd6872a369cb984e0e95e5a352be092fe](https://github.com/XBribo/CS2-Bot-Controller/tree/0ae8f18fd6872a369cb984e0e95e5a352be092fe). `src/features/controller/BotController.cpp` suppresses Update/Upkeep/LookAngles under All/Aim locks or replay; replay/input/weapon/buy/profile APIs can write native state. Lock tables default false. No Enemy/Alert/IgnoreEnemies/look-around field writes were found in that revision. Source/log version correspondence is evidence, not proof of binary reproducibility.

BotAI neither references its managed API nor calls its native exports/commands. It does not lock, unlock, replay, inject buttons, set angles, alter profiles, buy/switch weapons or override suppression. Existing external locks/replay can prevent Valve reacquisition; observation timeout reports that symptom rather than fighting the owner. No movement or perception writer handoff occurs. Navigation's pinned `14cda1f` consists of design documents, not an active runtime; its gitlink is unchanged. Future Navigation consumes validated bindings and requests reacquisition after route synchronization. It alone may own final movement intent/native goals.

## Awareness/reacquisition

This baseline is **ObserveOnly**, with **zero native writes**. CSS API 1.0.376 schema properties read from the current pawn's `Bot`: Enemy raw handle, IsEnemyVisible, IsAimingAtEnemy, IsAttacking, IsSleeping and AllowActive. Full serial-bearing Enemy handle is compared to the current assigned pawn; Enemy never replaces AssignedTarget. Unavailable schema observations degrade independently of targeting.

Binding/spawn establishes a three-second observation window. `RequestReacquire(binding, reason)` supports RecoveryTeleport/NavigationRequest/Diagnostic; repeated active requests coalesce without extending the deadline. A valid assigned enemy plus visibility/aim/attack ends the window as Succeeded; expiry ends it as TimedOut. These are observed engine outcomes, not proof of an assist's causal effect. After two seconds of lost perception, automatic observation windows may restart only after a ten-second cooldown. No persistent hate table, forced visibility or write bundle. Legacy Recovery is not modified and does not emit this API yet; no automatic post-teleport handshake is claimed.

Wake/alert/ignore-enemies/look-around/Enemy writes remain unimplemented experimental candidates requiring separate field evidence and ownership review in Lab. BotAI does not depend on those unverified writes or a Navigation runtime to function.

## Diagnostics

Server console or `@css/root`:

```text
css_zepve_ai
css_zepve_bot <bot-slot>
css_zepve_ai_events [1..64]
css_zepve_reacquire <bot-slot>
css_zepve_ai_probe <bot-slot> [0.1..60 seconds]
```

Status explains target slot, both connection/pawn generations, BindingVersion/selection reason, module GUID, permission gate, pending work, reacquire reason/deadline, last combat event and engine observation. Recorder holds at most 256 entries, details capped at 160 characters; snapshots at most 64. Combat/perception/target transitions stay in memory; no automatic disk dump or per-tick logging. Only explicit probes produce logger ACCEPT/REJECT lines. No names/SteamIDs needed.

## Build, deployment and rollback

```powershell
./scripts/build-core.ps1 -CounterStrikeSharpApiPath '<installed API DLL>' -IncludeBotAi -OutputDirectory '<fresh stage>'
./scripts/install-core-lifecycle.ps1 -ServerRoot '<CS2 root>' -PackageDirectory '<stage>'
```

The installer requires a clean committed manifest, exact installed API hash and stopped CS2. Optional `-AllowUnmoddedClient` permits only a visible non-dedicated client whose loaded modules can be inspected and contain no addon/MetaMod/CSS/CS2Fixes/BotController module; default remains strict. A dedicated or modded/unknown client is always refused. Deploy all **11 artifacts**: Core + adapted ZRPVE + BotAI DLL/deps/PDB and one shared Abstractions DLL/PDB. Remove private contract copies. Preserve configs/native modules/unrelated plugins. Before mutation the installer backs up previous inventory and hashes. Use that backup's `restore.ps1 -VerifyOnly`, then `restore.ps1` while stopped (client too) for whole-set rollback; a first-install rollback removes the newly added BotAI files. No live ABI replacement or mixed downgrade. The legacy old gameplay writer must remain disabled.

Do not guess numeric plugin IDs: `css_plugins list`, choose the current LOADED BotAI ID, then `css_plugins reload <id>`; manual `unload <id>` then `load ZEPVE.BotAI`. These commands were inspected in installed CSS 1.0.376. Unloaded historical entries can retain names; use the active numeric ID. Core's existing hot/late-load policy still applies.

## Acceptance

127 model/source checks (72 prior + 55 BotAI) pass; tests are not real-server evidence. Runtime evidence is tracked separately in `BOTAI_VERIFICATION.md`. Full ZE compatibility, multi-human geometry, perception efficacy, post-Recovery synchronization, native tally debt and sustained performance remain outside this smoke claim. No BotAI movement is expected before Navigation exists. Stop after v0.2d; do not automatically implement Navigation.
