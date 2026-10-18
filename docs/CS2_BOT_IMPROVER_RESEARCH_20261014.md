# CS2-Bot-Improver research and ZEPVE integration plan

Date: 2026-10-14

Status: **research / future integration guidance**. This document does not expand the current v0.3 acceptance scope. Runtime changes still require the owning component branch/PR and real CS2 evidence.

This review covers every plugin currently composed by `ed0ard/CS2-Bot-Improver` and the related NadeLauncher authoring workflow. The goal is not to copy the suite wholesale. The goal is to identify proven mechanisms, conflict domains and capability seams that can improve ZEPVE while preserving its one-writer architecture.

## 1. Executive conclusion

The strongest reusable pattern across Bot-Improver is not any single AI trick. It is the separation of specialized capabilities:

```text
identity / cosmetics
vision / perception
native AI patches
combat / behavior policy
aim selection
grenade decision + recorded data
buy / economy
low-level bot command backend
```

ZEPVE should keep the same separation, but with stricter authority:

```text
Core      -> identity / lifecycle / round authority
BotAI     -> AssignedTarget / target binding / bounded awareness policy
Navigation-> route / graph / progress / traversal / Recovery
BotController -> authorized low-level command application
WeaponSystem / Director / HUD -> their own later domains
```

The future `MapTraversalGraph` can become useful outside Navigation, but only through **read-only/advisory context** or explicit request APIs. It must not become a shared mutable object that lets every plugin steer a Bot.

The recommended reverse-integration direction is:

```text
                         MapTraversalGraph
                               |
            +------------------+------------------+
            |                  |                  |
      Navigation          Nade/Ability       Weapon/Director
    authoritative         advisory use        advisory use
        planner          region/stage        region/stage
            |             trigger context      profile context
            |
   movement/view ownership context
            |
      Bot improvers yield while Navigation owns traversal
```

This can improve both ZEPVE PvE and, later, ordinary Bot-Improver-style bots without creating two movement writers.

## 2. Reviewed Bot-Improver composition

`ed0ard/CS2-Bot-Improver` currently composes these nine plugin repositories:

1. `XBribo/CS2-Bot-Hider`
2. `XBribo/CS2-Bot-Controller`
3. `XBribo/CS2-Bot-Vision`
4. `ed0ard/CS2-Smarter-Bot`
5. `ed0ard/CS2-Bullseye-Bot`
6. `ed0ard/CS2-BotAI`
7. `ed0ard/CS2-Bot-Randomizer`
8. `ed0ard/CS2-Bot-NadeSystem`
9. `ed0ard/CS2-Bot-Buy`

Auxiliary authoring reference:

- `StefanKunde/NadeLauncher`

This research is source review, not a compatibility certification. Upstream code and game signatures can change after this date.

## 3. BotHider — identity publication and lifecycle-safe async application

### What it does

BotHider changes Bot-facing identity/presentation data such as names, synthetic Steam IDs, ping, avatars, crosshair/flair and related metadata while retaining Bot compatibility.

Its useful engineering patterns are more important to ZEPVE than the cosmetic feature itself.

### Reusable patterns

- Cross-plugin managed/native API rather than consumers reaching into implementation internals.
- Requests are applied asynchronously on the game thread and callers can re-query applied state instead of assuming request == commit.
- Slot reuse is treated as an incarnation problem. A new Bot entering the same slot must not inherit an old pending request.
- Steam ID / identity changes cause dependent presentation state to be cleared/reapplied.
- Shared avatar data uses bounded handoff and sequencing so a consumer does not observe a partially-written payload.
- Map/string-table recreation is treated as a lifecycle event requiring re-publication.

### ZEPVE adoption

Already aligned with Core's generation model. Future cross-plugin requests should bind at least:

```text
slot/controller identity
connection generation
pawn/spawn generation when pawn-specific
action/request generation
module/version where relevant
```

The important lesson is **not** to import BotHider into gameplay authority. It is to preserve incarnation-safe async work and applied-state observability.

Possible later use: Director/HUD may consume a stable Bot persona/identity view, but identity presentation must never decide Navigation or target authority.

## 4. BotController — capability boundary, record/replay primitives and command ownership

### What it does

Upstream BotController exposes a versioned API for:

