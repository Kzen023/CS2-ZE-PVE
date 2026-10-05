# Components

`CS2-ZE-PVE` is the integration/release repository. Independently developed production components are referenced here by **pinned Git revisions**.

## Current production component

- `ZEPVE-Navigation`
  - source: https://github.com/kzen1023/ZEPVE-Navigation
  - integration path: `components/ZEPVE-Navigation/`
  - design source: `NAVIGATION_DESIGN.md` in the component repository
  - runtime model: `ValveNavDriver -> TrailDriver -> RecoveryDriver`
  - optional adapter direction: BotNav-style native path access + BotController-style UserCmd movement execution
  - stuck/recovery model: multi-signal progress monitoring + staged recovery rather than checkpoint-first navigation

`ZEPVE-Navigation` is explicitly **not** a per-map checkpoint navigation system. No-NAV routing is based on recorded human Trails.

## Update rule

Do not make the suite automatically consume an arbitrary latest component build.

Use:

```text
component change
→ component PR/test
→ component main
→ suite branch
→ update submodule commit
→ suite validation
→ suite PR
```

The suite PR should state:

- previous component commit/version
- new component commit/version
- reason for the bump
- API/config changes
- runtime tests performed
- maps/NAV conditions tested where relevant

## Experimental repositories

`ZEPVE-Lab` is not a production component and must never be added here as a submodule or release dependency.

Successful Lab experiments must be redesigned into the owning production repository first.

## Future components

Add only when there is a clear independent lifecycle and integration contract. Possible examples:

- `ZEPVE-CS2Fixes`
- `ZEPVE-MovementBridge`

Do not create or pin a repository merely because a DLL exists.
