# Legacy Baseline

This directory preserves the existing pre-ZEPVE-split plugins as a migration and regression baseline.

The code here is not the target architecture. It is retained so verified behavior can be moved into the new ownership model without losing working gameplay.

## Legacy plugins

| Legacy plugin | Current role | Planned ownership |
| --- | --- | --- |
| `Kzen-ZRPVE` | Main PvE/Zombie runtime baseline | `ZEPVE.Core`, `ZEPVE.BotAI`, `ZEPVE-Navigation`, and limited `ZEPVE.Map` responsibilities after analysis |
| `Kzen-WeaponBalance` | PvE weapon balance, purchase handling, ammo and zombie damage scaling | `ZEPVE-WeaponSystem` |
| `Kzen-ZEAssist` | Countdown, entity/boss health HUD, button guidance and map/entity observation | presentation to `ZEPVE-HUD`; map/entity semantics to `ZEPVE.Map` or the owning suite service |

## Migration rule

Use this directory as the behavioral baseline:

```text
verified legacy behavior
→ identify current active writer
→ implement replacement behind a narrow boundary
→ verify replacement
→ explicitly disable old writer
→ transfer authority
```

Migration-time execution ownership is defined in [`../MIGRATION_AUTHORITY.md`](../MIGRATION_AUTHORITY.md).

Do not run the legacy and replacement executors in parallel for the same state-changing responsibility.

Do not develop new features in `legacy/` unless a temporary fix is required to preserve the baseline during migration.

Do not copy a whole legacy plugin into a new repository and call the migration complete. Mixed responsibilities must be separated by ownership while preserving verified behavior.

## Source layout

`CounterStrikeSharp/plugins/` contains the legacy plugin source projects.

`configs/` contains legacy runtime configuration captured with the baseline. Mutable server-local data, credentials, tokens, generated files, build output, and machine-specific configuration must not be committed here.

## Current migration priority

1. Reproduce the legacy build/dependency/runtime baseline and preserve a rollback target.
2. Document current writers and handoff rules in `MIGRATION_AUTHORITY.md`.
3. Add Core registry/identity/lifecycle invalidation as an observer while legacy remains the gameplay writer.
4. Transfer Core lifecycle authority one responsibility at a time, disabling the corresponding legacy writer in the same handoff.
5. Introduce BotAI `AssignedTarget` / awareness as a new capability after the lifecycle foundation is stable.
6. Integrate Navigation through narrow suite-defined contracts and replace legacy Trail/recovery only after runtime validation.
7. Migrate WeaponSystem and HUD responsibilities without duplicating map/gameplay authority.

The legacy ZRPVE source does not contain an authoritative `AssignedTarget`, and its respawn behavior relies on ZombieReborn/compatibility execution rather than a standalone ZEPVE respawn scheduler. Preserve these facts during migration instead of inventing false one-to-one extractions.
