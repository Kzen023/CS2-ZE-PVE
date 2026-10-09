# BotAI 实际验证

模型/source checks：127/127 PASS（保留 72 + BotAI 55），Release build 零 warning/error，不等于 runtime。新的 v0.2c baseline 为已合并 PR #10 / `ed480bd`。

实际服务器测试：2026-10-10 00:14–00:25（Asia/Shanghai），Windows dedicated、dust2/mirage、1 个真人 CT；CSS 1.0.376、BotController v0.7.1 @ 0ae8f18、CS2Fixes v2.0-0-gbb3be34。RCON/CSS log/native console 是独立真实证据。

首次 `3a73439` 包确实导致启动退出，日志停在 BotAI Load、Core 之前，用户也确认闪退。BotAI 初始 recorder 读取 Server.CurrentTime；已核对 installed IL 与 [该版本 native 源码](https://github.com/roflmuffin/CounterStrikeSharp/blob/653d651f1ac09ac1ddb423d588f871b891038860/src/scripting/natives/natives_engine.cpp#L58)，GetCurrentTime 不检查 global-vars 空指针。未做 native dump stackwalk；API/load-order 与对照启动验证支持修复归因。

已实际整套 rollback 到 v0.2c，旧 Core hash 匹配、移除新 BotAI，baseline 正常启动。没有启用旧 monolithic writer。修复改用单调 Stopwatch，重新 build/test/fixture restore，再停服部署整套 11 文件。当前源码/包 `238b98cdb40a01b422c16c3bab5a18030142fad0`；当前回滚备份 `ZEPVE_Backup/v0.2d-20261009-161410-5946327`。11/11 文件 hash、21 个当前 config/无关插件文件保留验证通过。无插件客户端例外只在检查其窗口/命令行/modules 无 addon 后允许，专用服始终须停。

| 项目 | Runtime 结果与范围 |
| --- | --- |
| Cold load / shared ABI | PASS：BotAI 在 Core 前安全加载，one LOADED BotAI/Core/bridge；Unbound bindings=0 |
| AssignedTarget / 稳定性 | PASS：十个 Bot 指向真人 slot 0；Enemy 为空也保留目标；身份不变版本稳定 |
| 多真人分配 | NOT TESTED；1–6 人、18 Bot 的均衡仅为模型 PASS |
| 当前 binding probe | PASS：00:15:40.043 Binding 11 ACCEPT；00:22:41.293 Binding 95 ACCEPT |
| Round restart | PASS：00:15:55.707 REJECT round epoch；准备期 bindings/pending=0 |
| Bot death / ZR respawn | PASS：旧 Binding 43 REJECT pawn generation；connection 42 不变，pawn 235→246（dead）→252（约 +7.25 秒 alive），新 Binding 53 |
| Human death/respawn | PARTIAL：实际 kill，pawn 67→137/138 dead→142 新 CT，Closed 清零/新轮重绑；probe 先拒绝 round epoch，独立 target pawn 失效未验证 |
| BotAI hot reload | PASS：旧工作 explicit cancellation REJECT，模块 GUID 更换/新绑定 |
| Core hot reload | PASS：旧 probe plugin lifetime REJECT，BotAI 重建 scope/绑定，quota 保持 |
| Core unload/late load | PASS：Core unavailable/Unbound bindings=0，当前 native quota 不变；下一轮恢复 |
| Map change | PASS：dust2↔mirage，重新 bootstrap/分配；当前 probe 在换图时先 REJECT round epoch，不能声称独立 map-epoch reason |
| Human disconnect/reconnect | PASS：connection 33→44 tombstone→55，空服 bindings/pending=0，重连恢复十个绑定；独立 target-only reject（Bot 不离线）未验证 |
| Reacquire lifecycle | PASS：自动观察窗口实际 Succeeded/TimedOut/cooldown；explicit coalescing NOT TESTED；不是 native awareness repair 的证明 |
| BindingVersion-only rejection | NOT TESTED：runtime 的拒绝同时涉及 lifetime/module；模型通过 |
| Core/native/legacy 无明显干扰 | 单人 smoke PASS：quota 10、T bots、ZR delay 7 与实际复活、legacy recovery placed；native replay 无、All/Aim/Weapon locks=0 |
| Bounded recorder / exceptions | Smoke PASS：256 上限/递增 Dropped/pending 归零；修复后未观察到 Core/BotAI/CSS exception 或自动重复 reload/writer。长期性能 NOT TESTED |
| Rollback | 首次问题包实际整套回滚 PASS；修复包恢复 fixture PASS，最终修复包的再次服务器 rollback NOT TESTED |

BotController `bc_status` 的 `drop=failed` 是观察到的外部 hook 限制，未验证此前 parity；BotAI 不使用 drop。`Round_End T tally 20 vs actual 10` 在断线日志再次出现，仍为独立债务，不修复、不当目标权威。

Native perception 写入/效果、legacy Recovery 自动通知、Navigation 移动、多真人实体布局、完整 ZE map、schema failure injection、长时间性能均保留 NOT TESTED/未实现边界。没有靠模型把这些标 PASS。

本地 raw evidence `.codex/runtime-botai-v02d-20261010` 不入 Git，排除姓名/网络标识。最终 transcript SHA256 `10E7CA2DE8AEC3BB83B1CD60314E2720C68DB4E4BF49B911075E8B57B502DB5A`。cheats=false、event log off、临时 RCON 禁用/凭证删除；修复后的测试服保持运行。PR #11 可 review，不 merge，不进入 Navigation。精确时间/程序和剩余步骤见英文 `BOTAI_VERIFICATION.md`。
