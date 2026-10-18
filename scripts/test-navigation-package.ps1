param([Parameter(Mandatory=$true)][string]$PackageDirectory,[Parameter(Mandatory=$true)][string]$FixtureRoot,[Parameter(Mandatory=$true)][string]$CounterStrikeSharpApiPath)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'navigation-package-common.ps1')
$package=(Resolve-Path -LiteralPath $PackageDirectory).Path
$fixture=[IO.Path]::GetFullPath($FixtureRoot)
if(Test-Path -LiteralPath $fixture){throw 'Use a fresh fixture root'}
New-Item -ItemType Directory -Path $fixture|Out-Null
function Put([string]$relative,[string]$content){$file=Join-Path $fixture $relative;New-Item -ItemType Directory -Path (Split-Path -Parent $file) -Force|Out-Null;[IO.File]::WriteAllText($file,$content)}
function Snapshot {return @((Get-ChildItem -LiteralPath (Join-Path $fixture 'game') -Recurse -File|Sort-Object FullName|ForEach-Object {[IO.Path]::GetRelativePath($fixture,$_.FullName)+' '+(Get-FileHash -LiteralPath $_.FullName).Hash}))}
function MustReject([scriptblock]$action,[string]$label){$rejected=$false;try{& $action|Out-Null}catch{$rejected=$true};if(-not $rejected){throw "FAIL expected rejection: $label"};Write-Output "PASS reject $label"}
foreach($path in Get-NavigationPackagePaths){if($path -notmatch '/ZEPVE\.(Navigation|MovementBridge)/'){Put $path ('old:'+ $path)}}
Put 'game/csgo/addons/counterstrikesharp/plugins/Kzen-ZRPVE/settings.json' '{"preserve":true}'
Put 'game/csgo/addons/counterstrikesharp/plugins/ZEPVE.Lab.Fixture/Test.dll' 'old experiment'
$api=Join-Path $fixture 'game/csgo/addons/counterstrikesharp/api/CounterStrikeSharp.API.dll'
New-Item -ItemType Directory -Path (Split-Path -Parent $api) -Force|Out-Null;Copy-Item -LiteralPath $CounterStrikeSharpApiPath -Destination $api
$before=Snapshot
& (Join-Path $PSScriptRoot 'install-navigation.ps1') -ServerRoot $fixture -PackageDirectory $package
$backup=(Get-ChildItem -LiteralPath (Join-Path $fixture 'ZEPVE_Backup') -Directory|Select-Object -First 1).FullName
if(Test-Path -LiteralPath (Join-Path $fixture 'game/csgo/addons/counterstrikesharp/plugins/ZEPVE.Lab.Fixture/Test.dll')){throw 'Lab module retained'}
Put 'game/csgo/addons/counterstrikesharp/plugins/Kzen-ZRPVE/settings.json' 'changed after install'
MustReject {& (Join-Path $backup 'restore.ps1')} 'changed preserved config before rollback'
Put 'game/csgo/addons/counterstrikesharp/plugins/Kzen-ZRPVE/settings.json' '{"preserve":true}'
& (Join-Path $backup 'restore.ps1')
if(Compare-Object $before (Snapshot)){throw 'Actual fixture rollback inventory differs'}
& (Join-Path $backup 'restore.ps1') -VerifyOnly
Write-Output 'PASS actual whole scoped fixture rollback (including absent/new modules and retired Lab files)'
$bad=Join-Path $fixture 'negative-package';Copy-Item -LiteralPath $package -Destination $bad -Recurse
$original=Get-Content -LiteralPath (Join-Path $bad 'manifest.json') -Raw
foreach($kind in @('gitlink','profile','artifact source','missing evidence','missing file')){
 $manifest=$original|ConvertFrom-Json
 switch($kind){
  'gitlink' {$manifest.ComponentGitlinks.'ZEPVE-BotController'='0000000000000000000000000000000000000000'}
  'profile' {$manifest.Profile='unsupported'}
  'artifact source' {$manifest.Files[0].SourceRevision='0000000000000000000000000000000000000000'}
  'missing evidence' {$manifest.Evidence=@()}
  'missing file' {$manifest.Files=@($manifest.Files|Select-Object -Skip 1)}
 }
 $manifest|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $bad 'manifest.json') -Encoding utf8
 MustReject {Assert-NavigationPackage $bad} $kind
}
$original|Set-Content -LiteralPath (Join-Path $bad 'manifest.json') -Encoding utf8
[IO.File]::AppendAllText((Join-Path $bad 'game/csgo/addons/BotController/bin/win64/BotController.dll'),'tamper')
MustReject {Assert-NavigationPackage $bad} 'foreign/tampered native artifact'
Write-Output 'TOTAL package fixture: install/actual rollback PASS; 7 rejection cases PASS; not CS2 runtime'
