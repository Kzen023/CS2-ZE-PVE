# CS2-ZE-PVE

**[English](README.md) | 简体中文**

CS2-ZE-PVE（简称 ZEPVE）是一个面向 Counter-Strike 2 Zombie Escape 的 **整合与发布仓库**，目标是让 **1–6 名人类玩家**可以通过 Bot 僵尸获得稳定的 ZE PvE / Coop 体验。

有可用 NAV 时优先使用 Valve 原生导航；无 NAV 或原生导航失效时，使用玩家 Trail 作为后备路径来源。

## 项目族

| 仓库 | 定位 | 是否进入正式 ZEPVE 发布 |
| --- | --- | --- |
| **CS2-ZE-PVE** | 套件整合、Core/runtime、配置、兼容、打包和 Release | 是 |
| **[ZEPVE-Navigation](https://github.com/kzen1023/ZEPVE-Navigation)** | 独立导航组件：Trail、Valve NAV、卡住检测和 Recovery | 是，固定到明确 commit/version |
| **[ZEPVE-Lab](https://github.com/kzen1023/ZEPVE-Lab)** | Bot/UserCmd/Native/引擎行为可复现实验 | 否 |
| `ZEPVE-CS2Fixes` | 如有必要才建立的 CS2Fixes 专用兼容 fork | 只有明确固定版本时 |
| `ZEPVE-MovementBridge` | 只有现有 Bot API 无法满足需求时才考虑的 Native bridge | 成熟后才可能进入 |

## 整合模式

ZEPVE 借鉴 CS2-Bot-Improver 这类整合项目的核心做法：

```text
独立组件仓库
     │
     │ 测试 / 合并 / 验证
     ▼
固定组件 commit
     │
     ▼
CS2-ZE-PVE 整合仓库
     │
     │ 套件验证 / 打包
     ▼
可直接安装的 ZEPVE Release
```

正式组件不会让整合仓库自动追随其最新 `main`。主仓库通过 Git submodule 固定使用的具体组件 commit；组件升级本身是一项独立、可审查的整合变更。

`ZEPVE-Lab` 不属于正式发布链。实验成功后，应先根据证据重新设计并实现到真正负责该功能的生产仓库，再进入套件。

## 运行目标

- ZE 地图兼容优先
- 人类保持 PvE 玩家身份，僵尸由 Bot 承担
- 有 NAV 时优先 Valve NAV
- 无 NAV 时使用玩家 Trail
- 与 CS2Fixes / ZombieReborn 协作，但避免重复状态所有权
- 低服务器开销，尽量减少逐图配置
- HUD、Weapons、Director 等功能保持可选
- 给 Bot/Nav 开发者提供稳定扩展边界

## 主仓库职责

```text
CS2-ZE-PVE/
├─ src/                         # Core / Map 等套件自有模块
├─ components/
│  └─ ZEPVE-Navigation/         # 固定版本的 Git submodule
├─ configs/
├─ maps/
├─ integrations/
├─ release/                     # 打包 / manifest 定义
├─ docs/
└─ .github/
```

已经拆成独立生产仓库的组件，不应再在 `src/` 下保留第二份实现。

## 发布模式

服务器管理员最终应该只需要下载 **一个 ZEPVE Release**，而不是手工拼装多个仓库。

Release 应记录：

- ZEPVE suite version 与 commit
- 所有第一方组件的精确 commit/version
- 测试过的 CS2 build
- Metamod 版本
- CounterStrikeSharp 版本
- 使用时的 CS2Fixes version/commit
- Native 依赖和平台信息

Release ZIP 应尽量整理成可以直接复制进 `game/csgo` 的结构。

## 当前优先级

1. 导入现有可工作的 PvE 源码到主仓库。
2. 建立可复现构建和 Release staging。
3. 在 `ZEPVE-Navigation` 独立开发导航，并把验证过的版本固定进主仓库。
4. 高风险 Bot/UserCmd/Native 行为先在 `ZEPVE-Lab` 验证。
5. 面向用户的文档保持英文 + 简体中文。

## 修改流程

README、仓库页面、管理说明和轻量模板在仓库所有者允许时可以直接修改 `main`。

源码行为、构建、依赖、组件版本固定以及 Release 内容变更，默认走：

```text
agent/* / feat/* / fix/*
        ↓
       PR
        ↓
验证 / Review
        ↓
       main
```
