import { Notice, TFolder, TFile } from "obsidian";
import type NotebookAutomationPlugin from "../main";
import { handleNotebookAutomationCommand } from "./commands";

function getFrontmatterStringValue(
  frontmatter: Record<string, unknown> | undefined,
  key: string,
): string | undefined {
  if (!frontmatter) {
    return undefined;
  }

  const value = (frontmatter as Record<string, unknown>)[key];
  if (typeof value === "string") {
    const trimmed = value.trim();
    return trimmed.length > 0 ? trimmed : undefined;
  }

  return undefined;
}

function isVideoNoteFile(file: TFile): boolean {
  return (file.name || "").toLowerCase().endsWith("-video.md");
}

function buildOneDriveResourceFullPath(
  config: any,
  oneDriveRelativePath: string,
): string {
  // eslint-disable-next-line @typescript-eslint/no-var-requires
  const path = require("path");

  const root = String(config?.paths?.onedrive_fullpath_root || "").trim();
  const baseRaw = String(
    config?.paths?.onedrive_resources_basepath || "",
  ).trim();
  const relativeRaw = String(oneDriveRelativePath || "").trim();

  const base = baseRaw.replace(/^[\\/]+|[\\/]+$/g, "");
  const relative = relativeRaw.replace(/^[\\/]+/g, "");

  return path.normalize(path.join(root, base, relative));
}

/**
 * Checks if a reading file needs HTML content extraction by examining its metadata
 * for the auto-generated-state: pending marker.
 *
 * @param plugin The NotebookAutomationPlugin instance.
 * @param file The markdown file to check.
 * @returns True if the file needs HTML content extraction.
 */
