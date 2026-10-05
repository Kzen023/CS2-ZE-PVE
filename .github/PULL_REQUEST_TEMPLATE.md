## Summary

Describe the change and why it is needed.

## Area

- [ ] Core / runtime
- [ ] Component integration / version pin
- [ ] Map / ZE compatibility
- [ ] HUD / Weapons / Director
- [ ] Build / Packaging / CI
- [ ] Documentation

## Component bump

Complete this section when a pinned production component changes.

- Component:
- Previous commit / version:
- New commit / version:
- Reason for update:
- API / capability changes:
- Config changes:
- Native / external dependency changes:

Do not auto-merge component bumps just because the component repository has a newer `main`.

## Verification

Check only what was actually verified:

- [ ] Build passed
- [ ] Component tests passed
- [ ] Plugin loaded
- [ ] Feature worked in runtime
- [ ] Suite integration worked
- [ ] Tested on a real map
- [ ] NAV map tested
- [ ] No-NAV map tested

## Environment

- ZEPVE suite commit/version:
- CS2 build:
- Metamod:
- CounterStrikeSharp:
- CS2Fixes:
- Map / Workshop ID:
- Human / Bot count:

## Risk

- [ ] No runtime behavior change
- [ ] Gameplay behavior change
- [ ] Performance-sensitive change
- [ ] Engine/native dependency
- [ ] Dependency/component version change
- [ ] Breaking API/config change
- [ ] Release/package content change

## Rollback

State the simplest rollback path. For a component bump, include the previous pinned commit.

## Notes

Mention known limitations, follow-up work, or anything reviewers should verify.
