# Architecture Decisions

Only record decisions future contributors or Codex sessions might otherwise reopen.

- **ADR-001 — ZE first.** ZEPVE is a ZE-first PvE runtime. Standard-map Survival/Horde support is secondary.
- **ADR-002 — Suite integration repository.** `CS2-ZE-PVE` is the user-facing integration/release repository; independently useful production components may live in separate repositories and are pinned here.
- **ADR-003 — Core does not own Trail data.** Trail recording, movement and recovery belong to Navigation.
- **ADR-004 — Navigation order.** Prefer `Valve NAV -> TrailDriver -> Recovery`.
- **ADR-005 — Checkpoints are fallback.** Recorded human trails are the default low-maintenance no-NAV mechanism.
- **ADR-006 — Teleport is recovery.** Do not use continuous teleport as normal locomotion.
- **ADR-007 — CS2Fixes remains a compatibility layer.** Keep mature ZE map semantics where useful; ZEPVE owns PvE rules and Bot lifecycle.
- **ADR-008 — One authority per state.** Do not let ZEPVE and another system independently own the same respawn/movement/round state.
- **ADR-009 — Optional native movement.** Try existing BotController-style APIs before creating a custom MovementBridge.
- **ADR-010 — User docs are bilingual.** English and Simplified Chinese are first-class user-facing documentation.
- **ADR-011 — Developer docs stay compact.** Avoid duplicated translations for Codex/internal docs unless translation materially helps contributors.
- **ADR-012 — No premature scripting/database systems.** Add complex subsystems only after a concrete ZE/PvE need exists.
- **ADR-013 — Production components are pinned.** The suite references independent production components by exact Git commit/submodule revision; it does not silently track moving component branches.
- **ADR-014 — Lab never ships.** `ZEPVE-Lab` is an evidence/PoC repository and is never a production submodule or release dependency.
- **ADR-015 — Component development and suite integration are separate PRs.** A Navigation change is reviewed in `ZEPVE-Navigation`; the suite then receives a separate component-bump PR after validation.
- **ADR-016 — One suite release for users.** Even when development spans several repositories, server operators should normally install one ZEPVE release package assembled by `CS2-ZE-PVE`.
- **ADR-017 — Upstream first.** Missing third-party capabilities should be addressed upstream when practical; local forks exist only for clear ZEPVE-specific needs or blocked upstream work.
- **ADR-018 — Agent changes use branches by default.** Agent/Codex work uses `agent/* -> PR -> main`; direct `main` changes require explicit owner instruction.
