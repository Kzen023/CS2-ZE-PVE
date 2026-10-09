# Components

`CS2-ZE-PVE` is the main integration and release repository. Production components that are developed independently are consumed only after their ownership boundary and runtime behavior have been verified.

## Pinned production component

### `ZEPVE-Navigation`

Zombie navigation and movement component for NAV and no-NAV maps.

It provides:

- native navigation integration
- player Trail navigation
- progress/stuck detection
- staged recovery
- movement/backend abstraction

Detailed design lives in the component repository rather than in this integration repository.

## Independent migration targets

The following repositories exist, but are **not yet pinned release dependencies**:

### `ZEPVE-HUD`

Owns player-facing presentation only. It may consume Core/Map/Boss state through narrow suite APIs, but must not independently own map discovery or gameplay authority.

Legacy migration source: `legacy/CounterStrikeSharp/plugins/Kzen-ZEAssist`.

### `ZEPVE-WeaponSystem`

Owns PvE weapon balance, ammo/magazine behavior, purchase handling and weapon-specific damage policy.

Legacy migration source: `legacy/CounterStrikeSharp/plugins/Kzen-WeaponBalance`.

The current GitHub repository is named `ZEPVE-WeponSystem`; the intended component name is `ZEPVE-WeaponSystem`.

These repositories become production components only after they have a reproducible build, stable ownership boundary, tested integration contract and an explicit suite pin/update path.

## Update rule

A component revision is updated only after it has been reviewed and verified in its own repository.

```text
component change
→ component verification
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

## Experimental repositories

`ZEPVE-Lab` is not a production component and must never be added here as a release dependency.

Experimental results must first become a clean implementation in the production repository that owns the behavior.

## Repository rule

A separate DLL or a newly created repository does not automatically make something a production component.

Production integration requires a clear independent responsibility, lifecycle, test value and explicit pin/update path into `CS2-ZE-PVE`.
