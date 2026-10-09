# v0.2c 生命周期 / authority 迁移

[English](CORE_LIFECYCLE_MIGRATION.md) | 简体中文

PR #9 已按用户授权 Ready → 合并，剩余风险保留 deferred acceptance。本阶段从合并后的 main `745fa4a` 开始；PR #9 的 observer 实测不能当作新 writer handoff 的实测。

## 责任边界

Core 是唯一 PlayerRegistry / lifecycle validity source，通过 shared Abstractions 的 `ICoreLifecycle`、`IPlayerContext` 和 `ICoreWorkScope` 提供只读状态与失效能力。`SuiteRuntime.Current` 只是单一 provider 引用，不是第二套 registry；重复 provider/bridge 会被拒绝。

Core 接管 map/round/spawn 协调、原有静态 profile、轮次准备、感染释放策略、Bot quota 和全局 Bot team 命令。适配后的 ZRPVE 删除旧执行函数及 `_roundToken`，只保留配置读取、倒计时展示、Trail/Recovery/HUD 本地逻辑。Core 必须看到适配 bridge 才允许 gameplay 命令；缺少依赖时停止，绝不自动恢复旧 writer。

感染由 ZR 执行；Core 保留原有设置与释放时序。**复活执行仍是 ZombieReborn / 现有兼容 runtime**；Core 只迁移原静态复活策略，未增加 respawn scheduler/API 调用。Recovery 的选点、距离校验和 teleport 函数与接受的 baseline 一致；只把延迟 callback 改为 Core token/取消机制。

## 时序和安全

原流程保持：round start 立即同步倒计时 → 1 秒后应用 profile → 再等待配置 infection_delay → release → bot_add_delay 后 quota → release_move_delay 后 team pass。现有 16 秒配置意味着 Core release 在 +17 秒；没有顺便“修正”为 +16 秒。solo/duo/coop/group quota 与复活延迟 7/5/4/3 秒保持原值。

player token 包含 plugin GUID、map/round epoch、slot、connection/pawn generation。执行前重新读取实体并验证 token；controller-only 操作也检查 connection/pawn generation。消费者卸载须 Dispose 自己的 work scope。Core 所有 pending work 上限 256；probe 上限 16。Map-only 的设置/采样明确只验证 plugin/map，跨轮继续采样，但每次读取当前实体；player-specific 延迟必须使用完整 player token。

FlightRecorder 是内存 256 条 ring，记录生命周期、角色/pawn 变化、stale reject、取消和策略切换，detail 限 160 字符，无自动磁盘写入或高频日志。`css_zepve_status` 扩展 Core authority/profile/phase/respawn executor；`css_zepve_events [1..64]` 读取有界记录。没有 BotAI/Navigation/Trail 诊断。

## Hot / late load

已有玩家立即 bootstrap。原子 Core hot reload 用 shared ABI 中只含值的 owned plan 保留待执行绝对 deadline，生成新 GUID，取消旧 player work，不重复已执行的 countdown/profile/release/quota，并重绑 map 服务而保留同地图 legacy Trail 数据。

手动 unload 不保留 plan；mid-map load 只观察和应用 map 设置，Phase=Unbound，等下一轮建立可靠的 round policy，不猜测感染时刻或重置 Bots。这期间依赖 Core 的 Recovery 可能暂停。升级 shared ABI 本身需要停服，不能用单个插件热更新代替整套升级。

## 配套构建、安装和回滚

需要 .NET 10 SDK、安装的 CounterStrikeSharp API/logging 程序集及包含 baseline Git object 的 checkout。

```powershell
./scripts/build-core.ps1 -CounterStrikeSharpApiPath '<server>/game/csgo/addons/counterstrikesharp/api/CounterStrikeSharp.API.dll' -OutputDirectory '<新的 staging 目录>'
```

生成 Core、适配 ZRPVE 和 shared Abstractions 的 8 个文件及 revision/hash manifest。Abstractions 仅安装在 `shared/ZEPVE.Abstractions/ZEPVE.Abstractions.dll`，不能各插件保存私有副本。该布局已经核对当前 API 的 shared resolver；真实跨插件加载仍需实测。

**先停止该安装中的 CS2，再安装 clean committed 产物：**

```powershell
./scripts/install-core-lifecycle.ps1 -ServerRoot '<CS2 安装目录>' -PackageDirectory '<staging 目录>'
```

安装脚本拒绝运行中的 CS2、dirty manifest、路径/API/hash 不符；先备份精确文件清单，再整体替换配套 DLL、移除旧私有 contract 副本并核对哈希。不覆盖配置、mutable data、WeaponBalance、ZEAssist 或 pre-Core backup。不要单独替换 Core/ZRPVE，也不要保留能加载的旧 monolithic ZRPVE 副本。

停服后用该次 backup 的 `restore.ps1 -VerifyOnly` 检查，再运行 `restore.ps1` 整套回滚；拒绝覆盖后来改过的文件。2026-10-09 已完成真实停服安装及备份校验；真实服务器执行回滚仍为 **NOT TESTED**。

## 验证与剩余风险

Core / Abstractions / 适配 ZRPVE Release：PASS，0 警告/错误。model/source checks：**72/72 PASS**。PowerShell 语法及 offline installer/restore roundtrip：PASS。

**部署：PASS。** 2026-10-09 已停服安装 `1cf6aa21503316ab2963c35e6e222312e14131ad` 的配套包：8/8 文件哈希一致，47 个配置/无关插件文件未变，回滚清单校验通过。备份目录为 `ZEPVE_Backup/v0.2c-20261009-151119-2996277`，尚未实际执行回滚。

此前用户“游戏内插件正常工作”仅证明概括的游戏内 smoke。随后在 2026-10-09 实际执行了 dust2/mirage **配套单人 runtime handoff smoke：PASS**，有 RCON、Core/CSS 日志及控制台快照。详见[真实验收记录与保留项目](CORE_MATCHED_RUNTIME_VERIFICATION.zh-CN.md)。本次覆盖 shared ABI/gate、1 Human/10 ZombieBots、round/profile/release/quota/team、外部复活、Recovery placement、换图、断连重连、三阶段 hot reload、manual late load 与 bridge loss。多人档位、精确时序、native team-tally parity、长时间性能及实际回滚仍为 NOT TESTED/PARTIAL；没有追溯修改 PR #9 的 observer 证据。

最小 runtime 验收：配套加载和单一 shared ABI/writer；原静态 quota、+1 preparation 和释放时序；round/map 取消；断连、同 slot 复活和 pawn 替换；ZR 复活及 Recovery 回归；在 preparing/waiting/released 三阶段 hot reload；manual late load 与依赖丢失；人类断连/hibernation；recorder 上限、服务重复、性能和整套回滚。每项期望见英文验证表。使用 `css_plugins list` 中当前 LOADED 的纯数字 ID；新版 ModuleName 是 `ZEPVE Core`，不要沿用旧 observer 名称/旧 session ID。

保留的技术债：legacy 共享 Trail、per-slot watch/display 集合、混合 Recovery/HUD/config、休眠 escort、Recovery 后游走；ZEAssist/WeaponBalance 的独立延迟工作仍未迁移。没有新的目标、AI、Navigation、Director、武器或地图系统。

v0.2c 源码、自动验证、配套部署和单人 runtime handoff smoke 完成；建议 PR #10 带明确 deferred tests 进入 Ready for review。不得将保留项目写成 PASS，不自动合并、不进入 BotAI/Navigation。
