# CS2-ZE-PVE

**[English](README.md) | 简体中文**

CS2-ZE-PVE（ZEPVE）是一套面向 **Counter-Strike 2 Zombie Escape** 的合作 PvE 框架，核心玩法是 **Human 玩家对抗 AI Zombie**。

项目始终以 ZE 为核心，主要面向约 **1–6 名人类玩家**的小队游玩。能交给 Valve/原生 Bot 的普通行为就尽量复用，只在 ZE 特有的目标管理、地图路线知识和特殊移动上补能力，减少与 Valve AI 的冲突。

## 核心模型

```text
AssignedTarget       = 这个 Zombie 要追谁
MapTraversalGraph    = Navigation 怎么合法到达这个 Human
Valve/native AI      = 正常感知、战斗和可用时的普通移动
ZEPVE Navigation     = 路线/岔路选择、rejoin/replan、特殊 traversal
MovementBridge       = 唯一授权的底层 movement commit 路径
```

PvE 中 Human 玩家始终保持 Human。玩家死亡只改变生命周期状态，ZEPVE 不把 Human 玩家转成 Zombie 玩家。

## 主要能力

- Bot-only Zombie PvE
- 普通移动优先复用 Valve/native navigation
- 未录制地图、无 NAV 或普通追逐不足时使用 per-Human Runtime Trail fallback
- BotAI `AssignedTarget` 目标权威
- Navigation 单一 movement owner 与分层 Recovery
- 规划中的持久化 `MapTraversalGraph`：主路、岔路、补路、Region 与 typed TraversalEdge
- 合法岔路分流：玩家全部走主路时，部分 Zombie 也可以走侧路/上层/下层绕后
- 被武器击退后按当前物理位置 rejoin/replan，而不是把 Bot 吸回录制轨迹
- 与 CS2Fixes / ZombieReborn 协作
- 可选 HUD 与 WeaponSystem 迁移目标

## 项目组成

