---
name: powershell-scripts-workflows
description: Safely run and modify PowerShell scripts under scripts/ (module import conventions, common tasks, and verification steps).
examples:
  - "Update a script in scripts/ to reuse functions from scripts/modules"
  - "Add a new safe flag to a script and document it in scripts/README.md"
  - "Validate script/module changes by running scripts/test-modules.ps1"
---

# PowerShell Scripts Workflows (scripts/)

## When to use

Use this skill when the user asks to:

- Update build/release tooling under `scripts/*.ps1`
- Run scripts locally (CI build, version/release ops, formatting, artifact download)
- Ensure scripts continue to use shared modules under `scripts/modules/`

## Repo conventions

- Scripts are PowerShell Core friendly and generally run via `pwsh`.
- Prefer importing and using module functions from `scripts/modules/`.
- Avoid inlining complex logic in scripts if a module is the right home.

## Step-by-step procedure

1. Identify the script(s) under `scripts/` involved in the request.
2. Find the module imports near the top of the script and prefer calling module functions.
3. If logic is reusable across scripts, add it to the appropriate module under `scripts/modules/` and call it.
4. Keep parameter behavior backward compatible where practical.
5. Update `scripts/README.md` if flags/behavior change.
6. Validate:
   - `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/test-modules.ps1`
   - Run the specific script with the safest relevant flags.

## Common scripts

- `scripts/build-ci-local.ps1` — local CI build pipeline
- `scripts/manage-version.ps1` — version + release automation (rollback-aware)
- `scripts/format-csharp-advanced.ps1` — advanced formatting
- `scripts/check-csharp-test-documentation.ps1` — test doc coverage
- `scripts/download-latest-artifact.ps1` — download CI artifacts

## Verification

After modifying scripts or modules, prefer:

- `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/test-modules.ps1`
- Run the most relevant script path end-to-end with safe flags (e.g., `-WhatIf`, `-StatusOnly`, `-SkipTests`).

## Examples (input → expected output)

### Example: Adjust a script to use a shared module

**Input:** “Stop duplicating prereq checks across scripts.”

**Expected output:**

- Scripts call `Test-*` functions from `Core/Prerequisites.psm1`
- No drift between scripts (one source of truth)
- `scripts/test-modules.ps1` still passes

## References

- Scripts overview: [scripts/README.md](../../../scripts/README.md)
- Module system: [powershell-module-system](../powershell-module-system/SKILL.md)
