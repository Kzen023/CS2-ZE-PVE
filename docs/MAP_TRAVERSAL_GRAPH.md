# MapTraversalGraph plan

Status: **planned architecture for v0.4/v0.5**. This document does not change the current v0.3 acceptance scope and is not a production-readiness claim.

This is the canonical cross-repository plan for persistent route recording, ZE route planning, ZR reachability planning, typed traversal and in-game visualization.

Related research: `docs/CS2_BOT_IMPROVER_RESEARCH_20261014.md`. Navigation-side compatibility details: `ZEPVE-Navigation/BOT_IMPROVER_INTEGRATION.md`.

## 1. Why this exists

ZEPVE should not use full Human command replay as its route planner.

The target architecture separates four questions:

```text
BotAI decides who to pursue
        ↓
PursuitPolicy decides whether/how far pursuit may backtrack
        ↓
Route planner decides where to go
        ↓
TraversalEdge decides how to cross one local connection
        ↓
Movement backend executes the authorized movement
```

A `MotionClip` is only an exceptional local executor. It is not the route source, stage planner or general gameplay replay format.

## 2. Shared map model

ZE and ZR use the same persistent graph primitives:

```text
MapTraversalGraph
├─ Regions[]
├─ Routes[]
├─ Nodes[]
└─ Edges[]
```

The high-level planner differs by game mode, but the traversal edges and execution stack are shared.

### Region

A spatially meaningful reachable area such as:

- normal ground area;
- roof / platform / ledge;
- camp position;
- stage-local area;
- room / corridor / landing area.

Regions are especially important for ZR target localization and reachability planning.

### Route

An ordered or branching curated path through the graph.

Routes are especially important for ZE, where map progress, stages and route direction matter.

### RouteNode

A stable spatial anchor or bounded node region. A Bot is not required to touch one exact recorded coordinate.

Expected fields include:

```text
Id
Position
Radius / Volume
RegionId
SegmentId
StageId / BranchId when applicable
Flags
```

### TraversalEdge

A directed graph connection describing how movement is expected to pass between two nodes/regions.

Initial semantic edge types:

```text
Walk
Jump
CrouchJump
Drop
Ladder
Door
Elevator
Wait
MotionClip
```

Add types only when runtime evidence requires them.

Each edge may declare:

```text
Direction: ForwardOnly | Bidirectional | DropOnly
EntryVolume
ExitVolume
PreferredDirection
Ground/support requirements
Allowed speed range
Map/stage availability predicate
Base cost / difficulty cost
Executor
```

## 3. ZE planning policy

ZE uses curated `RecordedRoute` / route branches as the primary persistent map knowledge.

Conceptually:

```text
AssignedTarget
    ↓
project target onto current legal ZE route/stage
    ↓
PursuitPolicy
    ↓
route branch / progress target
    ↓
TraversalGraph
```

Normal walk edges may still use Valve native NAV or local steering to reach the next EntryVolume/route region.

### Backtracking

ZE pursuit must not blindly reverse the full route just because the AssignedTarget moved backward.

Rules:

1. same local region / same stage / short bounded distance may temporarily backtrack for combat;
2. backtracking has a bounded progress window and hysteresis;
3. never cross a forbidden stage/segment boundary;
4. never reverse a `ForwardOnly` traversal edge;
5. a reverse movement is allowed only when the graph contains a legal reverse/bidirectional connection;
6. if the AssignedTarget leaves the currently legal reachable graph, Navigation reports `TargetUnavailableByRoute` to BotAI;
7. BotAI, not Navigation, decides whether to rebind another Human.

Navigation therefore distinguishes target authority from route reachability.

## 4. ZR planning policy

ZR does not require one fixed spawn-to-end route.

ZR uses the graph primarily as a **reachability graph** for Human camp locations and movement-skill connections.

Example:

```text
Ground Region
    ↓ Walk / Native
Box Entry
    ↓ CrouchJump
Box Region
    ↓ TurnJump / MotionClip
Window Region
    ↓ Walk
Camp Region
```

Runtime flow:

```text
AssignedTarget
    ↓
locate target Region
    ↓
locate Bot Region
    ↓
find legal graph path
    ↓
execute Walk / typed traversal edges
```

If a ZR map has useful NAV, Native navigation should handle ordinary sections between graph anchors. The graph supplies difficult connections the engine does not reliably understand.

If no persistent graph exists, Runtime Trail remains a dynamic fallback route source.

## 5. Route recording pipeline

Recording is an **authoring input**, not the production navigation format.

An authorized Human records a path while playing normally.

Raw capture may temporarily retain:

```text
position
velocity
move / facing direction
grounded state
duck state
jump transitions
ladder / water / forced-movement state
time / command sequence
teleport or large discontinuity
```

Do not permanently save full per-tick UserCmd history for normal route geometry.

After recording:

