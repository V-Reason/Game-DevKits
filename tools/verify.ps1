#!/usr/bin/env pwsh
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

$requiredFiles = @(
    'README.md',
    'LICENSE',
    'ATTRIBUTION.md',
    'GameDevKit.sln',
    'catalog/README.md',
    'case-studies/deepseaoil/README.md',
    'case-studies/deepseaoil/source-map.md',
    'case-studies/deepseaoil/pattern-ledger.md',
    'adapters/unity/com.gamedevkit.unity/package.json',
    'adapters/unity/com.gamedevkit.unity/Runtime/GameDevKit.Unity.asmdef'
)

$kitNames = @(
    'gameplay/state-machine',
    'gameplay/input-buffer',
    'gameplay/reaction-rules',
    'gameplay/grid-propagation',
    'gameplay/flow-field',
    'engineering/composition-root',
    'engineering/event-bus',
    'engineering/object-pool',
    'engineering/timer-wheel',
    'engineering/snapshot-save',
    'engineering/asset-lifecycle',
    'engineering/effect-orchestrator',
    'engineering/audio-arbitration',
    'engineering/ui-stack',
    'engineering/config-validation',
    'engineering/diagnostics-snapshot',
    'gameplay/actor-motion',
    'gameplay/steering-path-following',
    'gameplay/wave-sequencer',
    'gameplay/ballistics-aim'
)

$moduleNames = @(
    'M01-foundation.md',
    'M02-data.md',
    'M03-runtime.md',
    'M04-combat.md',
    'M05-grid.md',
    'M06-player.md',
    'M07-elements.md',
    'M08-enemies.md',
    'M09-levels.md',
    'M10-effects.md',
    'M11-lighting.md',
    'M12-ui.md',
    'M13-story.md',
    'M14-audio.md',
    'M15-save.md'
)

$submoduleNames = @(
    'asset-lifecycle.md',
    'effect-orchestration.md',
    'audio-arbitration.md',
    'ui-stack.md',
    'config-validation.md',
    'diagnostics-snapshot.md',
    'actor-motion.md',
    'steering-following.md',
    'wave-sequencer.md',
    'ballistics-aim.md'
)

foreach ($relativePath in $requiredFiles) {
    $path = Join-Path $root $relativePath

    if (-not (Test-Path -LiteralPath $path)) {
        throw "Missing required file: $relativePath"
    }
}

foreach ($kit in $kitNames) {
    $directory = Join-Path $root "kits/$kit"

    if (-not (Test-Path -LiteralPath (Join-Path $directory 'README.md'))) {
        throw "Kit README missing: $kit"
    }

    $projectCount = @(Get-ChildItem -LiteralPath $directory -Recurse -Filter *.csproj -File).Count

    if ($projectCount -ne 2) {
        throw "Kit $kit must contain exactly one source project and one test project; found $projectCount"
    }
}

foreach ($module in $moduleNames) {
    $path = Join-Path $root "case-studies/deepseaoil/modules/$module"

    if (-not (Test-Path -LiteralPath $path)) {
        throw "Missing deepseaoil module card: $module"
    }
}

foreach ($submodule in $submoduleNames) {
    $path = Join-Path $root "case-studies/deepseaoil/submodules/$submodule"

    if (-not (Test-Path -LiteralPath $path)) {
        throw "Missing deepseaoil submodule card: $submodule"
    }
}

$packageJsonPath = Join-Path $root 'adapters/unity/com.gamedevkit.unity/package.json'
$packageJson = Get-Content -Raw -LiteralPath $packageJsonPath | ConvertFrom-Json

if ($packageJson.name -ne 'com.gamedevkit.unity') {
    throw "Unexpected Unity package name: $($packageJson.name)"
}

$asmdefPath = Join-Path $root 'adapters/unity/com.gamedevkit.unity/Runtime/GameDevKit.Unity.asmdef'
$asmdef = Get-Content -Raw -LiteralPath $asmdefPath | ConvertFrom-Json

if ($asmdef.name -ne 'GameDevKit.Unity') {
    throw "Unexpected Unity assembly name: $($asmdef.name)"
}

$forbiddenUnityReference = Get-ChildItem -Path (Join-Path $root 'kits') -Recurse -File -Filter *.cs |
    Select-String -Pattern 'UnityEngine|UnityEditor'

if ($forbiddenUnityReference) {
    $details = $forbiddenUnityReference | ForEach-Object { "  $($_.Path):$($_.LineNumber)" }
    throw "Core Kits must remain engine-neutral:`n$($details -join "`n")"
}

$absolutePathReference = Get-ChildItem -Path $root -Recurse -File -Include *.md |
    Where-Object { $_.FullName -notmatch '[\\/]\.git[\\/]' } |
    Select-String -Pattern '(?i)\b[A-Z]:[\\/](Users|杂七杂八|GAMES|Open-Source)'

if ($absolutePathReference) {
    $details = $absolutePathReference | ForEach-Object { "  $($_.Path):$($_.LineNumber)" }
    throw "Public documentation must not expose local absolute paths:`n$($details -join "`n")"
}

$markdownFiles = Get-ChildItem -Path $root -Recurse -File -Include *.md |
    Where-Object { $_.FullName -notmatch '[\\/]\.git[\\/]' }

foreach ($markdownFile in $markdownFiles) {
    $content = Get-Content -Raw -LiteralPath $markdownFile.FullName

    foreach ($match in [regex]::Matches($content, '\[[^\]]+\]\((?<target>[^)]+)\)')) {
        $target = $match.Groups['target'].Value.Trim()

        if ($target.StartsWith('<') -and $target.EndsWith('>')) {
            $target = $target.Substring(1, $target.Length - 2)
        }

        if ($target -match '^(?i)(https?:|mailto:|#)') {
            continue
        }

        $relativeTarget = ($target -split '#', 2)[0]

        if ([string]::IsNullOrWhiteSpace($relativeTarget)) {
            continue
        }

        $decodedTarget = [Uri]::UnescapeDataString($relativeTarget)
        $resolvedTarget = [IO.Path]::GetFullPath((Join-Path $markdownFile.DirectoryName $decodedTarget))

        if (-not (Test-Path -LiteralPath $resolvedTarget)) {
            throw "Broken Markdown link in $($markdownFile.FullName): $target"
        }
    }
}

Write-Host 'Build: dotnet build GameDevKit.sln' -ForegroundColor Cyan
& dotnet build (Join-Path $root 'GameDevKit.sln') --nologo -warnaserror

if ($LASTEXITCODE -ne 0) {
    throw "dotnet build failed with exit code $LASTEXITCODE"
}

Write-Host 'Test: dotnet test GameDevKit.sln' -ForegroundColor Cyan
& dotnet test (Join-Path $root 'GameDevKit.sln') --no-build --nologo

if ($LASTEXITCODE -ne 0) {
    throw "dotnet test failed with exit code $LASTEXITCODE"
}

Push-Location $root

try {
    Write-Host 'Whitespace: git diff --check' -ForegroundColor Cyan
    & git diff --check

    if ($LASTEXITCODE -ne 0) {
        throw "git diff --check failed with exit code $LASTEXITCODE"
    }
}
finally {
    Pop-Location
}

Write-Host 'Verification complete.' -ForegroundColor Green
