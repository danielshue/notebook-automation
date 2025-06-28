# Product Requirements Document (PRD)

## Introduction/Overview

This feature enables Obsidian users to easily launch and import resource files (videos, PDFs, spreadsheets, HTML, etc.) from their mirrored OneDrive Resources folder for the selected Vault folder, without dropping to the CLI. The system will use existing configuration to map between the Vault and OneDrive roots, and will allow users to generate indexes for folders, either for a single folder or recursively. The goal is to streamline resource access and index generation within Obsidian, making it accessible to all users regardless of platform.

## Goals

- Allow users to launch/import resource files (videos, PDFs, spreadsheets, HTML) from OneDrive for the selected Vault folder in Obsidian.
- Provide an easy way to generate an index for the current folder or recursively for all subfolders.
- Eliminate the need for users to use the CLI directly.
- Ensure cross-platform compatibility (MacOS, Windows, iOS, Android).

## User Stories

- As an Obsidian user, I want to see and open resource files from my OneDrive Resources folder that correspond to my current Vault folder, so I can access relevant materials quickly.
- As an Obsidian user, I want to generate an index for the current folder or all subfolders, so I can easily navigate and organize my resources.
- As an Obsidian user, I want the system to use my existing configuration to map between Vault and OneDrive roots, so I don't have to set this up manually each time.

## Functional Requirements

1. The system must read the OneDrive and Vault root locations from `config.json`.
2. The system must display a list of resource files (videos, PDFs, spreadsheets, HTML) for the selected Vault folder, based on the mapped OneDrive folder.
3. The system must allow users to open/launch these files directly from Obsidian.
4. The system must provide an option to import/copy these files into the Vault.
5. The system must provide an option to generate an index for the current folder or recursively for all subfolders.
6. The system must use the existing core library for file operations and index generation, not require direct CLI invocation.
7. The system must work cross-platform (MacOS, Windows, iOS, Android).

## Non-Goals (Out of Scope)

- Editing or deleting resource files from OneDrive within Obsidian.
- Manual mapping of Vault and OneDrive roots by the user (should be automatic via config).
- Support for file types other than videos, PDFs, spreadsheets, and HTML.

## Design Considerations

- UI should be accessible from within Obsidian (e.g., command palette, context menu, or sidebar panel).
- Should provide clear feedback when indexes are created or files are imported.
- Should gracefully handle missing OneDrive folders or unsupported file types.

## Technical Considerations

- Must use the existing core library for file operations and index generation.
- Must read configuration from `config.json`.
- Should be implemented as an Obsidian plugin or integration.
- Must be cross-platform (MacOS, Windows, iOS, Android).

## Success Metrics

- Users can see and open resource files from OneDrive for any Vault folder.
- Users can generate indexes for folders and subfolders from within Obsidian.
- No need for users to use the CLI directly.
- Positive user feedback and increased usage of resource files and indexes.

## Open Questions


- Index generation should allow the user to choose whether to generate the index for just the current page/folder or recursively for all subfolders/pages below.
- All standard logging should be used. The user should receive a notification when the operation is complete (e.g., files imported or indexes created).
- Obsidian plugin API limitations on mobile platforms (iOS/Android) are currently unknown.
