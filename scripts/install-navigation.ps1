param([Parameter(Mandatory=$true)][string]$ServerRoot,[Parameter(Mandatory=$true)][string]$PackageDirectory,[switch]$AllowUnmoddedClient)
. (Join-Path $PSScriptRoot 'navigation-package-common.ps1')
$root=(Resolve-Path -LiteralPath $ServerRoot).Path.TrimEnd('\','/')
$package=(Resolve-Path -LiteralPath $PackageDirectory).Path
Assert-NavigationServerStopped $root ([bool]$AllowUnmoddedClient)
$manifest=Assert-NavigationPackage $package
if((Get-FileHash -LiteralPath (Join-Path $root 'game/csgo/addons/counterstrikesharp/api/CounterStrikeSharp.API.dll')).Hash-ne $manifest.ApiSHA256){throw 'CSS API mismatch'}
foreach($plugin in @('ZEPVE.Core','ZEPVE.BotAI','Kzen-ZRPVE','ZEPVE.Navigation')){foreach($shared in @('ZEPVE.Abstractions','ZEPVE.MovementBridge')){if(Test-Path -LiteralPath (Join-Path $root "game/csgo/addons/counterstrikesharp/plugins/$plugin/$shared.dll")){throw 'Private shared ABI copy detected'}}}
$backup=Join-Path $root ('ZEPVE_Backup/v0.3-'+[DateTimeOffset]::Now.ToString('yyyyMMdd-HHmmss-fffffff'))
New-Item -ItemType Directory -Path $backup|Out-Null
$inventory=@()
foreach($path in Get-NavigationInventoryPaths $root){
 $target=Resolve-NavigationTarget $root $path
 $exists=Test-Path -LiteralPath $target;$old=$null
 if($exists){$old=(Get-FileHash -LiteralPath $target).Hash;$copy=Join-Path $backup $path;New-Item -ItemType Directory -Path (Split-Path -Parent $copy) -Force|Out-Null;Copy-Item -LiteralPath $target -Destination $copy;if((Get-FileHash -LiteralPath $copy).Hash-ne $old){throw 'Recoverable backup hash mismatch'}}
 $new=$manifest.Files|Where-Object Path -eq $path
 $retired=$path -match '/plugins/ZEPVE\.Lab\.'
 $inventory+=[ordered]@{Path=$path;Existed=$exists;OldSHA256=$old;NewSHA256=$(if($retired){$null}elseif($new){$new.SHA256}else{$old});Retired=$retired}
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'restore-navigation.ps1') -Destination (Join-Path $backup 'restore.ps1')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'navigation-package-common.ps1') -Destination $backup
Copy-Item -LiteralPath (Join-Path $package 'manifest.json') -Destination (Join-Path $backup 'candidate-manifest.json')
$record=[ordered]@{Schema=2;ServerRoot=$root;Status='prepared';SourceRevision=$manifest.SourceRevision;NativeRevision=$manifest.NativeRevision;NavigationRevision=$manifest.NavigationRevision;ManifestSHA256=(Get-FileHash -LiteralPath (Join-Path $package 'manifest.json')).Hash;Files=$inventory}
$record|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $backup 'deployment.json') -Encoding utf8
Assert-NavigationServerStopped $root ([bool]$AllowUnmoddedClient)
try {
 foreach($file in $inventory|Where-Object Retired){$target=Resolve-NavigationTarget $root $file.Path;$retired=Join-Path $backup ('retired-experiments/'+$file.Path);New-Item -ItemType Directory -Path (Split-Path -Parent $retired) -Force|Out-Null;Move-Item -LiteralPath $target -Destination $retired}
 foreach($file in $manifest.Files){$target=Resolve-NavigationTarget $root $file.Path;New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force|Out-Null;Copy-Item -LiteralPath (Join-Path $package $file.Path) -Destination $target -Force}
 foreach($file in $inventory){$target=Resolve-NavigationTarget $root $file.Path;if($file.Retired){if(Test-Path -LiteralPath $target){throw 'Experimental module still active'}}elseif((Get-FileHash -LiteralPath $target).Hash-ne $file.NewSHA256){throw 'Installed/preserved hash mismatch'}}
 $record.Status='installed; runtime NOT TESTED'
 $record|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $backup 'deployment.json') -Encoding utf8
} catch {
 foreach($file in $inventory){$target=Resolve-NavigationTarget $root $file.Path;if($file.Existed){New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force|Out-Null;Copy-Item -LiteralPath (Join-Path $backup $file.Path) -Destination $target -Force}elseif(Test-Path -LiteralPath $target){$archive=Join-Path $backup ('failed-install/'+$file.Path);New-Item -ItemType Directory -Path (Split-Path -Parent $archive) -Force|Out-Null;Move-Item -LiteralPath $target -Destination $archive}}
 throw
}
Write-Output "MATCHED DISK INSTALL PASS: $($manifest.Files.Count) artifacts; $($inventory.Count) complete scoped inventory entries; backup=$backup; runtime NOT TESTED"
