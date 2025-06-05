# PowerShell script to download the latest GitHub Actions artifact for this repo/branch
# Requires GitHub CLI (https://cli.github.com/) and authentication (gh auth login)

param(
    [string]$Workflow = "ci-windows.yml",
    [string]$ArtifactName = "published-executables"
)

# Ensure dist folder exists
if (-not (Test-Path dist)) {
    New-Item -ItemType Directory -Path dist
}

# Delete contents of dist folder first
Write-Host "Cleaning dist folder..."
Get-ChildItem -Path dist -Recurse | Remove-Item -Recurse -Force

# Get the current branch name
$branch = git rev-parse --abbrev-ref HEAD

# Get the latest successful run ID for the workflow on the current branch
$run = gh run list --workflow $Workflow --branch $branch --limit 1 --json databaseId | ConvertFrom-Json

if ($run -and $run[0].databaseId) {
    Write-Host "Downloading artifact '$ArtifactName' from run $($run[0].databaseId) to dist/..."
    gh run download $run[0].databaseId --name $ArtifactName --dir dist
    
    # Copy config.json to dist/win-x64 and dist/win-arm64
    $winX64Path = Join-Path -Path "dist" -ChildPath "win-x64"
    $winArm64Path = Join-Path -Path "dist" -ChildPath "win-arm64"
    
    if (Test-Path $winX64Path) {
        Write-Host "Copying config.json to $winX64Path..."
        Copy-Item -Path ".\config.json" -Destination "$winX64Path\config.json" -Force
    } else {
        Write-Host "Warning: $winX64Path folder not found"
    }
    
    if (Test-Path $winArm64Path) {
        Write-Host "Copying config.json to $winArm64Path..."
        Copy-Item -Path ".\config.json" -Destination "$winArm64Path\config.json" -Force
    } else {
        Write-Host "Warning: $winArm64Path folder not found"
    }
} else {
    Write-Host "No workflow run found for $Workflow on branch $branch."
}
