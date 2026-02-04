---
mode: "agent"
tools: ["terminal", "codebase", "editFiles", "problems"]
description: "Create a new release using manage-version.ps1 (beta, stable, or patch)"
---

# Release Management

Your goal is to help create a new release for the Notebook Automation project using the `scripts/manage-version.ps1` script.

## Before Creating a Release

1. **Check current status** to understand the version state:

   ```powershell
   pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/manage-version.ps1 -StatusOnly
   ```

2. **Ensure all changes are committed** - the release script requires a clean working directory

3. **Verify the build passes** before creating a release:
   ```powershell
   pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/build-ci-local.ps1 -SkipTests
   ```

## Release Types

### Beta Release (Pre-release)

For testing new features before stable release:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/manage-version.ps1 `
  -Version "X.Y.Z-beta.N" `
  -Type beta `
  -CreateRelease `
  -PreRelease
```

### Stable Release

For production-ready releases:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/manage-version.ps1 `
  -Version "X.Y.Z" `
  -Type stable `
  -CreateRelease
```

### Patch Release

For bug fixes to existing releases:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/manage-version.ps1 `
  -Version "X.Y.Z" `
  -Type patch `
  -CreateRelease
```

## Common Options

| Option                  | Description                                                  |
| ----------------------- | ------------------------------------------------------------ |
| `-StatusOnly`           | Show current version status without making changes           |
| `-SyncOnly`             | Sync version numbers across files without creating a release |
| `-GenerateReleaseNotes` | Auto-generate release notes from commits                     |
| `-Detailed`             | Show detailed diagnostic output                              |
| `-Quiet`                | Suppress non-essential output                                |
| `-UseArtifacts`         | Download executables from latest CI artifacts                |
| `-ForceLocalBuild`      | Build executables locally instead of using CI artifacts      |

## Workflow Steps

1. **Ask for release type** (beta, stable, or patch) and version number
2. **Check status** using `-StatusOnly` to confirm current state
3. **Sync versions** if needed using `-SyncOnly`
4. **Create the release** with appropriate flags
5. **Verify the release** was created successfully on GitHub

## Release Notes

Release notes are generated automatically based on:

- Commits since the last release
- Changelog entries in `CHANGELOG.md`
- The prompt template at `scripts/release-notes-prompt.md`

## After Release

- Verify the release appears on GitHub releases page
- For beta releases, test via BRAT plugin in Obsidian
- For stable releases, verify community plugin update works

## Important Notes

- Always use `-StatusOnly` first to understand current state
- Beta releases should use `-PreRelease` flag
- The script handles version syncing across manifest.json, .csproj files, etc.
- CI artifacts are preferred over local builds for releases
