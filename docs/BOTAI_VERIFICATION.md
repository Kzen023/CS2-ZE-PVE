# BotAI verification

Source baseline: merged v0.2c `ed480bd`. BotAI runtime evidence must be recorded independently of earlier Core smoke. Automated/model checks: 127/127 PASS (72 retained, 55 new), Release build zero warnings/errors against installed CSS 1.0.376.

Before deployment all BotAI runtime items are **NOT TESTED**. The following acceptance procedure is bounded and uses existing authority; no old writer is enabled.

| Scenario | Procedure / expected result | Initial runtime status |
| --- | --- | --- |
| Shared ABI/load | Stopped matched deployment; `css_plugins list`, `meta list`, `bc_status`, `css_zepve_status`, `css_zepve_ai`; one LOADED BotAI/Core/bridge, zero native writes | NOT TESTED |
| Assignment | Real CT + release: `css_zepve_bot <current T bot slot>` has valid CT target; repeat stable version when identities stable | NOT TESTED |
| Multiple humans | 2–6 real CT humans, query every bot; max/min count <= 1, no churn once stable | NOT TESTED |
| Binding probe | `css_zepve_ai_probe <bot> 2`; ACCEPT unchanged. Queue 30 seconds before invalidation; REJECT | NOT TESTED |
| Bot life | Queue probe, controlled bot death; binding absent while dead, new pawn/version after external ZR respawn, original probe rejected | NOT TESTED |
| Human life | Queue probe, actual human death/respawn; old target binding rejected, new pawn token after CT spawn | NOT TESTED |
| Disconnect/reconnect | Queue probe, actual human disconnect; no eligible target => bindings zero; reconnect CT => new token/version | NOT TESTED |
| Round/map | Probe then `mp_restartgame 1` / `changelevel de_mirage`; old probe rejected, no bindings while closed/preparing, fresh bindings after release | NOT TESTED |
| Hot/late/provider | Probe then reload active BotAI ID; old module work rejected, fresh GUID. Core hot reload drops old token scope; late Core Unbound => no assignments | NOT TESTED |
| Reacquire | `css_zepve_reacquire <bot>`, events/status; coalesced current window or bounded 3-second success/timeout + 10-second cooldown | NOT TESTED |
| No interference | Native quota/team remain Core's policy, external respawn still works; BotAI native writes 0; no new scheduler/movement | NOT TESTED |

Native awareness field writes / efficacy: **NOT TESTED / not implemented**. ObserveOnly success is observation, not a perception repair claim. Legacy Recovery notification integration is not implemented; Navigation remains a future consumer. Full maps, endurance, multiple human physical layouts and native team-tally parity remain NOT TESTED.