- Bot locks (`All`, `Aim`, weapon slot);
- motion recording and replay;
- tick/subtick/command-frame capture;
- cancellable UserCmd button injection;
- persistent analog movement overrides;
- persistent button suppression;
- weapon switching;
- BotProfile reads;
- buy plans;
- voice sending.

The current upstream API is broad and intentionally useful to many plugins.

### Reusable patterns

- Explicit ABI version and shared capability contract.
- Token/cancellation model for temporary input actions.
- Rich Human/Bot capture data is useful as **authoring evidence**.
- BotProfile observation can support later evidence-based Bot policy work.
- Low-level implementation is semantically dumb; higher-level consumers decide why an action exists.

### ZEPVE boundary

Do **not** import upstream movement semantics wholesale into the ZEPVE production fork.

ZEPVE's stronger guarantees remain authoritative:

```text
one existing PlayerRunCommand dispatcher
one movement router
exclusive movement lease
one current movement intent
application-time identity/lifecycle/TTL/revision validation
final commit recheck
drain on detach/unload
```

Upstream independent movement/replay tokens are useful API examples, but ZEPVE must not allow independent consumers to become simultaneous final movement writers.

Also keep the existing rule: no pawn position/velocity copying to fake traversal success.

### Future reverse integration

BotController should remain the backend. If external Bot-Improver modules want locomotion, they should call a Navigation-owned request surface, not BotController movement APIs directly while ZEPVE Navigation is authoritative.

A compatibility layer may later expose ownership state such as:

```text
MovementOwned
TraversalActive
ScopedViewOwned
RecoveryActive
CurrentTraversalType
```

so other plugins can yield cleanly.

## 5. BotVision — bounded perception backend instead of target authority

### What it does

BotVision modifies native visibility through smoke and models temporary HE/bullet-created sight holes. It exposes diagnostics, smoke density tests, global/per-Bot density thresholds and selected temporary reveal slots.

Implementation patterns include:

- required and optional feature modules separated;
- native signature/gamedata resolution;
- bounded per-Bot caches;
- generation invalidation;
- per-Bot threshold context;
- reveal masks bound to player handles so slot reuse does not reveal the wrong player;
- diagnostic counters and test commands.

### ZEPVE adoption

The useful shape is a **perception service**, not a new target selector.

Potential future BotAI inputs:

```text
CanSeeThroughSmoke(bot, target)
SmokeDensity(bot, target)
TemporaryReveal(target, ttl, reason)
PerceptionBackendStatus
```

Possible PvE uses:

- explain why Valve Enemy/perception was lost;
- bounded post-damage or post-objective reveal experiments;
- smoke-aware combat policy;
- route danger/visibility metadata as read-only cost context where justified.

Hard boundary: `AssignedTarget` remains authoritative. BotVision-like results may influence awareness assistance, not silently replace target binding.

Do not copy permanent visibility forcing. Any reveal/threshold change needs bounded lifetime, lifecycle validation and evidence.

## 6. Smarter-Bot / BotState — the richest source of behavior ideas and conflicts

Smarter-Bot contains movement, combat, flash, FOV, bomb, weapon, map-state and awareness adjustments. It is the most useful repo for identifying both good mechanisms and things ZEPVE should deliberately avoid.

### 6.1 Movement and stuck handling

Observed patterns:

- door events create a short cooldown that temporarily leaves movement to the engine;
- ladder state has its own exit cooldown;
- stuck detection considers elapsed time, maximum speed **and actual displacement**;
- idle Bots can request native repath after sustained low movement;
- look-around state is reset together with some repath actions;
- per-round movement state is cleared because player indices are reused;
- one Inferno sewer fix is hard-coded by map coordinate.

Useful for ZEPVE:

- progress must be displacement/route-progress based, not low speed alone;
- `Door`, `Ladder`, `Wait` deserve explicit traversal/wait states;
- engine repath/look-around reset are good bounded candidates after evidence;
- stuck escalation should distinguish legitimate waiting from failed progress;
- map-specific exceptions belong in graph/MapPlan data, not generic Navigator code.

Do **not** copy the implementation's direct `AbsVelocity` mutation. ZEPVE movement must stay behind its validated MovementBridge/intent boundary.

### 6.2 Behavior / awareness fields

Smarter-Bot manipulates fields/timers including sleeping/active, alert, ignore-enemies, panic/surprise, look-around and teammate politeness.

