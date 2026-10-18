$ErrorActionPreference='Stop'
function Get-NavigationPackagePaths {
 $paths=@('game/csgo/addons/BotController/bin/win64/BotController.dll','game/csgo/addons/BotController/gamedata.json','game/csgo/addons/metamod/BotController.vdf')
 foreach($entry in @(@('ZEPVE.Core','plugins'),@('ZEPVE.BotAI','plugins'),@('Kzen-ZRPVE','plugins'),@('ZEPVE.Abstractions','shared'),@('ZEPVE.Navigation','plugins'),@('ZEPVE.MovementBridge','shared'))){foreach($suffix in @('dll','pdb','deps.json')){if($entry[0]-eq 'ZEPVE.Abstractions' -and $suffix-eq 'deps.json'){continue};$paths+='game/csgo/addons/counterstrikesharp/'+$entry[1]+'/'+$entry[0]+'/'+$entry[0]+'.'+$suffix}}
 return $paths
}
function Get-NavigationOwnedRoots {
 @('game/csgo/addons/BotController','game/csgo/addons/counterstrikesharp/plugins/ZEPVE.Core','game/csgo/addons/counterstrikesharp/plugins/ZEPVE.BotAI','game/csgo/addons/counterstrikesharp/plugins/ZEPVE.Navigation','game/csgo/addons/counterstrikesharp/plugins/Kzen-ZRPVE','game/csgo/addons/counterstrikesharp/shared/ZEPVE.Abstractions','game/csgo/addons/counterstrikesharp/shared/ZEPVE.MovementBridge')
}
function Assert-NavigationServerStopped([string]$Root,[bool]$AllowClient){
 foreach($process in @(Get-CimInstance Win32_Process -Filter "name='cs2.exe'")){
  if(-not $process.ExecutablePath){throw 'Cannot identify CS2 process'}
  if(-not $process.ExecutablePath.StartsWith($Root+'\',[StringComparison]::OrdinalIgnoreCase)){continue}
  if(-not $AllowClient -or $process.CommandLine -match '(?i)(^|\s)-dedicated(\s|$)'){throw 'Stop CS2 before native/shared matched replacement'}
  foreach($module in (Get-Process -Id $process.ProcessId -ErrorAction Stop).Modules){if($module.FileName -match '(?i)[\\/]addons[^\\/]*[\\/]|metamod|counterstrikesharp|cs2fixes|botcontroller'){throw 'Client has addon modules loaded'}}
 }
}
function Resolve-NavigationTarget([string]$Root,[string]$Relative){
 if($Relative -match '\\|(^|/)\.\.?(/|$)' -or [IO.Path]::IsPathRooted($Relative)){throw 'Invalid matched relative path'}
 $allowed=$Relative -in (Get-NavigationPackagePaths)
 foreach($prefix in Get-NavigationOwnedRoots){if($Relative.StartsWith($prefix+'/',[StringComparison]::Ordinal)){$allowed=$true}}
 if($Relative -match '^game/csgo/addons/counterstrikesharp/plugins/ZEPVE\.Lab\.[A-Za-z0-9_.-]+/[^:]+$'){$allowed=$true}
 if(-not $allowed){throw "Unexpected matched inventory path: $Relative"}
 $target=[IO.Path]::GetFullPath((Join-Path $Root $Relative))
 if(-not $target.StartsWith($Root+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Target escapes server root'}
 for($path=$target;$path.Length-ge $Root.Length;$path=Split-Path -Parent $path){if((Test-Path -LiteralPath $path) -and ((Get-Item -LiteralPath $path -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'Reparse point in matched target'};if($path-eq $Root){break}}
 return $target
}
function Get-NavigationInventoryPaths([string]$Root){
 $paths=@(Get-NavigationPackagePaths)
 $prefixes=@(Get-NavigationOwnedRoots)
 $plugins=Join-Path $Root 'game/csgo/addons/counterstrikesharp/plugins'
 if(Test-Path -LiteralPath $plugins){foreach($directory in Get-ChildItem -LiteralPath $plugins -Directory -Filter 'ZEPVE.Lab.*'){$prefixes+='game/csgo/addons/counterstrikesharp/plugins/'+$directory.Name}}
 foreach($prefix in $prefixes){$directory=Join-Path $Root $prefix;if(Test-Path -LiteralPath $directory){foreach($file in Get-ChildItem -LiteralPath $directory -Recurse -File -Force){$paths+=[IO.Path]::GetRelativePath($Root,$file.FullName).Replace('\','/')}}}
 return @($paths|Sort-Object -Unique)
}
function Assert-NavigationPackage([string]$Package){
 $manifest=Get-Content -LiteralPath (Join-Path $Package 'manifest.json') -Raw|ConvertFrom-Json
 if($manifest.Schema-ne 2 -or $manifest.Stage-ne 'v0.3 matched Navigation authority' -or $manifest.NativeABI-ne 1 -or $manifest.Profile-ne 'windows-x64-msvc14.44-sdk22621-release-mt-v1' -or $manifest.WorkingTreeChanges.Count -or $manifest.NativeWorkingTreeChanges.Count -or $manifest.NavigationWorkingTreeChanges.Count){throw 'Require clean matched provenance schema/profile/ABI'}
 foreach($revision in @($manifest.SourceRevision,$manifest.NativeRevision,$manifest.NavigationRevision)){if($revision -notmatch '^[0-9a-f]{40}$'){throw 'Invalid source revision'}}
 if($manifest.ComponentGitlinks.'ZEPVE-BotController'-ne $manifest.NativeRevision -or $manifest.ComponentGitlinks.'ZEPVE-Navigation'-ne $manifest.NavigationRevision){throw 'Gitlink/checkout mismatch'}
 $allowed=@(Get-NavigationPackagePaths)
 if($manifest.Files.Count-ne $allowed.Count -or @($manifest.Files.Path|Sort-Object -Unique).Count-ne $allowed.Count){throw 'Incomplete/duplicate matched package'}
 foreach($file in $manifest.Files){
  if($file.Path -notin $allowed){throw 'Unexpected install file'}
  if((Get-FileHash -LiteralPath (Join-Path $Package $file.Path)).Hash-ne $file.SHA256){throw 'Package hash mismatch'}
  $expected=$manifest.SourceRevision
  if($file.Path -match '/BotController/|/BotController\.vdf$|/ZEPVE\.MovementBridge/'){$expected=$manifest.NativeRevision}
  elseif($file.Path -match '/ZEPVE\.Navigation/'){$expected=$manifest.NavigationRevision}
  if($file.SourceRevision-ne $expected){throw 'Artifact/source ownership mismatch'}
 }
 foreach($required in @('native-provenance.json','build-inputs.json','compile_commands.json','build.log','ninja-deps.txt')){if(('provenance/'+$required) -notin $manifest.Evidence.Path){throw 'Missing build evidence'}}
 foreach($file in $manifest.Evidence){if($file.Path -notmatch '^provenance/[A-Za-z0-9_.-]+$' -or (Get-FileHash -LiteralPath (Join-Path $Package $file.Path)).Hash-ne $file.SHA256){throw 'Build evidence mismatch'}}
 $receiptPath=Join-Path $Package 'provenance/native-provenance.json'
 if((Get-FileHash -LiteralPath $receiptPath).Hash-ne $manifest.NativeProvenanceSHA256){throw 'Native receipt mismatch'}
 $receipt=Get-Content -LiteralPath $receiptPath -Raw|ConvertFrom-Json
 if($receipt.SourceRevision-ne $manifest.NativeRevision -or $receipt.MovementABI-ne 1 -or $receipt.CompatibilityABI-ne 24 -or $receipt.Profile-ne $manifest.Profile -or $receipt.SpdlogRevision-ne 'd6c93f38a08b4ea0f8ffcfd7f7857307d5d5c3fa'){throw 'Native provenance revision/ABI/profile/dependency mismatch'}
 if($receipt.BinarySHA256-ne ($manifest.Files|Where-Object Path -eq 'game/csgo/addons/BotController/bin/win64/BotController.dll').SHA256 -or $receipt.GamedataSHA256-ne ($manifest.Files|Where-Object Path -eq 'game/csgo/addons/BotController/gamedata.json').SHA256){throw 'Native receipt/artifact mismatch'}
 foreach($entry in @(@('build-inputs.json','InputsSHA256'),@('compile_commands.json','CommandsSHA256'),@('build.log','BuildLogSHA256'))){if((Get-FileHash -LiteralPath (Join-Path $Package ('provenance/'+$entry[0]))).Hash-ne $receipt.($entry[1])){throw 'Native build inputs/commands/log mismatch'}}
 return $manifest
}
