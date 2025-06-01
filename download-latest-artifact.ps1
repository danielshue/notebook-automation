# PowerShell script to download the latest GitHub Actions artifact for this repo/branch
# Requires GitHub CLI (https://cli.github.com/) and authentication (gh auth login)

param(
    [string]$Workflow = "ci-windows.yml",
    [string]$ArtifactName = "published-executables"
)

# Get the current branch name
$branch = git rev-parse --abbrev-ref HEAD

# Get the latest successful run ID for the workflow on the current branch
$run = gh run list --workflow $Workflow --branch $branch --limit 1 --json databaseId | ConvertFrom-Json

if ($run -and $run[0].databaseId) {
    Write-Host "Downloading artifact '$ArtifactName' from run $($run[0].databaseId)..."
    gh run download $run[0].databaseId --name $ArtifactName
} else {
    Write-Host "No workflow run found for $Workflow on branch $branch."
}
