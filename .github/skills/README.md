# Copilot Agent Skills (Project)

This folder contains project-scoped Agent Skills that GitHub Copilot can load on-demand.

See VS Code Agent Skills documentation: https://code.visualstudio.com/docs/copilot/customization/agent-skills

## Skills

- `csharp-code-generation` - Generates C# code aligned with repo conventions.
- `csharp-test-generation` - Generates MSTest + Moq tests aligned with the existing suite.
- `powershell-module-system` - PowerShell modules in `scripts/modules/` (import patterns, safe updates, doc sync).
- `powershell-scripts-workflows` - PowerShell scripts in `scripts/` (run/modify workflows, verification steps).
- `build-ci-local-script` - Workflow guidance for `scripts/build-ci-local.ps1`.
- `manage-version-script` - Workflow guidance for `scripts/manage-version.ps1`.

## Examples (how to use these skills)

In VS Code Copilot Chat, ask for what you want to do and include the relevant file/script path (or the skill name) so Copilot can match and load the skill.

### csharp-code-generation

- "Use the csharp-code-generation skill: add a new service under src/c-sharp that follows repo conventions and reuses existing utilities."
- "Create a new CLI handler in src/c-sharp/NotebookAutomation.Cli and wire it into DI."
- "Refactor this class to reduce duplication and keep changes minimal."

### csharp-test-generation

- "Use the csharp-test-generation skill: add MSTest + Moq tests for this service and cover null/empty/exception paths."
- "Create a regression test for this bug fix using Arrange-Act-Assert."
- "Increase coverage for this handler without adding flaky I/O."

### powershell-module-system

- "Use the powershell-module-system skill: add a reusable helper to scripts/modules/Core and update scripts to call it."
- "Move/rename a module under scripts/modules and update all Import-Module call sites + scripts/modules/README.md."
- "Update scripts/modules/README.md so it matches the current module tree."

### powershell-scripts-workflows

- "Use the powershell-scripts-workflows skill: update scripts/build-ci-local.ps1 to reuse module functions and keep flags backward compatible."
- "Add a safe flag to scripts/manage-version.ps1 and document it in scripts/README.md."
- "After changes, validate by running scripts/test-modules.ps1 and the narrowest safe workflow."

### build-ci-local-script

- "Use the build-ci-local-script skill: run a quick local CI build (skip tests/format) and tell me what command to run."
- "Add a new optional step to scripts/build-ci-local.ps1 and implement reusable logic in scripts/modules/Build."

### manage-version-script

- "Use the manage-version-script skill: run scripts/manage-version.ps1 in -StatusOnly mode and explain the output."
- "Use the manage-version-script skill: release another beta version (e.g., run `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/manage-version.ps1 -Version \"0.1.0-beta.2\" -Type beta -CreateRelease -PreRelease`) and confirm what will change."
- "Improve logging around artifact download failures without changing default behavior or breaking rollback tracking."

## Enablement (VS Code)

Agent Skills are currently in preview in VS Code.

- This repository enables `chat.useAgentSkills` in `.vscode/settings.json`.
- You can override it in your User settings if needed.

Then Copilot will automatically load relevant skill instructions when appropriate.
