# PowerShell script to download the latest GitHub Actions artifact for this repo/branch
# Requires GitHub CLI (https://cli.github.com/) and authentication (gh auth login)

param(
    [string]$Workflow = "ci-windows.yml",
    [string]$ArtifactName = "published-executables"
)

# Get the latest successful run ID for the workflow on the current branch
gh run list --workflow $Workflow --branch $(git rev-parse --abbrev-ref HEAD) --limit 1 --json databaseId | \
    ConvertFrom-Json | \
    ForEach-Object {
        if ($_.databaseId) {
            Write-Host "Downloading artifact '$ArtifactName' from run $($_.databaseId)..."
            gh run download $_.databaseId --name $ArtifactName
        } else {
            Write-Host "No workflow run found for $Workflow on this branch."
        }
    }
