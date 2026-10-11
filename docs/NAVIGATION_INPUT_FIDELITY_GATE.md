# Input fidelity capability audit (2026-10-10)

The accepted backend safety invariants are unchanged. This is a separate capability gap, not a rejection of the resident router/lease/validator design. Do not claim high-fidelity player operation reproduction from low-frequency position/velocity Trails.

## Verified source facts

- Production `MovementAbi.h::Intent` carries world XY, buttons, TTL and Native/WorldDirection mode. It contains no command timeline or sub-tick payload.
- `MovementHost.cpp::ApplyCommand` replaces authorized controlled movement and clears old controlled button edges/analog sub-tick deltas. Application-time validation remains before final mutation. Existing APIs cannot carry faithful human press/release timing.
- Upstream `MotionRecorder.cpp` uses slot arrays with growing vectors for ticks/subticks/commands; it is not a bounded Core-token record contract. Legacy replay/injection remains rejected by the production arbiter and must not be re-enabled.
- Installed class configuration contains human speed1.0 and zombie speed0.9, gravity1.0/scale1.0. These are configuration observations only; actually applied physics parity was NOT TESTED. Do not change Core/ZR class policy to fake equal results.

## Required path before claiming operation fidelity

Observe movement-only human command inputs in the existing PlayerRunCommand dispatcher; no parallel hook. Bound memory/history and bind records to Core identity, map/round/connection/pawn and trail generation/sequence. Record world-space wish direction and precise jump/duck press/release timing; exclude attack/weapon/view ownership. A future timed intent must pass the same exclusive owner/current intent/session/revision and full Bot/target/Core/BotAI/Navigation permission validation immediately before every native mutation. Atomic replacement/cancel/drain must erase all remaining timeline work, with no stack/resurrection. Re-run native race/interop and real stopped-server matched validation before promoting such ABI capability.

Production does not currently have that timed contract. Do not copy positions/velocities/angles or turn on anonymous replay as a substitute. Safety: accepted. Input fidelity: NOT IMPLEMENTED / NOT TESTED. Complex physical traversal: FAIL/PARTIAL. Matched rollout merge remains NO.
