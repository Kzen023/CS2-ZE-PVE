# GitHub Management

Repository and release rules for the ZEPVE project family. Coding rules belong in `AGENTS.md`.

## Repository Roles

### `CS2-ZE-PVE`

Main product and integration repository. It owns:

- Core runtime and shared abstractions
- `ZEPVE.BotAI`
- ZEPVE-owned map / ZE integration
- legacy migration/regression baseline
- suite configuration and compatibility data
- production-component pinning
- packaging, manifests and releases
- end-user documentation
- cross-component integration validation

### `ZEPVE-Navigation`

Independent production component for zombie navigation, Trail handling, movement and recovery.

The main suite consumes an exact tested Git revision rather than automatically following the latest component branch.

### `ZEPVE-HUD`

Independent migration target for player-facing presentation.

It owns HUD rendering and view-state presentation only. Map/entity discovery, boss semantics and gameplay authority remain in their owning suite modules and are exposed to HUD through explicit contracts.

The repository is not considered a pinned production dependency until its build, API boundary and suite integration are verified.

### `ZEPVE-WeaponSystem`

Independent migration target for PvE weapon balance and purchase behavior.

It owns weapon configuration, ammo/magazine tuning, purchase handling, aliases and weapon-specific damage policy.

The current repository slug is `ZEPVE-WeponSystem`; use `ZEPVE-WeaponSystem` as the component/code name and correct the repository slug when convenient.

The repository is not considered a pinned production dependency until its build, API boundary and suite integration are verified.

### `ZEPVE-Lab`

Experimental workspace for isolated Bot, movement, native API and engine-behavior tests.

Lab is not a production dependency and is never shipped as part of a ZEPVE release.

### Future repositories

Create another production repository only when a component has a clear independent responsibility and lifecycle. A separate DLL alone is not a reason to create another repository.

## Legacy Baseline

Pre-split plugins are preserved under `legacy/` in `CS2-ZE-PVE` so working behavior remains auditable during migration.

Current migration sources:

```text
Kzen-ZRPVE          -> Core / BotAI / Navigation / Map after ownership analysis
Kzen-WeaponBalance  -> ZEPVE-WeaponSystem
Kzen-ZEAssist       -> presentation to ZEPVE-HUD; map/entity semantics to ZEPVE.Map/owner
```

Do not treat `legacy/` as a second production tree. Once a responsibility has been migrated and verified, development continues in the new owner.

## Component Updates

Production-component changes and suite integration are separate histories:

```text
component branch / PR
→ component verification
→ merge component main
→ suite integration branch
→ update pinned component revision
→ suite validation
→ integration PR
→ merge
```

Do not auto-merge component bumps.

A component-bump PR should state:

- previous revision
- new revision
- reason for update
- API/config changes
- dependency changes
- runtime tests performed
- rollback target

`.github/dependabot.yml` may propose submodule updates, but those proposals are only update candidates and still require review.

Creating a component repository does not automatically add it as a submodule or release dependency. Pin it only after the production-integration criteria are met.

## Direct `main` vs Pull Request

Lightweight repository content may be updated directly on `main` when the owner allows it:

- README and project pages
- documentation
- Issue / PR templates
- CODEOWNERS and similar collaboration metadata
- wording or formatting changes with no runtime/build effect

Use a branch and PR for changes that can affect the product:

- source/runtime behavior
- build and package output
- config behavior or schema
- dependencies
- component/submodule revisions
- release contents
- public APIs/capabilities

Preferred branches:

```text
agent/<task>
feat/<task>
fix/<task>
refactor/<task>
experiment/<task>
```

Keep branches short-lived. No permanent `develop` branch unless a concrete need appears.

## Commits

Keep commits scoped and descriptive where practical.

Examples:

```text
feat(core): add bot pool lifecycle
fix(map): stop respawns after nuke signal
chore(components): bump ZEPVE-Navigation
build(release): add package staging
```

Do not commit credentials, crash dumps, machine-local configuration, generated release binaries or temporary debugging artifacts.

## Versioning

The public product uses one suite version:

```text
ZEPVE v0.x.x
```

Independent production components may also use their own SemVer tags.

Capability/API versions are separate from package versions:

```text
zepve:core:v1
zepve:botai:v1
zepve:navigation:v1
zepve:map:v1
zepve:hud:v1
zepve:weapons:v1
```

Breaking API changes should introduce a new capability version rather than silently changing an existing contract.

## Source and Release Layout

Source layout should remain developer-friendly:

```text
CS2-ZE-PVE/
├─ src/
├─ legacy/
├─ components/
├─ configs/
├─ maps/
├─ integrations/
├─ release/
├─ tools/
├─ tests/
└─ .github/
```

`legacy/` is a temporary long-lived migration baseline, not the final runtime layout.

Release staging should be generated and shaped for server deployment:

```text
release/stage/
└─ game/
   └─ csgo/
      ├─ addons/
      └─ cfg/
```

Do not maintain duplicate release binaries manually in source control.

## Release Contract

Players should normally install one ZEPVE suite release rather than assembling component repositories themselves.

A release should record:

- suite version and Git commit
- pinned first-party component revisions
- tested CS2 build
- Metamod version
- CounterStrikeSharp version
- CS2Fixes version/commit when used
- native dependency versions/platforms when applicable

Suggested assets once builds exist:

```text
CS2-ZE-PVE-v0.x.x-Minimal.zip
CS2-ZE-PVE-v0.x.x-Full.zip
manifest.json
```

Experimental Lab output is never included in release packages.

## Promotion from Lab

A successful experiment provides evidence, not production code.

```text
experiment result
→ production design decision
→ clean implementation in owning production repository
→ PR and runtime verification
→ optional suite component bump
```

Do not copy an entire experimental PoC unchanged into production.

## Third-Party Dependencies

Use narrow adapters around external APIs where practical. Prefer upstream fixes or contributions when missing behavior clearly belongs to an external dependency.

Maintain a fork only when there is a concrete reason, and document why it exists and how it diverges.

Required notices, licenses and source obligations must be preserved whenever third-party code or licensing terms require them.

## Rollback

Prefer revert commits/PRs over rewriting shared history.

For released versions, roll back to a previously tested tag/release instead of reconstructing an old environment manually.

Do not force-push `main` as a normal recovery method.

## Documentation Roles

```text
README.md / README.zh-CN.md -> project introduction and user entry point
AGENTS.md                    -> GPT/Codex execution rules
GITHUB_MANAGEMENT.md         -> repository/release workflow
ROADMAP.md                   -> milestones
DECISIONS.md                 -> durable architecture decisions
CONTRIBUTING.md              -> contributor guide
components/README.md         -> component integration rules
legacy/README.md             -> legacy baseline and migration map
```

Keep implementation detail out of the root README when a dedicated design document is more appropriate.
