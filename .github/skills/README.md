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

## Enablement (VS Code)

Agent Skills are currently in preview in VS Code.

- This repository enables `chat.useAgentSkills` in `.vscode/settings.json`.
- You can override it in your User settings if needed.

Then Copilot will automatically load relevant skill instructions when appropriate.
