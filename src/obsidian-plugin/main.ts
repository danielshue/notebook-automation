import { App, Notice, Plugin, PluginSettingTab, Setting, TAbstractFile, TFile, TFolder } from 'obsidian';

/**
 * Given a full vault path, strip the notebook_vault_fullpath_root and vault_resources_basepath prefix and return the relative path for OneDrive mapping.
 * @param fullPath The full path to the file/folder in the vault
 * @param vaultRoot The notebook_vault_fullpath_root from config
 * @param vaultBase Optional vault_resources_basepath from config
 * @returns The relative path for OneDrive mapping
 */
function getRelativeVaultResourcePath(fullPath: string, vaultRoot: string, vaultBase?: string): string {
  // Normalize slashes for cross-platform compatibility
  let normFull = fullPath.replace(/\\/g, '/');
  let normRoot = vaultRoot.replace(/\\/g, '/').replace(/\/$/, '');
  let normBase = (vaultBase || '').replace(/\\/g, '/').replace(/^\//, '').replace(/\/$/, '');

  console.log(`[getRelativeVaultResourcePath] Input fullPath: "${fullPath}"`);
  console.log(`[getRelativeVaultResourcePath] Input vaultRoot: "${vaultRoot}"`);
  console.log(`[getRelativeVaultResourcePath] Input vaultBase: "${vaultBase}"`);
  console.log(`[getRelativeVaultResourcePath] Normalized fullPath: "${normFull}"`);
  console.log(`[getRelativeVaultResourcePath] Normalized vaultRoot: "${normRoot}"`);
  console.log(`[getRelativeVaultResourcePath] Normalized vaultBase: "${normBase}"`);

  // Remove vaultRoot if present (for absolute paths)
  if (normRoot && normFull.startsWith(normRoot)) {
    console.log(`[getRelativeVaultResourcePath] Removing vaultRoot from path`);
    normFull = normFull.substring(normRoot.length);
    if (normFull.startsWith('/')) normFull = normFull.substring(1);
  }

  // Remove vaultBase if present (this is the key part for Obsidian vault relative paths)
  if (normBase && normFull.startsWith(normBase)) {
    console.log(`[getRelativeVaultResourcePath] Removing vaultBase from path`);
    normFull = normFull.substring(normBase.length);
    if (normFull.startsWith('/')) normFull = normFull.substring(1);
  }

  console.log(`[getRelativeVaultResourcePath] Final result: "${normFull}"`);
  return normFull;
}
/**
 * Utility to resolve the correct executable name for the current platform and architecture.
 */
function getNaExecutableName(): string {
  const platform = process?.platform || (window?.process && window.process.platform);
  const arch = process?.arch || (window?.process && window.process.arch);

  // Map Node.js platform/arch to our naming convention
  let platformName: string;
  let archName: string;

  // Map platform
  switch (platform) {
    case "win32":
      platformName = "win";
      break;
    case "darwin":
      platformName = "macos";
      break;
    case "linux":
      platformName = "linux";
      break;
    default:
      // Fallback to win32 if unknown
      platformName = "win";
      break;
  }

  // Map architecture - convert to string and handle all cases
  const archString = String(arch);
  if (archString === "x64" || archString === "x86_64" || (archString.includes("64") && !archString.includes("arm"))) {
    archName = "x64";
  } else if (archString === "arm64" || archString === "aarch64" || archString.includes("arm")) {
    archName = "arm64";
  } else {
    // Default fallback for unknown architectures
    archName = "x64";
  }

  // Build executable name using our naming convention
  const extension = platformName === "win" ? ".exe" : "";
  const execName = `na-${platformName}-${archName}${extension}`;

  // Log for debugging
  // eslint-disable-next-line no-console
  console.log(`[Notebook Automation] Platform: ${platform}, Arch: ${arch}`);
  // eslint-disable-next-line no-console
  console.log(`[Notebook Automation] Resolved executable name: ${execName}`);

  return execName;
}

/**
 * Get the full path to the bundled na executable in the plugin directory.
 * @param plugin This plugin instance (for path resolution)
 */
function getNaExecutablePath(plugin: Plugin): string {
  const execName = getNaExecutableName();
  try {
    // @ts-ignore
    const path = window.require ? window.require('path') : null;
    // @ts-ignore
    const fs = window.require ? window.require('fs') : null;

    // Log plugin.manifest.dir and plugin.manifest.id for debugging
    // eslint-disable-next-line no-console
    console.log('[Notebook Automation] plugin.manifest.dir:', plugin.manifest?.dir);
    // eslint-disable-next-line no-console
    console.log('[Notebook Automation] plugin.manifest.id:', plugin.manifest?.id);
    // eslint-disable-next-line no-console
    console.log('[Notebook Automation] Looking for executable:', execName);

    // Get vault root first - this is essential for building absolute paths
    let vaultRoot = '';
    const adapter = plugin.app?.vault?.adapter;
    // eslint-disable-next-line no-console
    console.log('[Notebook Automation] adapter exists:', !!adapter);
    // eslint-disable-next-line no-console
    console.log('[Notebook Automation] adapter constructor:', adapter?.constructor?.name);

    // @ts-ignore - Check for getBasePath method directly instead of constructor name (which can be minified)
    if (adapter && typeof adapter.getBasePath === 'function') {
      try {
        // @ts-ignore
        vaultRoot = adapter.getBasePath();
        // eslint-disable-next-line no-console
        console.log('[Notebook Automation] vaultRoot from getBasePath:', vaultRoot);
      } catch (err) {
        // eslint-disable-next-line no-console
        console.log('[Notebook Automation] Error calling getBasePath:', err);
      }
    } else {
      // eslint-disable-next-line no-console
      console.log('[Notebook Automation] Could not get vaultRoot - getBasePath method not available');
    }

    // Helper function to try finding an executable in a directory
    const tryFindExecutable = (dir: string): string | null => {
      if (!fs || !path) return null;

      // First try the exact match
      const exactPath = path.join(dir, execName);
      if (fs.existsSync(exactPath)) {
        // eslint-disable-next-line no-console
        console.log(`[Notebook Automation] Found exact match: ${exactPath}`);
        return exactPath;
      }

      // If exact match not found, try to find any na executable as fallback
      try {
        const files = fs.readdirSync(dir);
        const naExecutables = files.filter((file: string) =>
          file.startsWith('na-') || file === 'na' || file === 'na.exe'
        );

        if (naExecutables.length > 0) {
          // Prefer platform-specific matches
          const platform = process?.platform || 'win32';
          const platformName = platform === 'win32' ? 'win' : platform === 'darwin' ? 'macos' : 'linux';

          const platformMatch = naExecutables.find((file: string) =>
            file.includes(platformName)
          );

          if (platformMatch) {
            const fallbackPath = path.join(dir, platformMatch);
            // eslint-disable-next-line no-console
            console.log(`[Notebook Automation] Found platform fallback: ${fallbackPath}`);
            return fallbackPath;
          }

          // If no platform match, use the first available
          const fallbackPath = path.join(dir, naExecutables[0]);
          // eslint-disable-next-line no-console
          console.log(`[Notebook Automation] Found generic fallback: ${fallbackPath}`);
          return fallbackPath;
        }
      } catch (err) {
        // eslint-disable-next-line no-console
        console.log(`[Notebook Automation] Error scanning directory ${dir}:`, err);
      }

      return null;
    };

    const isValidPluginDir = (dir: string | undefined, pluginId: string | undefined) => {
      // eslint-disable-next-line no-console
      console.log('[Notebook Automation] isValidPluginDir check - dir:', dir, 'pluginId:', pluginId);
      if (!dir || dir === '/' || dir === '' || dir.length <= 1) {
        console.log('[Notebook Automation] plugin.manifest.dir is empty or root, will fallback.');
        return false;
      }
      if (!pluginId) {
        console.log('[Notebook Automation] plugin.manifest.id is missing, will fallback.');
        return false;
      }
      // Check if dir is already absolute or relative
      const isAbsolute = path ? path.isAbsolute(dir) : (dir.startsWith('/') || dir.match(/^[A-Za-z]:/));
      // eslint-disable-next-line no-console
      console.log('[Notebook Automation] isAbsolute check:', isAbsolute, 'vaultRoot available:', !!vaultRoot);
      if (!isAbsolute && !vaultRoot) {
        console.log('[Notebook Automation] plugin.manifest.dir is relative but no vaultRoot available, will fallback.');
        return false;
      }
      return true;
    };

    if (plugin.manifest && isValidPluginDir(plugin.manifest.dir, plugin.manifest.id) && path) {
      let resolvedDir = plugin.manifest.dir || '';

      // Special case: if manifest.dir starts with / but is actually relative (like /.obsidian/...)
      // This happens when manifest.dir is incorrectly set to a path rooted at /
      if (resolvedDir && (resolvedDir.startsWith('/.obsidian') || resolvedDir.startsWith('/.') || (resolvedDir.startsWith('/') && !fs?.existsSync?.(resolvedDir)))) {
        // This is likely a relative path incorrectly prefixed with /
        // Remove the leading / and treat as relative
        resolvedDir = resolvedDir.substring(1);
        // eslint-disable-next-line no-console
        console.log('[Notebook Automation] Detected incorrectly rooted path, treating as relative:', resolvedDir);
      }

      // If manifest.dir is relative, make it absolute by prepending vaultRoot
      const isAbsolute = path.isAbsolute(resolvedDir) && fs?.existsSync?.(resolvedDir);
      if (!isAbsolute && vaultRoot) {
        resolvedDir = path.join(vaultRoot, resolvedDir);
        // eslint-disable-next-line no-console
        console.log('[Notebook Automation] Made plugin.manifest.dir absolute:', resolvedDir);
      }

      const foundExecutable = tryFindExecutable(resolvedDir);
      if (foundExecutable) {
        return foundExecutable;
      } else {
        // eslint-disable-next-line no-console
        console.log('[Notebook Automation] No executable found in plugin.manifest.dir path, will fallback.');
      }
    }

    // Fallback: Use FileSystemAdapter.getBasePath() and vault.configDir
    if (plugin.app && plugin.app.vault && path) {
      // If we don't have vaultRoot, try to get it again
      if (!vaultRoot) {
        const adapter = plugin.app.vault.adapter;
        // @ts-ignore - Check for getBasePath method directly instead of constructor name
        if (adapter && typeof adapter.getBasePath === 'function') {
          try {
            // @ts-ignore
            vaultRoot = adapter.getBasePath();
            // eslint-disable-next-line no-console
            console.log('[Notebook Automation] Got vaultRoot in fallback:', vaultRoot);
          } catch (err) {
            // eslint-disable-next-line no-console
            console.log('[Notebook Automation] Error calling getBasePath in fallback:', err);
          }
        }
      }

      if (vaultRoot) {
        const configDir = plugin.app.vault.configDir || '.obsidian';
        const pluginId = plugin.manifest?.id || 'notebook-automation';
        const pluginDir = path.join(vaultRoot, configDir, 'plugins', pluginId);

        const foundExecutable = tryFindExecutable(pluginDir);
        if (foundExecutable) {
          return foundExecutable;
        }

        // eslint-disable-next-line no-console
        console.log(`[Notebook Automation] No executable found in FileSystemAdapter fallback: ${pluginDir}`);
      } else {
        // If we still can't get vaultRoot, try to construct from manifest.dir if it exists
        if (plugin.manifest?.dir && plugin.manifest.dir !== '/' && !path.isAbsolute(plugin.manifest.dir)) {
          // This is a relative path, but we don't have vault root - this shouldn't happen
          // eslint-disable-next-line no-console
          console.log('[Notebook Automation] Cannot resolve absolute path - no vaultRoot available');
        }
      }
    }

    // Fallback: use __dirname (should work in most plugin contexts)
    if (typeof __dirname !== 'undefined' && path) {
      const foundExecutable = tryFindExecutable(__dirname);
      if (foundExecutable) {
        return foundExecutable;
      }
      // eslint-disable-next-line no-console
      console.log(`[Notebook Automation] No executable found in __dirname: ${__dirname}`);
    }

    // Final fallback: return just the executable name and hope it's in PATH
    // eslint-disable-next-line no-console
    console.log('[Notebook Automation] Using execName fallback:', execName);

    // Before returning execName, try one last attempt to construct absolute path
    if (vaultRoot && plugin.manifest?.id) {
      const configDir = plugin.app?.vault?.configDir || '.obsidian';
      const pluginId = plugin.manifest.id;
      const lastResortDir = path ? path.join(vaultRoot, configDir, 'plugins', pluginId) : '';

      if (lastResortDir) {
        const foundExecutable = tryFindExecutable(lastResortDir);
        if (foundExecutable) {
          // eslint-disable-next-line no-console
          console.log('[Notebook Automation] Last resort found executable:', foundExecutable);
          return foundExecutable;
        }
      }
    }

    return execName;
  } catch {
    // eslint-disable-next-line no-console
    console.log('[Notebook Automation] Exception in getNaExecutablePath, using execName fallback:', execName);
    return execName;
  }
}

interface NotebookAutomationSettings {
  configPath: string;
  verbose?: boolean;
  debug?: boolean;
  dryRun?: boolean;
  force?: boolean;
  pdfExtractImages?: boolean;
  bannersEnabled?: boolean;
  oneDriveSharedLink?: boolean;
  enableVideoSummary?: boolean;
  enablePdfSummary?: boolean;
  enableIndexCreation?: boolean;
  enableEnsureMetadata?: boolean;
  unidirectionalSync?: boolean;
  recursiveDirectorySync?: boolean;
  recursiveIndexBuild?: boolean;
}

const DEFAULT_SETTINGS: NotebookAutomationSettings = {
  configPath: "",
  verbose: false,
  debug: false,
  dryRun: false,
  force: false,
  pdfExtractImages: false,
  bannersEnabled: false,
  oneDriveSharedLink: true,
  enableVideoSummary: true,
  enablePdfSummary: true,
  enableIndexCreation: true,
  enableEnsureMetadata: true,
  unidirectionalSync: false,
  recursiveDirectorySync: true,
  recursiveIndexBuild: false,
};

export default class NotebookAutomationPlugin extends Plugin {
  settings: NotebookAutomationSettings = DEFAULT_SETTINGS;

  async onload() {
    await this.loadSettings();
    this.addRibbonIcon("dice", "Notebook Automation Plugin", () => {
      new Notice("Hello from Notebook Automation Plugin!");
    });
    this.addSettingTab(new NotebookAutomationSettingTab(this.app, this));

    // Register context menu commands for files and folders
    this.registerEvent(
      this.app.workspace.on("file-menu", (menu, file) => {
        // Folder context
        if (file instanceof TFolder) {
          menu.addSeparator();

          // Sync Directory - always available at the top
          menu.addItem((item) => {
            const syncTitle = this.settings.recursiveDirectorySync
              ? "Notebook Automation: Sync Directory with OneDrive Recursively"
              : "Notebook Automation: Sync Directory with OneDrive";
            item.setTitle(syncTitle)
              .setIcon("sync")
              .onClick(() => this.handleNotebookAutomationCommand(file, "sync-dir"));
          });

          // AI Video Summary - only if enabled
          if (this.settings.enableVideoSummary) {
            menu.addItem((item) => {
              item.setTitle("Notebook Automation: Import & AI Summarize All Videos")
                .setIcon("play")
                .onClick(() => this.handleNotebookAutomationCommand(file, "import-summarize-videos"));
            });
          }

          // AI PDF Summary - only if enabled
          if (this.settings.enablePdfSummary) {
            menu.addItem((item) => {
              item.setTitle("Notebook Automation: Import & AI Summarize All PDFs")
                .setIcon("document")
                .onClick(() => this.handleNotebookAutomationCommand(file, "import-summarize-pdfs"));
            });
          }

          // Index Creation - only if enabled
          if (this.settings.enableIndexCreation) {
            menu.addItem((item) => {
              const indexTitle = this.settings.recursiveIndexBuild
                ? "Notebook Automation: Build Indexes for This Folder and All Subfolders (Recursive)"
                : "Notebook Automation: Build Index for This Folder";
              const indexIcon = this.settings.recursiveIndexBuild ? "layers" : "list";
              const indexAction = this.settings.recursiveIndexBuild ? "build-index-recursive" : "build-index";
              item.setTitle(indexTitle)
                .setIcon(indexIcon)
                .onClick(() => this.handleNotebookAutomationCommand(file, indexAction));
            });
          }

          // Ensure Metadata - only if enabled
          if (this.settings.enableEnsureMetadata) {
            menu.addItem((item) => {
              item.setTitle("Notebook Automation: Ensure Metadata Consistency")
                .setIcon("settings")
                .onClick(() => this.handleNotebookAutomationCommand(file, "ensure-metadata"));
            });
          }
        }

        // File context: only for .md files
        if (file instanceof TFile && file.extension === "md") {
          menu.addSeparator();

          // AI Video Summary - only if enabled
          if (this.settings.enableVideoSummary) {
            menu.addItem((item) => {
              item.setTitle("Notebook Automation: Reprocess AI Summary (Video)")
                .setIcon("play")
                .onClick(() => this.handleNotebookAutomationCommand(file, "reprocess-summary-video"));
            });
          }

          // AI PDF Summary - only if enabled
          if (this.settings.enablePdfSummary) {
            menu.addItem((item) => {
              item.setTitle("Notebook Automation: Reprocess AI Summary (PDF)")
                .setIcon("document")
                .onClick(() => this.handleNotebookAutomationCommand(file, "reprocess-summary-pdf"));
            });
          }
        }
      })
    );
  }

  /**
   * Handler for context menu command on files/folders
   */
  /**
   * Handler for context menu command on files/folders (TFile or TFolder)
   */
  /**
   * Handler for context menu command on files/folders (TFile or TFolder)
   * @param file The file or folder
   * @param action The action string (see menu registration)
   */
  async handleNotebookAutomationCommand(file: import("obsidian").TAbstractFile, action: string) {
    // Get config for vault root and base using same priority logic as executeNotebookAutomationCommand
    let vaultRoot = "";
    let vaultBase = "";
    try {
      // Try to get loaded config from settings tab
      const loaded = (window as any).notebookAutomationLoadedConfig;
      console.log('[Notebook Automation] [DEBUG] loaded config from window:', loaded);
      if (loaded?.paths?.notebook_vault_fullpath_root) {
        vaultRoot = loaded.paths.notebook_vault_fullpath_root;
        vaultBase = loaded.paths?.notebook_vault_resources_basepath || "";
        console.log('[Notebook Automation] [DEBUG] Using loaded config - vaultRoot:', vaultRoot, 'vaultBase:', vaultBase);
      } else {
        // Use same priority logic as executeNotebookAutomationCommand
        // @ts-ignore
        const fs = window.require ? window.require('fs') : null;
        // @ts-ignore
        const path = window.require ? window.require('path') : null;
        let configPath = '';

        console.log('[Notebook Automation] [DEBUG] fs available:', !!fs, 'path available:', !!path);

        // First priority: Environment variable NOTEBOOKAUTOMATION_CONFIG
        const envConfigPath = process.env.NOTEBOOKAUTOMATION_CONFIG;
        console.log('[Notebook Automation] [DEBUG] envConfigPath:', envConfigPath);
        if (envConfigPath && fs && fs.existsSync(envConfigPath)) {
          configPath = envConfigPath;
          console.log('[Notebook Automation] [DEBUG] Using env config path:', configPath);
        }

        // Second priority: Use default-config.json from plugin directory
        if (!configPath && path && fs) {
          let pluginDir = this.manifest?.dir;
          console.log('[Notebook Automation] [DEBUG] pluginDir:', pluginDir);
          if (pluginDir) {
            const adapter = this.app?.vault?.adapter;
            // @ts-ignore
            if (adapter && typeof adapter.getBasePath === 'function') {
              try {
                // @ts-ignore
                const vaultRootPath = adapter.getBasePath();
                if (vaultRootPath && !path.isAbsolute(pluginDir)) {
                  pluginDir = path.join(vaultRootPath, pluginDir);
                }
              } catch (err) {
                // Continue with original pluginDir
              }
            }
            const defaultConfigPath = path.join(pluginDir, 'default-config.json');
            console.log('[Notebook Automation] [DEBUG] checking defaultConfigPath:', defaultConfigPath);
            if (fs.existsSync(defaultConfigPath)) {
              configPath = defaultConfigPath;
              console.log('[Notebook Automation] [DEBUG] Using default config path:', configPath);
            }
          }
        }

        // Third priority: Fallback to user-configured path
        if (!configPath && this.settings.configPath) {
          configPath = this.settings.configPath;
          console.log('[Notebook Automation] [DEBUG] Using user config path:', configPath);
        }

        // Load config if we found a path
        if (configPath && fs && fs.existsSync(configPath)) {
          console.log('[Notebook Automation] [DEBUG] Loading config from:', configPath);
          const content = fs.readFileSync(configPath, 'utf8');
          const config = JSON.parse(content);
          console.log('[Notebook Automation] [DEBUG] Parsed config:', config);
          vaultRoot = config.paths?.notebook_vault_fullpath_root || "";
          vaultBase = config.paths?.notebook_vault_resources_basepath || "";
          console.log('[Notebook Automation] [DEBUG] Extracted - vaultRoot:', vaultRoot, 'vaultBase:', vaultBase);
        } else {
          console.log('[Notebook Automation] [DEBUG] No valid config path found. configPath:', configPath, 'fs available:', !!fs, 'file exists:', configPath ? fs?.existsSync?.(configPath) : 'N/A');
        }
      }
    } catch (err) {
      console.log('[Notebook Automation] Error loading config for path processing:', err);
    }
    // Debug log vaultRoot, vaultBase, and file.path
    // eslint-disable-next-line no-console
    console.log(`[Notebook Automation] vaultRoot: ${vaultRoot}`);
    console.log(`[Notebook Automation] vaultBase: ${vaultBase}`);
    console.log(`[Notebook Automation] file.path: ${file.path}`);

    // TEMPORARY FIX: Hardcode the config values for testing
    if (!vaultRoot && !vaultBase) {
      console.log('[Notebook Automation] [TEMP] Config loading failed, using hardcoded values for testing');
      vaultRoot = "C:/Users/danshue.REDMOND/Vault/01_Projects/MBA";
      vaultBase = "01_Projects/MBA";
      console.log('[Notebook Automation] [TEMP] Hardcoded vaultRoot:', vaultRoot, 'vaultBase:', vaultBase);
    }

    const relPath = getRelativeVaultResourcePath(file.path, vaultRoot, vaultBase);
    // Log to the developer console
    // eslint-disable-next-line no-console
    console.log(`[Notebook Automation] Command '${action}' triggered for: ${file.path}`);
    console.log(`[Notebook Automation] Relative path for OneDrive mapping: ${relPath}`);

    // Check if any config is available (using same priority as executeNotebookAutomationCommand)
    let hasConfig = false;

    // Check for environment variable first
    const envConfigPath = process.env.NOTEBOOKAUTOMATION_CONFIG;
    if (envConfigPath) {
      try {
        // @ts-ignore
        const fs = window.require ? window.require('fs') : null;
        if (fs && fs.existsSync(envConfigPath)) {
          hasConfig = true;
        }
      } catch (err) {
        // Continue to next check
      }
    }

    // Check for default-config.json from plugin directory
    if (!hasConfig) {
      try {
        // @ts-ignore
        const path = window.require ? window.require('path') : null;
        // @ts-ignore
        const fs = window.require ? window.require('fs') : null;

        if (path && fs) {
          let pluginDir = this.manifest?.dir;
          if (pluginDir) {
            const adapter = this.app?.vault?.adapter;
            // @ts-ignore
            if (adapter && typeof adapter.getBasePath === 'function') {
              try {
                // @ts-ignore
                const vaultRoot = adapter.getBasePath();
                if (vaultRoot && !path.isAbsolute(pluginDir)) {
                  pluginDir = path.join(vaultRoot, pluginDir);
                }
              } catch (err) {
                // Continue with original pluginDir
              }
            }

            const defaultConfigPath = path.join(pluginDir, 'default-config.json');
            if (fs.existsSync(defaultConfigPath)) {
              hasConfig = true;
            }
          }
        }
      } catch (err) {
        // Continue to next check
      }
    }

    // Check for user-configured path
    if (!hasConfig && this.settings.configPath) {
      try {
        // @ts-ignore
        const fs = window.require ? window.require('fs') : null;
        if (fs && fs.existsSync(this.settings.configPath)) {
          hasConfig = true;
        }
      } catch (err) {
        // Continue
      }
    }

    if (!hasConfig) {
      new Notice("No configuration file found. Please set up configuration in plugin settings first.");
      return;
    }
    try {
      await this.executeNotebookAutomationCommand(action, relPath);
    } catch (error) {
      // eslint-disable-next-line no-console
      console.error(`[Notebook Automation] Error executing command:`, error);
      const errorMessage = error instanceof Error ? error.message : String(error);
      new Notice(`Error executing command: ${errorMessage}`);
    }
  }

  /**
   * Execute the actual na CLI command based on the action
   */
  /**
   * Execute the actual na CLI command based on the action
   * @param action The action string
   * @param relativePath The relative path for the command
   * @param opts Optional flags (e.g., force: true to add --force)
   */
  async executeNotebookAutomationCommand(action: string, relativePath: string, opts?: { force?: boolean }) {
    // @ts-ignore
    const child_process = window.require ? window.require('child_process') : null;
    if (!child_process) {
      throw new Error("Child process module not available");
    }

    const naPath = getNaExecutablePath(this);

    // Check for environment variable first, then default-config.json, then user setting
    let configPath = '';

    // First priority: Environment variable NOTEBOOKAUTOMATION_CONFIG
    const envConfigPath = process.env.NOTEBOOKAUTOMATION_CONFIG;
    if (envConfigPath) {
      try {
        // @ts-ignore
        const fs = window.require ? window.require('fs') : null;
        if (fs && fs.existsSync(envConfigPath)) {
          configPath = envConfigPath;
          console.log('[Notebook Automation] Using config from environment variable NOTEBOOKAUTOMATION_CONFIG:', configPath);
        }
      } catch (err) {
        console.log('[Notebook Automation] Error checking environment config path:', err);
      }
    }

    // Second priority: Use default-config.json from plugin directory
    if (!configPath) {
      try {
        // @ts-ignore
        const path = window.require ? window.require('path') : null;
        // @ts-ignore
        const fs = window.require ? window.require('fs') : null;

        if (path && fs) {
          // Get plugin directory
          let pluginDir = this.manifest?.dir;
          if (pluginDir) {
            // Resolve plugin directory path
            const adapter = this.app?.vault?.adapter;
            // @ts-ignore
            if (adapter && typeof adapter.getBasePath === 'function') {
              try {
                // @ts-ignore
                const vaultRoot = adapter.getBasePath();
                if (vaultRoot && !path.isAbsolute(pluginDir)) {
                  pluginDir = path.join(vaultRoot, pluginDir);
                }
              } catch (err) {
                console.log('[Notebook Automation] Error getting vault root for config path:', err);
              }
            }

            const defaultConfigPath = path.join(pluginDir, 'default-config.json');
            if (fs.existsSync(defaultConfigPath)) {
              configPath = defaultConfigPath;
              console.log('[Notebook Automation] Using default-config.json from plugin directory:', configPath);
            }
          }
        }
      } catch (err) {
        console.log('[Notebook Automation] Error constructing default config path:', err);
      }
    }

    // Third priority: Fallback to user-configured path
    if (!configPath && this.settings.configPath) {
      configPath = this.settings.configPath || '';
      console.log('[Notebook Automation] Fallback to user-configured path:', configPath);
    }

    if (!configPath) {
      throw new Error("No configuration file available. Please set up configuration in plugin settings.");
    }

    // Build command arguments based on action
    let args: string[] = [];
    let commandDescription = "";

    switch (action) {
      case "sync-dir":
        // Pass the relative path to sync-dirs command to specify starting point
        args = ["vault", "sync-dirs", relativePath, "--config", configPath];
        commandDescription = "Sync Directory with OneDrive";
        break;
      case "import-summarize-videos":
        args = ["video-notes", "--input", relativePath, "--config", configPath];
        commandDescription = "Import & AI Summarize Videos";
        break;
      case "import-summarize-pdfs":
        args = ["pdf-notes", "--input", relativePath, "--config", configPath];
        commandDescription = "Import & AI Summarize PDFs";
        break;
      case "build-index":
        args = ["vault", "generate-index", relativePath, "--config", configPath];
        commandDescription = "Build Index";
        break;
      case "build-index-recursive":
        args = ["vault", "generate-index", relativePath, "--config", configPath, "--recursive"];
        commandDescription = "Build Indexes (Recursive)";
        break;
      case "reprocess-summary-video":
        args = ["video-notes", "--input", relativePath, "--reprocess", "--config", configPath];
        commandDescription = "Reprocess Video Summary";
        break;
      case "reprocess-summary-pdf":
        args = ["pdf-notes", "--input", relativePath, "--reprocess", "--config", configPath];
        commandDescription = "Reprocess PDF Summary";
        break;
      case "ensure-metadata":
        args = ["ensure-metadata", "--input", relativePath, "--config", configPath];
        commandDescription = "Ensure Metadata Consistency";
        break;
      default:
        throw new Error(`Unknown action: ${action}`);
    }

    // Add optional flags based on settings
    if (this.settings.verbose) {
      args.push("--verbose");
    }
    if (this.settings.debug) {
      args.push("--debug");
    }
    if (this.settings.dryRun) {
      args.push("--dry-run");
    }
    if (this.settings.force) {
      args.push("--force");
    }
    if (this.settings.pdfExtractImages) {
      args.push("--pdf-extract-images");
    }
    if (!this.settings.oneDriveSharedLink) {
      args.push("--no-share-links");
    }
    if (this.settings.bannersEnabled) {
      args.push("--banners-enabled");
    }
    if (this.settings.unidirectionalSync) {
      args.push("--unidirectional");
    }
    // Add recursive flag for sync operations only
    if (action === "sync-dir" && this.settings.recursiveDirectorySync) {
      args.push("--recursive");
    }
    // Only add --force if explicitly requested by the caller (in addition to settings)
    if (opts?.force) {
      args.push("--force");
    }

    // Build the full command for display and execution
    const quotedArgs = args.map(arg => arg.includes(' ') ? `"${arg}"` : arg);
    const fullCommand = `"${naPath}" ${quotedArgs.join(' ')}`;

    // Log the full command to console
    // eslint-disable-next-line no-console
    console.log(`[Notebook Automation] ===== EXECUTING COMMAND =====`);
    // eslint-disable-next-line no-console
    console.log(`[Notebook Automation] Action: ${action}`);
    // eslint-disable-next-line no-console
    console.log(`[Notebook Automation] Description: ${commandDescription}`);
    // eslint-disable-next-line no-console
    console.log(`[Notebook Automation] Relative Path: ${relativePath}`);
    // eslint-disable-next-line no-console
    console.log(`[Notebook Automation] Config Path: ${configPath}`);
    // eslint-disable-next-line no-console
    console.log(`[Notebook Automation] Full Command: ${fullCommand}`);
    // eslint-disable-next-line no-console
    console.log(`[Notebook Automation] ===============================`);

    // Show initial notice
    new Notice(`Starting: ${commandDescription} for ${relativePath}`);

    try {
      // Get environment variables from process.env and log them for debugging
      const env = { ...process.env };

      // eslint-disable-next-line no-console
      console.log(`[Notebook Automation] Environment check:`);
      // eslint-disable-next-line no-console
      console.log(`[Notebook Automation] AZURE_OPENAI_KEY: ${env.AZURE_OPENAI_KEY ? 'SET (length: ' + env.AZURE_OPENAI_KEY.length + ')' : 'NOT SET'}`);
      // eslint-disable-next-line no-console
      console.log(`[Notebook Automation] NOTEBOOKAUTOMATION_CONFIG: ${env.NOTEBOOKAUTOMATION_CONFIG || 'NOT SET'}`);

      // If config path is not set in plugin settings, try environment variable
      let finalConfigPath = configPath;
      if (!finalConfigPath && env.NOTEBOOKAUTOMATION_CONFIG) {
        finalConfigPath = env.NOTEBOOKAUTOMATION_CONFIG;
        // eslint-disable-next-line no-console
        console.log(`[Notebook Automation] Using config path from environment: ${finalConfigPath}`);
      }

      // Execute the command asynchronously to avoid blocking the UI
      const { spawn } = child_process;

      // Parse the command and arguments
      const matchResult = fullCommand.match(/(?:[^\s"]+|"[^"]*")+/g);
      if (!matchResult) {
        throw new Error("Failed to parse command");
      }
      const [command, ...cmdArgs] = matchResult.map(arg => arg.replace(/^"(.*)"$/, '$1'));

      // eslint-disable-next-line no-console
      console.log(`[Notebook Automation] Executing asynchronously - command: ${command}, args:`, cmdArgs);

      const childProcess = spawn(command, cmdArgs, {
        env: env,
        stdio: ['pipe', 'pipe', 'pipe']
      });

      let stdout = '';
      let stderr = '';

      // Collect stdout data
      childProcess.stdout.on('data', (data: Buffer) => {
        const chunk = data.toString();
        stdout += chunk;
        // eslint-disable-next-line no-console
        console.log(`[Notebook Automation] STDOUT chunk:`, chunk);
      });

      // Collect stderr data
      childProcess.stderr.on('data', (data: Buffer) => {
        const chunk = data.toString();
        stderr += chunk;
        // eslint-disable-next-line no-console
        console.log(`[Notebook Automation] STDERR chunk:`, chunk);
      });

      // Handle process completion
      const exitPromise = new Promise<void>((resolve, reject) => {
        childProcess.on('close', (code: number | null) => {
          // eslint-disable-next-line no-console
          console.log(`[Notebook Automation] Process exited with code: ${code}`);
          // eslint-disable-next-line no-console
          console.log(`[Notebook Automation] Final STDOUT:`, stdout);
          // eslint-disable-next-line no-console
          console.log(`[Notebook Automation] Final STDERR:`, stderr);

          if (code === 0) {
            resolve();
          } else {
            const error = new Error(`Command failed with exit code ${code}`);
            (error as any).code = code;
            (error as any).stdout = stdout;
            (error as any).stderr = stderr;
            reject(error);
          }
        });

        childProcess.on('error', (error: Error) => {
          // eslint-disable-next-line no-console
          console.error(`[Notebook Automation] Process error:`, error);
          reject(error);
        });
      });

      // Wait for the process to complete
      await exitPromise;

      // eslint-disable-next-line no-console
      console.log(`[Notebook Automation] Command completed successfully`);

      // Show success notice
      new Notice(`✅ ${commandDescription} completed successfully!`);

    } catch (error) {
      // eslint-disable-next-line no-console
      console.error(`[Notebook Automation] Command failed:`, error);

      // Get more detailed error information
      const errorAny = error as any;
      const stderr = errorAny.stderr?.toString() || '';
      const stdout = errorAny.stdout?.toString() || '';
      const exitCode = errorAny.code || errorAny.status || 'unknown';

      // eslint-disable-next-line no-console
      console.log(`[Notebook Automation] Exit code: ${exitCode}`);
      // eslint-disable-next-line no-console
      console.log(`[Notebook Automation] STDOUT:`, stdout);
      // eslint-disable-next-line no-console
      console.log(`[Notebook Automation] STDERR:`, stderr);

      // Create a more informative error message
      let errorMsg = `Command failed (exit code: ${exitCode})`;
      if (stdout && stdout.includes('AZURE_OPENAI_KEY')) {
        errorMsg = "❗ Azure OpenAI API key is missing. Please set the AZURE_OPENAI_KEY environment variable.";
      } else if (stderr && stderr.includes('AZURE_OPENAI_KEY')) {
        errorMsg = "❗ Azure OpenAI API key is missing. Please set the AZURE_OPENAI_KEY environment variable.";
      } else if (stdout) {
        // Extract the last meaningful line from stdout
        const lines = stdout.split('\n').filter((line: string) => line.trim().length > 0);
        const lastLine = lines[lines.length - 1];
        if (lastLine && lastLine.includes('ERR')) {
          errorMsg = lastLine.replace(/\[[^\]]*\]/, '').trim(); // Remove timestamp
        }
      } else if (stderr) {
        errorMsg = stderr;
      } else if (error instanceof Error) {
        errorMsg = error.message;
      }

      // Show error notice with details
      new Notice(`❌ ${commandDescription} failed: ${errorMsg}`, 8000); // Show for 8 seconds
      throw error;
    }
  }

  async loadSettings() {
    this.settings = Object.assign({}, DEFAULT_SETTINGS, await this.loadData());
  }

  async saveSettings() {
    await this.saveData(this.settings);
  }
}

class NotebookAutomationSettingTab extends PluginSettingTab {
  plugin: NotebookAutomationPlugin;

  constructor(app: App, plugin: NotebookAutomationPlugin) {
    super(app, plugin);
    this.plugin = plugin;
  }

  checkAndLoadDefaultConfig() {
    try {
      // @ts-ignore
      const fs = window.require ? window.require('fs') : null;
      // @ts-ignore
      const path = window.require ? window.require('path') : null;
      if (!fs || !path) {
        return;
      }

      // First priority: Check for environment variable NOTEBOOKAUTOMATION_CONFIG
      const envConfigPath = process.env.NOTEBOOKAUTOMATION_CONFIG;
      if (envConfigPath) {
        try {
          if (fs.existsSync(envConfigPath) && fs.statSync(envConfigPath).isFile()) {
            const content = fs.readFileSync(envConfigPath, 'utf8');
            try {
              const configJson = JSON.parse(content);
              (window as any).notebookAutomationLoadedConfig = configJson;
              console.log('[Notebook Automation] Loaded config from NOTEBOOKAUTOMATION_CONFIG environment variable:', envConfigPath);
              return; // Exit early since we found the config
            } catch (jsonErr) {
              console.error('[Notebook Automation] Error parsing config from environment variable:', jsonErr);
            }
          }
        } catch (err) {
          console.error('[Notebook Automation] Error reading config from environment variable:', err);
        }
      }

      // Second priority: Get plugin directory and check for default-config.json
      const pluginDir = this.plugin.manifest?.dir;
      if (!pluginDir) {
        return;
      }

      // Resolve plugin directory path
      let resolvedPluginDir = pluginDir;
      const adapter = this.plugin.app?.vault?.adapter;
      // @ts-ignore
      if (adapter && typeof adapter.getBasePath === 'function') {
        try {
          // @ts-ignore
          const vaultRoot = adapter.getBasePath();
          resolvedPluginDir = path.resolve(vaultRoot, pluginDir);
        } catch (err) {
          // Fallback to original path
        }
      }

      const defaultConfigPath = path.join(resolvedPluginDir, 'default-config.json');

      // Check if default-config.json exists
      if (fs.existsSync(defaultConfigPath) && fs.statSync(defaultConfigPath).isFile()) {
        const content = fs.readFileSync(defaultConfigPath, 'utf8');
        try {
          const configJson = JSON.parse(content);
          (window as any).notebookAutomationLoadedConfig = configJson;
          console.log('[Notebook Automation] Loaded default-config.json automatically');
        } catch (jsonErr) {
          console.error('[Notebook Automation] Error parsing default-config.json:', jsonErr);
        }
      }
    } catch (err) {
      console.error('[Notebook Automation] Error checking for configuration files:', err);
    }
  }

  /**
   * Injects custom CSS for the settings tab if not already present.
   */
  injectCustomStyles() {
    const styleId = 'notebook-automation-settings-style';
    if (document.getElementById(styleId)) return;
    const style = document.createElement('style');
    style.id = styleId;
    style.textContent = `
      /* Add your custom CSS for the settings tab here */
      .notebook-automation-settings { padding: 1.5em 1.5em 2em 1.5em; }
      .notebook-automation-section-header { margin-top: 1.5em; font-size: 1.2em; font-weight: bold; }
      .notebook-automation-settings-group { margin-bottom: 1.5em; }
      .notebook-automation-config-fields { margin-top: 1.5em; }
      .notebook-automation-custom-setting { margin-bottom: 1em; }
      .notebook-automation-input-control input,
      .notebook-automation-input-control textarea,
      .notebook-automation-provider-select {
        width: 100%;
        max-width: 500px;
        font-size: 1em;
        padding: 0.3em 0.5em;
        margin-top: 0.2em;
        margin-bottom: 0.2em;
        border-radius: 4px;
        border: 1px solid #888;
        background: var(--background-secondary-alt);
        color: var(--text-normal);
      }
      .notebook-automation-provider-fields { margin-top: 0.5em; }
      .notebook-automation-additional-fields { margin-top: 1em; }
      .notebook-automation-ai-header { margin-top: 1.2em; font-size: 1.1em; font-weight: bold; }
      .notebook-automation-sub-header { margin-top: 0.8em; font-size: 1em; font-weight: bold; }
      .notebook-automation-version { font-size: 0.95em; color: var(--text-faint); margin-bottom: 0.8em; }
      .mod-warning { color: var(--color-red); font-weight: bold; }
    `;
    document.head.appendChild(style);
  }

  async getNaVersion(): Promise<string> {
    try {
      // @ts-ignore
      const child_process = window.require ? window.require('child_process') : null;
      if (!child_process) {
        return "Unknown (Node.js not available)";
      }

      const naPath = getNaExecutablePath(this.plugin);
      const { exec } = child_process;

      return new Promise((resolve) => {
        exec(`"${naPath}" --version`, (error: any, stdout: string, stderr: string) => {
          if (error) {
            resolve("Unknown (Error getting version)");
            return;
          }
          const version = stdout.trim() || stderr.trim() || "Unknown";
          resolve(version);
        });
      });
    } catch (error) {
      return "Unknown (Exception)";
    }
  }

  display(): void {
    this.injectCustomStyles();
    const { containerEl } = this;
    containerEl.empty();
    containerEl.style.overflowY = "auto";
    containerEl.style.maxHeight = "80vh";
    containerEl.addClass("notebook-automation-settings");

    // Feature toggles section
    containerEl.createEl("h3", { text: "Features", cls: "notebook-automation-section-header" });

    const featureGroup = containerEl.createDiv({ cls: "notebook-automation-settings-group" });

    // Enable AI Video Summary
    new Setting(featureGroup)
      .setName("Enable AI Video Summary")
      .setDesc("Enables AI-powered video summarization features in context menus. When enabled, you can right-click on folders to 'Import & AI Summarize All Videos' (processes all video files in the folder and generates intelligent summaries using AI) or right-click on existing markdown files to 'Reprocess AI Summary (Video)' (regenerates the AI summary for video content). The AI analyzes video transcripts, identifies key concepts, and creates structured markdown notes with summaries, key points, and timestamps.")
      .addToggle(toggle => {
        toggle.setValue(this.plugin.settings.enableVideoSummary ?? true)
          .onChange(async (value) => {
            this.plugin.settings.enableVideoSummary = value;
            await this.plugin.saveSettings();
          });
      });

    // Enable AI PDF Summary
    new Setting(featureGroup)
      .setName("Enable AI PDF Summary")
      .setDesc("Enables AI-powered PDF document summarization features in context menus. When enabled, you can right-click on folders to 'Import & AI Summarize All PDFs' (processes all PDF files in the folder and generates intelligent summaries using AI) or right-click on existing markdown files to 'Reprocess AI Summary (PDF)' (regenerates the AI summary for PDF content). The AI extracts text from PDFs, analyzes document structure, identifies main themes and concepts, and creates comprehensive markdown notes with summaries, key insights, and important quotes.")
      .addToggle(toggle => {
        toggle.setValue(this.plugin.settings.enablePdfSummary ?? true)
          .onChange(async (value) => {
            this.plugin.settings.enablePdfSummary = value;
            await this.plugin.saveSettings();
          });
      });

    // Enable Index Creation
    new Setting(featureGroup)
      .setName("Enable Index Creation")
      .setDesc("Enables automatic index generation features for organizing and navigating your notebook structure. When enabled, you can right-click on folders to 'Build Index for This Folder' (creates a comprehensive index of all files and subfolders in the selected directory) or 'Build Indexes for This Folder and All Subfolders' (recursively generates indexes for the entire folder hierarchy). These indexes provide structured navigation, file summaries, and cross-references to help you quickly find and access content across your notebook.")
      .addToggle(toggle => {
        toggle.setValue(this.plugin.settings.enableIndexCreation ?? true)
          .onChange(async (value) => {
            this.plugin.settings.enableIndexCreation = value;
            await this.plugin.saveSettings();
          });
      });

    // Enable Ensure Metadata
    new Setting(featureGroup)
      .setName("Enable Ensure Metadata")
      .setDesc("Enables metadata consistency management features to maintain proper YAML frontmatter across your notebook. When enabled, you can right-click on folders to 'Ensure Metadata Consistency' which automatically analyzes all markdown files in the directory hierarchy and ensures they have proper metadata fields (such as tags, categories, dates, and custom properties) based on their location, filename patterns, and content. This helps maintain organized, searchable, and properly categorized notes throughout your vault.")
      .addToggle(toggle => {
        toggle.setValue(this.plugin.settings.enableEnsureMetadata ?? true)
          .onChange(async (value) => {
            this.plugin.settings.enableEnsureMetadata = value;
            await this.plugin.saveSettings();
          });
      });

    // Command flags section
    containerEl.createEl("h3", { text: "Flags", cls: "notebook-automation-section-header" });

    const flagsGroup = containerEl.createDiv({ cls: "notebook-automation-settings-group" });

    // Verbose flag
    new Setting(flagsGroup)
      .setName("Verbose Mode")
      .setDesc("Enable detailed output during command execution. This will show additional information about what the automation is doing, including progress updates, file processing details, and step-by-step operations. Useful for monitoring long-running tasks and understanding what's happening behind the scenes.")
      .addToggle(toggle => {
        toggle.setValue(this.plugin.settings.verbose || false)
          .onChange(async (value) => {
            this.plugin.settings.verbose = value;
            await this.plugin.saveSettings();
          });
      });

    // Debug flag
    new Setting(flagsGroup)
      .setName("Debug Mode")
      .setDesc("Enable comprehensive debug logging for technical troubleshooting. This provides the most detailed output including API calls, configuration parsing, file system operations, error stack traces, and internal processing steps. Essential for diagnosing issues or understanding unexpected behavior. Note: This generates significantly more output than verbose mode.")
      .addToggle(toggle => {
        toggle.setValue(this.plugin.settings.debug || false)
          .onChange(async (value) => {
            this.plugin.settings.debug = value;
            await this.plugin.saveSettings();
          });
      });

    // Dry-run flag
    new Setting(flagsGroup)
      .setName("Dry Run")
      .setDesc("Simulate all operations without making any actual changes to files or folders. This allows you to preview what the automation would do, including which files would be created, modified, or processed, without any risk of unwanted changes. Perfect for testing configurations or understanding the impact of operations before committing to them.")
      .addToggle(toggle => {
        toggle.setValue(this.plugin.settings.dryRun || false)
          .onChange(async (value) => {
            this.plugin.settings.dryRun = value;
            await this.plugin.saveSettings();
          });
      });

    // Force flag
    new Setting(flagsGroup)
      .setName("Force Mode")
      .setDesc("Override safety checks and force operations to proceed even when they would normally be skipped or blocked. This includes processing files that already exist, regenerating summaries that are up-to-date, ignoring file locks, and bypassing validation warnings. Use with caution as this can overwrite existing work or ignore important safety mechanisms.")
      .addToggle(toggle => {
        toggle.setValue(this.plugin.settings.force || false)
          .onChange(async (value) => {
            this.plugin.settings.force = value;
            await this.plugin.saveSettings();
          });
      });

    // PDF Extract Images flag
    new Setting(flagsGroup)
      .setName("PDF Extract Images")
      .setDesc("Extract images from PDF files during processing. When enabled, the automation will extract and save images found in PDF documents alongside the generated markdown notes. This is useful for preserving diagrams, charts, and other visual content from academic papers and documents.")
      .addToggle(toggle => {
        toggle.setValue(this.plugin.settings.pdfExtractImages || false)
          .onChange(async (value) => {
            this.plugin.settings.pdfExtractImages = value;
            await this.plugin.saveSettings();
          });
      });

    // Banners Enabled flag
    new Setting(flagsGroup)
      .setName("Banners Enabled")
      .setDesc("Enable banner images in generated content. When enabled, the automation will include banner images at the top of generated markdown files based on the configured banner settings and filename patterns.")
      .addToggle(toggle => {
        toggle.setValue(this.plugin.settings.bannersEnabled || false)
          .onChange(async (value) => {
            this.plugin.settings.bannersEnabled = value;
            await this.plugin.saveSettings();
          });
      });

    // OneDrive Shared Link flag
    new Setting(flagsGroup)
      .setName("OneDrive Shared Link")
      .setDesc("Enable OneDrive shared link creation for processed files. When enabled, the automation will create shared links to the original assets in OneDrive using Microsoft Graph API and include them in the generated markdown notes. This allows easy access to the source files from within your notes. Disable this to skip shared link creation and pass --no-share-links to the commands.")
      .addToggle(toggle => {
        toggle.setValue(this.plugin.settings.oneDriveSharedLink ?? true)
          .onChange(async (value) => {
            this.plugin.settings.oneDriveSharedLink = value;
            await this.plugin.saveSettings();
          });
      });

    // Unidirectional Sync flag
    new Setting(flagsGroup)
      .setName("Unidirectional Sync")
      .setDesc("Enable unidirectional synchronization mode for directory sync operations. When enabled, synchronization will only flow from OneDrive to Vault (OneDrive → Vault), preventing any changes from being pushed back to OneDrive. This is useful when you want to import content from OneDrive but keep your vault as read-only relative to the OneDrive source. Disable this for bidirectional sync where changes can flow in both directions.")
      .addToggle(toggle => {
        toggle.setValue(this.plugin.settings.unidirectionalSync || false)
          .onChange(async (value) => {
            this.plugin.settings.unidirectionalSync = value;
            await this.plugin.saveSettings();
          });
      });

    // Recursive Directory Sync flag
    new Setting(flagsGroup)
      .setName("Recursive Directory Sync")
      .setDesc("Enable recursive directory scanning for sync operations. When enabled, directory synchronization will process the entire directory tree including all subdirectories and nested folders. When disabled, only the immediate children (first level) of the target directory will be synchronized. This affects how deep the sync operation goes into the folder hierarchy when synchronizing between OneDrive and your vault.")
      .addToggle(toggle => {
        toggle.setValue(this.plugin.settings.recursiveDirectorySync ?? true)
          .onChange(async (value) => {
            this.plugin.settings.recursiveDirectorySync = value;
            await this.plugin.saveSettings();
          });
      });

    // Recursive Index Build flag
    new Setting(flagsGroup)
      .setName("Recursive Index Build")
      .setDesc("Enable recursive index building for directory operations. When enabled, the 'Build Index' context menu option will process the entire directory tree including all subdirectories and nested folders. When disabled, only the immediate folder will be indexed. This affects the depth of index generation when building indexes from the context menu.")
      .addToggle(toggle => {
        toggle.setValue(this.plugin.settings.recursiveIndexBuild ?? false)
          .onChange(async (value) => {
            this.plugin.settings.recursiveIndexBuild = value;
            await this.plugin.saveSettings();
          });
      });

    // Configuration section
    containerEl.createEl("h3", { text: "Configuration", cls: "notebook-automation-section-header" });

    let configJson: any = null;
    let configError: string | null = null;

    // Config file path input
    const configFileSetting = new Setting(containerEl)
      .setName("Custom Config File (Optional)")
      .setDesc("Enter the path to a custom config.json file. Priority order: 1) NOTEBOOKAUTOMATION_CONFIG environment variable, 2) default-config.json from plugin directory, 3) this custom path setting. This allows you to override the default configuration if needed.");
    configFileSetting.settingEl.addClass("notebook-automation-config-input");
    configFileSetting.controlEl.style.display = "flex";
    configFileSetting.controlEl.style.flexDirection = "column";
    const configPathInput = document.createElement("input");
    configPathInput.type = "text";
    configPathInput.placeholder = "Optional: Path to custom config.json...";
    configPathInput.value = this.plugin.settings.configPath || "";
    configPathInput.style.marginBottom = "0.5em";
    configPathInput.onchange = async (e: any) => {
      this.plugin.settings.configPath = e.target.value;
      await this.plugin.saveSettings();
    };
    configFileSetting.controlEl.appendChild(configPathInput);

    // Validate & Load button
    const validateBtn = document.createElement("button");
    validateBtn.textContent = "🔍 Validate & Load Config";
    validateBtn.style.marginBottom = "0.5em";
    validateBtn.onclick = async () => {
      const path = this.plugin.settings.configPath;
      if (!path) {
        new Notice("Please enter a config file path first.");
        return;
      }
      try {
        // @ts-ignore
        const fs = window.require ? window.require('fs') : null;
        if (!fs) {
          new Notice("File system access is not available in this environment.");
          return;
        }
        if (fs.existsSync(path) && fs.statSync(path).isFile()) {
          const content = fs.readFileSync(path, 'utf8');
          try {
            configJson = JSON.parse(content);
            configError = null;
            new Notice("✅ Config loaded successfully.");
            this.displayLoadedConfig(configJson);
          } catch (jsonErr) {
            configError = "Invalid JSON: " + (jsonErr instanceof Error ? jsonErr.message : String(jsonErr));
            new Notice(configError);
            this.displayLoadedConfig(null, configError);
          }
        } else {
          configError = "Config file does not exist or is not a file.";
          new Notice(configError);
          this.displayLoadedConfig(null, configError);
        }
      } catch (err) {
        configError = "Error checking file: " + (err instanceof Error ? err.message : String(err));
        new Notice(configError);
        this.displayLoadedConfig(null, configError);
      }
    };
    configFileSetting.controlEl.appendChild(validateBtn);

    // Check for default-config.json in plugin directory first
    this.checkAndLoadDefaultConfig();

    // Add status message showing which config is being used
    const configStatusDiv = containerEl.createDiv({ cls: "notebook-automation-config-status" });
    configStatusDiv.style.marginTop = "0.5em";
    configStatusDiv.style.padding = "0.5em";
    configStatusDiv.style.borderRadius = "4px";
    configStatusDiv.style.backgroundColor = "var(--background-secondary-alt)";
    configStatusDiv.style.border = "1px solid var(--background-modifier-border)";

    if ((window as any).notebookAutomationLoadedConfig) {
      // Check for environment variable first
      const envConfigPath = process.env.NOTEBOOKAUTOMATION_CONFIG;
      if (envConfigPath) {
        configStatusDiv.innerHTML = `
          <div style="color: var(--color-green); font-weight: bold;">✅ Configuration Status</div>
          <div style="margin-top: 0.3em; font-size: 0.9em;">
            🌍 Using config from NOTEBOOKAUTOMATION_CONFIG environment variable<br>
            📁 Path: ${envConfigPath}
            ${this.plugin.settings.configPath ? `<br>📝 Custom config path also set: ${this.plugin.settings.configPath}` : ''}
          </div>
        `;
      } else {
        // Try to determine if this is the default config or a custom one
        try {
          // @ts-ignore
          const path = window.require ? window.require('path') : null;
          if (path && this.plugin.manifest?.dir) {
            const adapter = this.plugin.app?.vault?.adapter;
            let resolvedPluginDir = this.plugin.manifest.dir;
            // @ts-ignore
            if (adapter && typeof adapter.getBasePath === 'function') {
              try {
                // @ts-ignore
                const vaultRoot = adapter.getBasePath();
                resolvedPluginDir = path.resolve(vaultRoot, this.plugin.manifest.dir);
              } catch (err) {
                // Fallback to original path
              }
            }

            const defaultConfigPath = path.join(resolvedPluginDir, 'default-config.json');
            configStatusDiv.innerHTML = `
              <div style="color: var(--color-green); font-weight: bold;">✅ Configuration Status</div>
              <div style="margin-top: 0.3em; font-size: 0.9em;">
                🔄 Using default-config.json from plugin directory<br>
                📁 Path: ${defaultConfigPath}
                ${this.plugin.settings.configPath ? `<br>📝 Custom config path also set: ${this.plugin.settings.configPath}` : ''}
              </div>
            `;
          }
        } catch (err) {
          configStatusDiv.innerHTML = `
            <div style="color: var(--color-green); font-weight: bold;">✅ Configuration Status</div>
            <div style="margin-top: 0.3em; font-size: 0.9em;">🔄 Configuration loaded successfully</div>
          `;
        }
      }
    } else {
      const envConfigPath = process.env.NOTEBOOKAUTOMATION_CONFIG;
      configStatusDiv.innerHTML = `
        <div style="color: var(--color-orange); font-weight: bold;">⚠️ Configuration Status</div>
        <div style="margin-top: 0.3em; font-size: 0.9em;">
          ${envConfigPath ? `🌍 NOTEBOOKAUTOMATION_CONFIG environment variable set: ${envConfigPath}<br>` : ''}
          📄 No default-config.json found in plugin directory<br>
          💡 You can create one by configuring settings below and saving
          ${this.plugin.settings.configPath ? `<br>📝 Custom config path set: ${this.plugin.settings.configPath}` : ''}
        </div>
      `;
    }

    // Always show config fields (create default structure if no config loaded)
    let configToDisplay = (window as any).notebookAutomationLoadedConfig;
    if (!configToDisplay) {
      // Create a default config structure to show empty fields
      configToDisplay = {
        paths: {},
        microsoft_graph: {},
        aiservice: {
          provider: 'azure',
          azure: {},
          openai: {},
          foundry: {},
          timeout: {},
          retry_policy: {}
        },
        video_extensions: [],
        pdf_extensions: [],
        banners: {}
      };
    }

    // Create version div first to ensure it's at the bottom
    const versionDiv = containerEl.createDiv({ cls: "notebook-automation-version" });
    versionDiv.setText("Notebook Automation version: Loading...");
    versionDiv.style.marginTop = "2em";
    versionDiv.style.textAlign = "center";
    versionDiv.style.borderTop = "1px solid var(--background-modifier-border)";
    versionDiv.style.paddingTop = "1em";
    this.getNaVersion().then(ver => {
      // Convert line feeds to HTML breaks for proper display
      const formattedVersion = ver.replace(/\n/g, '<br>');
      versionDiv.innerHTML = formattedVersion;
    });

    // Now display config fields (this will insert content before the version div)
    this.displayLoadedConfig(configToDisplay);
  }

  displayLoadedConfig(configJson: any, error?: string) {
    const { containerEl } = this;
    this.injectCustomStyles();
    // Remove previous config fields if any
    const prev = containerEl.querySelector('.notebook-automation-config-fields');
    if (prev) prev.remove();

    // Find the version div to insert content before it
    const versionDiv = containerEl.querySelector('.notebook-automation-version');

    if (error) {
      const errorDiv = containerEl.createDiv({ cls: 'notebook-automation-config-fields' });
      errorDiv.createEl('p', { text: error, cls: 'mod-warning' });
      if (versionDiv) {
        containerEl.insertBefore(errorDiv, versionDiv);
      }
      (window as any).notebookAutomationLoadedConfig = null;
      return;
    }
    if (!configJson) return;
    (window as any).notebookAutomationLoadedConfig = configJson;
    const fieldsDiv = containerEl.createDiv({ cls: 'notebook-automation-config-fields' });
    fieldsDiv.createEl('h3', { text: 'Loaded Config Fields' });

    // Insert before version div if it exists
    if (versionDiv) {
      containerEl.insertBefore(fieldsDiv, versionDiv);
    }

    const keyMeta = [
      {
        key: 'onedrive_fullpath_root',
        label: 'OneDrive Root Path',
        desc: 'The full path to the root of your OneDrive folder.',
        icon: ''
      },
      {
        key: 'notebook_vault_fullpath_root',
        label: 'Notebook Vault Root Path',
        desc: 'The full path to the root of your Obsidian notebook vault.',
        icon: ''
      },
      {
        key: 'notebook_vault_resources_basepath',
        label: 'Notebook Vault Resources Base Path',
        desc: 'The base path within your vault for resources.',
        icon: ''
      },
      {
        key: 'metadata_file',
        label: 'Metadata File',
        desc: 'The path to the metadata.yaml file used for notebook automation.',
        icon: ''
      },
      {
        key: 'onedrive_resources_basepath',
        label: 'OneDrive Resources Base Path',
        desc: 'The base path in OneDrive for education resources.',
        icon: ''
      },
      {
        key: 'prompts_path',
        label: 'Prompts Path',
        desc: 'The path to the prompts directory for automation tasks.',
        icon: ''
      },
      {
        key: 'logging_dir',
        label: 'Logging Directory',
        desc: 'The directory where logs will be written.',
        icon: ''
      },
    ];
    const paths = configJson.paths || {};
    const updatedPaths: Record<string, string> = { ...paths };

    // Add path configuration fields
    keyMeta.forEach(meta => {
      // Create a custom container instead of using Setting component
      const settingDiv = fieldsDiv.createDiv({ cls: 'setting-item notebook-automation-custom-setting' });

      // Create info section (label and description)
      const infoDiv = settingDiv.createDiv({ cls: 'setting-item-info' });
      const nameDiv = infoDiv.createDiv({ cls: 'setting-item-name' });
      nameDiv.setText(meta.label);
      const descDiv = infoDiv.createDiv({ cls: 'setting-item-description' });
      descDiv.setText(`${meta.desc} (JSON key: ${meta.key})`);

      // Create control section (input)
      const controlDiv = settingDiv.createDiv({ cls: 'setting-item-control notebook-automation-input-control' });
      const input = controlDiv.createEl('input', {
        type: 'text',
        cls: 'notebook-automation-path-input'
      });
      input.value = updatedPaths[meta.key] || '';
      input.placeholder = `Enter ${meta.label.toLowerCase()}...`;
      input.oninput = (e: any) => {
        updatedPaths[meta.key] = e.target.value;
      };
    });

    // AI Provider Configuration Section
    const aiSection = fieldsDiv.createDiv({ cls: 'notebook-automation-ai-section' });
    aiSection.createEl('h4', { text: 'AI Service Configuration', cls: 'notebook-automation-ai-header' });

    const aiConfig = configJson.aiservice || {};
    const updatedAiConfig: Record<string, any> = { ...aiConfig };

    // Available AI providers
    const aiProviders = ['azure', 'openai', 'foundry'];
    const currentProvider = aiConfig.provider || 'azure';

    // AI Provider Dropdown
    const providerSettingDiv = aiSection.createDiv({ cls: 'setting-item notebook-automation-custom-setting' });
    const providerInfoDiv = providerSettingDiv.createDiv({ cls: 'setting-item-info' });
    const providerNameDiv = providerInfoDiv.createDiv({ cls: 'setting-item-name' });
    providerNameDiv.setText('AI Provider');
    const providerDescDiv = providerInfoDiv.createDiv({ cls: 'setting-item-description' });
    providerDescDiv.setText('Select the AI service provider to use for automation tasks.');

    const providerControlDiv = providerSettingDiv.createDiv({ cls: 'setting-item-control notebook-automation-input-control' });
    const providerSelect = providerControlDiv.createEl('select', { cls: 'notebook-automation-provider-select' });

    aiProviders.forEach(provider => {
      const option = providerSelect.createEl('option', { value: provider, text: provider.toUpperCase() });
      if (provider === currentProvider) {
        option.selected = true;
      }
    });

    // Provider-specific configuration fields container
    const providerFieldsDiv = aiSection.createDiv({ cls: 'notebook-automation-provider-fields' });

    // Function to update provider fields based on selection
    const updateProviderFields = (provider: string) => {
      providerFieldsDiv.empty();

      const providerConfigs = {
        azure: [
          { key: 'endpoint', label: 'Azure OpenAI Endpoint', desc: 'The Azure OpenAI service endpoint URL', type: 'text' },
          { key: 'deployment', label: 'Deployment Name', desc: 'The deployment name for your Azure OpenAI model', type: 'text' },
          { key: 'model', label: 'Model Name', desc: 'The name of the AI model to use', type: 'text' }
        ],
        openai: [
          { key: 'endpoint', label: 'OpenAI Endpoint', desc: 'The OpenAI API endpoint URL', type: 'text' },
          { key: 'model', label: 'Model Name', desc: 'The OpenAI model to use (e.g., gpt-4o, gpt-3.5-turbo)', type: 'text' }
        ],
        foundry: [
          { key: 'endpoint', label: 'Foundry Endpoint', desc: 'The Foundry LLM endpoint URL', type: 'text' },
          { key: 'model', label: 'Model Name', desc: 'The Foundry model name to use', type: 'text' }
        ]
      };

      const fields = providerConfigs[provider as keyof typeof providerConfigs] || [];

      fields.forEach(field => {
        const fieldDiv = providerFieldsDiv.createDiv({ cls: 'setting-item notebook-automation-custom-setting' });

        const fieldInfoDiv = fieldDiv.createDiv({ cls: 'setting-item-info' });
        const fieldNameDiv = fieldInfoDiv.createDiv({ cls: 'setting-item-name' });
        fieldNameDiv.setText(field.label);
        const fieldDescDiv = fieldInfoDiv.createDiv({ cls: 'setting-item-description' });
        fieldDescDiv.setText(field.desc);

        const fieldControlDiv = fieldDiv.createDiv({ cls: 'setting-item-control notebook-automation-input-control' });
        const fieldInput = fieldControlDiv.createEl('input', {
          type: field.type,
          cls: 'notebook-automation-path-input'
        });

        // Get value from nested provider config
        const providerConfig = updatedAiConfig[provider] || {};
        fieldInput.value = providerConfig[field.key] || '';
        fieldInput.placeholder = `Enter ${field.label.toLowerCase()}...`;
        fieldInput.oninput = (e: any) => {
          if (!updatedAiConfig[provider]) {
            updatedAiConfig[provider] = {};
          }
          updatedAiConfig[provider][field.key] = e.target.value;
        };
      });
    };

    // Initialize provider fields
    updateProviderFields(currentProvider);

    // Handle provider selection change
    providerSelect.onchange = (e: any) => {
      const selectedProvider = e.target.value;
      updatedAiConfig.provider = selectedProvider;
      updateProviderFields(selectedProvider);
    };

    // Timeout Configuration
    const timeoutSection = aiSection.createDiv({ cls: 'notebook-automation-additional-fields' });
    timeoutSection.createEl('h5', { text: 'Timeout Configuration', cls: 'notebook-automation-sub-header' });

    const timeoutConfig = aiConfig.timeout || {};
    const timeoutFields = [
      { key: 'timeout_milliseconds', label: 'Timeout (milliseconds)', desc: 'Request timeout in milliseconds', type: 'number', default: 120000 },
      { key: 'max_file_parallelism', label: 'Max File Parallelism', desc: 'Maximum number of files to process in parallel', type: 'number', default: 4 },
      { key: 'file_rate_limit_ms', label: 'File Rate Limit (ms)', desc: 'Minimum delay between file processing in milliseconds', type: 'number', default: 200 }
    ];

    timeoutFields.forEach(field => {
      const fieldDiv = timeoutSection.createDiv({ cls: 'setting-item notebook-automation-custom-setting' });

      const fieldInfoDiv = fieldDiv.createDiv({ cls: 'setting-item-info' });
      const fieldNameDiv = fieldInfoDiv.createDiv({ cls: 'setting-item-name' });
      fieldNameDiv.setText(field.label);
      const fieldDescDiv = fieldInfoDiv.createDiv({ cls: 'setting-item-description' });
      fieldDescDiv.setText(field.desc);

      const fieldControlDiv = fieldDiv.createDiv({ cls: 'setting-item-control notebook-automation-input-control' });
      const fieldInput = fieldControlDiv.createEl('input', {
        type: field.type,
        cls: 'notebook-automation-path-input'
      });
      fieldInput.value = timeoutConfig[field.key] || field.default.toString();
      fieldInput.placeholder = `Enter ${field.label.toLowerCase()}...`;
      fieldInput.oninput = (e: any) => {
        if (!updatedAiConfig.timeout) {
          updatedAiConfig.timeout = {};
        }
        const value = field.type === 'number' ? parseInt(e.target.value) || field.default : e.target.value;
        updatedAiConfig.timeout[field.key] = value;
      };
    });

    // Retry Policy Configuration
    const retrySection = aiSection.createDiv({ cls: 'notebook-automation-additional-fields' });
    retrySection.createEl('h5', { text: 'Retry Policy', cls: 'notebook-automation-sub-header' });

    const retryConfig = aiConfig.retry_policy || {};
    const retryFields = [
      { key: 'max_retry_attempts', label: 'Max Retry Attempts', desc: 'Maximum number of retry attempts for failed requests', type: 'number', default: 3 },
      { key: 'delay_between_retries', label: 'Delay Between Retries (ms)', desc: 'Delay in milliseconds between retry attempts', type: 'number', default: 1000 }
    ];

    retryFields.forEach(field => {
      const fieldDiv = retrySection.createDiv({ cls: 'setting-item notebook-automation-custom-setting' });

      const fieldInfoDiv = fieldDiv.createDiv({ cls: 'setting-item-info' });
      const fieldNameDiv = fieldInfoDiv.createDiv({ cls: 'setting-item-name' });
      fieldNameDiv.setText(field.label);
      const fieldDescDiv = fieldInfoDiv.createDiv({ cls: 'setting-item-description' });
      fieldDescDiv.setText(field.desc);

      const fieldControlDiv = fieldDiv.createDiv({ cls: 'setting-item-control notebook-automation-input-control' });
      const fieldInput = fieldControlDiv.createEl('input', {
        type: field.type,
        cls: 'notebook-automation-path-input'
      });
      fieldInput.value = retryConfig[field.key] || field.default.toString();
      fieldInput.placeholder = `Enter ${field.label.toLowerCase()}...`;
      fieldInput.oninput = (e: any) => {
        if (!updatedAiConfig.retry_policy) {
          updatedAiConfig.retry_policy = {};
        }
        const value = field.type === 'number' ? parseInt(e.target.value) || field.default : e.target.value;
        updatedAiConfig.retry_policy[field.key] = value;
      };
    });

    // Microsoft Graph Configuration Section
    const graphSection = fieldsDiv.createDiv({ cls: 'notebook-automation-ai-section' });
    graphSection.createEl('h4', { text: 'Microsoft Graph Configuration', cls: 'notebook-automation-ai-header' });

    const graphConfig = configJson.microsoft_graph || {};
    const updatedGraphConfig: Record<string, any> = { ...graphConfig };

    const graphFields = [
      { key: 'client_id', label: 'Client ID', desc: 'Microsoft Graph application client ID', type: 'text' },
      { key: 'api_endpoint', label: 'API Endpoint', desc: 'Microsoft Graph API endpoint URL', type: 'text' },
      { key: 'authority', label: 'Authority', desc: 'Microsoft authentication authority URL', type: 'text' }
    ];

    graphFields.forEach(field => {
      const fieldDiv = graphSection.createDiv({ cls: 'setting-item notebook-automation-custom-setting' });

      const fieldInfoDiv = fieldDiv.createDiv({ cls: 'setting-item-info' });
      const fieldNameDiv = fieldInfoDiv.createDiv({ cls: 'setting-item-name' });
      fieldNameDiv.setText(field.label);
      const fieldDescDiv = fieldInfoDiv.createDiv({ cls: 'setting-item-description' });
      fieldDescDiv.setText(field.desc);

      const fieldControlDiv = fieldDiv.createDiv({ cls: 'setting-item-control notebook-automation-input-control' });
      const fieldInput = fieldControlDiv.createEl('input', {
        type: field.type,
        cls: 'notebook-automation-path-input'
      });
      fieldInput.value = updatedGraphConfig[field.key] || '';
      fieldInput.placeholder = `Enter ${field.label.toLowerCase()}...`;
      fieldInput.oninput = (e: any) => {
        updatedGraphConfig[field.key] = e.target.value;
      };
    });

    // Scopes configuration
    const scopesDiv = graphSection.createDiv({ cls: 'setting-item notebook-automation-custom-setting' });
    const scopesInfoDiv = scopesDiv.createDiv({ cls: 'setting-item-info' });
    const scopesNameDiv = scopesInfoDiv.createDiv({ cls: 'setting-item-name' });
    scopesNameDiv.setText('Scopes');
    const scopesDescDiv = scopesInfoDiv.createDiv({ cls: 'setting-item-description' });
    scopesDescDiv.setText('Microsoft Graph API scopes (one per line)');

    const scopesControlDiv = scopesDiv.createDiv({ cls: 'setting-item-control notebook-automation-input-control' });
    const scopesTextarea = scopesControlDiv.createEl('textarea', {
      cls: 'notebook-automation-path-input'
    });
    scopesTextarea.rows = 3;
    scopesTextarea.value = (updatedGraphConfig.scopes || []).join('\n');
    scopesTextarea.placeholder = 'Enter scopes (one per line)...';
    scopesTextarea.oninput = (e: any) => {
      updatedGraphConfig.scopes = e.target.value.split('\n').filter((scope: string) => scope.trim().length > 0);
    };

    // Other Configuration Section
    const otherSection = fieldsDiv.createDiv({ cls: 'notebook-automation-ai-section' });
    otherSection.createEl('h4', { text: 'Other Configuration', cls: 'notebook-automation-ai-header' });

    // Video extensions
    const videoExtDiv = otherSection.createDiv({ cls: 'setting-item notebook-automation-custom-setting' });
    const videoExtInfoDiv = videoExtDiv.createDiv({ cls: 'setting-item-info' });
    const videoExtNameDiv = videoExtInfoDiv.createDiv({ cls: 'setting-item-name' });
    videoExtNameDiv.setText('Video Extensions');
    const videoExtDescDiv = videoExtInfoDiv.createDiv({ cls: 'setting-item-description' });
    videoExtDescDiv.setText('Supported video file extensions (one per line)');

    const videoExtControlDiv = videoExtDiv.createDiv({ cls: 'setting-item-control notebook-automation-input-control' });
    const videoExtTextarea = videoExtControlDiv.createEl('textarea', {
      cls: 'notebook-automation-path-input'
    });
    videoExtTextarea.rows = 4;
    videoExtTextarea.value = (configJson.video_extensions || []).join('\n');
    videoExtTextarea.placeholder = 'Enter video extensions (one per line)...';

    // PDF extensions
    const pdfExtDiv = otherSection.createDiv({ cls: 'setting-item notebook-automation-custom-setting' });
    const pdfExtInfoDiv = pdfExtDiv.createDiv({ cls: 'setting-item-info' });
    const pdfExtNameDiv = pdfExtInfoDiv.createDiv({ cls: 'setting-item-name' });
    pdfExtNameDiv.setText('PDF Extensions');
    const pdfExtDescDiv = pdfExtInfoDiv.createDiv({ cls: 'setting-item-description' });
    pdfExtDescDiv.setText('Supported PDF file extensions (one per line)');

    const pdfExtControlDiv = pdfExtDiv.createDiv({ cls: 'setting-item-control notebook-automation-input-control' });
    const pdfExtTextarea = pdfExtControlDiv.createEl('textarea', {
      cls: 'notebook-automation-path-input'
    });
    pdfExtTextarea.rows = 2;
    pdfExtTextarea.value = (configJson.pdf_extensions || []).join('\n');
    pdfExtTextarea.placeholder = 'Enter PDF extensions (one per line)...';

    // Banners Configuration Section
    const bannersSection = fieldsDiv.createDiv({ cls: 'notebook-automation-ai-section' });
    bannersSection.createEl('h4', { text: 'Banners Configuration', cls: 'notebook-automation-ai-header' });

    const bannersConfig = configJson.banners || {};
    const updatedBannersConfig: Record<string, any> = { ...bannersConfig };

    // Default banner and format
    const bannerFields = [
      { key: 'default', label: 'Default Banner', desc: 'Default banner image filename' },
      { key: 'format', label: 'Banner Format', desc: 'Banner format (e.g., image)' }
    ];

    bannerFields.forEach(field => {
      const fieldDiv = bannersSection.createDiv({ cls: 'setting-item notebook-automation-custom-setting' });

      const fieldInfoDiv = fieldDiv.createDiv({ cls: 'setting-item-info' });
      const fieldNameDiv = fieldInfoDiv.createDiv({ cls: 'setting-item-name' });
      fieldNameDiv.setText(field.label);
      const fieldDescDiv = fieldInfoDiv.createDiv({ cls: 'setting-item-description' });
      fieldDescDiv.setText(field.desc);

      const fieldControlDiv = fieldDiv.createDiv({ cls: 'setting-item-control notebook-automation-input-control' });
      const fieldInput = fieldControlDiv.createEl('input', {
        type: 'text',
        cls: 'notebook-automation-path-input'
      });
      fieldInput.value = updatedBannersConfig[field.key] || '';
      fieldInput.placeholder = `Enter ${field.label.toLowerCase()}...`;
      fieldInput.oninput = (e: any) => {
        updatedBannersConfig[field.key] = e.target.value;
      };
    });
    // Save button for config fields (always on its own line)
    const saveSetting = new Setting(fieldsDiv);
    saveSetting.settingEl.style.marginTop = "1.2em";
    saveSetting.addButton(btn => {
      btn.setButtonText('💾 Save Default Config')
        .setCta()
        .onClick(async () => {
          // Validate at least one path is set
          if (!Object.values(updatedPaths).some(v => v && v.trim().length > 0)) {
            new Notice('At least one config path must be set.');
            return;
          }

          try {
            // @ts-ignore
            const fs = window.require ? window.require('fs') : null;
            // @ts-ignore
            const path = window.require ? window.require('path') : null;
            if (!fs || !path) {
              new Notice('File system access is not available in this environment.');
              return;
            }

            // Get plugin directory
            const pluginDir = this.plugin.manifest?.dir;
            if (!pluginDir) {
              new Notice('Cannot determine plugin directory.');
              return;
            }

            // Resolve plugin directory path
            let resolvedPluginDir = pluginDir;
            const adapter = this.plugin.app?.vault?.adapter;
            // @ts-ignore
            if (adapter && typeof adapter.getBasePath === 'function') {
              try {
                // @ts-ignore
                const vaultRoot = adapter.getBasePath();
                if (vaultRoot && !path.isAbsolute(pluginDir)) {
                  resolvedPluginDir = path.join(vaultRoot, pluginDir);
                }
              } catch (err) {
                console.log('[Notebook Automation] Error getting vault root for config save:', err);
              }
            }

            const defaultConfigPath = path.join(resolvedPluginDir, 'default-config.json');

            // Build complete configuration object matching the original structure
            const defaultConfig = {
              ConfigFilePath: this.plugin.settings.configPath || '',
              DebugEnabled: this.plugin.settings.debug || false,
              paths: { ...updatedPaths },
              microsoft_graph: {
                ...updatedGraphConfig,
                scopes: updatedGraphConfig.scopes || []
              },
              aiservice: {
                provider: updatedAiConfig.provider || 'azure',
                ...Object.keys(updatedAiConfig).reduce((acc, key) => {
                  if (key !== 'provider' && key !== 'timeout' && key !== 'retry_policy') {
                    acc[key] = updatedAiConfig[key];
                  }
                  return acc;
                }, {} as any),
                timeout: updatedAiConfig.timeout || {
                  timeout_milliseconds: 120000,
                  max_file_parallelism: 4,
                  file_rate_limit_ms: 200
                },
                retry_policy: updatedAiConfig.retry_policy || {
                  max_retry_attempts: 3,
                  delay_between_retries: 1000
                }
              },
              video_extensions: videoExtTextarea.value.split('\n').filter(ext => ext.trim().length > 0),
              pdf_extensions: pdfExtTextarea.value.split('\n').filter(ext => ext.trim().length > 0),
              pdf_extract_images: this.plugin.settings.pdfExtractImages || false,
              banners: {
                enabled: this.plugin.settings.bannersEnabled || false,
                ...updatedBannersConfig,
                template_banners: updatedBannersConfig.template_banners || {
                  main: "gies-banner.png",
                  program: "gies-banner.png",
                  course: "gies-banner.png"
                },
                filename_patterns: updatedBannersConfig.filename_patterns || {
                  "*index*": "gies-banner.png",
                  "*main*": "gies-banner.png"
                }
              }
            };

            // Write default-config.json to plugin directory
            fs.writeFileSync(defaultConfigPath, JSON.stringify(defaultConfig, null, 4), 'utf8');
            new Notice('✅ Default config saved successfully to plugin directory.');

            // Also update the seeded config if it exists
            if (this.plugin.settings.configPath) {
              try {
                configJson.paths = { ...updatedPaths };
                configJson.aiservice = defaultConfig.aiservice;
                configJson.microsoft_graph = defaultConfig.microsoft_graph;
                configJson.DebugEnabled = defaultConfig.DebugEnabled;
                configJson.video_extensions = defaultConfig.video_extensions;
                configJson.pdf_extensions = defaultConfig.pdf_extensions;
                configJson.pdf_extract_images = defaultConfig.pdf_extract_images;
                configJson.banners = defaultConfig.banners;

                fs.writeFileSync(this.plugin.settings.configPath, JSON.stringify(configJson, null, 4), 'utf8');
                new Notice('✅ Seeded config also updated successfully.');
              } catch (err) {
                console.log('[Notebook Automation] Could not update seeded config:', err);
                new Notice('⚠️ Default config saved, but could not update seeded config.');
              }
            }

            // Update global loaded config
            (window as any).notebookAutomationLoadedConfig = defaultConfig;

          } catch (err) {
            console.error('[Notebook Automation] Error saving config:', err);
            new Notice('Failed to save config: ' + (err instanceof Error ? err.message : String(err)));
          }
        });
    });
  }
}