| 仓库 | 用途 | 状态 |
| --- | --- | --- |
| **CS2-ZE-PVE** | 主运行框架、Core/BotAI、整合、迁移 authority、打包与发布 | 当前主整合仓库 |
| **[ZEPVE-Navigation](https://github.com/Kzen023/ZEPVE-Navigation)** | Runtime Trail、路线/岔路规划、Graph/traversal 执行、移动编排与 Recovery | v0.3 baseline + 后续 graph/traversal 工作 |
| **[ZEPVE-BotController](https://github.com/Kzen023/ZEPVE-BotController)** | 底层 leased movement backend、command capture/application 与 scoped traversal execution | Backend 组件；advanced traversal 仍是实验状态 |
| **[ZEPVE-Lab](https://github.com/Kzen023/ZEPVE-Lab)** | 隔离验证引擎/Bot/移动行为并保存证据 | 仅实验用途，永不进入 Release runtime |
| **[ZEPVE-HUD](https://github.com/Kzen023/ZEPVE-HUD)** | 可选玩家展示组件 | v0.7 迁移目标，尚未 pin |
| **[ZEPVE-WeaponSystem](https://github.com/Kzen023/ZEPVE-WeaponSystem)** | 可选 PvE 武器/购买策略 | v0.7 迁移目标，尚未 pin |

独立仓库只有在职责边界、构建和运行行为经过验证后才会正式接入套件。创建仓库或 pin 一个实验 revision，本身不代表它已经 release-ready。

## 当前版本边界

```text
v0.3  Navigation baseline
      AssignedTarget -> Native -> Trail -> Recovery
      leased UserCmd movement、lifecycle、matched install/rollback

v0.4  Typed local traversal
      Entry -> Execute -> Exit/Abort
      只有动作确实需要时才使用 timed/signed-subtick payload，
      jump/crouch-jump、scoped traversal view、short MotionClip

v0.5  Persistent MapTraversalGraph
      主路 / 岔路 / 补路 authoring，
      Region/Route/Node/TraversalEdge，
      route rejoin/replan、branch diversity、curated map data

v0.6  MapPlan + CS2Fixes integration
      stage/mechanism legality、canonical map state、PvE respawn rules
```

clean v0.3 cuts 已确认是正确的隔离起点，但**不能原样视为 release-ready**。普通追逐缺陷、Recovery permission、artifact provenance 和 fresh real-server acceptance 仍必须在 merge 前完成。

## 路线录制原则

录制只是 **authoring 输入**，不是运行时整段 replay。

普通 ZE 地图的测试员只需要理解三种录制用途：

```text
主路  — 正常通关路线
岔路  — 真正不同的入口/楼层/走廊/门/拓扑路线
补路  — 已知区域之间缺失的一小段连接
```

录制时可以临时采集丰富的 Human movement 数据，之后会简化成语义化 Graph。门、电梯、等待、地图 TP、普通跳跃等最终成为 Edge 语义，而不是再做一套套不同的录制系统。只有普通移动/Primitive 确实不够时，才进入高级 TraversalCapture。

Zombie 不需要复制 Human 实际走过的分支。只要岔路当前合法，即使所有 Human 都走主路，也可以让部分 Zombie 走其他路线，从侧面或后方形成压力；优先通过 route cost、拥挤和稳定分流实现，不另外造一套“包抄 AI”。

玩家武器击退只改变 Bot 的当前物理位置：能继续就继续，偏离后 bounded rejoin，再 graph replan/换合法岔路；Recovery 仍然只是最后且需要单独许可的 fallback。

## Recorder / replay 归属

当前不需要额外创建 production Recorder 仓库。

```text
ZEPVE-BotController -> 负责 bounded command 数据如何捕获/执行
ZEPVE-Navigation    -> 负责路线/traversal 数据代表什么、何时使用
ZEPVE-Lab           -> 只负责实验与证据
```

持久化路线 authoring 继续留在 Navigation；只有将来 offline tooling 真正形成独立价值时，才考虑单独的工具仓库。

## 旧版 Baseline

现有可运行插件保存在 [`legacy/`](legacy/README.md) 下，并作为新架构迁移期间的回归基线。

迁移期间每个会改变状态的行为只允许一个 active writer；交接规则记录在 [`MIGRATION_AUTHORITY.md`](MIGRATION_AUTHORITY.md)。

## 安装

ZEPVE 目前尚未提供通用公开 Release。测试包和未来 Release 都由本仓库使用精确 matched component revision 组装，并按 `game/csgo` 结构部署。

不要手工混搭不同实验 PR 的 DLL/revision。

## 当前状态

v0.1 和 v0.2 已作为历史基础阶段正式收口。已接受的 v0.2 Core/BotAI foundation 继续作为稳定运行基线。

当前工程阶段是审计后的 Phase 1：收口 BotController #3、Navigation #3、Suite #15 这条 clean v0.3 baseline，制作 provenance-bound matched Test Preview，完成真实 baseline acceptance 与 actual whole-package rollback，然后停止，不在同一阶段继续做 v0.4 traversal。

见 [ROADMAP.md](ROADMAP.md)、[docs/ZE_PATH_SYSTEM.md](docs/ZE_PATH_SYSTEM.md)、[docs/MAP_TRAVERSAL_GRAPH.md](docs/MAP_TRAVERSAL_GRAPH.md)、[docs/VERSION_SCOPE_RESET.md](docs/VERSION_SCOPE_RESET.md) 与 [docs/PROJECT_AUDIT_20261011.md](docs/PROJECT_AUDIT_20261011.md)。

## 文档

- [English README](README.md)
- [ZE 路径系统规则](docs/ZE_PATH_SYSTEM.md)
- [MapTraversalGraph 规划](docs/MAP_TRAVERSAL_GRAPH.md)
- [旧版迁移 baseline](legacy/README.md)
- [迁移 authority](MIGRATION_AUTHORITY.md)
- [版本 scope reset](docs/VERSION_SCOPE_RESET.md)
- [项目审计与 Phase 1 handoff](docs/PROJECT_AUDIT_20261011.md)
- [开发路线](ROADMAP.md)
- [贡献指南](CONTRIBUTING.md)
- [仓库与发布管理](GITHUB_MANAGEMENT.md)
- [架构决策](DECISIONS.md)
