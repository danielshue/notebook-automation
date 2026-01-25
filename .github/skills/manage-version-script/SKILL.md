---
name: manage-version-script
description: Run and modify scripts/manage-version.ps1 safely (version sync, CI artifacts, GitHub releases, rollback), keeping it aligned with scripts/modules.
examples:
  - "Run manage-version in safe inspection mode (-StatusOnly)"
  - "Improve release automation while preserving rollback tracking"
  - "Debug CI artifact download/release creation paths without changing defaults"
---

# manage-version.ps1 Workflow

## When to use

Use this skill when working on:

- `scripts/manage-version.ps1`
- Version sync, release creation, reissuing releases, pruning betas
- Rollback behavior and safety guardrails

## Module dependencies (repo-specific)

`manage-version.ps1` imports:

- `Core/Logging.psm1`
- `Core/Platform.psm1`
- `GitHub/CLI.psm1`
- `GitHub/Artifacts.psm1`
- `GitHub/ReleaseManagement.psm1`
- `Version/Management.psm1`
- `Safety/Rollback.psm1`
- `Quality/ReleaseNotes.psm1`
- `Quality/Dependencies.psm1`
- `Quality/Checksums.psm1`

## Safety rules

- Prefer `-StatusOnly` / dry-run style flags when validating logic.
- Keep rollback tracking intact when changing any operation that writes files, commits, tags, or releases.
- Avoid changing default behaviors without updating `Get-Help` and `scripts/README.md`.

## Step-by-step procedure

1. Start by reproducing/validating with a safe mode:
   - `-StatusOnly` for inspection
   - `-SyncOnly` for local-only version alignment
2. Identify which operation path is being touched (sync, build, create release, artifacts, prune/fix betas, release notes).
3. Ensure the change uses the existing module functions (don’t re-implement GitHub/rollback/version logic inline).
4. If the change affects any of these, update help text and docs:
   - Script parameters/flags
   - Output messaging relied on by automation
   - Any file paths produced/consumed
5. Validate in increasing risk order:
   - `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/manage-version.ps1 -StatusOnly`
   - The narrowest operation that proves the change (avoid creating releases unless explicitly requested)

## Examples (input → expected output)

### Example: Improve diagnostics without changing behavior

**Input:** “Add more diagnostics around artifact download failures.”

**Expected output:**

- More contextual logging (paths, tags, GH command output) without changing default control flow
- No secrets in logs
- No regression in `-Quiet` / `-Diagnostic` behavior

## How to run (examples)

- Status: `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/manage-version.ps1 -StatusOnly`
- Create beta release: `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/manage-version.ps1 -Version "0.1.0-beta.1" -Type beta -CreateRelease -PreRelease`

## References

- Script docs: [scripts/README.md](../../../scripts/README.md)
- Module catalog: [scripts/modules/README.md](../../../scripts/modules/README.md)
