# GitHub Management

Short repository-management guide for ZEPVE. Coding rules belong in `AGENTS.md`.

## Repository Strategy

Use a **monorepo first**.

```text
CS2-ZE-PVE/
├─ AGENTS.md
├─ GITHUB_MANAGEMENT.md
├─ README.md
├─ README.zh-CN.md
├─ ROADMAP.md
├─ DECISIONS.md
├─ CHANGELOG.md
├─ CONTRIBUTING.md
├─ src/
├─ configs/
├─ maps/
├─ docs/
├─ tests/
└─ .github/
```

Do not split a module merely because it becomes a separate DLL.
Only split repositories when a component has a stable public API, independent release lifecycle and clear value outside ZEPVE.

Likely future standalone projects:

```text
ZEPVE-Navigation
ZEPVE-MovementBridge
ZEPVE-CS2Fixes
```

## Branches

```text
main
├─ feat/<name>
├─ fix/<name>
├─ refactor/<name>
└─ experiment/<name>
```

`main` should compile. Use short-lived branches and PRs. No permanent `develop` branch for now.

## Commits

One logical change per commit.

```text
feat(nav): add trail segment detection
fix(core): prevent bot double respawn
docs(repo): update contributor workflow
```

Never commit credentials, logs, crash dumps, temporary debug output or machine-local configs.

## Versioning

Use one public suite version:

```text
ZEPVE v0.x.x
```

Capability/API versions remain independent:

```text
zepve:core:v1
zepve:navigation:v1
zepve:hud:v1
```

## Releases

Recommended assets:

```text
CS2-ZE-PVE-v0.x.x-Minimal.zip
CS2-ZE-PVE-v0.x.x-Full.zip
```

Minimal: Abstractions + Core + Navigation + Map.
Full: Minimal + stable optional modules.

Release packages should be install-ready for `game/csgo` and include a manifest with the ZEPVE commit plus tested CounterStrikeSharp / Metamod / CS2Fixes versions.

## CI

```text
restore -> build Release -> tests if available -> validate package
```

Release tags:

```text
tag -> build -> package -> manifest -> ZIP -> GitHub Release
```

## Issues

Useful labels:

```text
type:bug
type:feature
type:map
type:performance
type:limitation
area:core
area:navigation
area:map
area:cs2fixes
nav:no-nav
compat:cs2-update
status:needs-repro
```

Map issues should include map/workshop ID, ZEPVE/CSS/CS2Fixes versions, NAV status, player count, stage/location and relevant logs.

## Documentation Roles

```text
README.md / README.zh-CN.md -> users
AGENTS.md                    -> GPT/Codex coding rules
GITHUB_MANAGEMENT.md         -> repository/release workflow
ROADMAP.md                   -> current and next work
DECISIONS.md                 -> important architecture decisions
CONTRIBUTING.md              -> human contributors
docs/                        -> detailed technical documentation
```

Avoid duplicating the same rules across files.

## Codex Startup

Read only:

```text
1. AGENTS.md
2. GITHUB_MANAGEMENT.md
3. ROADMAP.md
4. DECISIONS.md
```

Then read only task-relevant source files. This is intentional to keep context/token usage low.

## External Contributors

```text
External Contributor -> PR -> Regular Contributor -> Module Maintainer
```

Keep extension interfaces small so Bot/Nav developers can contribute without understanding the whole project.

## Primary Rule

Optimize for low context usage, reproducible builds, clear module ownership, simple rollback and easy external contribution. Do not add process complexity without a real maintenance need.
