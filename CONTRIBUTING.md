# Contributing to ZEPVE

Thanks for helping improve CS2 Zombie Escape PvE.

## Before You Start

Read:

```text
AGENTS.md
GITHUB_MANAGEMENT.md
ROADMAP.md
DECISIONS.md
```

Then inspect only the module relevant to your change.

## Contribution Flow

```text
Issue / idea
→ short-lived branch
→ focused change
→ build/test
→ review diff
→ Pull Request
→ CI + review
→ merge
```

Suggested branch names:

```text
feat/<name>
fix/<name>
refactor/<name>
experiment/<name>
```

## Scope

Prefer small changes with clear ownership.

Do not mix unrelated refactors with behavior changes.

Do not introduce new frameworks, databases, scripting systems or native hooks without a demonstrated project need.

## Bot / Navigation Contributions

External Bot/Nav developers should be able to contribute through narrow adapters or interfaces without understanding the whole project.

Experimental engine behavior belongs in a PoC first. Document:

```text
Goal
CS2 build
CounterStrikeSharp version
Dependencies
Expected result
Observed result
PASS / FAIL / INCONCLUSIVE
```

Do not move experimental code directly into production without runtime verification.

## Testing

State what was actually verified:

```text
build passed
plugin loaded
feature worked
real map tested
```

For navigation changes, include whether testing used NAV/no-NAV maps and approximate player/Bot count.

## Documentation

User-visible changes should update English and Simplified Chinese documentation when practical.

Code, API names, commits and technical discussion should use English so international CS2 developers can participate.

## Third-Party Code

Do not copy third-party code without checking its license and preserving required notices.

Prefer upstream issues/PRs over maintaining unnecessary private forks.
