# CS2-ZE-PVE

**[English](README.md) | 简体中文**

CS2-ZE-PVE（简称 ZEPVE）是一套以 **1–6 人单机 / 合作体验 Zombie Escape 地图** 为核心目标的轻量 PvE 运行框架。

## 目标

- ZE 地图兼容优先
- AI 僵尸 Bot
- 有 NAV 时优先使用 Valve 原生导航
- 无 NAV 时使用玩家 Trail 导航
- 保留 CS2Fixes / ZombieReborn 的成熟 ZE 语义
- 低服务器开销
- 尽量减少逐图配置
- HUD、武器、Director 等功能模块化可选
- 为 Bot/Nav 开发者和地图作者提供扩展接口

## 当前状态

私人早期开发仓库。

当前优先级：

1. 稳定现有 PVE Core。
2. 清晰拆分 Navigation 职责。
3. 实现无 NAV TrailDriver。
4. 通过 CS2Fixes 兼容层保留成熟 ZE 地图机制。
5. 对外文档保持中英文双语。

## 模块结构

```text
ZEPVE.Abstractions
├─ ZEPVE.Core
├─ ZEPVE.Navigation
├─ ZEPVE.Map
├─ ZEPVE.Hud
├─ ZEPVE.Weapons
└─ ZEPVE.Director
```

ZE 始终是主模式。未来可扩展 Survival、Horde 和官方图 PvE，用于提升 CS2 单人及 Coop 可玩性。