This is valuable as a **candidate inventory** for Lab experiments, not as a preset to copy.

ZEPVE rule remains:

```text
one native awareness field/action at a time
bounded event-triggered window
measure causal effect
restore/expire
never force Enemy/visibility just because a field exists
```

The strongest candidates for controlled experiments are narrow wake/active/look-around/repath operations around spawn, respawn, rebind and Recovery.

### 6.3 Combat movement

Smarter-Bot implements weapon-dependent counter-strafe/crouch/peek behaviors and clears pathfinder eye control during attacks.

Reusable concept:

- combat, path following and traversal need explicit arbitration;
- short combat behavior should be scoped and have a clear return to navigation;
- view ownership must be visible to other modules.

Do not copy direct velocity writes or unconditional attack/crouch mutations into Navigation.

### 6.4 Flash handling

This is a particularly good state-machine pattern.

Smarter-Bot:

```text
observe projectile
→ evaluate FOV/LOS
→ make one per-(Bot, flash) decision
→ bind it to predicted detonation/event identity
→ consume at actual native/event blind call
→ expire stale decisions
```

It also fails open to native behavior if the native blind hook cannot safely operate.

ZEPVE can reuse this pattern generally:

```text
observe
→ decide once
→ bind to lifecycle/event identity
→ commit at actual event
→ expire
```

This is useful for traversal candidates, interaction requests, awareness assists and map events because it prevents repeated per-tick random decisions.

### 6.5 Smoke reveal

Smarter-Bot temporarily reveals an attacker through smoke after damage and uses bounded defuse reveal windows. Existing windows extend instead of stacking indefinitely, and all state is cleared on lifecycle transitions.

This is a good model for future **bounded AwarenessAssist**, but ZEPVE should use a typed capability instead of shelling through server console commands if BotVision integration is ever adopted.

### 6.6 Custom FOV and ownership arbitration

Smarter-Bot's custom FOV path explicitly yields when BotController reports that another system currently controls view.

This is directly relevant to ZEPVE:

```text
ordinary AI/perception view
        ↓
Traversal begins and acquires scoped view
        ↓
Aim/FOV enhancer yields
        ↓
Traversal Exit/Abort releases view
        ↓
Aim/FOV enhancer resumes
```

This should become a generic ownership contract if we integrate external enhancers later.

### 6.7 Weapons

Useful patterns:

- BotController capability is resolved as a dependency rather than duplicating input hooks;
- delayed actions re-resolve current Bot state before committing;
- the plugin tracks only locks it owns and only releases those locks;
- takeover by a Human disables Bot automation.

These are good ownership patterns for WeaponSystem and later action services.

### 6.8 Bomb/fake-defuse state

Although bomb gameplay is not a ZE priority, the implementation demonstrates:

- bounded state/action windows;
- cancellable UserCmd suppression tokens;
- native state hooks that are installed/removed symmetrically;
- temporary FOV changes scoped to a search state;
- re-entry only after checking the current live Bot.

The same lifecycle discipline applies to ZE map interactions and special traversal.

### 6.9 Rush antenna / map-state bridge

Smarter-Bot reads map-specific entity state, resolves ambiguous entity names conservatively, and republishes state through a simple marker for another behavior system to consume.

Useful concept:

- canonical map-state adapter -> small stable read model -> consumers.

ZEPVE should implement this in v0.6 through `MapConfig -> MapDefinition -> MapPlan -> MapRuntime`, not by creating hidden marker entities unless there is a specific compatibility need.

## 7. Bullseye-Bot / BotAimImprover — narrow post-hook overrides

### What it does

Bullseye-Bot hooks native aim-point selection, waits for native logic to run, checks whether an enemy is already generally visible, raytraces multiple body candidates, then replaces only the final target spot according to weapon/mode priorities.

### Reusable patterns

- Prefer a narrow post-hook over replacing the entire native AI routine.
- Gate expensive work on already-valid visibility.
- Generate multiple physical candidates and validate them with traces.
- Keep weapon/context-specific priorities separate from the low-level hook.
- If signature resolution fails, leave the plugin inactive rather than guessing offsets.
- Clear pointer/controller caches on lifecycle events and unhook on unload.

### ZEPVE adoption

For zombie melee, direct aim-point logic is not currently a priority. The architectural pattern is still useful:

