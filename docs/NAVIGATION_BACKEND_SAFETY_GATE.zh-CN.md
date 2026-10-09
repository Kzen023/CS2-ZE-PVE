# v0.3 Navigation movement backend 安全门槛

PR #11 已按用户授权合并。新的 v0.2 complete baseline：`main @ 7cf389157f7f09f08c399ba5ca6a33b5ccbf4d46`。现有 matched 安装仍对应 `238b98c`，本轮没有部署 DLL/native 模块。baseline 127/127 模型/source checks 复跑 PASS，原 deferred acceptance 保持。

**v0.3 未实现，尚不能 Ready。** 在 movement 执行前提处停止；没有禁用 legacy Trail/Recovery，也没有添加第二个 movement writer。Navigation gitlink 保持 `14cda1f`。这是 source/ABI 审查，不是 runtime PASS。

已审查 Core、BotAI、legacy shared Trail/Recovery、Navigation 当前 pin 与 component main `49e65f5` 的设计/AGENTS、现有 CSS Trace 接口，以及 BotController v0.7.1 / `0ae8f18` 源码。已安装 DLL 的只读 PE 检查确认 ABI **22** 与 Start/Update/CancelUsercmdMovement exports；未把 DLL 加载到审查进程执行，也未发出 movement command。

## 确定的接口缺口

BotController 的 `InputInjector.cpp`：

- StartUsercmdMovement 保存按 slot 索引的 `{id, forwardMove, leftMove}`，没有原 controller/pawn serial、Core generations、BotAI lifetime/BindingVersion 或 Navigation lifetime。
- native HookedPlayerRunCommand 解析**当前** services→slot 后使用这个 slot 的持久 override；当前实体解析不等于校验授权该 intent 的原身份/目标。
- ApplyUsercmdMovement 选择 `movements.back()`：允许多个 override，最后一个生效；取消最新的可能重新暴露下面的旧 intent，没有 exclusive owner 仲裁接口。
- replay 会清除该 slot 的 usercmd work；必须建立 replay 与 Navigation 的显式边界。

因此，只在 Navigation timer/OnTick 校验完整 token，再交给持久 slot-only native override，无法证明**native 真正执行时**仍属于当前身份和 target binding。事件取消可以缩小窗口，不能替代 application-time gate。

这**不是**发现当前已有两个活跃 movement caller。managed BotControllerImpl 目前 disabled，此前 smoke 没有 replay/locks；问题是新 handoff 拟使用的机制缺少可落实的生命周期/ownership 保证。源码/日志/export 对应关系已审查，未建立二进制可复现证明。精确文件/行号与 hash 见英文报告。

按用户“无法保证 Navigation 唯一 movement writer / 无法安全隔离 native ownership 时停止”的要求，本轮停止 runtime handoff。

## 没有采用的绕过方式

没有安装另一个 slot-based BotNav goal scheduler，没有未经验证地写 CBot speed/button/goal 字段，没有用 All/Aim 锁冻结 Valve combat/perception，没有用频繁 TP 替代移动，没有双开 legacy/new recovery，没有改变 Core/AssignedTarget authority 或强写 Enemy。

## 继续 v0.3 的前提

先建立可验证的 backend：exclusive owner lease；最终 native mutation 前可拒绝的身份/target-binding/gameplay-permission gate；按所属 generation 取消且不暴露旧 intent；明确拒绝其他 caller/replay；覆盖 map/round/disconnect/respawn/provider/module unload。

在 ZEPVE-Lab 验证提交→执行之间换 target、slot reuse、pawn 更换、provider/module invalidation、竞争 owner 和清理，再在所属 production 仓库独立实现。不能把 PoC 直接作为 release 依赖。

之后才完成 component runtime（per-human bounded Trail / 单调 sequence / segment / waiting-aware progress / Native→Trail→Recovery），并在 suite 中配套禁用 legacy writers。Recovery 单独校验 permission 与 hull/ground，重绑 route、请求 BotAI ObserveOnly reacquire，再恢复追逐。Navigation 必须是唯一 final movement/native-goal writer。

验证结果：v0.2 baseline 127/127 PASS；v0.3 build/model/runtime **NOT TESTED / 尚未实现**。没有部署、authority handoff、pin 更新。多真人、awareness efficacy、长期性能、完整 ZE map、T tally debt 没有被重新升级为 blocker。当前阻塞来自执行机制本身的安全契约缺口。
