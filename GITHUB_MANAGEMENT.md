# GitHub Management

Repository and release rules for the ZEPVE project family. Coding rules belong in `AGENTS.md`.

## Reference Model

ZEPVE borrows the useful parts of the **CS2-Bot-Improver** repository model:

- one integration repository is the user-facing entry point
- independently useful production components live in separate repositories
- the integration repository references production components as Git submodules
- Git pins the exact component commit used by the suite
- component updates are explicit, reviewable integration changes
- releases are install-ready packages rather than a list of repositories users must assemble manually

ZEPVE does **not** copy every implementation detail. Components are split only when responsibility and lifecycle are genuinely independent.

## Repository Roles

### `CS2-ZE-PVE`

The suite/integration repository. It owns:

- `ZEPVE.Abstractions`
- Core player/Bot lifecycle and round state
- map/ZE integration owned by ZEPVE
- suite configuration and compatibility data
- component pinning
- packaging and release manifests
- end-user documentation
- cross-component integration tests

### `ZEPVE-Navigation`

Independent production component. It owns:

- Trail recording and segmentation
- Valve NAV integration
- TrailDriver
- target binding
- progress/stuck detection
- movement/recovery pipeline
- BotController/BotNav adapters when appropriate

The suite consumes it through a pinned submodule revision.

### `ZEPVE-Lab`

Experimental workspace only. It owns PoCs for UserCmd, native hooks, signatures, Bot takeover, unusual movement and engine behavior.

**Lab is never a release dependency and must never be added as a production submodule.**

### Future repositories

Create only when justified:

- `ZEPVE-CS2Fixes`: focused compatibility fork, separate from the main source tree
- `ZEPVE-MovementBridge`: native bridge only if existing Bot APIs cannot satisfy production requirements

Do not create separate repositories merely because a C# assembly is a separate DLL.

## Component Pinning

A production component update follows this flow:

```text
component branch
→ component PR
→ component tests/runtime verification
→ merge component main
→ integration branch in CS2-ZE-PVE
→ bump pinned submodule commit
→ suite validation
→ integration PR
→ merge
```

Never make the suite silently follow a component's moving `main` branch during builds or packaging.

The `.gitmodules` file describes where a component comes from; the Git tree records the exact commit used by the suite.

## Dependency Updates

`.github/dependabot.yml` may propose Git submodule updates. Treat these as **update candidates**, not trusted automatic upgrades.

Do not auto-merge component bumps. A bump must be reviewed for:

- API/capability compatibility
- config changes
- runtime behavior
- native/signature requirements
- CS2 update compatibility
- release manifest accuracy

## Branches and Direct-Main Rules

Two classes of change are treated differently.

### Lightweight repository/documentation changes

The owner may allow direct `main` updates for:

- README and user-facing project pages
- repository-management documentation
- Issue / PR templates
- CODEOWNERS and similar lightweight collaboration metadata
- wording/formatting changes that do not affect runtime, build output or dependency versions

These still create normal Git commits and remain reversible through commit history.

### Production-impacting changes

Use a branch + PR for:

- source code and runtime behavior
- build scripts/workflows that affect produced binaries/packages
- config schema/behavior changes
- dependency upgrades
- Git submodule additions or component commit bumps
- release contents / packaging behavior
- public API/capability changes

Normal flow:

```text
main
↑
PR
↑
agent/<task> | feat/<task> | fix/<task> | refactor/<task>
```

Keep branches short-lived. No permanent `develop` branch for now. Never force-push `main` as a normal rollback mechanism.

## Commits

One logical change per commit where practical.

Examples:

```text
feat(core): add bot pool lifecycle
fix(map): stop respawns after nuke signal
chore(components): bump ZEPVE-Navigation
build(release): add install-ready package staging
```

For component bump commits, state the old/new component revision and reason in the PR.

## Versioning

The public product uses one suite version:

```text
ZEPVE v0.x.x
```

Independent production components may also have their own SemVer tags:

```text
ZEPVE-Navigation v0.x.x
```

Capability/API versions remain independent of package versions:

```text
zepve:core:v1
zepve:navigation:v1
zepve:map:v1
```

Breaking API changes add a new capability version rather than silently changing an existing version.

## Source vs Release Layout

Recommended source shape:

```text
CS2-ZE-PVE/
├─ src/
├─ components/
│  └─ ZEPVE-Navigation/      # submodule
├─ configs/
├─ maps/
├─ integrations/
├─ release/
├─ tools/
├─ tests/
└─ .github/
```

Recommended generated release staging shape:

```text
release/stage/
└─ game/
   └─ csgo/
      ├─ addons/
      └─ cfg/
```

Do not hand-maintain duplicate release binaries in source control.

## Release Contract

Players should download one suite release.

A release must record:

- suite version
- suite Git commit
- every pinned first-party component commit/version
- tested CS2 build
- Metamod version
- CounterStrikeSharp version
- CS2Fixes version/commit when used
- native dependency versions/platform when applicable

Suggested assets once builds exist:

```text
CS2-ZE-PVE-v0.x.x-Minimal.zip
CS2-ZE-PVE-v0.x.x-Full.zip
manifest.json
```

`Minimal`: required runtime components.

`Full`: Minimal plus stable optional modules. Experimental Lab outputs never belong in either package.

## Component Promotion Rules

A repository becomes a production component only when it has:

1. a clear responsibility boundary
2. a stable integration contract
3. independent tests/runtime verification
4. a reason to be developed or reused separately
5. a documented update path into the suite

Until then, keep the code in the repository that owns the product behavior.

## Experimental Promotion

```text
ZEPVE-Lab experiment
→ PASS/FAIL/INCONCLUSIVE evidence
→ production design decision
→ clean implementation in owning production repo
→ PR and runtime test
→ optional suite component bump PR
```

Do not promote Lab code by copying an entire PoC unchanged into production.

## Upstream First

When a missing capability belongs to BotController, BotNav, CounterStrikeSharp, CS2Fixes or another dependency:

1. prefer an upstream issue/PR
2. use a narrow adapter in ZEPVE
3. maintain a fork only when necessary
4. document why the fork exists and how it diverges

## Rollback

If a merged production change breaks the project, prefer a **revert commit/PR**. Do not rewrite shared history to hide mistakes.

Release rollback should use a previously tested tag/release rather than reconstructing an old environment manually.

## Documentation Roles

```text
README.md / README.zh-CN.md -> users and project-family overview
AGENTS.md                    -> GPT/Codex execution rules
GITHUB_MANAGEMENT.md         -> integration/release workflow
ROADMAP.md                   -> milestone order
DECISIONS.md                 -> durable architecture/repository decisions
CONTRIBUTING.md              -> human contributors
components/README.md         -> component pin/update rules
```

## Primary Rule

Keep **component development**, **suite integration**, and **experimentation** as three distinct histories. That separation is the main reason for the multi-repository model.
