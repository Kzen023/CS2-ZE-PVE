# v0.2d BotAI

新 baseline 为 PR #10 合并后的 `main @ ed480bd500ca01e5ed4da6a61a7391944d38905c`，v0.2c 已声明的 deferred acceptance 不自动升级。`AssignedTarget` 是新能力，不是 legacy 提取。

## 权威与目标模型

独立 `ZEPVE.BotAI.dll` 消费唯一 Core registry。Core 保持身份/lifecycle/round/quota/team/静态 respawn policy；ZombieReborn/CS2Fixes 保持唯一复活执行器；legacy Recovery、Trail、HUD、武器均不改。

窄接口 `IBotAi` 通过唯一 `BotAiRuntime.Current` 发布。绑定包含 BotAI 模块 GUID、Bot 和目标的完整 Core token、BindingVersion。token 含 Core plugin/map/round/slot/connection/pawn generations。Getter 和 callback 重新解析双端当前实体；模块 reload、Core provider 更换、目标换绑/断线/死亡/重生、Bot 生命周期、换轮/换图都使旧绑定失效。不建立第二套玩家 registry，不缓存 native 实体。

Core 无效、bridge 缺失、Unbound/Closed/Disabled/准备期/等待感染、角色不符或死亡时不允许分配。BotAI reload 不恢复旧请求；Core reload 换 scope、换绑定；late load 只有 Core 已 Released 才能分配。

## 分配策略

保持有效目标。新 Bot 选择当前分配数最少的存活 CT，平手用轮转 slot。人口变化只移动超额绑定，使最大/最小分配数差不超过 1。适用 1–6 人，不使用 Valve Enemy、可见性、距离或 engine tally 决定 AssignedTarget。无地图几何策略，Navigation 以后决定如何到达目标。

事件标记 + 每 0.5 秒 reevaluation；单个 0.25 秒 dispatch timer，unload 销毁。64 个 bounded Bot state；没有每 tick 全实体扫描。最多 16 个 probe，callback 校验 Core 双端 token、角色、gameplay permission、模块 lifetime 和 BindingVersion。

## Native / Awareness 边界

已审查安装日志对应 BotController v0.7.1 的精确源码 `0ae8f18fd6872a369cb984e0e95e5a352be092fe`：[源码](https://github.com/XBribo/CS2-Bot-Controller/tree/0ae8f18fd6872a369cb984e0e95e5a352be092fe)。它能锁 Update/Upkeep/aim、回放/注入 UserCmd、控制武器/买枪/profile；未发现 Enemy/Alert/IgnoreEnemies/look-around 字段写入。版本日志不是可复现二进制证明。

BotAI 不调用它的 API/exports/commands，不抢锁、不回放、不控制 movement/角度/武器。外部锁可能让 Valve 无法重新感知，BotAI 只记录 timeout。Navigation pin `14cda1f` 只有设计文档，未改 pin；未来唯一 final movement/native-goal writer 仍为 Navigation。

当前 awareness 为 **ObserveOnly，NativeWrites=0**。通过已安装 CSS 1.0.376 schema 只读 Enemy raw handle、Visible/Aiming/Attacking/Sleeping/AllowActive。将完整 Enemy handle 与当前目标 pawn 比较；Engine Enemy 不是 pursuit 权威。schema 不可用时保留目标、显示 observation unavailable。

绑目标后观察 3 秒；请求 coalesce，不延长窗口。引擎看到已分配目标且 Visible/Aiming/Attacking 时 Succeeded，超时 TimedOut；只表示观察到的结果，不表示辅助操作导致成功。连续丢感知 2 秒后可再次观察，10 秒 cooldown。没有强制 Visibility、Enemy 或其他 native 写入，没有 hate table。legacy Recovery 未修改，尚未发出 RecoveryTeleport 通知；API 已可供未来 Navigation 请求，不能宣称自动完成 post-teleport handshake。native assist 的实际效果仍须 Lab 证据。

## 诊断命令

服务器 console 或 `@css/root`：

```text
css_zepve_ai
css_zepve_bot <bot-slot>
css_zepve_ai_events [1..64]
css_zepve_reacquire <bot-slot>
css_zepve_ai_probe <bot-slot> [0.1..60 秒]
```

显示 AssignedTarget、BindingVersion、选择/换绑原因、双端 generations、permission gate、reacquire 状态/deadline、combat 与 engine observation。FlightRecorder 上限 256，detail 160 字符，读取最多 64；无自动磁盘写入、无每 tick 日志。显式 probe 输出 ACCEPT/REJECT。

## 整套安装/回滚

```powershell
./scripts/build-core.ps1 -CounterStrikeSharpApiPath '<安装的 API DLL>' -IncludeBotAi -OutputDirectory '<新的 staging 目录>'
./scripts/install-core-lifecycle.ps1 -ServerRoot '<CS2 根目录>' -PackageDirectory '<staging>'
```

要求 clean commit、API hash 一致、CS2 停止。可选 `-AllowUnmoddedClient` 只允许已显示窗口、非 dedicated、可完整检查 loaded modules 且没有 addon/MetaMod/CSS/CS2Fixes/BotController 模块的客户端；默认仍全部拒绝。专用服、加载插件或无法判断的客户端始终拒绝。整套 11 个文件：Core、adapted ZRPVE、BotAI 的 DLL/deps/PDB 与唯一 shared Abstractions DLL/PDB。先备份旧文件/hash，删除插件私有 contracts；config/native/其他插件保持。停服且退出客户端后用备份 `restore.ps1 -VerifyOnly` 验证，再执行 `restore.ps1` 整套回滚；第一次安装的 BotAI 新文件会移除。不现场混装、不复活旧 legacy writer。

已检查当前 CSS 命令：先 `css_plugins list` 找当前 LOADED BotAI 的数字 ID；`css_plugins reload <id>`，或 `unload <id>` 后 `load ZEPVE.BotAI`。历史 UNLOADED 条目可能保留名字，不猜 ID。Core 原有 hot/late policy 不变。

## 验证范围

127 模型/source checks（原 72 + BotAI 55）通过，不代表 runtime PASS。实际服务器证据独立记在 `BOTAI_VERIFICATION.md`。完整 ZE 兼容、多真人几何、native awareness 效果、Recovery handshake、长期性能均不能由模型推断。`T tally 20 vs 10` 继续独立债务，不修复。不进入 Navigation。
