# Components

`CS2-ZE-PVE` is the main integration and release repository. Production components that are developed independently are referenced here by **exact Git revisions**.

## Current Component

### `ZEPVE-Navigation`

Zombie navigation and movement component for NAV and no-NAV maps.

It provides:

- native navigation integration
- player Trail navigation
- progress/stuck detection
- staged recovery
- movement/backend abstraction

Detailed design lives in the component repository rather than in this integration repository.

## Update Rule

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

## Experimental Repositories

`ZEPVE-Lab` is not a production component and must never be added here as a release dependency.

Experimental results must first become a clean implementation in the production repository that owns the behavior.

## Future Components

Add another production repository only when there is a clear independent responsibility, lifecycle and integration contract.

A separate DLL by itself is not a reason to create or pin another repository.
