# ZE Path System — Canonical Runtime and Authoring Policy

Status: **canonical planned behavior for the v0.5 persistent route system**. This document does not expand the current v0.3 acceptance scope. v0.4 must first stabilize typed local traversal.

This file exists so future contributors and coding agents do not redesign the path system from scratch.

## 1. One-sentence model

```text
AssignedTarget tells the Bot WHO to pursue.
MapTraversalGraph tells Navigation HOW to reach that Human legally.
Valve/native movement handles ordinary movement where possible.
ZEPVE only supplies persistent map knowledge, route choice and exceptional local traversal.
```

Do not turn route recording into full-route replay and do not build a second general Bot AI.

## 2. Ownership

```text
Core
  identity / lifecycle / round / gameplay authority

BotAI
  AssignedTarget / target binding / bounded awareness

Navigation
  local-pursuit vs route decision
  route / branch selection
  graph progress / rejoin / replan
  typed local traversal
  final movement-intent ownership

BotController / MovementBridge
  low-level authorized command application only

Valve/native AI
  normal perception / combat / ordinary navigation where useful
```

Navigation never silently chooses another Human. If the current AssignedTarget is not legally reachable, Navigation reports that condition to BotAI.

## 3. When to chase the Human directly vs follow the recorded path

Do **not** decide from straight-line distance alone.

Direct local pursuit is allowed when the AssignedTarget is inside a short, legally reachable local envelope, for example:

```text
same reachable Region
same legal stage/segment
no closed door / one-way boundary / forbidden map mechanism between them
ordinary movement can reasonably make contact
```

Otherwise Navigation follows the graph/route toward the target Region.

Conceptually:

```text
AssignedTarget
    ↓
is target locally and legally reachable?
    ├─ YES → local/native pursuit
    └─ NO  → graph route / branch plan
```

Stage, branch, one-way and mechanism legality outrank Euclidean distance. A Human 100 units away through a closed wall is not a local target.

Use hysteresis so a Bot does not oscillate every frame between local pursuit and route mode near a threshold.

Short local backtracking is allowed only inside the existing legal backtrack window. Never reverse a forbidden stage boundary or `ForwardOnly` edge just because the Human moved backward.

## 4. Graph model

```text
MapTraversalGraph
├─ Regions[]
├─ Routes[]
├─ Nodes[]
└─ Edges[]
```

For ordinary ZE maps, persistent data should describe **map progression and legal connectivity**, not exact per-tick Human positions.

A route node is a stable anchor/volume. A Bot does not need to touch the exact point the recorder touched.

Typical edge semantics:

```text
Walk
Jump
CrouchJump
Drop
Ladder
Door
Elevator
Wait
Teleport / discontinuity
MotionClip (exceptional only)
```

Normal `Walk` edges should be corridor/region tolerant. Valve NAV or local steering may choose the exact physical line inside that corridor.

## 5. Recording model for map testers

The human-facing authoring model should stay simple. Testers only need to understand three normal recording purposes:

```text
1. Main path   — the normal map-completion route
2. Branch      — a genuinely different route that splits from and/or rejoins another route
3. Link        — a short missing connection between already-known areas
```

Examples:

```text
Main path:
spawn → defense 1 → defense 2 → boss → end

Branch:
main corridor → left route → upper floor → rejoin main

Link:
existing Region A → side door → existing Region B
```

A different breakthrough route is a **Branch** when it uses a genuinely different entrance, floor, corridor, door or topology.

Do not create a Branch merely because one Human ran on the left side of a wide room and another ran on the right side. If both belong to the same reachable corridor, simplification should merge them.

Doors, elevators, waits, teleports and ordinary jumps are not separate user-facing recording systems. Record the route normally; the compiler/admin review turns them into semantic edges.

Only use a dedicated advanced traversal capture when an ordinary path/primitive edge is proven insufficient.

## 6. Recording pipeline

```text
Human records main/branch/link
        ↓
temporary rich samples
        ↓
spatial simplification
        ↓
identify Regions / Nodes / branch split+merge / discontinuities
        ↓
propose semantic Edges
        ↓
admin world visualization + corrections
        ↓
Bot route test
        ↓
curated MapTraversalGraph data
```

Raw capture may temporarily include position, velocity, grounded/duck/jump state, facing, ladder/forced movement and timing evidence.

Ordinary route data must **not** become a permanent full UserCmd movie.

Teleport or large scripted reposition is a discontinuity/semantic transition. Never draw a fake Walk edge through the map between the pre-TP and post-TP coordinates.

## 7. Branches and flanking

Humans do not dictate which branch every Zombie must use.

Example:

```text
                 ┌─ side branch ─────────┐
Zombie start ────┤                       ├─ defense rear
                 └─ main route → Humans ─┘
```

If both routes are currently legal, some Zombies may use the side branch even when every Human used the main route. This naturally produces side/rear pressure without implementing a separate tactical "flank AI".

Branch selection order:

```text
1. reject illegal/unavailable branches
2. estimate route cost
3. prefer stable distribution rather than every Bot choosing the same edge
4. re-evaluate at legal graph decision points when state changes
```

