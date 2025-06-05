# Notebook Automation (C# Migration)

This repository contains the C# migration of the Notebook Automation tools, designed to help automate the management and organization of educational content across Obsidian and other platforms.

## Development Setup

1. Clone the repository
2. Open the solution in Visual Studio or your preferred IDE
3. Restore NuGet packages
4. Build the solution

## CI/CD

This project uses GitHub Actions for CI/CD with the following workflows:

- **ci-windows.yml**: Builds and tests the C# code on Windows
- **build-exe.yml**: Builds CLI executables
- **cleanup-artifacts.yml**: Automatically cleans up old artifacts and workflow runs

### Artifact Storage Management

To manage GitHub storage usage and prevent quota issues, the project implements:

1. **Artifact Retention Policies**:
   - Test results and coverage reports: 7-day retention
   - Executable artifacts: 14-day retention

2. **Workflow Run Cleanup**:
   - Automatic weekly cleanup of workflow runs older than 14 days
   - Always keeps at least 10 most recent workflow runs
   - Skips tagged releases

3. **Manual Cleanup**:
   - The cleanup workflow can be manually triggered via the Actions tab when needed

These policies balance the need to keep recent build artifacts for debugging while preventing storage quota issues in GitHub.

## Testing

Run tests using the standard .NET testing commands:

```shell
dotnet test src/c-sharp/NotebookAutomation.sln
```

Or use the dedicated CI script for a full verification:

```shell
pwsh -File ../../scripts/build-ci-local.ps1
```
