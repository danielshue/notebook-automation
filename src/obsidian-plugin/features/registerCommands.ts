
import { TFolder } from 'obsidian';
import type NotebookAutomationPlugin from '../main';
import { handleNotebookAutomationCommand, runConfigurationValidation } from './commands';

/**
 * Registers all Notebook Automation commands with the Obsidian Command Palette.
 *
 * This function adds commands for directory sync, AI summarization, index building,
 * metadata enforcement, and folder opening. Each command is registered with a
 * checkCallback that determines if the command should be enabled based on the current
 * selection and plugin settings.
 *
 * @param plugin The NotebookAutomationPlugin instance to register commands for.
 */
export function registerCommands(plugin: NotebookAutomationPlugin) {
  // Register Command Palette commands for selected files/folders
  plugin.addCommand({
    id: 'validate-configuration',
    name: 'Validate Notebook Automation Configuration',
    callback: async () => {
      await runConfigurationValidation(plugin);
    }
  });

  plugin.addCommand({
    id: 'sync-directory',
    name: 'Vault Sync with OneDrive',
    /**
     * Checks if a folder is selected and triggers the sync-dir command.
     * @param checking If true, only checks if the command should be enabled.
     * @returns True if a folder is selected, false otherwise.
     */
    checkCallback: (checking: boolean) => {
      const file = plugin.app.workspace.getActiveFile();
      const selectedFiles = (plugin.app as any).workspace.activeLeaf?.view?.file;
      const activeFile = file || selectedFiles;
      // Check if we have a selected folder or file's parent folder
      if (activeFile) {
        const folder = activeFile instanceof TFolder ? activeFile : activeFile.parent;
        if (folder) {
          if (!checking) {
            handleNotebookAutomationCommand(plugin, folder, "sync-dir");
          }
          return true;
        }
      }
      return false;
    }
  });

  plugin.addCommand({
    id: 'import-summarize-videos',
    name: 'Import & AI Summarize All Videos',
    /**
     * Checks if video summary is enabled and a folder is selected, then triggers the import-summarize-videos command.
     * @param checking If true, only checks if the command should be enabled.
     * @returns True if enabled and a folder is selected, false otherwise.
     */
    checkCallback: (checking: boolean) => {
      if (!plugin.settings.enableVideoSummary) return false;
      const file = plugin.app.workspace.getActiveFile();
      const selectedFiles = (plugin.app as any).workspace.activeLeaf?.view?.file;
      const activeFile = file || selectedFiles;
      if (activeFile) {
        const folder = activeFile instanceof TFolder ? activeFile : activeFile.parent;
        if (folder) {
          if (!checking) {
            handleNotebookAutomationCommand(plugin, folder, "import-summarize-videos");
          }
          return true;
        }
      }
      return false;
    }
  });

  plugin.addCommand({
    id: 'import-summarize-pdfs',
    name: 'Import & AI Summarize All PDFs',
    /**
     * Checks if PDF summary is enabled and a folder is selected, then triggers the import-summarize-pdfs command.
     * @param checking If true, only checks if the command should be enabled.
     * @returns True if enabled and a folder is selected, false otherwise.
     */
    checkCallback: (checking: boolean) => {
      if (!plugin.settings.enablePdfSummary) return false;
      const file = plugin.app.workspace.getActiveFile();
      const selectedFiles = (plugin.app as any).workspace.activeLeaf?.view?.file;
      const activeFile = file || selectedFiles;
      if (activeFile) {
        const folder = activeFile instanceof TFolder ? activeFile : activeFile.parent;
        if (folder) {
          if (!checking) {
            handleNotebookAutomationCommand(plugin, folder, "import-summarize-pdfs");
          }
          return true;
        }
      }
      return false;
    }
  });

  plugin.addCommand({
    id: 'build-indexes',
    name: 'Build Index for Folder',
    /**
     * Checks if index creation is enabled and a folder is selected, then triggers the build-indexes command.
     * @param checking If true, only checks if the command should be enabled.
     * @returns True if enabled and a folder is selected, false otherwise.
     */
    checkCallback: (checking: boolean) => {
      if (!plugin.settings.enableIndexCreation) return false;
      const file = plugin.app.workspace.getActiveFile();
      const selectedFiles = (plugin.app as any).workspace.activeLeaf?.view?.file;
      const activeFile = file || selectedFiles;
      if (activeFile) {
        const folder = activeFile instanceof TFolder ? activeFile : activeFile.parent;
        if (folder) {
          if (!checking) {
            handleNotebookAutomationCommand(plugin, folder, "build-indexes");
          }
          return true;
        }
      }
      return false;
    }
  });

  plugin.addCommand({
    id: 'ensure-metadata',
    name: 'Ensure Metadata for Files',
    /**
     * Checks if metadata enforcement is enabled and a folder is selected, then triggers the ensure-metadata command.
     * @param checking If true, only checks if the command should be enabled.
     * @returns True if enabled and a folder is selected, false otherwise.
     */
    checkCallback: (checking: boolean) => {
      if (!plugin.settings.enableEnsureMetadata) return false;
      const file = plugin.app.workspace.getActiveFile();
      const selectedFiles = (plugin.app as any).workspace.activeLeaf?.view?.file;
      const activeFile = file || selectedFiles;
      if (activeFile) {
        const folder = activeFile instanceof TFolder ? activeFile : activeFile.parent;
        if (folder) {
          if (!checking) {
            handleNotebookAutomationCommand(plugin, folder, "ensure-metadata");
          }
          return true;
        }
      }
      return false;
    }
  });

  plugin.addCommand({
    id: 'open-onedrive-folder',
    name: 'Open OneDrive Folder',
    /**
     * Checks if a folder is selected and triggers the open-onedrive-folder command.
     * @param checking If true, only checks if the command should be enabled.
     * @returns True if a folder is selected, false otherwise.
     */
    checkCallback: (checking: boolean) => {
      const file = plugin.app.workspace.getActiveFile();
      const selectedFiles = (plugin.app as any).workspace.activeLeaf?.view?.file;
      const activeFile = file || selectedFiles;
      if (activeFile) {
        const folder = activeFile instanceof TFolder ? activeFile : activeFile.parent;
        if (folder) {
          if (!checking) {
            handleNotebookAutomationCommand(plugin, folder, "open-onedrive-folder");
          }
          return true;
        }
      }
      return false;
    }
  });

  plugin.addCommand({
    id: 'open-local-folder',
    name: 'Open Local Folder',
    /**
     * Checks if a folder is selected and triggers the open-local-folder command.
     * @param checking If true, only checks if the command should be enabled.
     * @returns True if a folder is selected, false otherwise.
     */
    checkCallback: (checking: boolean) => {
      const file = plugin.app.workspace.getActiveFile();
      const selectedFiles = (plugin.app as any).workspace.activeLeaf?.view?.file;
      const activeFile = file || selectedFiles;
      if (activeFile) {
        const folder = activeFile instanceof TFolder ? activeFile : activeFile.parent;
        if (folder) {
          if (!checking) {
            handleNotebookAutomationCommand(plugin, folder, "open-local-folder");
          }
          return true;
        }
      }
      return false;
    }
  });
}
