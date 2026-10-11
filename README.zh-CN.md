# CS2-ZE-PVE

**[English](README.md) | 简体中文**

CS2-ZE-PVE（ZEPVE）是一套面向 **Counter-Strike 2 Zombie Escape** 的轻量 Coop PvE 运行框架，目标是让 **1–6 名人类玩家**可以和 Bot 僵尸稳定游玩 ZE 地图。

项目始终以 ZE 为核心：尽量保留地图原有机制，同时减少服务器和地图作者需要额外维护的配置。

## 主要能力

- Bot-only 僵尸 PvE
- 有可用 NAV 时优先使用 Valve 原生导航
- 原生导航不可用或无进展时使用 per-Human Runtime Trail
- 卡住检测与分层 Recovery
- BotAI `AssignedTarget` 目标权威
- 与 CS2Fixes / ZombieReborn 协作
- 单一经过验证的最终 movement writer
- 可选 HUD 与 WeaponSystem 组件

## 项目组成

| 仓库 | 用途 | 状态 |
| --- | --- | --- |
| **CS2-ZE-PVE** | 主运行框架、Core/BotAI、整合、迁移 authority、打包与发布 | 当前主整合仓库 |
| **[ZEPVE-Navigation](https://github.com/Kzen023/ZEPVE-Navigation)** | Runtime Trail、路线/traversal 选择、移动编排与 Recovery | v0.3 baseline + v0.4 traversal 开发 |
| **[ZEPVE-BotController](https://github.com/Kzen023/ZEPVE-BotController)** | 底层 leased movement backend、command capture/application 与 scoped traversal execution | Backend 组件；advanced traversal 仍是实验状态 |
| **[ZEPVE-Lab](https://github.com/Kzen023/ZEPVE-Lab)** | 隔离验证引擎/Bot/移动行为并保存证据 | 仅实验用途，永不进入 Release runtime |
| **[ZEPVE-HUD](https://github.com/Kzen023/ZEPVE-HUD)** | 可选玩家展示组件 | v0.7 迁移目标，尚未 pin |
| **[ZEPVE-WeaponSystem](https://github.com/Kzen023/ZEPVE-WeaponSystem)** | 可选 PvE 武器/购买策略 | v0.7 迁移目标，尚未 pin |

独立仓库只有在职责边界、构建和运行行为经过验证后才会正式接入套件。创建仓库或 pin 一个实验 revision，本身不代表它已经 release-ready。

## 当前版本边界

Roadmap 已重新切分，避免 advanced movement research 无限扩大 v0.3 merge gate：

```text
v0.3  Navigation baseline
      AssignedTarget -> Native -> Trail -> Recovery

v0.4  Advanced traversal
      UserCmd/timed payload、jump/crouch-jump、scoped view、MotionClip

v0.5  Persistent routes
      RecordedRoute、RouteGraph、typed TraversalEdge、Entry/Exit volume
```

复杂跳箱复刻、旋转 replay、精确 Human physics parity 都属于 v0.4，不是 v0.3 baseline 必须项。

## Recorder / replay 归属

当前不需要额外创建 production Recorder 仓库。

```text
ZEPVE-BotController -> 负责 bounded command 数据如何捕获/执行
ZEPVE-Navigation    -> 负责动作/路线数据代表什么、何时使用
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

已接受的 v0.2 Core/BotAI foundation 继续作为稳定基线。当前工作重点是把 v0.3 Navigation baseline 与已经提前完成的大量 v0.4 traversal research 重新分层，然后产出一个干净的 matched v0.3 acceptance/test package，而不是继续把 advanced traversal 失败当成 baseline blocker。

见 [ROADMAP.md](ROADMAP.md) 与 [docs/VERSION_SCOPE_RESET.md](docs/VERSION_SCOPE_RESET.md)。

## 文档

- [English README](README.md)
- [旧版迁移 baseline](legacy/README.md)
- [迁移 authority](MIGRATION_AUTHORITY.md)
- [版本 scope reset](docs/VERSION_SCOPE_RESET.md)
- [开发路线](ROADMAP.md)
- [贡献指南](CONTRIBUTING.md)
- [仓库与发布管理](GITHUB_MANAGEMENT.md)
- [架构决策](DECISIONS.md)