```text
raw Human samples
    ↓
spatial simplification
    ↓
preserve meaningful state transitions
    ↓
segment/discontinuity detection
    ↓
traversal candidate detection
    ↓
MapTraversalGraph draft
    ↓
admin review / correction
```

The simplifier may remove redundant straight-line samples but must preserve meaningful turns, height changes, stage/segment boundaries, jump takeoff/landing evidence, ladder transitions, teleport breaks and authored branch points.

Automatic classification creates **candidates**, not unquestioned truth. For example, a detected airborne height transition may propose `Jump` or `CrouchJump`; an admin may correct the edge type or bind a MotionClip later.

A short bounded recent-authoring buffer may support a NadeLauncher-style “save last maneuver” workflow. It is authoring evidence only and must never become permanent full-route replay history.

## 6. MotionClip rule

Prefer the simplest reliable executor:

```text
ordinary steering / Native NAV
        ↓ if insufficient
primitive Jump / CrouchJump / Drop / Ladder action
        ↓ if still insufficient
short bounded MotionClip
```

A MotionClip may encode a short command/view sequence for a difficult local action, but:

- route selection must already have chosen the edge;
- Entry conditions must already be valid;
- the clip cannot copy pawn position/velocity to force success;
- playback completion is not physical success;
- success requires the expected ExitVolume/route state;
- failure is scoped to that action/edge and triggers replan/alternative-path logic before Recovery.

## 7. Runtime route costs and legality

Edges should support route cost and availability instead of global heuristics.

Conceptual cost sources:

```text
base distance
+ traversal difficulty
+ recent edge failure penalty
+ danger / map-state penalty
+ bounded backtrack penalty
```

A forbidden stage transition or unavailable door/edge is effectively infinite cost / unavailable.

A failed traversal edge must not globally suppress unrelated Native/Trail/graph alternatives.

## 8. Failure and replan order

Preferred escalation:

```text
no meaningful progress
    ↓
local steering correction / route cursor repair
    ↓
rejoin current legal route/region
    ↓
replan graph path
    ↓
select alternative edge/branch
    ↓
report target unreachable when no legal route exists
    ↓
Recovery only when separately permitted
```

Recovery remains a final fallback. Geometry safety does not grant ZE map/stage permission.

## 9. In-game authoring and visualization

The first production authoring surface should be **chat-first + world-visible**, admin-only and usable without a custom HUD or offline editor.

Expected workflow:

```text
!zepath record [name]
→ Human runs the section/map normally
→ !zepath stop
→ auto-simplify and propose graph
→ !zepath show
→ correct a few nodes/edges/volumes
→ test one edge or route
→ save local draft
→ review/export curated data into suite
```

Exact commands are not API contracts yet, but the capability should cover the equivalent of:

```text
record start / stop / cancel
show / hide
select node / edge
set edge type / direction
set EntryVolume / ExitVolume
single-edge test
route test
save / load / list / delete local draft
export for suite review
```

A richer `custom_hud_layout` author panel may be added later as presentation, but it must not be required for the first useful route recorder.

### Spatial lookup and trigger discipline

EntryVolume lookup should use a bounded spatial index/grid or equivalent local query instead of scanning every edge against every Bot every tick.

Conceptually:

```text
Bot position
→ nearby EntryVolume candidates
→ geometry/state entry check
→ edge availability/policy check
→ final lifecycle/ownership validation
→ one selected traversal commit
```

Use entry latching/hysteresis so remaining inside an EntryVolume does not repeatedly retrigger the same action. Edge failure/cooldown remains local to that edge/action.

### Author visualization

In-world debug visualization should show, when requested:

- route nodes / regions;
- edge arrows and direction;
- edge semantic type;
- segment/stage/branch boundaries;
- EntryVolume and ExitVolume;
- disabled/unavailable edge state;
- executor type (`Primitive`, `Native`, `MotionClip`, etc.);
- test status (`UNTESTED`, `PASS`, `FAIL/PARTIAL` where applicable).

### Runtime debug visualization

For a selected Bot, show bounded diagnostics such as:

```text
AssignedTarget
TargetRegion
CurrentRegion
Route / Branch
CurrentEdge
Traversal state: Entry | Execute | Exit | Abort
Entry-condition reasons
Progress / route cursor
Failure reason
Edge-local cooldown / penalty
Next planned edge
```

The goal is that an operator can answer “why is this Bot not moving?” from current graph/traversal state rather than guessing from raw coordinates.

Visualization is disabled by default and must remain bounded to avoid server/client spam.

## 10. External Bot-improver linkage

`MapTraversalGraph` may later improve other Bot systems, but the graph exposes **knowledge and ownership context**, not shared movement mutation.

### Read-only graph context

After the model is stable, Navigation may expose a versioned capability conceptually like:

```text
IMapTraversalContext

MapGeneration
SchemaVersion
LocateRegion(position)
TryGetRouteContext(position / bot)
EstimateReachability(fromRegion, toRegion, constraints)
QueryEdge(edgeId)
QueryNearbyEntryVolumes(position, radius)
Get canonical Stage/Branch context when available
```

