<#!
.SYNOPSIS
    GitHub release management helpers used by scripts/manage-version.ps1.

.DESCRIPTION
    Provides small wrappers for maintaining GitHub Releases via the `gh` CLI.

    This lives under scripts/modules/GitHub (not a folder named "Release") to avoid
    clashing with the repo's .gitignore pattern that ignores "Release/".

.NOTES
    Requires GitHub CLI (gh) installed and authenticated.
#>

function Get-GitHubReleaseByTag {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Tag
    )

    $tagName = if ($Tag.StartsWith('v')) { $Tag } else { "v$Tag" }

    try {
        $json = gh api "repos/:owner/:repo/releases/tags/$tagName" 2>$null
        if (-not $json) { return $null }
        return ($json | ConvertFrom-Json)
    }
    catch {
        return $null
    }
}

function Set-ReleasePrerelease {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Tag,

        [Parameter(Mandatory = $true)]
        [bool]$Prerelease
    )

    $tagName = if ($Tag.StartsWith('v')) { $Tag } else { "v$Tag" }

    $release = Get-GitHubReleaseByTag -Tag $tagName
    if (-not $release -or -not $release.id) {
        Write-Host "✗ Release not found for tag: $tagName" -ForegroundColor Red
        return $false
    }

    $value = if ($Prerelease) { 'true' } else { 'false' }

    try {
        gh api "repos/:owner/:repo/releases/$($release.id)" -X PATCH -f "prerelease=$value" | Out-Null
        Write-Host "✓ Updated prerelease=$value for $tagName" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Host "✗ Failed to update prerelease flag for ${tagName}: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

function Get-BetaReleaseStats {
    [CmdletBinding()]
    param(
        [int]$Limit = 200
    )

    $releases = @()
    try {
        $releases = gh release list --limit $Limit --json tagName, isPrerelease, publishedAt | ConvertFrom-Json
    }
    catch {
        Write-Host "✗ Unable to list releases: $($_.Exception.Message)" -ForegroundColor Red
        return
    }

    $betas = @($releases | Where-Object { $_.tagName -match '^v\d+\.\d+\.\d+-beta\.\d+$' })
    $betaPrerelease = @($betas | Where-Object { $_.isPrerelease })
    $betaStable = @($betas | Where-Object { -not $_.isPrerelease })

    Write-Host "📈 Release Stats" -ForegroundColor Cyan
    Write-Host "  Total releases: $($releases.Count)" -ForegroundColor Gray
    Write-Host "  Beta releases:  $($betas.Count)" -ForegroundColor Gray
    Write-Host "    - prerelease: $($betaPrerelease.Count)" -ForegroundColor Gray
    Write-Host "    - stable:     $($betaStable.Count)" -ForegroundColor Gray
}

function Remove-OldBetaReleases {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [ValidateRange(1, 100)]
        [int]$KeepCount,

        [switch]$WhatIf
    )

    $releases = @()
    try {
        $releases = gh release list --limit 200 --json tagName, publishedAt, isPrerelease | ConvertFrom-Json
    }
    catch {
        throw "Unable to list releases via gh: $($_.Exception.Message)"
    }

    $betas = @(
        $releases |
        Where-Object { $_.tagName -match '^v\d+\.\d+\.\d+-beta\.\d+$' } |
        Sort-Object -Property publishedAt -Descending
    )

    if ($betas.Count -le $KeepCount) {
        return [pscustomobject]@{
            Deleted = 0
            Kept    = $betas.Count
        }
    }

    $toDelete = @($betas | Select-Object -Skip $KeepCount)

    $deleted = 0
    foreach ($r in $toDelete) {
        if ($WhatIf) {
            Write-Host "[WhatIf] Would delete: $($r.tagName)" -ForegroundColor Yellow
            continue
        }

        Write-Host "Deleting old beta release: $($r.tagName)" -ForegroundColor Yellow
        gh release delete $r.tagName -y | Out-Null
        if ($LASTEXITCODE -eq 0) { $deleted++ }
    }

    return [pscustomobject]@{
        Deleted = $deleted
        Kept    = [Math]::Min($KeepCount, $betas.Count)
    }
}

Export-ModuleMember -Function @(
    'Get-BetaReleaseStats',
    'Remove-OldBetaReleases',
    'Set-ReleasePrerelease'
)