async function shouldShowHtmlExtractionOption(
  plugin: NotebookAutomationPlugin,
  file: TFile,
): Promise<boolean> {
  try {
    // Read the file content to check for auto-generated-state: pending
    const cache = plugin.app.metadataCache.getFileCache(file);
    if (
      cache?.frontmatter &&
      cache.frontmatter["auto-generated-state"] === "pending"
    ) {
      return true;
    }

    // Also check the raw content in case metadata cache isn't updated
    const fileContent = await plugin.app.vault.cachedRead(file);
    return fileContent.includes("auto-generated-state: pending");
  } catch (error) {
    console.log(
      "[Notebook Automation] Error checking if file needs HTML extraction:",
      error,
    );
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
  const normalizedFileName = (fileName || "").toLowerCase().trim();

  // Priority-based filtering: More specific patterns take precedence
  let options = {
    showHtmlExtraction: false,
    showVideoSummary: false,
    showPdfSummary: false,
    showHtmlEpubTxtSummary: false,
  };

  if (normalizedFileName.endsWith("-video")) {
    options.showVideoSummary = true;
  } else if (
    normalizedFileName.endsWith("-pdf") ||
    normalizedFileName.includes("pdf")
  ) {
    options.showPdfSummary = true;
  } else if (
    normalizedFileName.includes("html") ||
    normalizedFileName.includes("epub") ||
    normalizedFileName.includes("txt")
  ) {
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
            ? "Vault Sync with OneDrive (Recursive)"
            : "Vault Sync with OneDrive";
          item
            .setTitle(syncTitle)
            .setIcon("sync")
            .onClick(() =>
              handleNotebookAutomationCommand(plugin, file, "sync-dir"),
            );
        });
        // AI Video Summary - only if enabled
        if (plugin.settings.enableVideoSummary) {
          menu.addItem((item) => {
            item
              .setTitle("Import & AI Summarize All Videos")
              .setIcon("play")
              .onClick(() =>
                handleNotebookAutomationCommand(
                  plugin,
                  file,
                  "import-summarize-videos",
                ),
              );
          });
        }
        menu.addItem((item) => {
          const transcriptTitle = plugin.settings
            .recursiveTranscriptConsolidation
            ? "Create Consolidated Video Transcript(s) (Recursive)"
            : "Create Consolidated Video Transcript(s)";
          item
            .setTitle(transcriptTitle)
            .setIcon("file-text")
            .onClick(() =>
              handleNotebookAutomationCommand(
                plugin,
                file,
                "consolidate-transcripts",
              ),
            );
        });
        // AI PDF Summary - only if enabled
        if (plugin.settings.enablePdfSummary) {
          menu.addItem((item) => {
            item
              .setTitle("Import & AI Summarize All PDFs")
              .setIcon("document")
              .onClick(() =>
                handleNotebookAutomationCommand(
                  plugin,
                  file,
                  "import-summarize-pdfs",
                ),
              );
          });
        }
        // AI HTML/EPUB/TXT Summary - only if enabled
        if (plugin.settings.enableHtmlEpubTxtSummary) {
          menu.addItem((item) => {
            item
              .setTitle("Import & AI Summarize All HTML/EPUB/TXT")
              .setIcon("file-text")
              .onClick(() =>
                handleNotebookAutomationCommand(
                  plugin,
                  file,
                  "import-summarize-html-epub-txt",
                ),
              );
          });
        }
        // Index Creation - only if enabled
        if (plugin.settings.enableIndexCreation) {
          menu.addItem((item) => {
            const indexTitle = plugin.settings.recursiveIndexBuild
              ? "Build Indexes for this Folder and All Subfolders (Recursive)"
              : "Build Index for this Folder";
            const indexIcon = plugin.settings.recursiveIndexBuild
              ? "layers"
              : "list";
            const indexAction = plugin.settings.recursiveIndexBuild
              ? "build-index-recursive"
              : "build-indexes";
            item
              .setTitle(indexTitle)
              .setIcon(indexIcon)
              .onClick(() =>
                handleNotebookAutomationCommand(plugin, file, indexAction),
              );
          });
        }
        // Ensure Metadata - only if enabled
        if (plugin.settings.enableEnsureMetadata) {
          menu.addItem((item) => {
            item
              .setTitle("Ensure Metadata Consistency")
              .setIcon("settings")
              .onClick(() =>
                handleNotebookAutomationCommand(
                  plugin,
                  file,
                  "ensure-metadata",
                ),
              );
          });
        }
        // Open OneDrive Folder
        menu.addItem((item) => {
          item
            .setTitle("Open OneDrive Folder")
            .setIcon("external-link")
            .onClick(() =>
              handleNotebookAutomationCommand(
                plugin,
                file,
                "open-onedrive-folder",
              ),
            );
        });

        // Close out the Notebook Automation section
        menu.addSeparator();
      }
      // File context: only for .md files
      if (file instanceof TFile && file.extension === "md") {
        let notebookAutomationSectionStarted = false;
        const ensureNotebookAutomationSection = () => {
          if (!notebookAutomationSectionStarted) {
            menu.addSeparator();
            notebookAutomationSectionStarted = true;
          }
        };

        // Get file-specific context options based on filename
        const contextOptions = getFileContextOptions(file.basename);

        // HTML Content Extraction - for reading files that might need content extraction
        if (contextOptions.showHtmlExtraction) {
          ensureNotebookAutomationSection();
          menu.addItem((item) => {
            item
              .setTitle("Extract HTML Content")
              .setIcon("download")
              .onClick(async () => {
                const needsExtraction = await shouldShowHtmlExtractionOption(
                  plugin,
                  file,
                );
                if (needsExtraction) {
                  handleNotebookAutomationCommand(
                    plugin,
                    file,
                    "extract-html-content",
                  );
                } else {
                  console.log(
                    "[Notebook Automation] File does not need HTML extraction",
                  );
                }
              });
          });
        }

        const cache = plugin.app.metadataCache.getFileCache(file);

        // Play Video - only for *-video.md notes with the required frontmatter property
        if (isVideoNoteFile(file)) {
          const oneDriveRelativePath = getFrontmatterStringValue(
            cache?.frontmatter as Record<string, unknown> | undefined,
            "video-onedrive-relative-path",
          );

          if (oneDriveRelativePath) {
            ensureNotebookAutomationSection();
            menu.addItem((item) => {
              item
                .setTitle("Play Video")
                .setIcon("play")
                .onClick(async () => {
                  try {
                    let config = (window as any).notebookAutomationLoadedConfig;

                    if (!config) {
                      await plugin.loadConfigurationForCommands();
                      config = (window as any).notebookAutomationLoadedConfig;
                    }

                    if (!config?.paths?.onedrive_fullpath_root) {
                      new Notice("OneDrive root path not configured");
                      return;
                    }

                    const fullVideoPath = buildOneDriveResourceFullPath(
                      config,
                      oneDriveRelativePath,
                    );
                    if (!fullVideoPath || fullVideoPath.trim().length === 0) {
                      new Notice("Video path could not be constructed");
                      return;
                    }

                    // Use Electron shell directly because Obsidian's openWithDefaultApp
                    // may treat the string as vault-relative and incorrectly prefix the vault path.
                    // eslint-disable-next-line @typescript-eslint/no-var-requires
                    const path = require("path");
                    // eslint-disable-next-line @typescript-eslint/no-var-requires
                    const fs = require("fs");

                    const absoluteVideoPath = path.resolve(fullVideoPath);
                    if (!fs.existsSync(absoluteVideoPath)) {
                      console.warn(
                        "[Notebook Automation] Video file not found:",
                        absoluteVideoPath,
                      );
                      // Fall back to the OneDrive shared link stored on the note.
                      const sharedLink = getFrontmatterStringValue(
                        cache?.frontmatter as
                          | Record<string, unknown>
                          | undefined,
                        "onedrive-shared-link",
                      );
                      if (!sharedLink) {
                        new Notice(
                          `Video file not found and onedrive-shared-link is missing: ${absoluteVideoPath}`,
                        );
                        return;
                      }

                      // eslint-disable-next-line @typescript-eslint/no-var-requires
                      const electron = require("electron");
                      const remote =
                        electron?.remote ||
                        (() => {
                          try {
                            // eslint-disable-next-line @typescript-eslint/no-var-requires
                            return require("@electron/remote");
                          } catch {
                            return undefined;
                          }
                        })();
                      const shell = electron?.shell || remote?.shell;
                      if (!shell?.openExternal) {
                        new Notice(
                          "Unable to open shared link (shell not available)",
                        );
                        return;
                      }

                      await shell.openExternal(sharedLink);
                      return;
                    }

                    // eslint-disable-next-line @typescript-eslint/no-var-requires
                    const electron = require("electron");
                    const remote =
                      electron?.remote ||
                      (() => {
                        try {
                          // eslint-disable-next-line @typescript-eslint/no-var-requires
                          return require("@electron/remote");
                        } catch {
                          return undefined;
                        }
                      })();
                    const shell = electron?.shell || remote?.shell;

                    if (!shell?.openPath) {
                      new Notice("Unable to open video (shell not available)");
                      return;
                    }

                    await shell.openPath(absoluteVideoPath);
                  } catch (error) {
                    console.error(
                      "[Notebook Automation] Error opening video:",
                      error,
                    );
                    new Notice(
                      `Failed to open video: ${error instanceof Error ? error.message : "Unknown error"}`,
                    );
                  }
                });
            });
          }
        }

        // AI Video Summary - only if enabled and file name suggests video content
        if (
          plugin.settings.enableVideoSummary &&
          contextOptions.showVideoSummary
        ) {
          ensureNotebookAutomationSection();
          menu.addItem((item) => {
            item
              .setTitle("Reprocess AI Summary (Video)")
              .setIcon("play")
              .onClick(() =>
                handleNotebookAutomationCommand(
                  plugin,
                  file,
                  "reprocess-summary-video",
                ),
              );
          });
        }

        // AI PDF Summary - only if enabled and file name suggests PDF content
        if (plugin.settings.enablePdfSummary && contextOptions.showPdfSummary) {
          ensureNotebookAutomationSection();
          menu.addItem((item) => {
            item
              .setTitle("Reprocess AI Summary (PDF)")
              .setIcon("document")
              .onClick(() =>
                handleNotebookAutomationCommand(
                  plugin,
                  file,
                  "reprocess-summary-pdf",
                ),
              );
          });
        }

        // AI HTML/EPUB/TXT Summary - only if enabled and file name suggests HTML/EPUB/TXT content
        if (
          plugin.settings.enableHtmlEpubTxtSummary &&
          contextOptions.showHtmlEpubTxtSummary
        ) {
          ensureNotebookAutomationSection();
          menu.addItem((item) => {
            item
              .setTitle("Reprocess AI Summary (HTML/EPUB/TXT)")
              .setIcon("file-text")
              .onClick(() =>
                handleNotebookAutomationCommand(
                  plugin,
                  file,
                  "reprocess-summary-html-epub-txt",
                ),
              );
          });
        }

        // Close out the Notebook Automation section (only if we added any items)
        if (notebookAutomationSectionStarted) {
          menu.addSeparator();
        }
      }
    }),
  );
}
