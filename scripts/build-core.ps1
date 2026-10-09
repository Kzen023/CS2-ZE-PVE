param(
    [Parameter(Mandatory = $true)][string]$CounterStrikeSharpApiPath,
    [string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$apiFile = (Resolve-Path -LiteralPath $CounterStrikeSharpApiPath).Path
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $repoRoot 'artifacts/core-observer' }
$stageRoot = [System.IO.Path]::GetFullPath($OutputDirectory)
$project = Join-Path $repoRoot 'src/ZEPVE.Core/ZEPVE.Core.csproj'
$tests = Join-Path $repoRoot 'tests/ZEPVE.Core.Tests/ZEPVE.Core.Tests.csproj'
& dotnet build $project -c Release "-p:CounterStrikeSharpApiPath=$apiFile"
if ($LASTEXITCODE -ne 0) { throw 'Core Release build failed.' }
& dotnet run --project $tests -c Release "-p:CounterStrikeSharpApiPath=$apiFile"
if ($LASTEXITCODE -ne 0) { throw 'Core lifecycle model tests failed.' }

$pluginDirectory = Join-Path $stageRoot 'game/csgo/addons/counterstrikesharp/plugins/ZEPVE.Core'
New-Item -ItemType Directory -Force -Path $pluginDirectory | Out-Null
$buildDirectory = Join-Path $repoRoot 'src/ZEPVE.Core/bin/Release/net10.0'
$files = @('ZEPVE.Core.dll', 'ZEPVE.Core.deps.json', 'ZEPVE.Core.pdb', 'ZEPVE.Abstractions.dll', 'ZEPVE.Abstractions.pdb')
foreach ($file in $files) {
    Copy-Item -LiteralPath (Join-Path $buildDirectory $file) -Destination (Join-Path $pluginDirectory $file) -Force
}
$manifest = [ordered]@{
    Stage = 'v0.2b observer only'
    SourceRevision = (& git -C $repoRoot rev-parse HEAD)
    WorkingTreeChanges = @(& git -C $repoRoot status --porcelain)
    SDK = (& dotnet --version)
    ApiSHA256 = (Get-FileHash -LiteralPath $apiFile -Algorithm SHA256).Hash
    GameplayWriter = 'legacy / existing external executors; no authority handoff'
    Runtime = 'NOT TESTED'
    Files = @($files | ForEach-Object {
        [ordered]@{ Name = $_; SHA256 = (Get-FileHash -LiteralPath (Join-Path $pluginDirectory $_) -Algorithm SHA256).Hash }
    })
}
$manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $stageRoot 'manifest.json') -Encoding utf8
Write-Output "Observer staging ready: $stageRoot (no live deployment performed)"
