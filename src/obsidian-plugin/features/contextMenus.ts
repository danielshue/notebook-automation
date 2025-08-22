
import { TFolder, TFile, Menu } from 'obsidian';
import type NotebookAutomationPlugin from '../main';
import { handleNotebookAutomationCommand } from './commands';

/**
 * Checks if a reading file needs HTML content extraction by examining its metadata
 * for the auto-generated-state: pending marker.
 *
 * @param plugin The NotebookAutomationPlugin instance.
 * @param file The markdown file to check.
 * @returns True if the file needs HTML content extraction.
 */
async function shouldShowHtmlExtractionOption(plugin: NotebookAutomationPlugin, file: TFile): Promise<boolean> {
  try {
    // Read the file content to check for auto-generated-state: pending
    const cache = plugin.app.metadataCache.getFileCache(file);
    if (cache?.frontmatter && cache.frontmatter['auto-generated-state'] === 'pending') {
      return true;
    }
    
    // Also check the raw content in case metadata cache isn't updated
    const fileContent = await plugin.app.vault.cachedRead(file);
    return fileContent.includes('auto-generated-state: pending');
  } catch (error) {
    console.log('[Notebook Automation] Error checking if file needs HTML extraction:', error);
    // If we can't check, err on the side of showing the option
    return true;
  }
}

/**
 * Determines which context menu options should be shown based on the file name.
 * Uses priority-based filtering to show the most specific/relevant options.
 *
 * @param fileName The name of the file (without path).
 * @returns An object indicating which options should be shown.
 */
function getFileContextOptions(fileName: string) {
  const normalizedFileName = (fileName || '').toLowerCase().trim();
  
  // Priority-based filtering: More specific patterns take precedence
  let options = {
    showHtmlExtraction: false,
    showVideoSummary: false,
    showPdfSummary: false,
    showHtmlEpubTxtSummary: false
  };
  
  // Highest priority: File type specific endings
  if (normalizedFileName.endsWith("-video")) {
    options.showVideoSummary = true;
  } else if (normalizedFileName.endsWith("-pdf") || normalizedFileName.includes("pdf")) {
    options.showPdfSummary = true;
  } else if (normalizedFileName.includes("html") || normalizedFileName.includes("epub") || normalizedFileName.includes("txt")) {
    options.showHtmlEpubTxtSummary = true;
  } else if (normalizedFileName.includes("reading")) {
    // Lower priority: Reading files get HTML extraction + HTML/EPUB/TXT processing
    options.showHtmlExtraction = true;
    options.showHtmlEpubTxtSummary = true;
  }
  
  return options;
}

/**
 * Registers context menu commands for files and folders in Obsidian.
 *
 * Adds Notebook Automation actions to the right-click menu based on file type and plugin settings.
 *
 * @param plugin The NotebookAutomationPlugin instance.
 */
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
            ? "Notebook Automation: Vault Sync with OneDrive (Recursive)"
            : "Notebook Automation: Vault Sync with OneDrive";
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
        // AI HTML/EPUB/TXT Summary - only if enabled
        if (plugin.settings.enableHtmlEpubTxtSummary) {
          menu.addItem((item) => {
            item.setTitle("Notebook Automation: Import & AI Summarize All HTML/EPUB/TXT")
              .setIcon("file-text")
              .onClick(() => handleNotebookAutomationCommand(plugin, file, "import-summarize-html-epub-txt"));
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
      }
      // File context: only for .md files
      if (file instanceof TFile && file.extension === "md") {
        menu.addSeparator();
        
        // Get file-specific context options based on filename
        const contextOptions = getFileContextOptions(file.basename);
        
        // HTML Content Extraction - for reading files that might need content extraction
        if (contextOptions.showHtmlExtraction) {
          menu.addItem((item) => {
            item.setTitle("Notebook Automation: Extract HTML Content")
              .setIcon("download")
              .onClick(async () => {
                const needsExtraction = await shouldShowHtmlExtractionOption(plugin, file);
                if (needsExtraction) {
                  handleNotebookAutomationCommand(plugin, file, "extract-html-content");
                } else {
                  console.log('[Notebook Automation] File does not need HTML extraction');
                }
              });
          });
        }
        
        // AI Video Summary - only if enabled and file name suggests video content
        if (plugin.settings.enableVideoSummary && contextOptions.showVideoSummary) {
          menu.addItem((item) => {
            item.setTitle("Notebook Automation: Reprocess AI Summary (Video)")
              .setIcon("play")
              .onClick(() => handleNotebookAutomationCommand(plugin, file, "reprocess-summary-video"));
          });
        }
        
        // AI PDF Summary - only if enabled and file name suggests PDF content
        if (plugin.settings.enablePdfSummary && contextOptions.showPdfSummary) {
          menu.addItem((item) => {
            item.setTitle("Notebook Automation: Reprocess AI Summary (PDF)")
              .setIcon("document")
              .onClick(() => handleNotebookAutomationCommand(plugin, file, "reprocess-summary-pdf"));
          });
        }
        
        // AI HTML/EPUB/TXT Summary - only if enabled and file name suggests HTML/EPUB/TXT content
        if (plugin.settings.enableHtmlEpubTxtSummary && contextOptions.showHtmlEpubTxtSummary) {
          menu.addItem((item) => {
            item.setTitle("Notebook Automation: Reprocess AI Summary (HTML/EPUB/TXT)")
              .setIcon("file-text")
              .onClick(() => handleNotebookAutomationCommand(plugin, file, "reprocess-summary-html-epub-txt"));
          });
        }
      }
    })
  );
}
