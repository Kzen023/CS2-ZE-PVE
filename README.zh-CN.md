# CS2-ZE-PVE

**[English](README.md) | 简体中文**

CS2-ZE-PVE（简称 ZEPVE）是一套面向 **Counter-Strike 2 Zombie Escape** 的轻量 PvE / Coop 运行框架，目标是让 **1–6 名人类玩家**可以和 Bot 僵尸进行稳定的 ZE 游戏。

项目始终以 ZE 为核心：尽量保留地图原有机制，同时减少服务器和地图作者需要额外维护的配置。

## 主要功能

- Bot-only 僵尸 PvE
- 有可用 NAV 时使用 Valve 原生导航
- 无可用 NAV 时使用玩家 Trail 导航
- Bot 卡住检测与恢复
- 与 CS2Fixes / ZombieReborn 协作
- 面向小人数场景的低服务器开销
- 可选 HUD 与 WeaponSystem 组件
- 为导航、地图和 Bot 集成保留扩展边界

## 项目组成

| 仓库 | 用途 | 状态 |
| --- | --- | --- |
| **CS2-ZE-PVE** | 主运行框架、Core/BotAI 整合、旧版迁移 baseline、配置、文档与发布 | 当前主整合仓库 |
| **[ZEPVE-Navigation](https://github.com/Kzen023/ZEPVE-Navigation)** | 有 NAV / 无 NAV 地图中的僵尸 Bot 导航与移动 | 已登记设计/整合 revision；运行时基线仍在开发 |
| **[ZEPVE-HUD](https://github.com/Kzen023/ZEPVE-HUD)** | 可选的玩家 HUD / 展示组件 | 迁移目标，尚未 pin |
| **[ZEPVE-WeaponSystem](https://github.com/Kzen023/ZEPVE-WeaponSystem)** | 可选的 PvE 武器平衡与购买组件 | 迁移目标，尚未 pin |
| **[ZEPVE-Lab](https://github.com/Kzen023/ZEPVE-Lab)** | 隔离验证 Bot、移动和引擎行为 | 仅实验用途 |

独立仓库只有在职责边界、构建和运行行为经过验证后才会正式接入套件。创建仓库或固定一个设计 revision，本身不代表它已经成为可发布的运行依赖。

## 旧版 Baseline

现有可运行插件保存在 [`legacy/`](legacy/README.md) 下，并作为新架构迁移期间的回归基线。

目前旧版包含主 PvE 运行插件、武器平衡插件和 ZE Assist/HUD 插件。迁移原则是保留已经验证的行为，再逐步重新划分 ownership，而不是把旧代码当成废弃原型重写。

迁移期间每个会改变状态的行为只允许一个 active writer；交接规则记录在 [`MIGRATION_AUTHORITY.md`](MIGRATION_AUTHORITY.md)。

## 安装

ZEPVE 目前仍处于私人早期开发阶段，暂时没有公开 Release。

开始发布后，可直接安装的套件包会从本仓库提供，并按 `game/csgo` 的部署结构整理。

## 当前状态

现阶段主要工作是复现旧版 baseline、明确迁移 authority、建立安全的 Core 身份/生命周期基础，并在不随意改变已验证玩法的前提下接入独立组件。

里程碑见 [ROADMAP.md](ROADMAP.md)。

## 文档

- [English README](README.md)
- [旧版迁移 baseline](legacy/README.md)
- [迁移 authority](MIGRATION_AUTHORITY.md)
- [开发路线](ROADMAP.md)
- [贡献指南](CONTRIBUTING.md)
- [仓库与发布管理](GITHUB_MANAGEMENT.md)
- [架构决策](DECISIONS.md)

实现细节和仓库管理流程不放在首页展开，避免 README 变成开发设计文档。
