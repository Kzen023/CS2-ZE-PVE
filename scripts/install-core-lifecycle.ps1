param(
    [Parameter(Mandatory = $true)][string]$ServerRoot,
    [Parameter(Mandatory = $true)][string]$PackageDirectory
)
$ErrorActionPreference = 'Stop'
$installRoot = (Resolve-Path -LiteralPath $ServerRoot).Path.TrimEnd('\', '/')
$packageRoot = (Resolve-Path -LiteralPath $PackageDirectory).Path.TrimEnd('\', '/')
if (-not (Test-Path -LiteralPath (Join-Path $installRoot 'game/csgo/addons/counterstrikesharp/api/CounterStrikeSharp.API.dll'))) { throw 'ServerRoot is not a CS2 CounterStrikeSharp installation.' }
foreach ($process in Get-CimInstance Win32_Process -Filter "name = 'cs2.exe'") {
    if (-not $process.ExecutablePath -or $process.ExecutablePath.StartsWith($installRoot + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Stop CS2 first; paired DLL/contract upgrades must be offline.' }
}
$manifest = Get-Content -LiteralPath (Join-Path $packageRoot 'manifest.json') -Raw | ConvertFrom-Json
if ($manifest.Stage -ne 'v0.2c paired lifecycle/round authority migration' -or $manifest.WorkingTreeChanges.Count -ne 0) { throw 'Require a clean, committed paired build manifest.' }
$apiPath = Join-Path $installRoot 'game/csgo/addons/counterstrikesharp/api/CounterStrikeSharp.API.dll'
if ((Get-FileHash -LiteralPath $apiPath -Algorithm SHA256).Hash -ne $manifest.ApiSHA256) { throw 'Server API differs from the verified build input.' }
$allowed = @(
    'game/csgo/addons/counterstrikesharp/plugins/ZEPVE.Core/ZEPVE.Core.dll',
    'game/csgo/addons/counterstrikesharp/plugins/ZEPVE.Core/ZEPVE.Core.deps.json',
    'game/csgo/addons/counterstrikesharp/plugins/ZEPVE.Core/ZEPVE.Core.pdb',
    'game/csgo/addons/counterstrikesharp/plugins/Kzen-ZRPVE/Kzen-ZRPVE.dll',
    'game/csgo/addons/counterstrikesharp/plugins/Kzen-ZRPVE/Kzen-ZRPVE.deps.json',
    'game/csgo/addons/counterstrikesharp/plugins/Kzen-ZRPVE/Kzen-ZRPVE.pdb',
    'game/csgo/addons/counterstrikesharp/shared/ZEPVE.Abstractions/ZEPVE.Abstractions.dll',
    'game/csgo/addons/counterstrikesharp/shared/ZEPVE.Abstractions/ZEPVE.Abstractions.pdb'
)
if ($manifest.Files.Count -ne $allowed.Count -or @($manifest.Files.Path | Select-Object -Unique).Count -ne $allowed.Count) { throw 'Unexpected/duplicate package inventory.' }
foreach ($file in $manifest.Files) {
    if ($file.Path -notin $allowed -or (Get-FileHash -LiteralPath (Join-Path $packageRoot $file.Path) -Algorithm SHA256).Hash -ne $file.SHA256) { throw 'Invalid package path/hash.' }
}
$privateCopies = @(
    'game/csgo/addons/counterstrikesharp/plugins/ZEPVE.Core/ZEPVE.Abstractions.dll',
    'game/csgo/addons/counterstrikesharp/plugins/ZEPVE.Core/ZEPVE.Abstractions.pdb',
    'game/csgo/addons/counterstrikesharp/plugins/Kzen-ZRPVE/ZEPVE.Abstractions.dll',
    'game/csgo/addons/counterstrikesharp/plugins/Kzen-ZRPVE/ZEPVE.Abstractions.pdb'
)
$backupRoot = Join-Path $installRoot ('ZEPVE_Backup/v0.2c-' + [DateTimeOffset]::UtcNow.ToString('yyyyMMdd-HHmmss-fffffff'))
New-Item -ItemType Directory -Path $backupRoot | Out-Null
$inventory = @()
foreach ($relative in @($allowed + $privateCopies)) {
    $target = [System.IO.Path]::GetFullPath((Join-Path $installRoot $relative))
    if (-not $target.StartsWith($installRoot + [System.IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Target escapes the named server root.' }
    $existed = Test-Path -LiteralPath $target
    $oldHash = $null
    if ($existed) {
        $oldHash = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash
        $backup = Join-Path $backupRoot $relative
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $backup) | Out-Null
        Copy-Item -LiteralPath $target -Destination $backup
        if ((Get-FileHash -LiteralPath $backup -Algorithm SHA256).Hash -ne $oldHash) { throw 'Backup hash mismatch.' }
    }
    $inventory += [ordered]@{ Path = $relative; Existed = $existed; OldSHA256 = $oldHash; NewSHA256 = ($manifest.Files | Where-Object Path -eq $relative | Select-Object -ExpandProperty SHA256) }
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'restore-core-lifecycle.ps1') -Destination (Join-Path $backupRoot 'restore.ps1')
$record = [ordered]@{ ServerRoot = $installRoot; SourceRevision = $manifest.SourceRevision; Status = 'prepared'; Files = $inventory }
$record | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $backupRoot 'deployment.json') -Encoding utf8
try {
    foreach ($relative in $allowed) {
        $target = Join-Path $installRoot $relative
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $target) | Out-Null
        Copy-Item -LiteralPath (Join-Path $packageRoot $relative) -Destination $target -Force
    }
    foreach ($relative in $privateCopies) {
        $target = Join-Path $installRoot $relative
        if (Test-Path -LiteralPath $target) { Remove-Item -LiteralPath $target }
    }
    foreach ($file in $manifest.Files) {
        if ((Get-FileHash -LiteralPath (Join-Path $installRoot $file.Path) -Algorithm SHA256).Hash -ne $file.SHA256) { throw 'Installed artifact hash mismatch.' }
    }
    $record.Status = 'installed; runtime NOT TESTED'
    $record | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $backupRoot 'deployment.json') -Encoding utf8
} catch {
    foreach ($item in $inventory) {
        $target = Join-Path $installRoot $item.Path
        if ($item.Existed) { Copy-Item -LiteralPath (Join-Path $backupRoot $item.Path) -Destination $target -Force }
        elseif (Test-Path -LiteralPath $target) { Remove-Item -LiteralPath $target }
    }
    throw
}
Write-Output "Paired migration installed. Rollback: $backupRoot/restore.ps1. Runtime NOT TESTED."
