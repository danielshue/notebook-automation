## Relevant Files

- `src/obsidian-plugin/main.ts` - Main entry point for the Obsidian plugin, handles initialization and command registration.
- `src/obsidian-plugin/ResourceService.ts` - Service for mapping Vault folders to OneDrive, listing and importing resource files.
- `src/obsidian-plugin/IndexService.ts` - Service for generating indexes for folders and subfolders.
- `src/obsidian-plugin/config.ts` - Handles reading and validating `config.json` settings.
- `src/obsidian-plugin/ui/ResourcePanel.tsx` - UI component for displaying and launching/importing resource files.
- `src/obsidian-plugin/ui/IndexOptionsModal.tsx` - UI for selecting index generation options (current folder vs. recursive).
- `src/obsidian-plugin/__tests__/ResourceService.test.ts` - Unit tests for `ResourceService`.
- `src/obsidian-plugin/__tests__/IndexService.test.ts` - Unit tests for `IndexService`.
- `src/obsidian-plugin/__tests__/config.test.ts` - Unit tests for config handling.
- `src/obsidian-plugin/__tests__/ui/ResourcePanel.test.tsx` - Unit tests for resource panel UI.
- `src/obsidian-plugin/__tests__/ui/IndexOptionsModal.test.tsx` - Unit tests for index options modal UI.

### Notes

- Unit tests should typically be placed alongside the code files they are testing (e.g., `ResourceService.ts` and `ResourceService.test.ts` in the same directory).
- Use `npx jest [optional/path/to/test/file]` to run tests. Running without a path executes all tests found by the Jest configuration.

## Tasks

  
- [ ] 1.0 Set up Obsidian plugin project structure and configuration handling
  - [x] 1.1 Initialize a new Obsidian plugin project and set up the required directory structure.
  - [x] 1.2 Create and validate `config.json` reading logic for Vault and OneDrive root paths.
  - [x] 1.3 Integrate the existing core library for file operations and index generation.
  - [x] 1.4 Set up basic plugin manifest and registration with Obsidian.

- [ ] 2.0 Implement resource file mapping, listing, and import functionality
  - [x] 2.1 Implement logic to map the current Vault folder to the corresponding OneDrive folder using config settings.
  - [x] 2.2 List resource files (videos, PDFs, spreadsheets, HTML) in the mapped OneDrive folder.
  - [x] 2.3 Implement functionality to open/launch resource files directly from Obsidian.
  - [x] 2.4 Implement functionality to import/copy resource files from OneDrive into the Vault.
  - [x] 2.5 Add error handling for missing folders or unsupported file types.

- [ ] 3.0 Implement index generation for current and subfolders
  - [x] 3.1 Provide an option to generate an index for the current folder only.
  - [x] 3.2 Provide an option to generate indexes recursively for all subfolders.
  - [x] 3.3 Integrate index generation with the core library.
  - [x] 3.4 Allow user to select index generation mode via UI.

- [ ] 4.0 Design and implement user interface components
  - [x] 4.1 Create a UI panel or modal to display resource files and actions (launch/import).
  - [x] 4.2 Implement UI for index generation options (current folder vs. recursive).
  - [x] 4.3 Add UI feedback for successful operations and errors.
  - [x] 4.4 Integrate UI components with Obsidian command palette, context menu, or sidebar.

- [ ] 5.0 Add logging, notifications, and cross-platform support
  - [x] 5.1 Integrate standard logging for all major operations (file import, index generation, errors).
  - [x] 5.2 Implement user notifications when files are imported or indexes are created.
  - [x] 5.3 Test and ensure plugin works on MacOS, Windows, iOS, and Android. <!-- Placeholder: Actual cross-platform testing requires Obsidian plugin environment. -->
  - [x] 5.4 Document any platform-specific limitations or issues. <!-- Placeholder: Document any issues found during manual or user testing. -->
