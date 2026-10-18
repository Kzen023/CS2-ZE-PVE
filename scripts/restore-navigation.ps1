param([switch]$VerifyOnly,[switch]$AllowUnmoddedClient)
. (Join-Path $PSScriptRoot 'navigation-package-common.ps1')
$record=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'deployment.json') -Raw|ConvertFrom-Json
$root=(Resolve-Path -LiteralPath $record.ServerRoot).Path.TrimEnd('\','/')
Assert-NavigationServerStopped $root ([bool]$AllowUnmoddedClient)
if($record.Schema-ne 2){throw 'Unsupported rollback inventory'}
$inventory=@(Get-NavigationInventoryPaths $root)
foreach($path in $inventory){if($path -notin $record.Files.Path){throw 'New scoped file appeared: preserve it before rollback'}}
foreach($file in $record.Files){
 $target=Resolve-NavigationTarget $root $file.Path
 if($file.Existed -and (Get-FileHash -LiteralPath (Join-Path $PSScriptRoot $file.Path)).Hash-ne $file.OldSHA256){throw 'Backup integrity mismatch'}
 $expected=if($record.Status.StartsWith('restored')){$file.OldSHA256}else{$file.NewSHA256}
 if($expected){if((Get-FileHash -LiteralPath $target).Hash-ne $expected){throw "Installed/preserved file changed: $($file.Path); preserve before rollback"}}elseif(Test-Path -LiteralPath $target){throw 'Expected absent artifact exists'}
}
if($VerifyOnly -or $record.Status.StartsWith('restored')){Write-Output 'Whole scoped rollback inventory/hash verification PASS';return}
$archiveRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot ('retired-'+[DateTimeOffset]::Now.ToString('yyyyMMdd-HHmmss-fffffff'))))
if(-not $archiveRoot.StartsWith($PSScriptRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Invalid retired artifact target'}
Assert-NavigationServerStopped $root ([bool]$AllowUnmoddedClient)
foreach($file in $record.Files){
 $target=Resolve-NavigationTarget $root $file.Path
 if(Test-Path -LiteralPath $target){$archive=Join-Path $archiveRoot $file.Path;New-Item -ItemType Directory -Path (Split-Path -Parent $archive) -Force|Out-Null;Move-Item -LiteralPath $target -Destination $archive}
 if($file.Existed){New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force|Out-Null;Copy-Item -LiteralPath (Join-Path $PSScriptRoot $file.Path) -Destination $target -Force}
}
foreach($file in $record.Files){$target=Resolve-NavigationTarget $root $file.Path;if($file.Existed){if((Get-FileHash -LiteralPath $target).Hash-ne $file.OldSHA256){throw 'Restored original mismatch'}}elseif(Test-Path -LiteralPath $target){throw 'Added artifact still installed'}}
$record.Status='restored; whole matched baseline'
$record|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $PSScriptRoot 'deployment.json') -Encoding utf8
Write-Output "ACTUAL WHOLE-PACKAGE DISK ROLLBACK PASS: $($record.Files.Count) entries; native/Core/BotAI/legacy/shared and original experiments restored together; runtime NOT TESTED"
