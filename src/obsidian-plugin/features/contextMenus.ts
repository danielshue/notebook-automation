
import { TFolder, TFile, Menu } from 'obsidian';
import type NotebookAutomationPlugin from '../main';
import { handleNotebookAutomationCommand } from './commands';

export function registerContextMenus(plugin: NotebookAutomationPlugin) {
  // Register context menu commands for files and folders
  plugin.registerEvent(
    plugin.app.workspace.on("file-menu", (menu, file) => {
      // Folder context
      if (file instanceof TFolder) {
        menu.addSeparator();
        // Sync Directory - always available at the top
        menu.addItem((item) => {
          const syncTitle = plugin.settings.recursiveDirectorySync
            ? "Notebook Automation: Sync Directory with OneDrive (Recursive)"
            : "Notebook Automation: Sync Directory with OneDrive";
          item.setTitle(syncTitle)
            .setIcon("sync")
            .onClick(() => handleNotebookAutomationCommand(plugin, file, "sync-dir"));
        });
        // AI Video Summary - only if enabled
        if (plugin.settings.enableVideoSummary) {
          menu.addItem((item) => {
            item.setTitle("Notebook Automation: Import & AI Summarize All Videos")
              .setIcon("play")
              .onClick(() => handleNotebookAutomationCommand(plugin, file, "import-summarize-videos"));
          });
        }
        // AI PDF Summary - only if enabled
        if (plugin.settings.enablePdfSummary) {
          menu.addItem((item) => {
            item.setTitle("Notebook Automation: Import & AI Summarize All PDFs")
              .setIcon("document")
              .onClick(() => handleNotebookAutomationCommand(plugin, file, "import-summarize-pdfs"));
          });
        }
        // Index Creation - only if enabled
        if (plugin.settings.enableIndexCreation) {
          menu.addItem((item) => {
            const indexTitle = plugin.settings.recursiveIndexBuild
              ? "Notebook Automation: Build Indexes for this Folder and All Subfolders (Recursive)"
              : "Notebook Automation: Build Index for this Folder";
            const indexIcon = plugin.settings.recursiveIndexBuild ? "layers" : "list";
            const indexAction = plugin.settings.recursiveIndexBuild ? "build-index-recursive" : "build-indexes";
            item.setTitle(indexTitle)
              .setIcon(indexIcon)
              .onClick(() => handleNotebookAutomationCommand(plugin, file, indexAction));
          });
        }
        // Ensure Metadata - only if enabled
        if (plugin.settings.enableEnsureMetadata) {
          menu.addItem((item) => {
            item.setTitle("Notebook Automation: Ensure Metadata Consistency")
              .setIcon("settings")
              .onClick(() => handleNotebookAutomationCommand(plugin, file, "ensure-metadata"));
          });
        }
        // Open OneDrive Folder
        menu.addItem((item) => {
          item.setTitle("Notebook Automation: Open OneDrive Folder")
            .setIcon("external-link")
            .onClick(() => handleNotebookAutomationCommand(plugin, file, "open-onedrive-folder"));
        });
        // Open Local Folder
        menu.addItem((item) => {
          item.setTitle("Notebook Automation: Open Local Folder")
            .setIcon("folder")
            .onClick(() => handleNotebookAutomationCommand(plugin, file, "open-local-folder"));
        });
      }
      // File context: only for .md files
      if (file instanceof TFile && file.extension === "md") {
        menu.addSeparator();
        // AI Video Summary - only if enabled
        if (plugin.settings.enableVideoSummary) {
          menu.addItem((item) => {
            item.setTitle("Notebook Automation: Reprocess AI Summary (Video)")
              .setIcon("play")
              .onClick(() => handleNotebookAutomationCommand(plugin, file, "reprocess-summary-video"));
          });
        }
        // AI PDF Summary - only if enabled
        if (plugin.settings.enablePdfSummary) {
          menu.addItem((item) => {
            item.setTitle("Notebook Automation: Reprocess AI Summary (PDF)")
              .setIcon("document")
              .onClick(() => handleNotebookAutomationCommand(plugin, file, "reprocess-summary-pdf"));
          });
        }
      }
    })
  );
}
