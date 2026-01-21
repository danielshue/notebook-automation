---
name: build-ci-local-script
description: Run and modify scripts/build-ci-local.ps1 safely, keeping it aligned with scripts/modules and VS Code tasks.
examples:
  - "Run a quick local CI build (skip tests/format)"
  - "Adjust plugin-only build workflow and keep module usage consistent"
  - "Add a new build step using scripts/modules/Build"
---

# build-ci-local.ps1 Workflow

## When to use

Use this skill when working on:

- `scripts/build-ci-local.ps1`
- Build pipeline steps, flags, or output
- How the script uses modules in `scripts/modules/Build` and `scripts/modules/Core`

## How it’s structured (repo-specific)

- Imports modules from `scripts/modules`:
  - `Core/Logging.psm1`
  - `Core/Prerequisites.psm1`
  - `Build/DotNetBuild.psm1`
  - `Build/PluginBuild.psm1`

## Safe changes

- Prefer adding reusable behavior in the appropriate module (`Build/*` or `Core/*`) and calling it from the script.
- Keep flags backward compatible when possible.

## Step-by-step procedure

1. Confirm which scenario is being modified (full CI, plugin-only, formatting, test docs).
2. Find the corresponding pipeline step in `scripts/build-ci-local.ps1`.
3. Prefer implementing reusable behavior in:

- `scripts/modules/Build/*` for build operations
- `scripts/modules/Core/*` for shared utilities/prereqs

4. Ensure the script continues to import modules from `$ModulesDir` (no hard-coded absolute paths).
5. Validate by running an appropriate command:

- Quick sanity: `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/build-ci-local.ps1 -SkipTests -SkipFormat`
- Full pipeline: `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/build-ci-local.ps1`

## Examples (input → expected output)

### Example: Add a new optional build step

**Input:** “Add a flag to run an extra verification step.”

**Expected output:**

- A new `param()` switch with help text
- Step wired into the pipeline with clear output
- Reusable logic lives in a module if it’s non-trivial
- Script still works with existing flags and defaults

## How to run (examples)

- Full build: `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/build-ci-local.ps1`
- Quick: `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/build-ci-local.ps1 -SkipTests -SkipFormat`
- Plugin-only: `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/build-ci-local.ps1 -PluginOnly -DeployPlugin`

## References

- Script docs: [scripts/README.md](../../../scripts/README.md)
- Module catalog: [scripts/modules/README.md](../../../scripts/modules/README.md)
