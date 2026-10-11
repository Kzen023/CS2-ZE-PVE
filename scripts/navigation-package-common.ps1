$ErrorActionPreference='Stop'
function Get-NavigationPackagePaths {
    $paths=@('game/csgo/addons/BotController/bin/win64/BotController.dll','game/csgo/addons/BotController/gamedata.json')
    foreach($entry in @(@('ZEPVE.Core','plugins'),@('ZEPVE.BotAI','plugins'),@('Kzen-ZRPVE','plugins'),@('ZEPVE.Abstractions','shared'),@('ZEPVE.Navigation','plugins'),@('ZEPVE.MovementBridge','shared'))){foreach($suffix in @('dll','pdb','deps.json')){if($entry[0]-eq 'ZEPVE.Abstractions' -and $suffix-eq 'deps.json'){continue};$paths+='game/csgo/addons/counterstrikesharp/'+$entry[1]+'/'+$entry[0]+'/'+$entry[0]+'.'+$suffix}}
    return $paths
}
function Assert-NavigationServerStopped([string]$Root,[bool]$AllowClient){
    foreach($process in @(Get-CimInstance Win32_Process -Filter "name='cs2.exe'")){
        if(-not $process.ExecutablePath){throw 'Cannot identify CS2 process'}
        if(-not $process.ExecutablePath.StartsWith($Root+'\',[StringComparison]::OrdinalIgnoreCase)){continue}
        if(-not $AllowClient -or $process.CommandLine -match '(?i)(^|\s)-dedicated(\s|$)'){throw 'Stop server before native/shared matched replacement'}
        $client=Get-Process -Id $process.ProcessId -ErrorAction Stop
        foreach($module in $client.Modules){if($module.FileName -match '(?i)[\\/]addons[^\\/]*[\\/]|metamod|counterstrikesharp|cs2fixes|botcontroller'){throw 'Client has addon modules loaded'}}
    }
}
function Resolve-NavigationTarget([string]$Root,[string]$Relative){
    if($Relative -notin (@(Get-NavigationPackagePaths)+@(Get-NavigationLabPaths))){throw 'Unexpected matched package path'}
    $target=[IO.Path]::GetFullPath((Join-Path $Root $Relative));if(-not $target.StartsWith($Root+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Target escapes server root'};return $target
}

function Get-NavigationLabPaths {
    @('dll','pdb','deps.json') | ForEach-Object { 'game/csgo/addons/counterstrikesharp/plugins/ZEPVE.Lab.CommandFidelity/ZEPVE.Lab.CommandFidelity.'+$_ }
}
