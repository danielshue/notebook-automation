/**
 * Given a full vault path, strip the notebook_vault_fullpath_root and vault_resources_basepath prefix and return the relative path for OneDrive mapping.
 * @param fullPath The full path to the file/folder in the vault
 * @parconst DEFAULT_SETTINGS: NotebookAutomationSettings = {
  configPath: '',
  verbose: false,
  debug: false,
  dryRun: false,
  enableVideoSummary: true,
  enablePdfSummary: true,
  enableIndexCreation: true,
};ltRoot The notebook_vault_fullpath_root from config
 * @param vaultBase Optional vault_resources_basepath from config
 * @returns The relative path for OneDrive mapping
 */
function getRelativeVaultResourcePath(fullPath: string, vaultRoot: string, vaultBase?: string): string {
  // Normalize slashes for cross-platform compatibility
  let normFull = fullPath.replace(/\\/g, '/');
  let normRoot = vaultRoot.replace(/\\/g, '/').replace(/\/$/, '');
  let normBase = (vaultBase || '').replace(/\\/g, '/').replace(/^\//, '').replace(/\/$/, '');
  // Remove vaultRoot if present
  if (normRoot && normFull.startsWith(normRoot)) {
    normFull = normFull.substring(normRoot.length);
    if (normFull.startsWith('/')) normFull = normFull.substring(1);
  }
  // Remove vaultBase if present
  if (normBase && normFull.startsWith(normBase)) {
    normFull = normFull.substring(normBase.length);
    if (normFull.startsWith('/')) normFull = normFull.substring(1);
  }
  return normFull;
}
/**
 * Utility to resolve the correct executable name for the current platform.
 */
function getNaExecutableName(): string {
  const platform = process?.platform || (window?.process && window.process.platform);
  if (platform === "win32") {
    return "na.exe";
  }
  return "na";
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
    // Also try vault.getRoot() for additional validation
    const vaultRootFolder = plugin.app?.vault?.getRoot?.();
    if (vaultRootFolder) {
      // eslint-disable-next-line no-console
      console.log('[Notebook Automation] vault.getRoot() returned:', vaultRootFolder.path, 'name:', vaultRootFolder.name);
    } else {
      // eslint-disable-next-line no-console
      console.log('[Notebook Automation] vault.getRoot() not available or returned null');
    }
    // Log final vaultRoot value
    // eslint-disable-next-line no-console
    console.log('[Notebook Automation] Final vaultRoot value:', vaultRoot);
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
      let resolvedDir = plugin.manifest.dir;
      // Special case: if manifest.dir starts with / but is actually relative (like /.obsidian/...)
      // This happens when manifest.dir is incorrectly set to a path rooted at /
      if (resolvedDir.startsWith('/.obsidian') || resolvedDir.startsWith('/.') || (resolvedDir.startsWith('/') && !fs?.existsSync?.(resolvedDir))) {
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
      const resolved = path.join(resolvedDir, execName);
      // Check if file exists
      const exists = fs && fs.existsSync && fs.existsSync(resolved);
      // eslint-disable-next-line no-console
      console.log(`[Notebook Automation] Using plugin.manifest.dir for naPath: ${resolved} (exists: ${exists})`);
      if (exists) {
        return resolved;
      } else {
        // eslint-disable-next-line no-console
        console.log('[Notebook Automation] File does not exist at plugin.manifest.dir path, will fallback.');
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
        const resolved = path.join(pluginDir, execName);
        // Check if file exists
        const exists = fs && fs.existsSync && fs.existsSync(resolved);
        // eslint-disable-next-line no-console
        console.log(`[Notebook Automation] Using FileSystemAdapter fallback for naPath: ${resolved} (exists: ${exists})`);
        return resolved;
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
      const resolved = path.join(__dirname, execName);
      const exists = fs && fs.existsSync && fs.existsSync(resolved);
      // eslint-disable-next-line no-console
      console.log(`[Notebook Automation] Using __dirname fallback for naPath: ${resolved} (exists: ${exists})`);
      return resolved;
    }
    // Final safety check: if we're still returning a path that starts with / but isn't properly absolute
    // (like /.obsidian/plugins/...), force it to use the fallback construction
    // eslint-disable-next-line no-console
    console.log('[Notebook Automation] Using execName fallback for naPath:', execName);
    // Before returning execName, try one last attempt to construct absolute path
    if (vaultRoot && plugin.manifest?.id) {
      const configDir = plugin.app?.vault?.configDir || '.obsidian';
      const pluginId = plugin.manifest.id;
      const lastResortPath = path ? path.join(vaultRoot, configDir, 'plugins', pluginId, execName) : execName;
      // eslint-disable-next-line no-console
      console.log('[Notebook Automation] Last resort absolute path attempt:', lastResortPath);
      const exists = fs && fs.existsSync && fs.existsSync(lastResortPath);
      if (exists) {
        // eslint-disable-next-line no-console
        console.log('[Notebook Automation] Last resort path exists, using it instead of execName');
        return lastResortPath;
      }
    }
    return execName;
  } catch {
    // eslint-disable-next-line no-console
    console.log('[Notebook Automation] Exception in getNaExecutablePath, using execName fallback:', execName);
    return execName;
  }
}
import { Modal, TFile, TFolder } from "obsidian";



import { Plugin, Notice, PluginSettingTab, App, Setting } from "obsidian";

interface NotebookAutomationSettings {
  configPath: string;
  verbose?: boolean;
  debug?: boolean;
  dryRun?: boolean;
  force?: boolean;
  enableVideoSummary?: boolean;
  enablePdfSummary?: boolean;
  enableIndexCreation?: boolean;
  enableEnsureMetadata?: boolean;
}


const DEFAULT_SETTINGS: NotebookAutomationSettings = {
  configPath: "",
  verbose: false,
  debug: false,
  dryRun: false,
  force: false,
  enableVideoSummary: true,
  enablePdfSummary: true,
  enableIndexCreation: true,
  enableEnsureMetadata: true,
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
          
          // AI Video Summary - only if enabled
          if (this.settings.enableVideoSummary) {
            menu.addItem((item) => {
              item.setTitle("Notebook Automation: Import & AI Summarize All Videos")
                .setIcon("video-file")
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
              item.setTitle("Notebook Automation: Build Index for This Folder")
                .setIcon("list")
                .onClick(() => this.handleNotebookAutomationCommand(file, "build-index"));
            });
            menu.addItem((item) => {
              item.setTitle("Notebook Automation: Build Indexes for This Folder and All Subfolders")
                .setIcon("layers")
                .onClick(() => this.handleNotebookAutomationCommand(file, "build-index-recursive"));
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
                .setIcon("video-file")
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
    // Get config for vault root and base
    let vaultRoot = "";
    let vaultBase = "";
    try {
      // Try to get loaded config from settings tab
      const loaded = (window as any).notebookAutomationLoadedConfig;
      if (loaded?.paths?.notebook_vault_fullpath_root) {
        vaultRoot = loaded.paths.notebook_vault_fullpath_root;
        vaultBase = loaded.paths?.notebook_vault_resources_basepath || "";
      } else {
        // Fallback: read config.json directly (synchronously)
        // @ts-ignore
        const fs = window.require ? window.require('fs') : null;
        if (fs) {
          let configPath = this.settings.configPath;
          if (!configPath) configPath = 'config.json';
          if (fs.existsSync(configPath)) {
            const content = fs.readFileSync(configPath, 'utf8');
            const config = JSON.parse(content);
            vaultRoot = config.paths?.notebook_vault_fullpath_root || "";
            vaultBase = config.paths?.notebook_vault_resources_basepath || "";
          }
        }
      }
    } catch {}
    // Debug log vaultRoot, vaultBase, and file.path
    // eslint-disable-next-line no-console
    console.log(`[Notebook Automation] vaultRoot: ${vaultRoot}`);
    console.log(`[Notebook Automation] vaultBase: ${vaultBase}`);
    console.log(`[Notebook Automation] file.path: ${file.path}`);
    const relPath = getRelativeVaultResourcePath(file.path, vaultRoot, vaultBase);
    // Log to the developer console
    // eslint-disable-next-line no-console
    console.log(`[Notebook Automation] Command '${action}' triggered for: ${file.path}`);
    console.log(`[Notebook Automation] Relative path for OneDrive mapping: ${relPath}`);
    // Check if config is loaded
    if (!this.settings.configPath) {
      new Notice("Please configure the config file path in plugin settings first.");
      return;
    }
    try {
      await this.executeNotebookAutomationCommand(action, relPath);
    } catch (error) {
      // eslint-disable-next-line no-console
      console.error(`[Notebook Automation] Error executing command:`, error);
      new Notice(`Error executing command: ${error?.message || error}`);
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
    const configPath = this.settings.configPath;
    
    // Build command arguments based on action
    let args: string[] = [];
    let commandDescription = "";
    
    switch (action) {
      case "import-summarize-videos":
        args = ["video-notes", "--input", relativePath, "--config", `"${configPath}"`];
        commandDescription = "Import & AI Summarize Videos";
        break;
      case "import-summarize-pdfs":
        args = ["pdf-notes", "--input", relativePath, "--config", `"${configPath}"`];
        commandDescription = "Import & AI Summarize PDFs";
        break;
      case "build-index":
        args = ["build-index", "--input", relativePath, "--config", `"${configPath}"`];
        commandDescription = "Build Index";
        break;
      case "build-index-recursive":
        args = ["build-index", "--input", relativePath, "--config", `"${configPath}"`, "--recursive"];
        commandDescription = "Build Index (Recursive)";
        break;
      case "reprocess-summary-video":
        args = ["video-notes", "--input", relativePath, "--reprocess", "--config", `"${configPath}"`];
        commandDescription = "Reprocess Video Summary";
        break;
      case "reprocess-summary-pdf":
        args = ["pdf-notes", "--input", relativePath, "--reprocess", "--config", `"${configPath}"`];
        commandDescription = "Reprocess PDF Summary";
        break;
      case "ensure-metadata":
        args = ["ensure-metadata", "--input", relativePath, "--config", `"${configPath}"`];
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
      const [command, ...cmdArgs] = fullCommand.match(/(?:[^\s"]+|"[^"]*")+/g).map(arg => arg.replace(/^"(.*)"$/, '$1'));
      
      // eslint-disable-next-line no-console
      console.log(`[Notebook Automation] Executing asynchronously - command: ${command}, args:`, cmdArgs);
      
      const childProcess = spawn(command, cmdArgs, {
        env: env,
        stdio: ['pipe', 'pipe', 'pipe']
      });
      
      let stdout = '';
      let stderr = '';
      
      // Collect stdout data
      childProcess.stdout.on('data', (data) => {
        const chunk = data.toString();
        stdout += chunk;
        // eslint-disable-next-line no-console
        console.log(`[Notebook Automation] STDOUT chunk:`, chunk);
      });
      
      // Collect stderr data
      childProcess.stderr.on('data', (data) => {
        const chunk = data.toString();
        stderr += chunk;
        // eslint-disable-next-line no-console
        console.log(`[Notebook Automation] STDERR chunk:`, chunk);
      });
      
      // Handle process completion
      const exitPromise = new Promise<void>((resolve, reject) => {
        childProcess.on('close', (code) => {
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
        
        childProcess.on('error', (error) => {
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
      const stderr = error?.stderr?.toString() || '';
      const stdout = error?.stdout?.toString() || '';
      const exitCode = error?.code || error?.status || 'unknown';
      
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
        const lines = stdout.split('\n').filter(line => line.trim().length > 0);
        const lastLine = lines[lines.length - 1];
        if (lastLine && lastLine.includes('ERR')) {
          errorMsg = lastLine.replace(/\[[^\]]*\]/, '').trim(); // Remove timestamp
        }
      } else if (stderr) {
        errorMsg = stderr;
      } else if (error?.message) {
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
  /**
   * Injects custom CSS for the settings tab if not already present.
   */
  injectCustomStyles() {
    const styleId = 'notebook-automation-settings-style';
    if (document.getElementById(styleId)) return;
    const style = document.createElement('style');
    style.id = styleId;
    style.textContent = `
      .notebook-automation-version {
        font-size: 1.1em;
        font-weight: 500;
        margin-bottom: 1.2em;
        color: var(--text-accent);
      }
      .notebook-automation-config-fields {
        margin-top: 2em;
        margin-bottom: 2em;
        padding: 1.2em 1.5em 1.5em 1.5em;
        background: var(--background-secondary-alt);
        border-radius: 8px;
        box-shadow: 0 1px 4px rgba(0,0,0,0.04);
      }
      .notebook-automation-config-fields h3 {
        margin-top: 0;
        margin-bottom: 1.2em;
        font-size: 1.15em;
        font-weight: 600;
        color: var(--text-normal);
      }
      .notebook-automation-config-fields .notebook-automation-custom-setting {
        margin-bottom: 1.5em;
        display: block !important;
      }
      .notebook-automation-config-fields .notebook-automation-custom-setting .setting-item-info {
        margin-bottom: 0.8em !important;
        display: block !important;
      }
      .notebook-automation-config-fields .notebook-automation-input-control {
        display: block !important;
        width: 100% !important;
        max-width: none !important;
        margin-top: 0.5em !important;
      }
      .notebook-automation-config-fields .notebook-automation-custom-setting {
        display: block !important;
        width: 100% !important;
        flex-direction: column !important;
        margin-bottom: 2em !important;
      }
      .notebook-automation-config-fields .notebook-automation-path-input {
        width: 100% !important;
        max-width: none !important;
        min-width: 600px !important;
        font-family: var(--font-monospace);
        font-size: 1.2em;
        background: var(--background-primary-alt);
        color: var(--text-normal);
        border-radius: 8px;
        padding: 1em 1.2em;
        box-sizing: border-box;
        border: 2px solid var(--background-modifier-border);
        min-height: 3em;
        transition: border-color 0.2s ease;
        display: block !important;
        margin-top: 0.5em !important;
        margin-bottom: 0.8em !important;
      }
      .notebook-automation-config-fields .notebook-automation-path-input:focus {
        border-color: var(--interactive-accent);
        outline: none;
      }
      .notebook-automation-config-fields input[type="text"] {
        width: 100% !important;
        max-width: none !important;
        min-width: 600px !important;
        font-family: var(--font-monospace);
        font-size: 1.2em;
        background: var(--background-primary-alt);
        color: var(--text-normal);
        border-radius: 8px;
        padding: 1em 1.2em;
        box-sizing: border-box;
        border: 2px solid var(--background-modifier-border);
        min-height: 3em;
        margin-top: 0.5em;
        transition: border-color 0.2s ease;
      }
      .notebook-automation-config-fields input[type="text"]:focus {
        border-color: var(--interactive-accent);
        outline: none;
      }
      .notebook-automation-config-fields .setting-item-description {
        font-size: 0.93em;
        color: var(--text-muted);
        margin-top: 0.1em;
      }
      .notebook-automation-config-fields .mod-warning {
        color: var(--color-red);
        font-weight: 500;
        margin-bottom: 1em;
      }
      .notebook-automation-config-fields .mod-cta button {
        font-weight: 600;
        font-size: 1em;
        padding: 0.5em 1.2em;
        border-radius: 5px;
      }
    `;
    document.head.appendChild(style);
  }
  plugin: NotebookAutomationPlugin;

  constructor(app: App, plugin: NotebookAutomationPlugin) {
    super(app, plugin);
    this.plugin = plugin;
  }


  async getNaVersion(): Promise<string> {
    // Try to run na --version and parse the first line
    try {
      // @ts-ignore
      const child_process = window.require ? window.require('child_process') : null;
      const path = window.require ? window.require('path') : null;
      if (!child_process || !path) {
        // eslint-disable-next-line no-console
        console.log('[Notebook Automation] child_process or path not available');
        return "Unavailable - Node modules not accessible";
      }
      const naPath = getNaExecutablePath(this.plugin);
      const cmd = `"${naPath}" --version`;
      // eslint-disable-next-line no-console
      console.log(`[Notebook Automation] Running version command: ${cmd}`);
      // eslint-disable-next-line no-console
      console.log(`[Notebook Automation] naPath: ${naPath}`);
      const result = child_process.execSync(cmd, { encoding: 'utf8', timeout: 5000 });
      // eslint-disable-next-line no-console
      console.log(`[Notebook Automation] Command result:`, result);
      // Split into lines and find the first non-empty line that contains "version"
      const lines = result.split(/\r?\n/).map((line: string) => line.trim()).filter((line: string) => line.length > 0);
      let version = "";
      // Look for a line containing "version"
      const versionLine = lines.find((line: string) => line.toLowerCase().includes('version'));
      if (versionLine) {
        // Extract just the version part after "version "
        const versionMatch = versionLine.match(/version\s+(.+)/i);
        if (versionMatch && versionMatch[1]) {
          version = versionMatch[1].trim();
        } else {
          // Fallback: try to extract everything after "version"
          const versionIndex = versionLine.toLowerCase().indexOf('version');
          if (versionIndex !== -1) {
            version = versionLine.substring(versionIndex + 7).trim(); // 7 = length of "version"
          } else {
            version = versionLine;
          }
        }
      } else {
        version = lines[0] || "Unknown version";
      }
      // eslint-disable-next-line no-console
      console.log(`[Notebook Automation] All lines:`, lines);
      // eslint-disable-next-line no-console
      console.log(`[Notebook Automation] Selected version line:`, versionLine);
      // eslint-disable-next-line no-console
      console.log(`[Notebook Automation] Extracted version:`, version);
      return version || "Unknown version";
    } catch (err: any) {
      // eslint-disable-next-line no-console
      console.log(`[Notebook Automation] Error running na --version:`, err);
      // eslint-disable-next-line no-console
      console.log(`[Notebook Automation] Error message:`, err?.message);
      return `Error: ${err?.message || 'Unknown error'}`;
    }
  }

  async display(): Promise<void> {
    this.injectCustomStyles();
    const { containerEl } = this;
    containerEl.empty();
    containerEl.style.overflowY = "auto";
    containerEl.style.maxHeight = "80vh";

    // Show NA version at the top
    const versionDiv = containerEl.createDiv({ cls: "notebook-automation-version" });
    versionDiv.setText("Notebook Automation CLI version: Loading...");
    this.getNaVersion().then(ver => {
      versionDiv.setText(`Notebook Automation CLI version: ${ver}`);
    });

    containerEl.createEl("h2", { text: "Notebook Automation Settings" });

    // Feature toggles section - moved to top
    containerEl.createEl("h3", { text: "Feature Controls" });

    // Enable AI Video Summary
    new Setting(containerEl)
      .setName("Enable AI Video Summary")
      .setDesc("Show 'Import & AI Summarize All Videos' and 'Reprocess AI Summary (Video)' options in context menus.")
      .addToggle(toggle => {
        toggle.setValue(this.plugin.settings.enableVideoSummary ?? true)
          .onChange(async (value) => {
            this.plugin.settings.enableVideoSummary = value;
            await this.plugin.saveSettings();
          });
      });

    // Enable AI PDF Summary
    new Setting(containerEl)
      .setName("Enable AI PDF Summary")
      .setDesc("Show 'Import & AI Summarize All PDFs' and 'Reprocess AI Summary (PDF)' options in context menus.")
      .addToggle(toggle => {
        toggle.setValue(this.plugin.settings.enablePdfSummary ?? true)
          .onChange(async (value) => {
            this.plugin.settings.enablePdfSummary = value;
            await this.plugin.saveSettings();
          });
      });

    // Enable Index Creation
    new Setting(containerEl)
      .setName("Enable Index Creation")
      .setDesc("Show 'Build Index' options in context menus for folders.")
      .addToggle(toggle => {
        toggle.setValue(this.plugin.settings.enableIndexCreation ?? true)
          .onChange(async (value) => {
            this.plugin.settings.enableIndexCreation = value;
            await this.plugin.saveSettings();
          });
      });

    // Enable Ensure Metadata
    new Setting(containerEl)
      .setName("Enable Ensure Metadata")
      .setDesc("Show 'Ensure Metadata Consistency' option in context menus for folders to maintain metadata consistency across markdown files based on directory hierarchy.")
      .addToggle(toggle => {
        toggle.setValue(this.plugin.settings.enableEnsureMetadata ?? true)
          .onChange(async (value) => {
            this.plugin.settings.enableEnsureMetadata = value;
            await this.plugin.saveSettings();
          });
      });

    // Command flags section
    containerEl.createEl("h3", { text: "Command Flags" });

    // Verbose flag
    new Setting(containerEl)
      .setName("Verbose Mode")
      .setDesc("Enable verbose output for automation commands.")
      .addToggle(toggle => {
        toggle.setValue(this.plugin.settings.verbose || false)
          .onChange(async (value) => {
            this.plugin.settings.verbose = value;
            await this.plugin.saveSettings();
          });
      });

    // Debug flag
    new Setting(containerEl)
      .setName("Debug Mode")
      .setDesc("Enable debug output for troubleshooting.")
      .addToggle(toggle => {
        toggle.setValue(this.plugin.settings.debug || false)
          .onChange(async (value) => {
            this.plugin.settings.debug = value;
            await this.plugin.saveSettings();
          });
      });

    // Dry-run flag
    new Setting(containerEl)
      .setName("Dry Run")
      .setDesc("Simulate actions without making changes.")
      .addToggle(toggle => {
        toggle.setValue(this.plugin.settings.dryRun || false)
          .onChange(async (value) => {
            this.plugin.settings.dryRun = value;
            await this.plugin.saveSettings();
          });
      });

    // Force flag
    new Setting(containerEl)
      .setName("Force Mode")
      .setDesc("Force operations to proceed even when they might normally be skipped or blocked.")
      .addToggle(toggle => {
        toggle.setValue(this.plugin.settings.force || false)
          .onChange(async (value) => {
            this.plugin.settings.force = value;
            await this.plugin.saveSettings();
          });
      });

    // Configuration section
    containerEl.createEl("h3", { text: "Configuration" });

    let configJson: any = null;
    let configError: string | null = null;

    // Config file path input (on its own line)
    const configFileSetting = new Setting(containerEl)
      .setName("Config File")
      .setDesc("Enter the path to the config.json file to use for notebook automation. This can be anywhere accessible to the plugin.");
    configFileSetting.controlEl.style.display = "flex";
    configFileSetting.controlEl.style.flexDirection = "column";
    const configPathInput = document.createElement("input");
    configPathInput.type = "text";
    configPathInput.placeholder = "Path to config.json...";
    configPathInput.value = this.plugin.settings.configPath || "";
    configPathInput.style.marginBottom = "0.5em";
    configPathInput.onchange = async (e: any) => {
      this.plugin.settings.configPath = e.target.value;
      await this.plugin.saveSettings();
    };
    configFileSetting.controlEl.appendChild(configPathInput);

    // Validate & Load button (on its own line)
    const validateBtn = document.createElement("button");
    validateBtn.textContent = "Validate & Load";
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
            new Notice("Config loaded successfully.");
            this.displayLoadedConfig(configJson);
          } catch (jsonErr) {
            configError = "Invalid JSON: " + (jsonErr?.message || jsonErr);
            new Notice(configError);
            this.displayLoadedConfig(null, configError);
          }
        } else {
          configError = "Config file does not exist or is not a file.";
          new Notice(configError);
          this.displayLoadedConfig(null, configError);
        }
      } catch (err) {
        configError = "Error checking file: " + (err?.message || err);
        new Notice(configError);
        this.displayLoadedConfig(null, configError);
      }
    };
    configFileSetting.controlEl.appendChild(validateBtn);

    // If config was previously loaded and valid, show fields
    if ((window as any).notebookAutomationLoadedConfig) {
      this.displayLoadedConfig((window as any).notebookAutomationLoadedConfig);
    }
  }

  displayLoadedConfig(configJson: any, error?: string) {
    const { containerEl } = this;
    this.injectCustomStyles();
    // Remove previous config fields if any
    const prev = containerEl.querySelector('.notebook-automation-config-fields');
    if (prev) prev.remove();
    if (error) {
      const errorDiv = containerEl.createDiv({ cls: 'notebook-automation-config-fields' });
      errorDiv.createEl('p', { text: error, cls: 'mod-warning' });
      (window as any).notebookAutomationLoadedConfig = null;
      return;
    }
    if (!configJson) return;
    (window as any).notebookAutomationLoadedConfig = configJson;
    const fieldsDiv = containerEl.createDiv({ cls: 'notebook-automation-config-fields' });
    fieldsDiv.createEl('h3', { text: 'Loaded Config Fields' });
    const keyMeta = [
      {
        key: 'onedrive_fullpath_root',
        label: 'OneDrive Root Path',
        desc: 'The full path to the root of your OneDrive folder.',
      },
      {
        key: 'notebook_vault_fullpath_root',
        label: 'Notebook Vault Root Path',
        desc: 'The full path to the root of your Obsidian notebook vault.',
      },
      {
        key: 'metadata_file',
        label: 'Metadata File',
        desc: 'The path to the metadata.yaml file used for notebook automation.',
      },
      {
        key: 'onedrive_resources_basepath',
        label: 'OneDrive Resources Base Path',
        desc: 'The base path in OneDrive for education resources.',
      },
      {
        key: 'prompts_path',
        label: 'Prompts Path',
        desc: 'The path to the prompts directory for automation tasks.',
      },
      {
        key: 'logging_dir',
        label: 'Logging Directory',
        desc: 'The directory where logs will be written.',
      },
    ];
    const paths = configJson.paths || {};
    const updatedPaths: Record<string, string> = { ...paths };
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
    // Save button for config fields (always on its own line)
    const saveSetting = new Setting(fieldsDiv);
    saveSetting.settingEl.style.marginTop = "1.2em";
    saveSetting.addButton(btn => {
      btn.setButtonText('Save Config Changes')
        .setCta()
        .onClick(async () => {
          // Validate at least one path is set
          if (!Object.values(updatedPaths).some(v => v && v.trim().length > 0)) {
            new Notice('At least one config path must be set.');
            return;
          }
          // Save to config.json
          try {
            // @ts-ignore
            const fs = window.require ? window.require('fs') : null;
            if (!fs) {
              new Notice('File system access is not available in this environment.');
              return;
            }
            const configPath = this.plugin.settings.configPath;
            if (!configPath) {
              new Notice('Config file path is not set in plugin settings.');
              return;
            }
            // Update configJson in memory
            configJson.paths = { ...updatedPaths };
            // Write to disk
            fs.writeFileSync(configPath, JSON.stringify(configJson, null, 2), 'utf8');
            new Notice('Config updated and saved successfully.');
            // Update global loaded config
            (window as any).notebookAutomationLoadedConfig = configJson;
          } catch (err) {
            new Notice('Failed to save config: ' + (err?.message || err));
          }
        });
    });
  }
  }