```text
native result first
→ validate context
→ minimally adjust one output
```

Later melee/contact positioning could use visible contact-point candidates, but this must not become a v0.3/v0.4 requirement.

If an aim enhancer is present beside ZEPVE, it must yield while scoped traversal owns view.

## 8. BotAI patches — binary patch safety is more reusable than the broad behavior patches

### What it does

This BotAI applies many native binary patches affecting weapon choice, combat, movement, bomb logic, vision/notice behavior and other engine decisions.

The strongest engineering pattern is its patch transaction discipline:

```text
resolve signatures first
→ compute dependent rel32/cave fixups
→ validate expected original bytes
→ apply dependency groups in safe order
→ if partner fails, roll back orphan patch/cave
→ retain original bytes
→ restore on unload
```

### ZEPVE adoption

This is excellent guidance for any future native patch experiment:

- exact build/signature provenance;
- expected-original validation;
- dependent patches applied atomically;
- no half-applied code-cave pair;
- explicit rollback;
- diagnostics for which patches actually applied.

### What not to import

Do not automatically adopt broad process-wide patches such as always-notice/always-watch, unrestricted spray/dodge or other global behavior rewrites.

They conflict with ZEPVE's evidence-driven policy and can be impossible to scope to one traversal state or one Bot.

If a patch is ever needed:

```text
Lab evidence
→ identify mutation domain
→ prove no conflict with Navigation/BotAI
→ isolate smallest patch
→ exact expected bytes/build profile
→ production review
```

A full BotAI patch bundle should not become a hidden dependency of ZEPVE.

## 9. BotRandomizer — stable generated profiles and generation-safe delayed work

### What it does

BotRandomizer generates stable per-Bot cosmetic loadouts and reapplies them through map/team/spawn lifecycle events.

Its state store is directly relevant to ZEPVE architecture:

```text
slot
+ userId
+ generation
```

Delayed application checks whether the same incarnation is still current before writing. Team changes and rerolls bump generation. Disconnect/reset removes state and advances generation.

### ZEPVE adoption

This supports two ideas:

1. Continue binding all delayed gameplay work to lifecycle generations.
2. Future PvE personality/difficulty should be a **stable Bot profile**, not fresh per-tick randomness.

Possible future Director shape:

```text
BotProfileCatalog
→ stable BotBehaviorProfile
→ BotAI policy modifiers
→ Navigation cost/confidence modifiers
→ WeaponSystem profile
```

Keep cosmetic identity and gameplay profile as separate data domains even if they share one persona key.

Traversal skill could later influence graph edge cost/eligibility, but cosmetic randomization itself must never change gameplay.

## 10. NadeSystem — closest analogue to future MapTraversalGraph action triggering

This is one of the most valuable repos for ZEPVE.

### Data conversion

NadeSystem consumes compact converted lineup records containing semantic runtime data such as:

```text
id
map
grenade type
projectile origin
projectile velocity
landing position
tags/team
```

The Human recording is therefore **compiled into runtime data**. The Bot does not need to reproduce the entire Human recording.

This strongly validates ZEPVE's planned pipeline:

```text
Human raw capture
→ simplify/classify/compile
→ MapTraversalGraph draft
→ runtime semantic graph
```

### Spatial trigger system

NadeSystem:

- scans on a bounded cadence rather than every possible object every frame;
- uses a spatial grid to narrow nearby candidate zones;
- evaluates horizontal radius and vertical tolerance separately;
- checks facing/direction where needed;
- latches zone entry to avoid repeated triggering;
- clears the latch after leaving the zone;
- handles one grenade trigger per Bot per scan;
- uses per-record cooldowns rather than one global action cooldown.

These map almost directly onto future `EntryVolume` handling.

Recommended ZEPVE equivalent:

```text
spatial index
→ nearby EntryVolume candidates
→ geometric/state entry check
→ edge availability/policy check
→ select one edge
→ acquire traversal authority
→ execute
```

### Decision then commit

NadeSystem separates:

```text
candidate nearby
→ situation/policy checks
→ resource/cooldown checks
→ commit action
```

ZEPVE should follow the same separation:

```text
Entry candidate
→ TraversalPolicy
→ final lifecycle/ownership validation
→ executor commit
```

### Context-aware action selection

