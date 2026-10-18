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
| Navigation automated/model cases | PASS 104/104 |
| Retained Core/BotAI/source cases | PASS 127/127 |
| Native router / synthetic managed-native interop | PASS 26/26 and 14/14 |
| Final matched Release build and receipt | Pending |
| Package mismatch and actual fixture rollback | Pending |
| Actual installation and whole-package rollback | Pending |
| Current candidate server load | NOT TESTED |
| Flat ground / ramps / distant stationary Human | NOT TESTED |
| No-NAV Trail / Native→Trail / failed-action fallback | NOT TESTED |
| Two real Human targets and Trails | NOT TESTED |
| Death/respawn/rebind / disconnect/reconnect | NOT TESTED |
| Round/map / managed reload/detach/drain | NOT TESTED |
| Legacy writer disabled in loaded candidate | NOT TESTED (source absence confirmed) |
| Unknown Recovery permission / intentional map waits | NOT TESTED in CS2 (model permission/hold checks PASS) |

Required human observations and final disk/runtime evidence will be appended after the exact package is built and exercised. No model PASS implies gameplay PASS. Complex jumps/full ZE certification/long endurance are outside this preview's current acceptance claim.
