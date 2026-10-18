param(
 [Parameter(Mandatory=$true)][string]$CounterStrikeSharpApiPath,
 [Parameter(Mandatory=$true)][string]$ToolchainRoot,
 [Parameter(Mandatory=$true)][string]$NativeBuildRoot,
 [Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$api=(Resolve-Path -LiteralPath $CounterStrikeSharpApiPath).Path
$native=Join-Path $root 'components/ZEPVE-BotController'
$nav=Join-Path $root 'components/ZEPVE-Navigation'
$stage=[IO.Path]::GetFullPath($OutputDirectory)
function GitValue([string]$directory,[string[]]$arguments){$value=& git -C $directory @arguments;if($LASTEXITCODE){throw 'Git evidence unavailable'};return ($value|Out-String).Trim()}
function AssertClean([string]$directory){if(GitValue $directory @('status','--porcelain','--untracked-files=all')){throw "Require clean committed source: $directory"}}
AssertClean $root;AssertClean $native;AssertClean $nav
$revision=GitValue $root @('rev-parse','HEAD')
$pins=@{}
foreach($component in @('ZEPVE-Navigation','ZEPVE-BotController')){
 $line=GitValue $root @('ls-tree','HEAD',"components/$component")
 if($line -notmatch '^160000 commit ([0-9a-f]{40})\s'){throw 'Missing component gitlink'}
 $pins[$component]=$Matches[1]
 if((GitValue (Join-Path $root "components/$component") @('rev-parse','HEAD'))-ne $pins[$component]){throw 'Component checkout differs from suite gitlink'}
}
if(Test-Path -LiteralPath $stage){throw 'Use a fresh staging directory'}
# Build from the exact pinned component. An arbitrary prebuilt DLL is never accepted.
& (Join-Path $native 'scripts/build-native.ps1') -ToolchainRoot $ToolchainRoot -OutputDirectory $NativeBuildRoot
$receiptPath=Join-Path $NativeBuildRoot 'native-provenance.json'
$receipt=Get-Content -LiteralPath $receiptPath -Raw|ConvertFrom-Json
if($receipt.SourceRevision-ne $pins['ZEPVE-BotController'] -or $receipt.MovementABI-ne 1 -or $receipt.Profile-ne 'windows-x64-msvc14.44-sdk22621-release-mt-v1'){throw 'Native provenance does not match pinned source/profile/ABI'}
$properties=@("-p:CounterStrikeSharpApiPath=$api","-p:SuiteRootPath=$root","-p:MovementBridgeRootPath=$native")
New-Item -ItemType Directory -Path (Join-Path $stage 'provenance') -Force|Out-Null
$projects=@('src/ZEPVE.Core/ZEPVE.Core.csproj','src/ZEPVE.BotAI/ZEPVE.BotAI.csproj','legacy/CounterStrikeSharp/plugins/Kzen-ZRPVE/Kzen-ZRPVE.csproj','components/ZEPVE-Navigation/src/ZEPVE.Navigation/ZEPVE.Navigation.csproj')
foreach($project in $projects){& dotnet build (Join-Path $root $project) -c Release @properties;if($LASTEXITCODE){throw "Build failed: $project"}}
foreach($test in @('tests/ZEPVE.Core.Tests/ZEPVE.Core.Tests.csproj','components/ZEPVE-Navigation/tests/ZEPVE.Navigation.Tests/ZEPVE.Navigation.Tests.csproj')){
 & dotnet run --project (Join-Path $root $test) -c Release @properties|Tee-Object -FilePath (Join-Path $stage ('provenance/'+(Split-Path -Leaf (Split-Path -Parent $test))+'.log'))
 if($LASTEXITCODE){throw "Retained tests failed: $test"}
}
$files=@()
function AddArtifact([string]$source,[string]$relative,[string]$sourceRevision){
 $dest=Join-Path $stage $relative
 New-Item -ItemType Directory -Path (Split-Path -Parent $dest) -Force|Out-Null
 Copy-Item -LiteralPath $source -Destination $dest
 $script:files+=[ordered]@{Path=$relative;SHA256=(Get-FileHash -LiteralPath $dest).Hash;SourceRevision=$sourceRevision}
}
foreach($entry in @(@('ZEPVE.Core','src/ZEPVE.Core','plugins',$revision),@('ZEPVE.BotAI','src/ZEPVE.BotAI','plugins',$revision),@('Kzen-ZRPVE','legacy/CounterStrikeSharp/plugins/Kzen-ZRPVE','plugins',$revision),@('ZEPVE.Abstractions','src/ZEPVE.Abstractions','shared',$revision),@('ZEPVE.Navigation','components/ZEPVE-Navigation/src/ZEPVE.Navigation','plugins',$pins['ZEPVE-Navigation']),@('ZEPVE.MovementBridge','components/ZEPVE-BotController/sdk/ZEPVE.MovementBridge','shared',$pins['ZEPVE-BotController']))){
 foreach($suffix in @('dll','pdb','deps.json')){
  if($entry[0]-eq 'ZEPVE.Abstractions' -and $suffix-eq 'deps.json'){continue}
  AddArtifact (Join-Path $root ($entry[1]+'/bin/Release/net10.0/'+$entry[0]+'.'+$suffix)) ('game/csgo/addons/counterstrikesharp/'+$entry[2]+'/'+$entry[0]+'/'+$entry[0]+'.'+$suffix) $entry[3]
 }
}
foreach($relative in @('addons/BotController/bin/win64/BotController.dll','addons/BotController/gamedata.json','addons/metamod/BotController.vdf')){AddArtifact (Join-Path $NativeBuildRoot ('package/'+$relative)) ('game/csgo/'+$relative) $pins['ZEPVE-BotController']}
if(($files|Where-Object Path -eq 'game/csgo/addons/BotController/bin/win64/BotController.dll').SHA256-ne $receipt.BinarySHA256){throw 'Native build artifact mismatch'}
$evidence=@()
foreach($name in @('native-provenance.json','build-inputs.json','compile_commands.json','build.log','ninja-deps.txt')){Copy-Item -LiteralPath (Join-Path $NativeBuildRoot $name) -Destination (Join-Path $stage "provenance/$name")}
foreach($file in Get-ChildItem -LiteralPath (Join-Path $stage 'provenance') -File){$evidence+=[ordered]@{Path='provenance/'+$file.Name;SHA256=(Get-FileHash -LiteralPath $file.FullName).Hash}}
AssertClean $root;AssertClean $native;AssertClean $nav
if((GitValue $root @('rev-parse','HEAD'))-ne $revision){throw 'Suite changed during build'}
$manifest=[ordered]@{Schema=2;Stage='v0.3 matched Navigation authority';SourceRevision=$revision;NativeRevision=$pins['ZEPVE-BotController'];NavigationRevision=$pins['ZEPVE-Navigation'];ComponentGitlinks=$pins;NativeABI=1;SharedABI='ZEPVE.Abstractions + ZEPVE.MovementBridge; install once';Profile=$receipt.Profile;NativeProvenanceSHA256=(Get-FileHash -LiteralPath $receiptPath).Hash;ApiSHA256=(Get-FileHash -LiteralPath $api).Hash;DotNetSDK=(& dotnet --version);Projects=$projects;ManagedBuildProperties=$properties;WorkingTreeChanges=@();NativeWorkingTreeChanges=@();NavigationWorkingTreeChanges=@();Runtime='NOT TESTED';Writer='Navigation movement/Trail/Recovery; Core identity/policy; BotAI target; external ZR respawn';Files=$files;Evidence=$evidence}
$manifest|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $stage 'manifest.json') -Encoding utf8
foreach($name in @('install-navigation.ps1','restore-navigation.ps1','navigation-package-common.ps1')){Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination $stage}
Write-Output "Matched build/provenance PASS: $stage; $($files.Count) files; no deployment; runtime NOT TESTED"
