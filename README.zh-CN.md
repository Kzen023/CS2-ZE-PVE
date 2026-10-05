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
- HUD、Weapons、Director 等可选模块
- 为导航、地图和 Bot 集成保留扩展边界

## 项目组成

| 仓库 | 用途 |
| --- | --- |
| **CS2-ZE-PVE** | 主运行框架、整合、配置、文档与发布 |
| **[ZEPVE-Navigation](https://github.com/Kzen023/ZEPVE-Navigation)** | 负责有 NAV / 无 NAV 地图中的僵尸 Bot 导航与移动 |
| **[ZEPVE-Lab](https://github.com/Kzen023/ZEPVE-Lab)** | 用于隔离验证 Bot、移动和引擎行为的实验仓库 |

只有确实需要独立生命周期的组件才会拆成新的仓库。

## 安装

ZEPVE 目前仍处于私人早期开发阶段，暂时没有公开 Release。

开始发布后，可直接安装的套件包会从本仓库提供，并按 `game/csgo` 的部署结构整理。

## 当前状态

现阶段主要工作是导入并稳定现有 PvE 原型、建立可复现构建，以及把独立 Navigation 组件正式接入主套件。

里程碑见 [ROADMAP.md](ROADMAP.md)。

## 文档

- [English README](README.md)
- [开发路线](ROADMAP.md)
- [贡献指南](CONTRIBUTING.md)
- [仓库与发布管理](GITHUB_MANAGEMENT.md)
- [架构决策](DECISIONS.md)

实现细节和仓库管理流程不放在首页展开，避免 README 变成开发设计文档。