Conceptual route cost may include:

```text
base distance
+ traversal difficulty
+ recent edge failure penalty
+ map-state/danger penalty
+ bounded backtrack penalty
+ local branch congestion penalty
+ small stable per-Bot tie-break/preference
```

A longer branch may therefore be chosen when the main route is heavily congested or repeatedly failing.

Do not bypass closed doors, old stages, forward-only teleports or other map legality merely to reach a Human from behind.

## 8. Weapon knockback and physical deviation

Knockback is **not** a separate Recovery system.

For ordinary route movement:

```text
Bot follows route
→ Human weapon pushes Bot aside/backward
→ current physical position changes
→ semantic route remains valid unless topology/legality changed
```

Preferred handling:

```text
still inside current route/Region
→ continue naturally

slightly outside route corridor
→ bounded rejoin near current legal progress

moved into another legal Region
→ local graph replan

no legal rejoin/path
→ report route unavailable / final fallback policy
```

Never snap the Bot back to recorded samples just because it was pushed away.

Do not globally search the nearest Euclidean waypoint: stacked floors and nearby-but-disconnected geometry make that unsafe. Rejoin must be bounded by current stage/branch/segment/Region and legal reachability.

## 9. Intentional waiting and Zombie confinement

A Bot that is not moving is not automatically stuck.

ZE maps may intentionally hold Zombies behind a door, cage, timer, elevator or scripted release for many seconds.

Represent that as a legitimate map wait/confinement state, conceptually:

```text
WaitingByMap
WaitingForMechanism
MapConfinement
```

While intentionally constrained:

```text
do not accumulate normal stuck failure
avoid repeated route/rebind spam
do not Recovery-teleport out of the hold
keep AssignedTarget unless another real target-binding reason invalidates it
```

Map permission for Recovery is separate from geometric safety. Unknown map permission fails closed.

## 10. Failure escalation

For ordinary path execution use this order:

```text
local steering correction / cursor repair
        ↓
bounded rejoin current legal route/Region
        ↓
graph replan
        ↓
alternative legal edge/branch
        ↓
report target/route unavailable
        ↓
Recovery only when separately and explicitly permitted
```

Do not create a new global cooldown because one edge failed. Failure penalty/cooldown belongs to that edge/action.

## 11. Special traversal boundary

A special `TraversalEdge` solves one local connection; it does not select the route.

```text
route planner chooses edge
        ↓
Entry conditions become valid
        ↓
Entry → Execute → Exit / Abort
        ↓
return to ordinary route planning
```

Prefer:

```text
ordinary native/local steering
→ primitive Jump/CrouchJump/Drop/Ladder
→ short bounded MotionClip only if necessary
```

Playback completion is not physical success. Success requires reaching the expected ExitVolume/state.

Do not copy pawn position/velocity to force traversal success.

## 12. Map support workflow

A normal ZE map should be supportable incrementally:

```text
record main route once
+ record genuinely different branches once each
+ test Bots
+ add short links where topology is missing
+ annotate/fix only special mechanisms or traversal that actually fail
```

Do not require every corner of the map to be recorded before basic support is useful.

A mature authoring workflow should allow a familiar tester to establish a usable baseline quickly, then improve the map data from runtime evidence.

## 13. Diagnostics that must exist

For a selected Bot, operators should be able to answer:

```text
AssignedTarget
CurrentRegion / TargetRegion
LocalPursuit or Route mode
Current Route / Branch / Segment
Current Edge
Why this branch was selected / rejected
Progress / rejoin state
Waiting/confinement reason
Failure reason
Next planned edge
Recovery permission/result
```

Without these diagnostics, route bugs will be confused with Valve AI, map mechanisms or movement-backend bugs.

## 14. Agent guardrails

Before adding route/navigation code, future agents must preserve these constraints:

- inspect existing ZEPVE Navigation capabilities before introducing another planner/route executor;
- inspect relevant existing Valve/community behavior before implementing a new AI subsystem;
- extend the current Navigation owner instead of creating parallel movement writers;
- `AssignedTarget` is target authority; graph reachability is not target authority;
- ordinary route recording means map knowledge, not exact replay;
- ordinary knockback means rejoin/replan, not teleport-to-track;
- Branches are parallel legal graph options and may be used even if Humans chose another branch;
- intentional map confinement/wait is not stuck;
- stage/door/one-way legality always outranks straight-line proximity;
- keep `MotionClip` local and exceptional;
- no second PlayerRunCommand hook, movement router, Trail, Recovery writer or full replay engine;
- no Lab runtime dependency.

## 15. Version boundary

```text
v0.3  close Native → Runtime Trail → Recovery baseline
v0.4  stabilize typed local traversal Entry/Execute/Exit/Abort
v0.5  implement persistent MapTraversalGraph + recording/authoring + branch planning
v0.6  feed canonical MapPlan/stage/mechanism legality into graph availability
```

Do not pull persistent graph implementation into the current v0.3 baseline merely because this policy is documented now.
