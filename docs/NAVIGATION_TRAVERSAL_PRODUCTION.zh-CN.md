# v0.3 短动作 traversal 候选

本次是 production 仓库中的正式候选实现，Lab 只提供研究证据，不随包部署。native backend 属于 ZEPVE-BotController；自动动作采集、路线选择和执行属于 ZEPVE-Navigation。Core、BotAI、外部 ZR 复活执行器、HUD 和 Weapon 权威不变。未 merge，未宣称复杂地形全部通过。

普通路段仍走 Native→Trail→Recovery。特殊路段从同一个 Core Human 身份采集短 command clip：站定起步、真实离地、三个落地 command。保留 signed subtick、jump/duck 边沿和视角变化；不移动事件、不裁剪超长动作。每段最多256 command，约四秒；每人最多八段，选取最近30秒的数据。

Bot 正常走到起点，检查位置、落地、速度，再通过同一个独占 lease 执行。播放中不复制位置或速度。临时视角也归同一个 router 仲裁；动作结束或失效后交回 Valve。全生命周期、BindingVersion、路线 generation、native handle、permission、lease/revision 均在最终写入前验证。取消的 clip 不能再出现，取消也不算成功通过障碍。

当前 timed/view capability 限于已审计的 Windows CS2 14190 /64Hz 与精确 game DLL hash。其他 profile 会关闭该 capability，普通移动继续可用。连续助跑跳链没有站定起步时暂不采集；长动作、人物碰撞和人类/僵尸物理差异仍有限制。不要据此承诺完美 KZ 复刻。

整套停服安装：native DLL/gamedata、Core、BotAI、adapted ZRPVE、一个 Abstractions、Navigation、一个 MovementBridge SDK。安装器先建立可恢复备份并校验，再替换；同时归档三个 Lab consumer 程序集，保留录制数据与配置。默认保留现有 quota，包括当前实验配置的 solo=1，不偷偷改为10。回滚使用本次安装备份的 restore.ps1，先 VerifyOnly，再实际 restore；前一套可能仍是实验 baseline，不能声称自动恢复到了 v0.2 main。

测试时连接 CT，等 Released 后在每次动作前站定。先 W 跳、蹲跳，再 A 包一箱→二箱旋转跳，无需 Lab 聊天指令。检查 css_zepve_nav 的 profile/clip 数，以及 css_zepve_nav_bot、css_zepve_nav_events 的 clip start/complete/failed。实际看 Bot 是否通过；仅 command 完成不是物理成功。验收执行 css_zepve_nav_recovery 0，Navigation reload 后需要再次关闭，避免传送掩盖失败。

自动测试115/115 Navigation、26/26 retained native、26/26 traversal、14/14 retained interop 与新增1544-byte signed payload 往返检查通过。安装/实际回滚 fixture 通过。**本 production 候选真实自动 traversal、旋转跳和 reload/drain 验收仍 NOT TESTED；历史 Lab PASS 不代替它。**

## 真实服首轮状态（2026-10-11）

07:38 整套停服安装，19/19 live hash 一致；当前回滚为 v0.3-20261011-073807-7690790，22 文件备份与 VerifyOnly 通过。**这一新 production 候选实际 rollback 尚 NOT TESTED**，不要把 fixture/以前 Lab 回滚混作本轮。

六个正式/保留插件加载，Lab 未加载；Core Released、1 真人/1 ZombieBot、LegacyMovementDisabled=True、一个 owner/lease，drain pending/lookup failure 为0。只读临时 inspector 确认 Human command 连续有效且 origin=tick-1、站定 prelude 已 armed；随后已卸载并移出 loader 路径，不随包提供。当前没有观察到新 clip 动作，W 跳、蹲跳、旋转跳和 timed reload/drain 仍待实际测试，未标 PASS。

Recovery 关闭，测试临时 sv_cheats=1 / bot_dont_shoot=1，原值均0已记录；普通 PvE 验收前恢复。服务端 PID23516 保持运行，三个 PR 为 Draft，没有 merge。

## 朝向/起点对齐修补

用户反馈无原生 aggro 时倒走、锁定玩家后无法旋转跳。实服只读取样看到 clip-approach，没有 clip-start，不能据此宣称 active clip 的 view lease 被抢。已修复确定性问题：寻找新 clip 的间隔不能绕过已选定的起点对齐。traversal ABI2 新增同一 lease 中的 FacingDirection，普通受控移动朝向当前移动目标并输出向前；Native/combat 保留 Valve，clip 内继续忠实录制视角。没有新增 hook、Enemy/visibility、flags 或 Upkeep bypass。117/117 Navigation、26+33 native 与 interop/安装回滚 fixture 通过；修补版真实朝向与旋转跳仍待 matched retest，未标 PASS。

08:21 整套 ABI2 修补包已安装并启动（suite9989d30/nativeaa42948/Navd6650d9），19 payload hash 由安装器验证。回滚根 v0.3-20261011-082105-7849465，恢复上一套 production ABI1 候选；VerifyOnly 通过，实际本轮 rollback 仍未测。PID24416，六个插件、无临时 inspector、capability2 已启用。Recovery/攻击关闭用于复测；向前朝向、起点入场与旋转跳物理结果继续 NOT TESTED，等待用户反馈。

## B 点起跳/连续跳修补候选

用户真实反馈：起跳点卡住、近身才追、单跳时机不对、第一落点来回转头；本轮取样为有效目标约302单位、Native输入0、未进入 timed playback。保持这些 FAIL/PARTIAL，不以模型伪造通过。新增减速对齐/惯性停稳，移动入场不跨到达平面反向；waypoint 不再因96单位距离就无限 Waiting。失败入场有明确事件与退出。第二跳允许从两条连续落地移动 command 起步，不再要求重新站定，也不重放已发布第一段的 command。原始 timing/when 不变，native ABI2/Core/BotAI/Recovery 边界不改。123/123 模型通过，B 点实际过障碍、真实单跳 timing 和连续跳仍需复测。

08:42 package3 已整套安装启动（suite15d7b98/Navbebc4ff/nativeaa42948），19 payload 校验通过，回滚 v0.3-20261011-084219-5649154 恢复 package2；VerifyOnly 通过，实际本轮回滚未测。PID22360、六插件、无 Lab/helper、capability2 已启用，Recovery/攻击关闭用于 B 点复测。实际入场、单跳时机、连续跳结果继续待反馈，不新增 PASS。
