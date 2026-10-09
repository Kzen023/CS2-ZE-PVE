# Core observer（v0.2b）

**[English](CORE_OBSERVER.md) | 简体中文**

Core 是可选的独立观察插件，基于已接受的 pre-Core baseline：`CS2-ZE-PVE @ 89b5c0c0`，回滚包 `ZEPVE_Backup/pre-core-2026-10-09`。**legacy 仍然是唯一 gameplay writer；本 PR 不发生 authority handoff。** ZombieReborn/CS2Fixes 继续承担现有复活执行。

## 构建、安装、回滚

需要 .NET 10 SDK 和服务器 CounterStrikeSharp API 目录（含 Logging.Abstractions）。没有新增 NuGet/native 依赖，也没有调整组件 pin。

```powershell
./scripts/build-core.ps1 -CounterStrikeSharpApiPath 'D:/CS2/game/csgo/addons/counterstrikesharp/api/CounterStrikeSharp.API.dll'
```

脚本执行 Release 构建和确定性生命周期模型测试，在 `artifacts/core-observer` 生成安装目录及 SHA256 manifest，不会部署到运行中的服务器。把产物中的 `game/csgo/addons/counterstrikesharp/plugins/ZEPVE.Core` 目录复制到服务器对应的 plugins 目录，保留旁边的 `ZEPVE.Abstractions.dll`，通过平时的 CounterStrikeSharp 方式加载插件。CounterStrikeSharp 和 logging 程序集由服务器提供。

保留全部现有 legacy 插件及配置。Core 独立订阅引擎事件并读取 controller/pawn，每 0.5 秒校对一次；不修改 legacy 执行路径，也不登记跨插件 provider/capability。本阶段不会执行 Bot quota/team、感染、复活调度、recovery、target、awareness、移动、武器、HUD 或地图 gameplay。

回滚时卸载 **ZEPVE Core Observer**，只移除其 `ZEPVE.Core` 插件目录。现有 legacy 部署和 pre-Core 回滚包不变。

## 命令和角色

- `css_zepve_status`：服务器或玩家控制台可用，显示 MapEpoch、RoundEpoch、Humans、ZombieBots，以及每个 slot 的 Role、Connected、Alive、ConnectionGeneration、PawnGeneration；包含有界的已断开记录，不显示其他模块状态。
- `css_zepve_probe <slot> [seconds]`：服务器控制台或 `@css/root` 可用；默认 5 秒，范围 0.1–60 秒，最多 16 个待执行探针。捕获当前活 pawn 的 token，延迟后重新解析并在服务器日志输出 ACCEPT/REJECT，不执行任何 gameplay。断连、死亡、pawn 更换和轮次变化会拒绝旧探针；地图结束/卸载会取消 timer，模型测试另行验证旧 token 同样失效。

Human 是非 Bot、非 HLTV 的 CT；ZombieBot 是非 HLTV 的 T Bot，与 legacy 的 Trail/recovery 分类一致。其他队伍、CT Bot、HLTV 为 Other。死亡但仍连接的玩家保留角色分类。这些只是观察分类，不代表 ZombieReborn 的 canonical infection 状态。

## 身份和生命周期

PlayerRegistry 是唯一 identity source。Players/Humans/ZombieBots 是同一批不可变 ZepvePlayerContext 的只读 view，不是三个可写 registry。Adapter 只提供引擎观察；generation 只由 Registry 生成。旧 context 是快照，不能作为延迟有效性的证明。

PlayerLifetime 包含 plugin lifetime GUID、map/round epoch、slot、connection/pawn generation。完整的 controller/pawn handle（含 serial）和 UserId 用于检测实体或连接更换，不能只依赖 slot/SteamID。连接/断连会失效连接 lifetime；spawn/death、alive 变化和 pawn handle 更换会失效 pawn lifetime，包括复用同 pawn 的复活。当前 pawn 必须仍属于当前 controller。

捕获必须存在当前连接、活着且绑定有效的 pawn。callback 执行时重新读取当前实体，再核对五层 lifetime，返回当前 context。调用者仍需验证所需角色/状态；token 不授予 gameplay 权限，也不代替未来的 target/route binding 验证。

round prestart/start/end 都递增 RoundEpoch，prestart/end 到下一次 start 之间关闭捕获。map end/start 都递增 map/round epoch 并清空 context。epoch 是失效计数，不是地图 ID 或比赛轮数。hot/late load 使用全新的 plugin GUID，立即观察现有地图和玩家；当时的比赛阶段未知，初始 observation epoch 可用，但不提供 canonical round permission。空地图加载等待 map start。

卸载先失效 Registry，再停止 timers、移除订阅/命令并清空状态。延迟探针不保存 controller/pawn 引用；context/token 仅含值。所有实体访问都在服务器线程执行。

## 验证与限制

详见 [验证记录](CORE_OBSERVER_VERIFICATION.md)。构建和模型测试不等于插件加载、引擎事件顺序或真实服务器验证；runtime 场景均为 **NOT TESTED**。

校对范围是 CS2 slot 0–63，每秒两次；status/probe 会同步刷新。若同 handle 的整个死亡/复活周期发生在两次采样之间，需要 spawn/death 事件才能观察，轮询无法还原未观察到的事件。generation 不跨进程持久化。实际 adapter、hot load 和清理语义需服务器验证后才能考虑 handoff。legacy 的旧 callback 风险和已知 recovery 后游走问题均未改变。
