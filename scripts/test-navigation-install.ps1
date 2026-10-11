$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'navigation-package-common.ps1')
$repo=Split-Path -Parent $PSScriptRoot
$fixture=Join-Path $repo ('artifacts/install-fixture-'+[DateTimeOffset]::Now.ToString('yyyyMMdd-HHmmss-fffffff'))
$root=Join-Path $fixture 'server';$package=Join-Path $fixture 'package'
New-Item -ItemType Directory -Path $root,$package -Force|Out-Null
$apiRelative='game/csgo/addons/counterstrikesharp/api/CounterStrikeSharp.API.dll'
$api=Join-Path $root $apiRelative;New-Item -ItemType Directory -Path (Split-Path -Parent $api) -Force|Out-Null
[IO.File]::WriteAllText($api,'synthetic fixture API; not a runtime DLL')
$payload=@();$original=@{}
foreach($path in (Get-NavigationPackagePaths)){
 $old=Join-Path $root $path;$new=Join-Path $package $path
 New-Item -ItemType Directory -Path (Split-Path -Parent $old),(Split-Path -Parent $new) -Force|Out-Null
 [IO.File]::WriteAllText($old,'old-'+$path);[IO.File]::WriteAllText($new,'new-'+$path)
 $original[$path]=(Get-FileHash -LiteralPath $old).Hash
 $payload+=[ordered]@{Path=$path;SHA256=(Get-FileHash -LiteralPath $new).Hash}
}
foreach($path in (Get-NavigationLabPaths)){
 $old=Join-Path $root $path;New-Item -ItemType Directory -Path (Split-Path -Parent $old) -Force|Out-Null
 [IO.File]::WriteAllText($old,'old-lab-'+$path);$original[$path]=(Get-FileHash -LiteralPath $old).Hash
}
$manifest=[ordered]@{Stage='v0.3 matched Navigation authority';TraversalABI=2;SourceRevision='fixture';NativeRevision='fixture';NavigationRevision='fixture';WorkingTreeChanges=@();NativeWorkingTreeChanges=@();NavigationWorkingTreeChanges=@();ApiSHA256=(Get-FileHash -LiteralPath $api).Hash;Files=$payload}
$manifestPath=Join-Path $package 'manifest.json';$manifest|ConvertTo-Json -Depth 6|Set-Content -LiteralPath $manifestPath
& (Join-Path $PSScriptRoot 'install-navigation.ps1') -ServerRoot $root -PackageDirectory $package
$backup=(Get-ChildItem (Join-Path $root 'ZEPVE_Backup') -Directory|Sort-Object Name|Select-Object -Last 1).FullName
foreach($path in (Get-NavigationLabPaths)){if(Test-Path -LiteralPath (Join-Path $root $path)){throw 'Lab consumer still loadable'}}
& (Join-Path $backup 'restore.ps1') -VerifyOnly
& (Join-Path $backup 'restore.ps1')
foreach($path in $original.Keys){if((Get-FileHash -LiteralPath (Join-Path $root $path)).Hash-ne $original[$path]){throw 'Whole fixture restore mismatch'}}
Write-Output 'PASS matched19 install + Lab3 quarantine + actual22-file fixture restore (NOT CS2 runtime)'
$manifest.Files=@($payload|Select-Object -Skip 1);$manifest|ConvertTo-Json -Depth 6|Set-Content -LiteralPath $manifestPath
$refused=$false;try{& (Join-Path $PSScriptRoot 'install-navigation.ps1') -ServerRoot $root -PackageDirectory $package}catch{$refused=$true}
if(-not $refused){throw 'Incomplete package accepted'}
Write-Output 'PASS incomplete matched package rejected before mutation'