The grenade decision layer checks enemy presence, information, line of sight, facing, team, timing, cooldown, overlap and economy before committing.

For ZEPVE, the transferable idea is not those exact thresholds. It is that a recorded action should have **context requirements**, not only a coordinate.

A future graph-linked ability record could reference:

```text
RegionId / EdgeId
StageId / BranchId
EntryVolume
facing window
map-state availability
target/enemy condition
cooldown
```

### Important implementation difference

NadeSystem often reproduces the **result** by spawning a grenade projectile with recorded origin/velocity rather than replaying all Human movement input.

For route traversal we cannot generally spawn the Bot at the result, because that would fake locomotion. But the broader lesson remains valuable: store and execute the smallest semantic result/mechanism that preserves gameplay, instead of replaying irrelevant Human history.

## 11. BotBuy — team-level planning and resource ownership

### What it does

BotBuy broadens available purchases, coordinates team economy, handles poor teammates and weapon drops, and modifies buy decisions based on current inventory/economy.

### Reusable patterns

- Team state matters; not every Bot should independently greedily optimize itself.
- Round-level resource decisions can use a stable snapshot.
- Delayed actions must revalidate the current player/controller.
- “poor teammate”/team-budget concepts belong at policy level, not inside low-level weapon mutation.
- map exceptions exist, but hard-coded map names in generic logic do not scale.

### ZEPVE adoption

Most classic CS economy behavior is not directly important to zombie Navigation, but the architecture is useful for `ZEPVE-WeaponSystem` and Director:

```text
Map/Stage context
+ Human/Zombie pressure
+ team/global budget policy
→ loadout policy
→ WeaponSystem applies result
```

Future graph/MapPlan context may inform profiles such as close-defense, corridor, boss, final hold or open-area combat. It must remain advisory; WeaponSystem does not acquire movement authority.

## 12. NadeLauncher — authoring UX reference

NadeLauncher demonstrates the desired first-generation authoring experience:

```text
chat command
→ player performs the action normally
→ plugin captures movement/action metadata automatically
→ in-world marker / browse / replay tools
→ save/share/import/export
```

For ZEPVE v0.5, prefer chat-first authoring plus world visualization:

```text
!zepath record [name]
!zepath stop
!zepath cancel
!zepath show / hide
!zepath test
!zepath save
!zepath list
```

Advanced editing may use a contextual `!zepath edit ...` command and world-space Entry/Exit visualization.

A custom HUD/editor is optional later. It is not a prerequisite for useful route authoring.

Also adopt the `save last` idea where practical: retaining a short bounded recent authoring buffer can let an admin convert the just-completed maneuver into a traversal candidate without remembering to start a dedicated clip before every attempt. This must remain an authoring buffer, not a permanent global replay history.

## 13. What ZEPVE should borrow across the whole suite

### Adopt strongly

- Capability/ABI boundaries between plugins.
- Incarnation/generation-bound async work.
- Re-query current state immediately before mutation.
- Clear owned state on round/map/disconnect/unload.
- Track only resources/locks/tokens owned by the current module.
- Narrow post-hook/native override before full AI replacement.
- Bounded cache and spatial indexing.
- Event-triggered state windows rather than permanent per-tick forcing.
- `observe -> decide once -> bind -> commit -> expire` for transient events.
- Context gate before action commit.
- Local/object-specific cooldown rather than global suppression.
- Human recording compiled to compact semantic runtime data.
- Diagnostics that expose why a decision was skipped/accepted.
- Fail open to native behavior for optional enhancements; fail closed where safety/authority requires it.
- Expected-original validation and atomic rollback for binary patches.

### Adopt only after evidence

- look-around reset;
- wake/active/alert/ignore-enemies adjustments;
- native repath timer nudges;
- custom FOV;
- through-smoke reveal;
- combat aim-point overrides;
- BotProfile changes;
- native binary AI patches.

Each candidate needs a narrow Lab test and a named owner.

### Do not import into ZEPVE architecture

- direct `AbsVelocity`/pawn position correction as normal traversal;
- a second `PlayerRunCommand` movement writer;
- full-route Human replay as Navigation;
- permanent forced Enemy/visibility/notice state;
- global movement cooldowns caused by one failed special action;
- hard-coded per-map coordinate fixes in generic Navigation;
- plugins silently changing each other's owned state;
- process-wide native patches whose conflict domain is unknown.

