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
→ identify ownership
→ extract behind a narrow boundary
→ verify no regression
→ remove duplicate legacy responsibility only after replacement is proven
```

Do not develop new features in `legacy/` unless a temporary fix is required to preserve the baseline during migration.

Do not copy a whole legacy plugin into a new repository and call the migration complete. Mixed responsibilities must be separated by ownership while preserving verified behavior.

## Source layout

`CounterStrikeSharp/plugins/` contains the legacy plugin source projects.

`configs/` contains legacy runtime configuration captured with the baseline. Mutable server-local data, credentials, tokens, generated files, build output, and machine-specific configuration must not be committed here.

## Current migration priority

1. Stabilize/import the legacy baseline and reproduce its build.
2. Extract Core lifecycle and runtime contexts.
3. Extract BotAI target/awareness ownership.
4. Integrate Navigation through narrow contracts.
5. Migrate WeaponSystem and HUD responsibilities without duplicating map/gameplay authority.
