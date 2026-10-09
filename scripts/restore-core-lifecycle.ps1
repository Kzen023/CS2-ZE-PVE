param([switch]$VerifyOnly)
$ErrorActionPreference = 'Stop'
$record = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'deployment.json') -Raw | ConvertFrom-Json
$installRoot = (Resolve-Path -LiteralPath $record.ServerRoot).Path.TrimEnd('\', '/')
foreach ($process in Get-CimInstance Win32_Process -Filter "name = 'cs2.exe'") {
    if (-not $process.ExecutablePath -or $process.ExecutablePath.StartsWith($installRoot + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Stop CS2 before rollback or verification.' }
}
foreach ($file in $record.Files) {
    $target = [System.IO.Path]::GetFullPath((Join-Path $installRoot $file.Path))
    if (-not $target.StartsWith($installRoot + [System.IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or
        $file.Path -notmatch '^game/csgo/addons/counterstrikesharp/(plugins/(ZEPVE\.Core|Kzen-ZRPVE)|shared/ZEPVE\.Abstractions)/(ZEPVE\.Core|ZEPVE\.Abstractions|Kzen-ZRPVE)\.(dll|pdb|deps\.json)$') { throw 'Unsafe recorded rollback path.' }
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
Write-Output 'Restored the whole previous Core/ZRPVE/contracts set. Configs and unrelated plugins were not touched.'