## 14. Reverse linkage: how MapTraversalGraph can enhance Bot-Improver-style bots

Yes, the future path system can be useful in the opposite direction, but it should expose **knowledge and ownership**, not shared movement mutation.

### 14.1 Read-only graph capability

After the v0.5 graph is real and stable, Navigation may expose a narrow versioned capability conceptually similar to:

```text
IMapTraversalContext

MapGeneration
SchemaVersion
LocateRegion(position)
TryGetRouteContext(position / bot)
EstimateReachability(fromRegion, toRegion, constraints)
QueryEdge(edgeId)
QueryNearbyEntryVolumes(position, radius)
GetStage / Branch context where canonical
```

Responses should be immutable snapshots/IDs, not references to mutable Navigation internals.

This allows another AI module to know:

- which Region a Bot/target occupies;
- whether a high-ground/camp point is actually reachable;
- whether a one-way link exists;
- the legal direction of a special connection;
- approximate path cost/difficulty;
- current stage/branch context.

### 14.2 Ownership/arbitration capability

A separate read-only runtime context should let cooperating plugins know when to yield:

```text
NavigationMovementOwned
TraversalActive
TraversalState
ScopedViewOwned
RecoveryActive
CurrentEdgeType / EdgeId
```

Examples:

- Smarter-Bot stuck/velocity logic yields while ZEPVE traversal owns movement.
- custom FOV/aim enhancement yields while scoped traversal owns view.
- combat code may continue perception/weapon logic if it does not mutate the owned movement/view domain.

### 14.3 Optional request surface

If generic non-ZEPVE Bot integrations later need special traversal, they should submit a request to Navigation rather than mutate BotController directly:

```text
request local goal / traversal edge
→ Navigation validates identity + graph + ownership
→ Navigation obtains the sole movement lease
→ action runs
→ result reported
```

Do not expose a generic “write movement now” API from the graph service.

### 14.4 NadeSystem integration

This is the strongest reverse-link candidate.

Graph context can improve recorded grenade/action selection:

```text
Bot Region
Current Route/Stage/Branch
Current/next Edge
          ↓
select only lineups/actions legal for this context
```

A grenade/action record could optionally reference Region/Edge/Stage tags. The grenade module still decides and performs its own grenade action, while Navigation remains movement authority.

Conversely, temporary smoke/fire/map-state effects may later contribute a **read-only edge cost/availability input** through a defined hazard service. They should not directly edit graph topology from arbitrary plugins.

### 14.5 Smarter-Bot integration

Graph/runtime context can prevent false “stuck” recovery:

- if current edge is `Wait`, low speed is legitimate;
- if a Door/Elevator edge is pending, do not fire generic repath/escape logic;
- if Traversal is active, do not modify velocity/crouch/view externally;
- if ordinary native movement is active and Navigation reports no progress, a bounded repath candidate may be useful.

This would make Smarter-Bot-style improvements **graph-aware** instead of competing with the graph.

### 14.6 Aim/FOV integration

Expose scoped view ownership. Aim/FOV enhancers yield during `TraversalEdge.Execute` when Navigation owns facing, then resume immediately on Exit/Abort.

This solves a class of 180-degree/view-fighting problems without disabling better combat aim globally.

### 14.7 Vision integration

BotVision-like services can provide read-only visibility/smoke context to BotAI or combat policy. Graph planners may consume a bounded hazard/visibility cost only after a concrete use case.

Do not let perception services alter `AssignedTarget` or graph reachability truth.

### 14.8 Weapon/Director integration

Region/stage metadata can later help select behavior/loadout profiles:

```text
open region
narrow corridor
boss arena
final hold
high-ground camp
special traversal section
```

The Director/WeaponSystem can consume these tags. They must not write Navigation state.

## 15. Compatibility modes

### ZEPVE PvE mode

```text
Navigation = sole final locomotion authority
BotController = low-level backend
BotAI = target authority
external enhancers = optional readers/advisors that yield on owned domains
```

### Generic Bot-Improver mode, later

A future adapter could use ZEPVE Navigation/MapTraversalGraph to improve ordinary Bots on maps with curated graph data:

- normal ground remains native NAV where useful;
- graph supplies difficult off-mesh links;
- Navigation temporarily acquires movement only for a selected special edge;
- control returns to native AI after Exit/Abort.

