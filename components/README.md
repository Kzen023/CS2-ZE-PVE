# Components

`CS2-ZE-PVE` is the main integration and release repository. Independent repositories are consumed only after their ownership boundary and runtime behavior have been verified.

## Registered integration component

### `ZEPVE-Navigation`

The suite currently contains a pinned Navigation gitlink/design revision so component integration can be developed against an exact reference.

This pin is **not yet equivalent to a runtime-verified production Navigation implementation**. The component is still establishing its first executable baseline.

Planned responsibilities include:

- native navigation integration
- per-human Runtime Trail navigation
- progress/stuck detection
- staged recovery
- movement/backend abstraction

Detailed design lives in the component repository rather than in this integration repository.

A future suite bump should explicitly identify the first revision that has been built, integrated and runtime verified as production-ready.

## Independent migration targets

The following repositories exist, but are **not yet pinned release dependencies**:

### `ZEPVE-HUD`

Owns player-facing presentation only. It may consume Core/Map/Boss state through narrow suite APIs, but must not independently own map discovery or gameplay authority.

Legacy migration source: `legacy/CounterStrikeSharp/plugins/Kzen-ZEAssist`.

### `ZEPVE-WeaponSystem`

Owns PvE weapon balance, ammo/magazine behavior, purchase handling and weapon-specific damage policy.

Legacy migration source: `legacy/CounterStrikeSharp/plugins/Kzen-WeaponBalance`.

Repository: `https://github.com/Kzen023/ZEPVE-WeaponSystem`.

These repositories become production components only after they have a reproducible build, stable ownership boundary, tested integration contract and an explicit suite pin/update path.

## Readiness levels

Use these terms consistently:

```text
repository exists
→ migration/design target
→ registered integration revision
→ build-verified revision
→ suite-integrated revision
→ runtime-verified production revision
```

Do not call a component production-ready only because its repository or gitlink exists.

## Update rule

A production component revision is updated only after it has been reviewed and verified in its own repository.

```text
component change
→ component verification
→ merge component main
→ suite branch
→ update pinned revision
→ suite validation
→ suite PR
```

The suite PR should record:

- previous revision
- new revision
- reason for the bump
- API/config/dependency changes
- runtime verification performed
- rollback target

Do not automatically consume an arbitrary latest component build.

Release tooling must initialize the exact suite gitlink and must not use a remote-tracking submodule update to choose a component revision.

## Experimental repositories

`ZEPVE-Lab` is not a production component and must never be added here as a release dependency.

Experimental results must first become a clean implementation in the production repository that owns the behavior.

## Repository rule

A separate DLL or a newly created repository does not automatically make something a production component.

Production integration requires a clear independent responsibility, lifecycle, test value and explicit pin/update path into `CS2-ZE-PVE`.
