param([switch]$VerifyOnly,[switch]$AllowUnmoddedClient)
. (Join-Path $PSScriptRoot 'navigation-package-common.ps1')
$record=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'deployment.json') -Raw|ConvertFrom-Json;$root=(Resolve-Path -LiteralPath $record.ServerRoot).Path.TrimEnd('\','/')
Assert-NavigationServerStopped $root ([bool]$AllowUnmoddedClient)
foreach($file in $record.Files){$target=Resolve-NavigationTarget $root $file.Path;if($file.Existed -and (Get-FileHash -LiteralPath (Join-Path $PSScriptRoot $file.Path)).Hash-ne $file.OldSHA256){throw 'Backup integrity mismatch'};if($record.Status.StartsWith('installed') -and ((Get-FileHash -LiteralPath $target).Hash-ne $file.NewSHA256)){throw 'Installed file changed: preserve it before rollback'}}
if($VerifyOnly){Write-Output 'Whole matched rollback inventory/hash PASS';return}
$archiveRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot ('retired-'+[DateTimeOffset]::Now.ToString('yyyyMMdd-HHmmss'))));if(-not $archiveRoot.StartsWith($PSScriptRoot+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Invalid retired artifact target'}
foreach($file in $record.Files){$target=Resolve-NavigationTarget $root $file.Path;$archive=Join-Path $archiveRoot $file.Path;New-Item -ItemType Directory -Path (Split-Path -Parent $archive) -Force|Out-Null;Copy-Item -LiteralPath $target -Destination $archive;if($file.Existed){Copy-Item -LiteralPath (Join-Path $PSScriptRoot $file.Path) -Destination $target -Force}else{Move-Item -LiteralPath $target -Destination ($archive+'.removed')}}
foreach($file in $record.Files){$target=Resolve-NavigationTarget $root $file.Path;if($file.Existed){if((Get-FileHash -LiteralPath $target).Hash-ne $file.OldSHA256){throw 'Restored original mismatch'}}elseif(Test-Path -LiteralPath $target){throw 'New module still installed'}}
$record.Status='restored; whole matched baseline';$record|ConvertTo-Json -Depth 6|Set-Content -LiteralPath (Join-Path $PSScriptRoot 'deployment.json') -Encoding utf8
Write-Output 'ROLLBACK PASS: native/Core/BotAI/legacy/shared ABI restored together; Navigation/SDK added files archived; no mixed writers'
