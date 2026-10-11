# v0.3 matched 安装与回滚

整套 candidate：native BotController DLL/gamedata、Core、BotAI、adapted Kzen-ZRPVE、单 shared Abstractions、Navigation、单 shared MovementBridge SDK。native fork / Navigation 独立组件持有实现，suite 持有契约/legacy adaptation/packaging。Lab 不发布或作为运行时依赖。native/router 与 SDK 常驻，managed Navigation 可重载；物理 native hot unload unsupported。

按精确组件 checkout/build，用 scripts/build-navigation.ps1 参数 CounterStrikeSharpApiPath / NativeComponentRoot / NativeBuildRoot / fresh OutputDirectory 打包。manifest 保存各源码revision/dirty state/API和19文件 hash。native 使用组件自己的 build-native.ps1 和固定 toolchain，不导入实验源码。

停 dedicated server 后运行 install-navigation.ps1 -ServerRoot <root> -PackageDirectory <stage>。已有无插件客户端可加 AllowUnmoddedClient，脚本会核验模块。安装前验证19完整文件/API、拒绝 private shared copy、建立当前可恢复备份及独立 restore.ps1/hash。保留 configs/maps/WeaponBalance/ZEAssist。旧 adapter/缺 marker 时 Navigation 拒绝，无恢复旧 writer 的 fallback。

启动并连接 CT，用 css_zepve_status / css_zepve_ai / css_zepve_nav / css_zepve_nav_bot / css_zepve_nav_events 验收。Core 仍拥有身份/lifecycle/round/quota/team，external ZR/CS2Fixes 仍是唯一 respawn executor。不迁移 perception/HUD/weapon。

严重回归整套 rollback：停服，备份 restore.ps1 -VerifyOnly，再 restore.ps1（无插件客户端情况加 AllowUnmoddedClient）。验证安装/备份 hash，归档新 Navigation/SDK 文件并恢复 native/Core/BotAI/legacy/shared originals，最后核验。不得现场混装或在 Navigation 活跃时恢复旧 Recovery。配置不改，native/shared SDK 不热替换。

css_zepve_nav_probe_stale <slot> 提交 synthetic wrong BindingVersion packet 到真实 native frame，不修改 BotAI target。reload 使用 css_plugins list 当前 LOADED 数字 ID。模型/构建不是实际 PASS；真实结果记录 NAVIGATION_VERIFICATION.md。复杂垂直/no-NAV、完整 ZE trigger/permission、真实多真人 isolation、长期性能继续依赖真实证据。
