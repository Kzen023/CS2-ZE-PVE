# Architecture Decisions

Only record decisions future contributors or Codex sessions might otherwise reopen.

- **ADR-001 — ZE first.** ZEPVE is a ZE-first PvE runtime. Standard-map Survival/Horde support is secondary.
- **ADR-002 — Monorepo first.** Core modules stay in one repository until a component has a stable API and independent lifecycle.
- **ADR-003 — Core does not own Trail data.** Trail recording, movement and recovery belong to Navigation.
- **ADR-004 — Navigation order.** Prefer `Valve NAV -> TrailDriver -> Recovery`.
- **ADR-005 — Checkpoints are fallback.** Recorded human trails are the default low-maintenance no-NAV mechanism.
- **ADR-006 — Teleport is recovery.** Do not use continuous teleport as normal locomotion.
- **ADR-007 — CS2Fixes remains a compatibility layer.** Keep mature ZE map semantics where useful; ZEPVE owns PvE rules and Bot lifecycle.
- **ADR-008 — One authority per state.** Do not let ZEPVE and another system independently own the same respawn/movement/round state.
- **ADR-009 — Optional native movement.** Try existing BotController-style APIs before creating a custom MovementBridge.
- **ADR-010 — User docs are bilingual.** English and Simplified Chinese are first-class user-facing documentation.
- **ADR-011 — Developer docs stay compact.** Avoid duplicated translations for Codex/internal docs to reduce maintenance and context usage.
- **ADR-012 — No premature scripting/database systems.** Add complex subsystems only after a concrete ZE/PvE need exists.
