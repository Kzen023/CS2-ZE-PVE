param(
    [Parameter(Mandatory = $true)][string]$CounterStrikeSharpApiPath,
    [string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$apiFile = (Resolve-Path -LiteralPath $CounterStrikeSharpApiPath).Path
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $repoRoot 'artifacts/core-lifecycle' }
$stageRoot = [System.IO.Path]::GetFullPath($OutputDirectory)
$serverCssRoot = Split-Path -Parent (Split-Path -Parent $apiFile)
if ($stageRoot.StartsWith($serverCssRoot + [System.IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or $stageRoot -eq $serverCssRoot) {
    throw 'Build staging must not target the installed CounterStrikeSharp directory.'
}
$relativeFiles = @(
    'game/csgo/addons/counterstrikesharp/plugins/ZEPVE.Core/ZEPVE.Core.dll',
    'game/csgo/addons/counterstrikesharp/plugins/ZEPVE.Core/ZEPVE.Core.deps.json',
    'game/csgo/addons/counterstrikesharp/plugins/ZEPVE.Core/ZEPVE.Core.pdb',
    'game/csgo/addons/counterstrikesharp/plugins/Kzen-ZRPVE/Kzen-ZRPVE.dll',
    'game/csgo/addons/counterstrikesharp/plugins/Kzen-ZRPVE/Kzen-ZRPVE.deps.json',
    'game/csgo/addons/counterstrikesharp/plugins/Kzen-ZRPVE/Kzen-ZRPVE.pdb',
    'game/csgo/addons/counterstrikesharp/shared/ZEPVE.Abstractions/ZEPVE.Abstractions.dll',
    'game/csgo/addons/counterstrikesharp/shared/ZEPVE.Abstractions/ZEPVE.Abstractions.pdb'
)
foreach ($relative in $relativeFiles) {
    if (Test-Path -LiteralPath (Join-Path $stageRoot $relative)) { throw 'Use a fresh staging directory; existing artifacts are not overwritten.' }
}
& dotnet build (Join-Path $repoRoot 'src/ZEPVE.Core/ZEPVE.Core.csproj') -c Release "-p:CounterStrikeSharpApiPath=$apiFile"
if ($LASTEXITCODE -ne 0) { throw 'Core Release build failed.' }
& dotnet build (Join-Path $repoRoot 'legacy/CounterStrikeSharp/plugins/Kzen-ZRPVE/Kzen-ZRPVE.csproj') -c Release "-p:CounterStrikeSharpApiPath=$apiFile"
if ($LASTEXITCODE -ne 0) { throw 'Adapted ZRPVE Release build failed.' }
& dotnet run --project (Join-Path $repoRoot 'tests/ZEPVE.Core.Tests/ZEPVE.Core.Tests.csproj') -c Release "-p:CounterStrikeSharpApiPath=$apiFile"
if ($LASTEXITCODE -ne 0) { throw 'Lifecycle/migration tests failed.' }
foreach ($relative in $relativeFiles) {
    $name = Split-Path -Leaf $relative
    $buildRoot = if ($name.StartsWith('Kzen-ZRPVE')) { 'legacy/CounterStrikeSharp/plugins/Kzen-ZRPVE/bin/Release/net10.0' }
        elseif ($name.StartsWith('ZEPVE.Abstractions')) { 'src/ZEPVE.Abstractions/bin/Release/net10.0' }
        else { 'src/ZEPVE.Core/bin/Release/net10.0' }
    $destination = Join-Path $stageRoot $relative
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $destination) | Out-Null
    Copy-Item -LiteralPath (Join-Path $repoRoot "$buildRoot/$name") -Destination $destination
}
$manifest = [ordered]@{
    Stage = 'v0.2c paired lifecycle/round authority migration'
    SourceRevision = (& git -C $repoRoot rev-parse HEAD)
    WorkingTreeChanges = @(& git -C $repoRoot status --porcelain)
    SDK = (& dotnet --version)
    ApiSHA256 = (Get-FileHash -LiteralPath $apiFile -Algorithm SHA256).Hash
    Runtime = 'NOT TESTED for v0.2c'
    GameplayWriter = 'Core lifecycle/round/quota/team/compatibility policy; adapted legacy recovery; external ZR respawn executor'
    Files = @($relativeFiles | ForEach-Object {
        [ordered]@{ Path = $_; SHA256 = (Get-FileHash -LiteralPath (Join-Path $stageRoot $_) -Algorithm SHA256).Hash }
    })
}
$manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $stageRoot 'manifest.json') -Encoding utf8
Write-Output "Paired suite staging ready: $stageRoot (no live deployment performed)"
