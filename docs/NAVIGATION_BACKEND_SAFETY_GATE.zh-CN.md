# v0.3 Navigation movement backend 安全门槛

## 2026-10-10 Lab 后续：mechanism gate 解除

**Production safety：PASS（native 进程常驻 + managed consumer hot reload）。v0.3 可继续独立实现；本 PR 没有 Navigation runtime 或 authority handoff。** [Lab PR#1](https://github.com/Kzen023/ZEPVE-Lab/pull/1)包含 actual existing PlayerRunCommand adapter 与 `experiments/movement-hook/DETACH_RUNTIME_EVIDENCE.json`。下文原审查保留为历史记录。

用户授权使用自己的真实服务端，同一 dedicated 进程在20/20 accepted detach 记录期间保持运行：validator 内 actual CSS unload；完整 managed 校验后/native commit 前 unload；正在 applying 时 reload；replace/cancel 后 unload；worker detach；立即 reload；pending root 保守 release fence 期间真实 Core/BotAI provider replacement；fresh lease/lifetime 与旧 token 拒绝；10 次连续重载。旧 owner 仅在 nativeDrained=true/frame0/callback0 时释放一次，没有旧 callback/application/resurrection 或 context lookup failure。独立自动化回归 native44/44、interop35/35 PASS。

native router/hook 与 shared callback adapter 进程常驻；**native DLL 物理热卸载 unsupported-by-design，不是 NOT TESTED blocker**，native replacement 必须停服。managed actual CSS Unload/Dispose/Load 已验证；不宣称 CSS 历史 loader/assembly 内存立即释放。旧 movement/ownership/API 仲裁采用此前真实证据，本轮没有重复。

这是 patched candidate 的证据，原版 ABI22 不会因此变安全。下一步仍须在 owning native repo 独立实现 backend，再做 Navigation consumer 与 matched handoff，Lab 不成为 release 依赖。实验结束后停服回滚：原版 native 恢复、Lab 模块移出 addons、15 baseline 文件 hash 一致。Core/BotAI/legacy 源码、共享 ABI、native/component pin、MIGRATION_AUTHORITY current writers 不变，原 deferred 项目继续 deferred。本轮到此停止，不开始 Trail/Recovery/stuck/Navigation runtime。

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
