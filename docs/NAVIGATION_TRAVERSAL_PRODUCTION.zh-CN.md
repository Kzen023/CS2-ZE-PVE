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

实服 package3 抓到 clip13 距离起点52.3、已越过移动入场平面、wish=0，TimedPlaying=False；clip12/13 超时后交替重选，单个失败ID覆盖了前一段冷却。修补为越过不可逆移动入口立即退出、最多八段独立冷却，并让可达近身真人打断起点对齐。126/126 模型通过；不把这次入场失败解释为 subtick 失真，也未宣布物理跳跃已通过。


## package4 实服 B 点复测（2026-10-11 08:49–08:54）

实际运行包 suite23f3a77 / Nav66bd278 / nativeaa42948，ABI2。停服整套安装19 payload 校验、六插件加载、无 Lab/helper、Core Released、1真人/1Bot、legacy movement disabled；备份 v0.3-20261011-084918-3340115 恢复 package3，VerifyOnly PASS，实际本轮 rollback NOT TESTED。Recovery 关闭，测试攻击关闭，不代表普通 PvE 条件。

实服 clip1 越过移动入口后0.10秒退出，随后距离356.5→77.7，没有继续四秒零输入等待。后来 clip8 在 t238.44选定、t238.58启动38帧、t239.20记录 grounded physical endpoint accepted。起点Z108.03125→终点Z2.7847567，是下落高差，不是向上箱子/旋转连续跳 PASS。同一 lease15/revision1，完整 command + grounded近端点 + 几何验证才接受，没有回放位置/速度/perception写入。

clip9 起点Z73.16078→终点Z127.578514，t243.45开始对齐、t243.65越过入口立即退出；没有clip-start，因此向上动作未播放，不能推断subtick失真。之后目标距离约586仍Native fallback，完整向上/连续跳与可靠追逐仍PARTIAL，用户视觉确认待反馈。owner1/lease1、drain pending0/lookup failures0仅是当前快照，不替代新timed卸载回归。原始样本仅保留本地，不提交身份或凭据。


后续 clip19：t310.79对齐、t311.53启动105帧、t313.20 physical endpoint accepted；静止起点Z86.89904→终点Z116.5977。这次向上动作也有自动播放/端点检查实服证据，但具体障碍通过的用户视觉确认、重复性与连续跳仍未验收。t317.09 Bot落在Z3.49，cursor369指向Z108.03，本地arc拒绝、Recovery仍关闭；单段成功不等于整条路径可靠，落地/掉下后的路线重新入场仍是已知未解决问题，不宣称完美KZ复刻。


用户 package4 视觉验收仍FAIL/PARTIAL：B门及其他入场卡住、起跳常失败、平地斜坡多余跳跃、连续/旋转跳不可靠；单段端点事件不能替代这个结果。新增仅已选定、距起点64单位内的OnTick入场检查；250单位/秒在50ms内走12.5单位，大于6单位窗口。普通规划/采样仍50ms、几何仍低频，当前Core/BotAI身份/provider/permission及最终native校验保留，原始timing/起点阈值不修改；播放中的clip不被刷新。bounded jump-command记录Navigation起跳原因/clearance，区分Valve保留输入。130/130模型PASS，新运行包仍待整套复测，未改变nativeABI/Core/BotAI权威。


package5 于2026-10-11 11:51停服整套安装：实际二进制suite2c0a90e/Nav972c767/nativeaa42948。live19/19 hash、22项备份VerifyOnly PASS；回滚根v0.3-20261011-115127-4147527恢复package4，实际restore仍NOT TESTED。PID26076、六插件、无Lab/helper、ABI2精确profile启用，真人连接前owner1/leases0/frames0/pending0；Core Unbound无真人时不写movement。Recovery关闭，sv_cheats1/bot_dont_shoot1用于取样，普通PvE前恢复0/0。构建0警告/错误、保留suite及Navigation130/130模型PASS；新逐帧入场/物理结果和timed lifecycle实服回归待测试。后续仅文档commit不改变这里记录的二进制manifest。


package5 用户无跳行走复测仍出现跳跃；实服bounded事件明确是Navigation marker=None局部蹲跳，非clip播放。T出生下坡及其他坡地/平地路线有记录。地面直线hull可能拒绝普通接缝；新增完整18单位抬脚/跨过/落回hull检查，保留连续支撑地面/坡度/高度/端点约束；跳跃事件带具体地面失败项。130/130模型PASS，真实engine结果待package6复测，native/Core/BotAI不改，Recovery不掩盖。


package6 于2026-10-11 11:56停服整套安装：实际二进制suite026c79e/Nav575f111/nativeaa42948；安装器验证19项live hash，22项备份VerifyOnly PASS。备份v0.3-20261011-115639-8550254恢复package5，实际rollback NOT TESTED。PID9192、六插件、无Lab/helper、ABI2 profile可用，真人连接前owner1/leases0/pending0；Recovery/测试攻击关闭。130/130模型、保留suite、构建PASS；新增step hull、入场与连续跳实服结果等待复测，不标PASS。


package6 用户仍反馈可绕障碍旁跳/蹲跳，floor all-solid诊断不能解释为所有地面已修复。确定性修补：仅存在local arc不再抢占正在前进的Native绕行，Native至少blocked1秒或Trail才启用局部跳；Native保留帧不再推进未提交的辅助起跳状态，避免后续幻影pulse。132/132模型PASS含绕行及幻影pulse回归；新包真实效果待复测，B门/连续/旋转跳仍未验收。


package7 于2026-10-11 12:01停服整套安装：实际suite5f05f6c/Nav05a50f8/nativeaa42948。安装器验证19 payload hash、22项备份VerifyOnly PASS；备份v0.3-20261011-120109-2260123恢复package6，实际rollback NOT TESTED。PID16372，六插件、无Lab/helper、ABI2可用，连接前owner1/leases0/pending0；Recovery/测试攻击关闭。132/132模型/保留suite/构建PASS；绕行及未提交pulse真实回归待用户复测，B门/连续/旋转跳仍未验收，无merge。