Return immutable snapshots/IDs rather than mutable internal references.

Useful consumers include Nade/ability policy, WeaponSystem/Director, HUD/debug and later generic Bot compatibility adapters.

### Runtime ownership context

Cooperating aim/FOV/movement helpers need to know when to yield:

```text
NavigationMovementOwned
TraversalActive
TraversalState
ScopedViewOwned
RecoveryActive
CurrentEdgeId / CurrentEdgeType
```

Examples:

- generic stuck/velocity logic yields during active traversal;
- aim/custom FOV yields while scoped traversal owns view;
- `Wait`, `Door` and `Elevator` are not misclassified as generic stuck states;
- normal enhancement resumes immediately after Exit/Abort.

### Optional traversal request

If ordinary Bots later need a curated special edge, they submit a lifecycle-bound request to Navigation. Navigation validates graph legality and acquires the sole movement lease. Consumers must not write BotController movement directly while Navigation is authoritative.

This enables a later generic mode:

```text
Valve/native movement on ordinary path
→ reach known special EntryVolume
→ Navigation temporarily executes typed edge
→ Exit/Abort
→ release movement/view back to native Bot AI
```

This reverse linkage is a compatibility feature, not a v0.5 release requirement.

## 11. Storage and promotion

`ZEPVE-Navigation` owns schema, validation, recording, simplification, authoring behavior, visualization and runtime graph consumption.

`CS2-ZE-PVE` owns curated map data that ships with releases.

Recommended suite layout:

```text
maps/
└─ routes/
   └─ <map>.json
```

Server-local recordings are drafts and never silently overwrite curated data. Promotion into suite data requires explicit review/export.

Do not create a production Recorder repository. Consider a separate offline route-tool repository only if a real standalone GUI/import/export/compiler workflow later justifies its own lifecycle.

## 12. Version staging

### v0.3

No change. Finish the clean Navigation baseline, package provenance, real runtime acceptance and actual rollback first.

### v0.4

Stabilize the local traversal contract:

```text
Entry -> Execute -> Exit -> Abort
```

Prove primitive traversal, edge-local failure scope, physical success checks and scoped view authorization. MotionClip remains optional and exceptional.

Research from Bot-Improver may inform ownership/arbitration and bounded native-assist experiments, but broad AI patches or external movement writers are not v0.4 scope.

### v0.5 — Persistent Routes & Traversal Graph

Implement the first persistent graph slice:

- `MapTraversalGraph` schema;
- Region / Node / Edge identity;
- ZE `RecordedRoute` branches;
- ZR reachability regions/connections;
- typed persistent TraversalEdges;
- Entry/Exit volumes;
- raw Human route capture -> simplification -> draft graph;
- chat-first authoring + in-game world visualization;
- spatial EntryVolume/Region lookup and local trigger hysteresis;
- safe save/load/versioning/backups;
- runtime graph/action diagnostics;
- single-edge and route testing;
- curated suite export/review flow.

A minimal read-only graph/runtime context contract may be designed if it does not delay the graph itself. Generic Bot-Improver compatibility is not part of the v0.5 acceptance gate.

Do not build a large offline editor, general scripting engine or automatic AI map-understanding system in the first slice.

## 13. Agent guardrails

Future agents should treat these as hard boundaries unless a later ADR explicitly changes them:

- BotAI decides **who**; Navigation decides **how/where**; BotController executes authorized low-level commands.
- ZE and ZR share one graph/traversal model; do not create two independent traversal engines.
- ZE uses curated route/stage progress; ZR uses target-region reachability.
- Runtime Trail remains useful for unknown/unrecorded maps and dynamic fallback.
- Recorded Human input is authoring evidence, not a full-route replay format.
- MotionClip is a short local executor only.
- typed edge direction/availability/cost must replace broad global cooldown heuristics where possible.
- no reverse traversal across a forward-only ZE edge.
- no automatic target reassignment inside Navigation.
- no map-specific special case should be hard-coded into the generic Navigator when it belongs in map graph/override data.
- external Bot enhancers consume context or submit Navigation requests; they do not become second movement/view writers.
- no Lab runtime dependency.

## 14. Expected end state

The intended operator experience is:

```text
chat command to record a map/section once
→ graph appears in-world
→ fix a few special edges and Entry/Exit volumes
→ test edges/routes with Bots
→ save/export curated map data
→ runtime Bots plan through the graph and explain their current state
```

The intended compatibility experience later is:

```text
Bot improver keeps normal combat/tactical behavior
→ graph supplies region/reachability/special-edge knowledge
→ cooperating enhancers yield on movement/view ownership
→ Navigation temporarily executes difficult local traversal
→ control returns cleanly
```

The system should behave like persistent navigation knowledge plus typed off-route traversal, not like a long prerecorded movie that Bots attempt to mimic exactly.
