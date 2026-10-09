# v0.2c 配套真实服务器验收

[English](CORE_MATCHED_RUNTIME_VERIFICATION.md) | 简体中文

## 范围与判断

**单人配套 smoke：PASS。PR #10 可以 Draft → Ready for review；未执行合并。** 证据来自本地 Windows dedicated server 的 RCON、CounterStrikeSharp 日志和只读控制台快照，用户以真实 CT 玩家参与。不是 model/unit test，也不是把此前“游戏内正常”的概括反馈推断为全部 PASS。

时间：2026-10-09 23:21–23:33（Asia/Shanghai）。地图：`de_dust2`、`de_mirage`。安装产物：`1cf6aa21503316ab2963c35e6e222312e14131ad`。CSS 1.0.376、CS2Fixes `v2.0-0-gbb3be34`；原有 native BotController 0.7.1 保留。验收期间未修改 DLL、gameplay 配置或源码，也未开发 BotAI/Navigation。

## 部署与回滚

此前已停服整套安装。验收前再次核对 8/8 live 文件哈希，并在 `.codex/runtime-acceptance-v02c-20261009/before-test` 保存当前配套快照。原 v0.2b observer/legacy/contracts 可恢复备份：`ZEPVE_Backup/v0.2c-20261009-151119-2996277`，旧文件备份哈希校验通过。

验收后 8/8 产物和 47/47 保留的配置/无关插件文件均未变。不曾单独替换 DLL，也未启用旧 legacy writer。未出现必须回滚的严重回归；**真实服务器执行整套回滚仍为 NOT TESTED**。回滚需停止 CS2 并使用备份中的整套 restore 脚本；运行中的同安装游戏客户端也会触发该脚本的停服保护。

## 实际结果

| 项目 | 结果 | 证据与边界 |
| --- | --- | --- |
| shared ABI / plugin loading / 单一 provider | PASS | 一个 LOADED Core `0.2.0-c`、一个 LOADED adapted ZRPVE，`Authority=Core GameplayAuthority=True`；bridge 正常互通，无加载/类型/provider 异常。CSS 保留的 UNLOADED 条目不是 active writer。未人为注入重复 provider。 |
| Human / ZombieBot registry | PASS | 1 Human、10 ZombieBots；断连行 Unknown/Connected=False，排除在 view 数量外。 |
| round start / restart | PASS | Preparing → AwaitingRelease → Released；RoundEpoch 更新，旧 human probe 明确 REJECT/round epoch。 |
| preparation profile | PASS | Preparing 后为 solo/quota 10/respawn delay 7；等待期 native quota 0、join team CT。 |
| infection/release timing | PASS（smoke 精度） | ZR 的 16 秒感染倒计时及开始消息实际出现。round start 23:23:57.493；23:23:57.767 Preparing，23:23:58.777 AwaitingRelease，23:24:14.160 仍等待；23:24:14.690 join team T，23:24:14.971 Released/10 bots。符合 +1 preparation 再等待 +16；不证明精确 sub-frame 或任意配置时序。 |
| quota / bot team | PASS（solo） | native quota 10、join team T；Core 恰好 10 ZombieBots，控制台有实际转 T 记录，pending work 归零。旧 quota/team 执行器未安装。 |
| ZombieReborn respawn / pawn invalidation | PASS | 23:25:28.879 定点杀死一个当前 Bot；同 slot/connection generation 42 从死亡变为约 7 秒后存活，pawn generation 更新。旧 probe 在 23:25:28.939 因 pawn generation REJECT，Core pending work 为零。仅测试击杀短暂启用 cheats，随即恢复 false；复活仍由外部 ZR 执行。 |
| Recovery | PASS（placement smoke） | 捕获多次 `recovery placed`（stuck/no_damage、不同路线点），以及无合格点时的 skipped。这证明 retained legacy 恢复路径在 Core validity 下执行；不覆盖全部几何、escort/恢复后行为或精确 respawn-triggered 路径。 |
| map change | PASS | dust2 → mirage，MapEpoch 1→3，recorder 记录 map end/start、新玩家与轮次策略。换图中的 probe 首先因 disconnected/invalid entity REJECT，不能写成明确 map-epoch 原因。 |
| disconnect/reconnect | PASS（本次含 hibernation） | 23:28:33 断开真人；休眠中 Humans=0/ZombieBots=0/PendingWork=0，slot 0 断连 generation 44；重连复用 slot 0，generation 55，Human 与 solo 策略恢复。旧 probe 因断连 REJECT。不认证一般休眠计时。 |
| 三阶段 hot reload | PASS（实测阶段） | Preparing、AwaitingRelease、Released 都实际重载；新 GUID，旧 probe 因 plugin lifetime REJECT，保留阶段并完成待执行计划，最终 quota 10。未见重复 active Core/重复策略或倒计时。 |
| manual late load / bridge loss | PASS（smoke） | Core 手动卸载失效旧 probe；load 即 bootstrap 1+10 玩家，Unbound 等下一轮。卸载 bridge 后 authority=False/pending=0，下次 restart 保持 Unbound/quota 0；同套 bridge load 后等后续轮次正常工作。没有混装 DLL。 |
| current identity probe | PASS | probe 0 2 在 23:31:05.208 返回 ACCEPT/current identity。 |
| bounded recorder / exception / 重复症状 | PASS（smoke） | recorder 到 256/256 后只增加 Dropped，pending 归零；未发现 Core/CSS exception、自发重复加载或明显重复 timer/policy。原 legacy debug 输出仍存在；未做长期泄漏认证。 |
| 性能 | PARTIAL | 一次 stats：64.08 FPS、11 players；没有系统性能或耐久测试。 |

## 保留风险与 NOT TESTED

一次 engine Round_End 的 T 队人数统计显示 20，而 Core 和 native status 实际枚举 10 bots；这不能证明有 20 个 active bots 或两个 suite writer。旧 ChangeTeam→SwitchTeam 顺序保留在唯一 Core adapter，native infection/team 路径也会产生 Bot death/pawn 变化。具体原因和迁移前真实 tally 对比未验证，**native team-tally parity 保持 NOT TESTED**。没有观察到由该诊断差异导致的 gameplay 故障，未猜测性修复。

其他 **NOT TESTED**：duo/coop/group 真人档位、任意配置与精确 sub-frame 时序、重复 provider 注入、quota/team 待执行的亚秒级 hot-reload 窗口、多真人同时断连、完整 Recovery/复活权限、长期重复 timer/泄漏/性能、真实整套回滚、完整 ZE map compatibility。PR #9 deferred 项保留为历史记录；本次只按 v0.2c 的新证据标注对应场景。

## 证据与最终状态

原始记录包含玩家/网络信息，仅保存在本机 `.codex/runtime-acceptance-v02c-20261009`。RCON transcript `commands.jsonl` SHA256：`F118B302C3C07A78E0E578326C0B1FF9534E9CA62E7AE25B3F88E96626D8EDB1`；含 Recovery 的 `console-232906.txt` SHA256：`F7DA76B18A16202D345B0A1D65B21164FF9750561E794501370E12E654B2F149`。目录还保留 Core/CSS 日志副本及 `acceptance-summary.json`。

测试服保持本地运行，配套模块及正常 solo 策略有效。临时 game-event logging 已关闭、cheats=false、临时 RCON 已关闭并删除凭据文件。未找到需要改源码的 Core/bridge 缺陷，未重建或回滚。建议带明确 deferred tests 进入 review；PR 保持 Draft，等待用户要求切换。到此停止，不合并、不继续下一阶段。
