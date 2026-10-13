# Rebuilds the documentation site from scratch and serves it locally.
#
# The site's template, SingulinkFX (https://github.com/Singulink/SingulinkFX), is not stored in this repository: its latest version tag (or, with
# -TemplateHead, the head of its default branch) is downloaded into templates/singulinkfx, which git ignores, so every build uses the latest template. The
# version downloaded is recorded in templates/singulinkfx.version, and the template is only downloaded again when a newer one has been published. Without
# network access, the copy already downloaded is used.
#
# The generated API metadata (api/*.yml) is cleared first because docfx never deletes metadata for types that were renamed or removed, and stale
# files cause invalid link warnings and ghost pages in the built site.

param(
    # Build the site without serving it.
    [switch] $NoServe,

    # Build with the head of SingulinkFX's default branch instead of its latest version tag.
    [switch] $TemplateHead
)

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

$templateRepo = 'https://github.com/Singulink/SingulinkFX'
$templateDir = 'templates/singulinkfx'
$templateVersionFile = 'templates/singulinkfx.version'

# The version wanted is a tag name such as "v3.3", or "head@<commit>" for the head of the default branch.
$currentVersion = if ((Test-Path $templateDir) -and (Test-Path $templateVersionFile)) { (Get-Content -Raw $templateVersionFile).Trim() } else { $null }
$wantedVersion = $null

try {
    if ($TemplateHead) {
        $head = git ls-remote "$templateRepo.git" HEAD
        if ($LASTEXITCODE -eq 0 -and $head) { $wantedVersion = 'head@' + ($head -split '\s+')[0] }
    }
    else {
        $tags = @(git ls-remote --tags --refs "$templateRepo.git" | ForEach-Object { ($_ -split '\s+')[1] -replace '^refs/tags/', '' } |
            Where-Object { $_ -match '^v\d+(\.\d+){1,3}$' })
        if ($LASTEXITCODE -eq 0 -and $tags.Count -gt 0) { $wantedVersion = $tags | Sort-Object { [version]$_.Substring(1) } | Select-Object -Last 1 }
    }
}
catch {
    # Git is missing or failed; handled below like no network access.
}

if (-not $wantedVersion) {
    if (-not $currentVersion) { throw "Could not look up the latest SingulinkFX version at $templateRepo, and no copy of it has been downloaded yet." }
    Write-Warning "Could not look up the latest SingulinkFX version at $templateRepo; building with the copy already downloaded ($currentVersion)."
}
elseif ($wantedVersion -eq $currentVersion) {
    Write-Host "SingulinkFX $currentVersion is up to date."
}
else {
    Write-Host "Downloading SingulinkFX $wantedVersion..."
    $archiveRef = if ($wantedVersion.StartsWith('head@')) { $wantedVersion.Substring(5) } else { "refs/tags/$wantedVersion" }
    $temp = Join-Path ([System.IO.Path]::GetTempPath()) ([guid]::NewGuid())
    New-Item -ItemType Directory -Path $temp | Out-Null

    try {
        # Windows PowerShell's progress display slows downloads down considerably.
        $ProgressPreference = 'SilentlyContinue'
        Invoke-WebRequest "$templateRepo/archive/$archiveRef.zip" -OutFile "$temp/template.zip" -UseBasicParsing
        Expand-Archive "$temp/template.zip" -DestinationPath $temp

        # The archive holds the repository in one folder (e.g. SingulinkFX-3.3), with the template in its singulinkfx folder.
        $source = Get-ChildItem $temp -Directory | ForEach-Object { Join-Path $_.FullName 'singulinkfx' } | Where-Object { Test-Path $_ } |
            Select-Object -First 1
        if (-not $source) { throw "The SingulinkFX archive has no singulinkfx folder." }

        if (Test-Path $templateDir) { Remove-Item $templateDir -Recurse -Force }
        New-Item -ItemType Directory -Force -Path (Split-Path $templateDir) | Out-Null
        Copy-Item $source $templateDir -Recurse
        Set-Content -Path $templateVersionFile -Value $wantedVersion
    }
    finally {
        Remove-Item $temp -Recurse -Force -ErrorAction SilentlyContinue
    }
}

Write-Host 'Clearing generated API metadata and previous site output...'
Get-ChildItem -Path 'api' -Filter '*.yml' -File | Remove-Item -Force
if (Test-Path 'api/.manifest') { Remove-Item 'api/.manifest' -Force }
if (Test-Path '_site') { Remove-Item '_site' -Recurse -Force }

Write-Host 'Generating API metadata...'
docfx metadata docfx.json
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host 'Building site...'
docfx build docfx.json
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if ($NoServe) { exit 0 }

Write-Host 'Serving site (Ctrl+C to stop)...'
docfx serve _site
