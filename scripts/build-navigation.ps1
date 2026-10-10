param([Parameter(Mandatory=$true)][string]$CounterStrikeSharpApiPath,[Parameter(Mandatory=$true)][string]$NativeComponentRoot,[Parameter(Mandatory=$true)][string]$NativeBuildRoot,[Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop';$root=Split-Path -Parent $PSScriptRoot;$api=(Resolve-Path -LiteralPath $CounterStrikeSharpApiPath).Path
$native=(Resolve-Path -LiteralPath $NativeComponentRoot).Path;$nav=Join-Path $root 'components/ZEPVE-Navigation';$stage=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $stage){throw 'Use a fresh staging directory'}
$properties=@("-p:CounterStrikeSharpApiPath=$api","-p:SuiteRootPath=$root","-p:MovementBridgeRootPath=$native")
foreach($project in @('src/ZEPVE.Core/ZEPVE.Core.csproj','src/ZEPVE.BotAI/ZEPVE.BotAI.csproj','legacy/CounterStrikeSharp/plugins/Kzen-ZRPVE/Kzen-ZRPVE.csproj','components/ZEPVE-Navigation/src/ZEPVE.Navigation/ZEPVE.Navigation.csproj')){& dotnet build (Join-Path $root $project) -c Release @properties;if($LASTEXITCODE){throw "Build failed: $project"}}
& dotnet run --project (Join-Path $root 'tests/ZEPVE.Core.Tests/ZEPVE.Core.Tests.csproj') -c Release @properties
if($LASTEXITCODE){throw 'Retained suite tests failed'}
& dotnet run --project (Join-Path $nav 'tests/ZEPVE.Navigation.Tests/ZEPVE.Navigation.Tests.csproj') -c Release @properties
if($LASTEXITCODE){throw 'Navigation model tests failed'}
$files=@()
foreach($entry in @(@('ZEPVE.Core','src/ZEPVE.Core','plugins'),@('ZEPVE.BotAI','src/ZEPVE.BotAI','plugins'),@('Kzen-ZRPVE','legacy/CounterStrikeSharp/plugins/Kzen-ZRPVE','plugins'),@('ZEPVE.Abstractions','src/ZEPVE.Abstractions','shared'),@('ZEPVE.Navigation','components/ZEPVE-Navigation/src/ZEPVE.Navigation','plugins'),@('ZEPVE.MovementBridge',(Join-Path $native 'sdk/ZEPVE.MovementBridge'),'shared'))){
 $source=if([IO.Path]::IsPathRooted($entry[1])){$entry[1]}else{Join-Path $root $entry[1]}
 foreach($suffix in @('dll','pdb','deps.json')){
  if($entry[0]-eq 'ZEPVE.Abstractions' -and $suffix-eq 'deps.json'){continue}
  $path='game/csgo/addons/counterstrikesharp/'+$entry[2]+'/'+$entry[0]+'/'+$entry[0]+'.'+$suffix
  $dest=Join-Path $stage $path;New-Item -ItemType Directory -Path (Split-Path -Parent $dest) -Force|Out-Null;Copy-Item -LiteralPath (Join-Path $source ('bin/Release/net10.0/'+$entry[0]+'.'+$suffix)) -Destination $dest
  $files+=[ordered]@{Path=$path;SHA256=(Get-FileHash -LiteralPath $dest).Hash}
 }
}
foreach($entry in @(@('game/csgo/addons/BotController/bin/win64/BotController.dll','package/addons/BotController/bin/win64/BotController.dll'),@('game/csgo/addons/BotController/gamedata.json','package/addons/BotController/gamedata.json'))){$dest=Join-Path $stage $entry[0];New-Item -ItemType Directory -Path (Split-Path -Parent $dest) -Force|Out-Null;Copy-Item -LiteralPath (Join-Path $NativeBuildRoot $entry[1]) -Destination $dest;$files+=[ordered]@{Path=$entry[0];SHA256=(Get-FileHash -LiteralPath $dest).Hash}}
$manifest=[ordered]@{Stage='v0.3 matched Navigation authority';SourceRevision=(& git -C $root rev-parse HEAD);NativeRevision=(& git -C $native rev-parse HEAD);NavigationRevision=(& git -C $nav rev-parse HEAD);WorkingTreeChanges=@(& git -C $root status --porcelain);NativeWorkingTreeChanges=@(& git -C $native status --porcelain);NavigationWorkingTreeChanges=@(& git -C $nav status --porcelain);ApiSHA256=(Get-FileHash -LiteralPath $api).Hash;NativeABI=1;NativeLifetime='ProcessResident; physical hot unload unsupported';Writer='Navigation movement/Trail/Recovery; Core identity/policy; BotAI target; external ZR respawn';Runtime='NOT TESTED for this build';Files=$files}
$manifest|ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $stage 'manifest.json') -Encoding utf8
Write-Output "Matched package ready: $stage; files=$($files.Count); no deployment"