This can provide jump/crouch-jump/ladder/drop/camp reachability to ordinary bots without making ZEPVE own their complete tactical AI.

This is a later compatibility feature, not a v0.5 acceptance requirement.

## 16. Suggested capability seams

Names are conceptual, not frozen APIs.

```text
IMapTraversalContext        # read-only graph/map knowledge
INavigationRuntimeContext   # read-only ownership/current-edge state
ITraversalRequestService    # optional validated local traversal request
IPerceptionContext          # optional BotVision-like query service
IMapHazardContext           # later read-only route cost input
```

Rules:

- version every cross-plugin contract;
- snapshots/IDs over mutable references;
- bind Bot-specific requests to Core lifecycle identity;
- consumers cannot manufacture generations;
- mutating request APIs return an owned token/result and support cancellation;
- optional integrations fail back to native/ZEPVE baseline behavior;
- no integration may bypass MovementBridge final validation.

## 17. Version placement

### v0.3

No change. Finish baseline pursuit, Recovery permission, provenance, matched install and actual rollback.

### v0.4

Use research only to improve the **typed local traversal contract** and ownership arbitration:

- Entry / Execute / Exit / Abort;
- edge-local cooldown/failure;
- physical success checks;
- scoped view ownership;
- no direct external movement writers.

Do not import broad Bot-Improver behavior patches into v0.4.

### v0.5

Implement core `MapTraversalGraph` and chat-first authoring first.

Useful research-derived implementation details:

- spatial index for EntryVolumes/Regions;
- entry latch/hysteresis;
- candidate -> policy -> commit separation;
- raw capture -> compiler/simplifier -> semantic graph;
- bounded recent authoring buffer / optional “save last maneuver” workflow;
- runtime ownership/context diagnostics.

A minimal read-only graph/runtime context contract may be designed here if it does not delay the graph itself. Generic Bot-Improver compatibility is not part of the v0.5 release gate.

### v0.6

MapPlan becomes the canonical source for map/stage/entity state used by graph availability and other consumers.

### v0.7

WeaponSystem may consume stable map/region/stage context for policy. HUD may present Navigation/graph diagnostics without owning state.

### v0.8

Director/BotAI can add stable Bot behavior profiles and evidence-driven perception/awareness policy. BotRandomizer's generation-safe stable-profile pattern is a useful reference, but gameplay profiles remain a separate domain.

### v0.9

Evaluate optional compatibility/upstream adapters for Bot-Improver-style modules and ordinary bots. Maintain a mutation-domain compatibility matrix and prove no competing movement/view/perception writers in each supported combination.

## 18. Research-driven test requirements for future integrations

Any external enhancer integration must prove:

```text
no second final movement writer
no competing scoped view writer
no stale slot/incarnation work
no ownership token leak after round/map/unload
taken-over Human Bot is excluded where appropriate
optional plugin missing -> baseline still works
optional plugin reload -> baseline still works
one failed edge/action does not suppress unrelated routes
Wait/Door/Elevator does not trigger false stuck recovery
Traversal active -> external movement/view modifiers yield
Exit/Abort -> ownership returns immediately
perception enhancement does not replace AssignedTarget authority
map-specific behavior is data/adapter-driven rather than generic hard-code
```

## 19. Licensing / reuse boundary

This research recommends **ideas, contracts and behavior patterns**. It is not permission to copy upstream source wholesale.

Before importing source code from any reviewed repository:

- inspect that repository's current license and any dual/commercial terms;
- preserve notices/obligations;
- prefer clean reimplementation of small concepts when code reuse is unnecessary;
- record exact upstream revision for any derived patch or fork.

ZEPVE should depend on external plugins only when the capability has independent value and a stable, reviewable integration contract. Otherwise use the research as design evidence, not as a new dependency.

## 20. Agent handoff

Future agents should read this document together with:

```text
docs/MAP_TRAVERSAL_GRAPH.md
docs/VERSION_SCOPE_RESET.md
docs/PROJECT_AUDIT_20261011.md
DECISIONS.md
MIGRATION_AUTHORITY.md
```

Then read the owning component docs before changing runtime code.

Hard rule: this research is **not authorization** to reopen v0.3 scope, merge the advanced experimental PRs, add a second movement hook, or enable broad native AI patches without focused evidence.
