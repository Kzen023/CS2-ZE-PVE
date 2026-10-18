# v0.3 Test Preview closeout — 2026-10-18

Status: candidate under validation; **KEEP DRAFT / do not merge** until the refreshed real-server acceptance gate is complete. Historical v0.2 and mixed v0.3/v0.4 results are not current evidence.

## Source and authority

Starting PRs: Suite #15 at d1bf52c; Navigation #3 at aa50134; BotController #3 at df67e21. Current main documentation/ADRs through 246fe54 are synchronized into this line. Runtime fixes are reviewed in their owning component PRs before this preview pin; neither component is promoted to main.

Navigation remediation is described in the pinned component's `BASELINE_REMEDIATION.md`. The existing router, exclusive lease, full identity validator and managed callback drain remain unchanged. Legacy movement/Trail/watchdog/Recovery implementation remains physically absent from the matched adapted ZRPVE source. Missing Navigation does not reactivate legacy. Core/BotAI/ZR ownership is unchanged.

Production Recovery map permission is unknown, so every Recovery teleport fails closed. Geometry is a separate check. The admin Recovery toggle does not grant map permission. Recognized blocking doors/movers/brushes wait; unknown map-specific cages and timing cannot be classified authoritatively in this version. Validate those maps before promotion.

No timed/subtick payload, scoped view, MotionClip, persistent graph, route recorder, Human respawn feature or new movement hook/registry was added.

## Rebuild and provenance

```powershell
./scripts/build-navigation.ps1 -CounterStrikeSharpApiPath <CounterStrikeSharp.API.dll> -ToolchainRoot <supported cache> -NativeBuildRoot <fresh native output> -OutputDirectory <fresh package>
./scripts/test-navigation-package.ps1 -PackageDirectory <package> -FixtureRoot <fresh fixture> -CounterStrikeSharpApiPath <CounterStrikeSharp.API.dll>
./scripts/install-navigation.ps1 -ServerRoot <installation> -PackageDirectory <package> -AllowUnmoddedClient
# stop the dedicated server before every native/shared install or rollback
& <backup>/restore.ps1
```

The native build is performed from the exact clean gitlink by the build entry point. No external prebuilt DLL is accepted. Source changes, checkout/gitlink mismatch, unsupported profile/ABI, artifact/source mismatch, missing evidence or inconsistent hashes reject packaging/installation.

`manifest.json` binds Suite source, exact component gitlinks/checkouts, each artifact's owner revision, CSS API hash, .NET SDK/build properties and native receipt. `provenance/native-provenance.json` binds the native source/tree/profile/ABI to the output DLL and exact build input/compiler-command evidence. Lab is not a build source or runtime dependency. See component `BUILD_PROVENANCE.md`; external compiler/SDK cache location may historically be under Lab's ignored tools directory, but no Lab source is compiled.

The installer backs up **all files in owned native/plugin/shared subtrees plus the VDF**, including preserved files and explicit absence entries. It retires any existing experimental `ZEPVE.Lab.*` module files for the matched deployment, retaining their originals for whole rollback. It validates a stopped server twice and rejects changed preserved files before rollback. Native/shared components are replaced/restored together. The candidate includes the native VDF, previously omitted from the package.

These records establish the trusted local source/build/artifact chain; they are not signed remote attestations or a claim of byte-identical builds across machines. The source commit in a built manifest remains its source even when a later documentation-only commit records runtime evidence.

## Acceptance ledger

| Check | Current result |
| --- | --- |
| Navigation automated/model cases | PASS 108/108 on Navigation c80eeec; earlier candidate was 105/105 |
| Retained Core/BotAI/source cases | PASS 127/127 |
| Native router / synthetic managed-native interop | PASS 26/26 and 14/14 |
| Final matched Release build and receipt | Previous d540ab8 build PASS; refreshed candidate pending |
| Package mismatch and actual fixture rollback | PASS; install/whole rollback and 7 rejection cases |
| Actual installation and whole-package rollback | PASS; 169 original inventory entries restored and verified, then candidate reinstalled |
| Current candidate server load | Previous d540ab8 load PASS with upstream CS2Fixes PR 471 CI; refreshed build NOT TESTED |
| Infection / Bot generation | PASS user-reported on d540ab8 with CS2Fixes development dependency |
| Flat ground / ramps / distant stationary Human | FAIL user-reported jumping, intermittent pursuit/wall sticking on d540ab8; refreshed correction NOT TESTED |
| Initial combat aggression | FAIL user-reported; existing BotAI is ObserveOnly; no forced Enemy/visibility writes added |
| No-NAV Trail / Native→Trail / failed-action fallback | NOT TESTED |
| Two real Human targets and Trails | NOT TESTED |
| Death/respawn/rebind / disconnect/reconnect | NOT TESTED |
| Round/map / managed reload/detach/drain | Empty isolated-host managed reload PASS; active lease drain and full lifecycle acceptance NOT TESTED |
| Legacy writer disabled in loaded candidate | PASS source absence, 20/20 matched artifacts and runtime LegacyMovementDisabled=True; gameplay coverage incomplete |
| Unknown Recovery permission / intentional map waits | NOT TESTED in CS2 (model permission/hold checks PASS) |

See [recorded evidence](evidence/v03-20261018/validation-snapshot.json), [matched manifest](evidence/v03-20261018/matched-manifest.json), [native receipt](evidence/v03-20261018/native-provenance.json), and [external CS2Fixes compatibility](CS2FIXES_COMPAT_20261018.md). Human observations remain pending. No model PASS implies gameplay PASS. Complex jumps/full ZE certification/long endurance are outside this preview's current acceptance claim.

The previous built source was exactly `d540ab8ed3f2e7aad6980751ef23194050992940`, with Navigation `0b4b1a989ca00c5057e45800dd3b8e9734c4e3cf` and BotController `0627aefc4860e09c114f776c23f944877157fdef`. Local package `.codex/v03-test-preview-4` and its archived evidence describe that **failed gameplay candidate**, not an accepted preview. Follow-up Navigation is `c80eeecad48aab1a3b020c04f35f27d3c3434597`; the refreshed Suite build revision will be recorded by its own manifest. Later evidence-only commits never replace the manifest source revision. Current rollback root: `ZEPVE_Backup/v0.3-20261018-110148-7399984`; the first actual original restoration used sibling `v0.3-20261018-110125-6911450`.

After server use, scoped native logs may be new or changed. Preserve them under the same backup root before invoking the strict rollback preflight; do not discard runtime evidence or bypass preserved-file checks. Host-control originals (gameinfo, start batch, server.cfg) and external CS2Fixes snapshots are retained separately under that root. The original installation's byte restoration is proven; successful boot of that old mixed baseline is not claimed.

An isolated-host `quit` produced a separate `tier0.dll + 0x6a58e` dump. Its cause is unresolved; shutdown/native detach safety is **FAIL / investigate**, and physical native hot unload remains unsupported. This is separate from the reproduced CS2Fixes startup fault and cannot be called a passed drain test.
