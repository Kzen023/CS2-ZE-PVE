param([switch]$VerifyOnly, [switch]$AllowUnmoddedClient)
$ErrorActionPreference = 'Stop'
$record = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'deployment.json') -Raw | ConvertFrom-Json
$installRoot = (Resolve-Path -LiteralPath $record.ServerRoot).Path.TrimEnd('\', '/')
foreach ($process in Get-CimInstance Win32_Process -Filter "name = 'cs2.exe'") {
    if (-not $process.ExecutablePath) { throw 'Cannot identify CS2 process; stop it before rollback.' }
    if (-not $process.ExecutablePath.StartsWith($installRoot + '\', [StringComparison]::OrdinalIgnoreCase)) { continue }
    if (-not $AllowUnmoddedClient -or -not $process.CommandLine -or $process.CommandLine -match '(?i)(^|\s)-dedicated(\s|$)') { throw 'Stop CS2 before rollback or verification.' }
    $client = Get-Process -Id $process.ProcessId -ErrorAction Stop
    if ($client.MainWindowHandle -eq [IntPtr]::Zero -or $client.Modules.Count -eq 0) { throw 'Cannot establish an active unmodded client.' }
    foreach ($module in $client.Modules) {
        if ($module.FileName -match '(?i)[\\/]addons[^\\/]*[\\/]|metamod|counterstrikesharp|cs2fixes|botcontroller') { throw 'Client has addon modules loaded; stop it before rollback.' }
    }
}
foreach ($file in $record.Files) {
    $target = [System.IO.Path]::GetFullPath((Join-Path $installRoot $file.Path))
    if (-not $target.StartsWith($installRoot + [System.IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or
        $file.Path -notmatch '^game/csgo/addons/counterstrikesharp/(plugins/(ZEPVE\.Core|ZEPVE\.BotAI|Kzen-ZRPVE)|shared/ZEPVE\.Abstractions)/(ZEPVE\.Core|ZEPVE\.BotAI|ZEPVE\.Abstractions|Kzen-ZRPVE)\.(dll|pdb|deps\.json)$') { throw 'Unsafe recorded rollback path.' }
    if ($file.Existed -and (Get-FileHash -LiteralPath (Join-Path $PSScriptRoot $file.Path) -Algorithm SHA256).Hash -ne $file.OldSHA256) { throw 'Backup integrity failure.' }
    if ($record.Status.StartsWith('installed')) {
        if ($file.NewSHA256) {
            if (-not (Test-Path -LiteralPath $target) -or (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -ne $file.NewSHA256) { throw 'Installed file changed; preserve it before rollback.' }
        } elseif (Test-Path -LiteralPath $target) { throw 'Removed private contract was replaced; preserve it before rollback.' }
    }
}
if ($VerifyOnly) { Write-Output 'Rollback inventory/path/hash verification PASS.'; return }
foreach ($file in $record.Files) {
    $target = Join-Path $installRoot $file.Path
    if ($file.Existed) { Copy-Item -LiteralPath (Join-Path $PSScriptRoot $file.Path) -Destination $target -Force }
    elseif (Test-Path -LiteralPath $target) { Remove-Item -LiteralPath $target }
}
Write-Output 'Restored the whole previous matched suite (including BotAI inventory). Configs and unrelated plugins were not touched.'
