$ErrorActionPreference = "Stop"

function Assert-Condition {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$packageRoot = Join-Path $repoRoot "Packages/com.vareiko.foundation"
$packageJsonPath = Join-Path $packageRoot "package.json"
$changelogPath = Join-Path $packageRoot "CHANGELOG.md"
$projectVersionPath = Join-Path $repoRoot "ProjectSettings/ProjectVersion.txt"

Assert-Condition (Test-Path $packageJsonPath) "package.json not found at '$packageJsonPath'."
Assert-Condition (Test-Path $changelogPath) "CHANGELOG.md not found at '$changelogPath'."
Assert-Condition (Test-Path $projectVersionPath) "ProjectVersion.txt not found at '$projectVersionPath'."

$packageJson = Get-Content -Path $packageJsonPath -Raw | ConvertFrom-Json
$packageVersion = $packageJson.version
Assert-Condition (-not [string]::IsNullOrWhiteSpace($packageVersion)) "Package version is empty."

$changelogRaw = Get-Content -Path $changelogPath -Raw
$versionMatch = [regex]::Match($changelogRaw, "(?m)^##\s+([0-9]+\.[0-9]+\.[0-9]+)\s*$")
Assert-Condition $versionMatch.Success "Unable to find top changelog version heading in CHANGELOG.md."
$changelogVersion = $versionMatch.Groups[1].Value
Assert-Condition ($changelogVersion -eq $packageVersion) "Version mismatch: package.json=$packageVersion, changelog=$changelogVersion."

$requiredDependencies = @("com.cysharp.unitask", "net.bobbo.extenject")
$declaredDependencies = @($packageJson.dependencies.PSObject.Properties | ForEach-Object { $_.Name })
foreach ($dependency in $requiredDependencies) {
    Assert-Condition ($declaredDependencies -contains $dependency) "Missing required dependency '$dependency' in package.json."
}

$csFiles = Get-ChildItem -Path $packageRoot -Recurse -File -Filter *.cs
$missingMeta = @()
foreach ($csFile in $csFiles) {
    $metaPath = "$($csFile.FullName).meta"
    if (-not (Test-Path $metaPath)) {
        $missingMeta += $csFile.FullName
    }
}

Assert-Condition ($missingMeta.Count -eq 0) ("Missing .meta files for scripts:`n" + ($missingMeta -join "`n"))

$scanExtensions = @(".cs", ".md", ".json", ".asmdef", ".yml", ".yaml", ".txt")
$scanRoots = @(
    (Join-Path $repoRoot "Packages"),
    (Join-Path $repoRoot "ProjectSettings"),
    (Join-Path $repoRoot "Tools"),
    (Join-Path $repoRoot ".github")
) | Where-Object { Test-Path $_ }

$scanFiles = @()
foreach ($scanRoot in $scanRoots) {
    $scanFiles += Get-ChildItem -Path $scanRoot -Recurse -File | Where-Object { $scanExtensions -contains $_.Extension }
}

$mergeMarkerHits = @()
foreach ($scanFile in $scanFiles) {
    $hits = Select-String -Path $scanFile.FullName -Pattern "^(<<<<<<<|=======|>>>>>>>)" -SimpleMatch:$false
    if ($hits) {
        $mergeMarkerHits += $scanFile.FullName
    }
}

Assert-Condition ($mergeMarkerHits.Count -eq 0) ("Merge conflict markers found in:`n" + (($mergeMarkerHits | Sort-Object -Unique) -join "`n"))

$projectVersionRaw = Get-Content -Path $projectVersionPath -Raw
$unityVersionMatch = [regex]::Match($projectVersionRaw, "(?m)^m_EditorVersion:\s*([0-9a-zA-Z\.\-]+)\s*$")
Assert-Condition $unityVersionMatch.Success "Unable to parse Unity version from ProjectVersion.txt."

Write-Host "Package validation passed."
Write-Host "Version: $packageVersion"
Write-Host "Unity: $($unityVersionMatch.Groups[1].Value)"
