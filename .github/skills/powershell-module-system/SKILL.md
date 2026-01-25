---
name: powershell-module-system
description: Work with the PowerShell module system under scripts/modules (import patterns, updating modules, updating scripts/modules/README.md safely).
examples:
  - "Add a reusable helper to scripts/modules/Core and call it from multiple scripts"
  - "Move/rename a module and update all Import-Module callsites"
  - "Update scripts/modules/README.md to reflect current module structure"
---

# PowerShell Module System (scripts/modules)

## When to use

Use this skill when making changes to:

- PowerShell modules in `scripts/modules/**/*.psm1`
- Module documentation in `scripts/modules/README.md`
- Scripts that import these modules (under `scripts/*.ps1`)

## What this skill helps accomplish

- Keep script logic **DRY** by pushing reusable behavior into `scripts/modules/`.
- Keep documentation accurate by ensuring `scripts/modules/README.md` reflects the real folder structure.
- Provide a safe checklist for validating module changes.

## Key facts (repo-specific)

- Modules are plain `.psm1` files organized by category under `scripts/modules/`.
- Scripts import modules using:
  - `$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path`
  - `$ModulesDir = Join-Path $ScriptDir "modules"`
  - `Import-Module (Join-Path $ModulesDir "Category\Module.psm1") -Force`

## Safe workflow

1. Identify the module(s) involved and their category:
   - `Core/*` (logging, platform, prerequisites)
   - `Build/*` (dotnet build, plugin build)
   - `GitHub/*` (CLI, artifacts, release maintenance)
   - `Version/*` (version sync, git tags)
   - `Safety/*` (rollback)
   - `Quality/*` (release notes, checksums, dependencies)
2. Prefer **reusing existing module functions** instead of duplicating logic in scripts.
3. Keep modules cohesive (don’t turn a module into a grab-bag).
4. Update `scripts/modules/README.md` when:
   - Adding/removing a module
   - Moving a module between categories
   - Adding a major new exported function
5. Validate module import and core behavior by running:
   - `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/test-modules.ps1`

## Examples (input → expected output)

### Example 1: Add a new helper function to a module

**Input:** “Add a function to normalize paths used across scripts.”

**Expected output:**

- A change in the correct module (usually `Core/Platform.psm1`)
- Any scripts updated to call the new function (instead of duplicating logic)
- `scripts/modules/README.md` updated if the new function materially changes the module surface

### Example 2: Move a module between categories

**Input:** “Move Release management functions into the GitHub category.”

**Expected output:**

- File moved under the correct folder
- All `Import-Module` callsites updated
- `scripts/modules/README.md` updated (catalog + tree + import examples)
- `scripts/test-modules.ps1` updated to import/test the moved module

## Documentation conventions

- `scripts/modules/README.md` should match the real folder structure under `scripts/modules/`.
- Import examples in docs should use the same `Join-Path` patterns used in scripts.

## References

- Module catalog: [scripts/modules/README.md](../../../scripts/modules/README.md)
- Scripts overview: [scripts/README.md](../../../scripts/README.md)

## Related skills

- [powershell-scripts-workflows](../powershell-scripts-workflows/SKILL.md)
