import { TFolder } from 'obsidian';
import type NotebookAutomationPlugin from '../main';
import { handleNotebookAutomationCommand } from './commands';

export function registerCommands(plugin: NotebookAutomationPlugin) {
  // Register Command Palette commands for selected files/folders
  plugin.addCommand({
    id: 'sync-directory',
    name: 'Sync Directory with OneDrive',
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
