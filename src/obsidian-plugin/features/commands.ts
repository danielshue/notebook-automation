import { TFolder, TFile, Notice } from 'obsidian';
import type NotebookAutomationPlugin from '../main';
import { getRelativeVaultResourcePath, ensureExecutableExists } from '../utils/na-executable';

// Handles notebook automation commands for a given file/folder and action
export async function handleNotebookAutomationCommand(plugin: NotebookAutomationPlugin, file: TFile | TFolder, action: string) {
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
        let pluginDir = plugin.manifest?.dir;
        console.log('[Notebook Automation] [DEBUG] pluginDir:', pluginDir);
        if (pluginDir) {
          const adapter = plugin.app?.vault?.adapter;
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
      if (!configPath && plugin.settings.configPath) {
        configPath = plugin.settings.configPath;
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
  
  const relPath = getRelativeVaultResourcePath(file.path, vaultRoot, vaultBase);
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
        let pluginDir = plugin.manifest?.dir;
        if (pluginDir) {
          const adapter = plugin.app?.vault?.adapter;
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
  if (!hasConfig && plugin.settings.configPath) {
    try {
      // @ts-ignore
      const fs = window.require ? window.require('fs') : null;
      if (fs && fs.existsSync(plugin.settings.configPath)) {
        hasConfig = true;
      }
    } catch (err) {
      // Continue
    }
  }
  
  if (!hasConfig) {
    new Notice("❌ No configuration file found. Please set up configuration in plugin settings.");
    return;
  }
  
  try {
    await executeNotebookAutomationCommand(plugin, action, relPath);
    new Notice(`✅ ${action} completed successfully`);
  } catch (error) {
    console.error(`[Notebook Automation] Error executing command '${action}':`, error);
    new Notice(`❌ Error executing ${action}: ${error instanceof Error ? error.message : String(error)}`);
  }
}

export async function executeNotebookAutomationCommand(plugin: NotebookAutomationPlugin, action: string, relativePath: string, opts?: { force?: boolean }) {
  // @ts-ignore
  const child_process = window.require ? window.require('child_process') : null;
  if (!child_process) {
    throw new Error("Child process module not available");
  }
  
  const naPath = await ensureExecutableExists(plugin);
  
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
        let pluginDir = plugin.manifest?.dir;
        if (pluginDir) {
          // Resolve plugin directory path
          const adapter = plugin.app?.vault?.adapter;
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
  if (!configPath && plugin.settings.configPath) {
    configPath = plugin.settings.configPath || '';
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
    case "build-indexes":
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
    case "open-onedrive-folder":
      args = ["vault", "open-onedrive", relativePath, "--config", configPath];
      commandDescription = "Open OneDrive Folder";
      break;
    case "open-local-folder":
      args = ["vault", "open-local", relativePath, "--config", configPath];
      commandDescription = "Open Local Folder";
      break;
    default:
      throw new Error(`Unknown action: ${action}`);
  }
  
  // Add optional flags based on settings
  if (plugin.settings.verbose) {
    args.push("--verbose");
  }
  if (plugin.settings.debug) {
    args.push("--debug");
  }
  if (plugin.settings.dryRun) {
    args.push("--dry-run");
  }
  if (plugin.settings.force) {
    args.push("--force");
  }
  if (plugin.settings.pdfExtractImages) {
    args.push("--pdf-extract-images");
  }
  if (!plugin.settings.oneDriveSharedLink) {
    args.push("--no-share-links");
  }
  if (plugin.settings.bannersEnabled) {
    args.push("--banners-enabled");
  }
  if (plugin.settings.unidirectionalSync) {
    args.push("--unidirectional");
  }
  
  // Add recursive flag for sync operations only
  if (action === "sync-dir" && plugin.settings.recursiveDirectorySync) {
    args.push("--recursive");
  }
  
  // Only add --force if explicitly requested by the caller (in addition to settings)
  if (opts?.force) {
    args.push("--force");
  }
  
  console.log(`[Notebook Automation] Executing: ${naPath} ${args.join(' ')}`);
  new Notice(`🔄 Starting ${commandDescription}...`);
  
  return new Promise<void>((resolve, reject) => {
    const process = child_process.spawn(naPath, args, {
      stdio: ['pipe', 'pipe', 'pipe'],
      shell: false
    });
    
    let stdout = '';
    let stderr = '';
    
    process.stdout.on('data', (data: any) => {
      stdout += data.toString();
    });
    
    process.stderr.on('data', (data: any) => {
      stderr += data.toString();
    });
    
    process.on('close', (code: number) => {
      if (code === 0) {
        console.log(`[Notebook Automation] ${commandDescription} completed successfully`);
        if (stdout) console.log(`[Notebook Automation] Output: ${stdout}`);
        resolve();
      } else {
        console.error(`[Notebook Automation] ${commandDescription} failed with code ${code}`);
        if (stderr) console.error(`[Notebook Automation] Error: ${stderr}`);
        reject(new Error(`${commandDescription} failed with code ${code}: ${stderr}`));
      }
    });
    
    process.on('error', (error: any) => {
      console.error(`[Notebook Automation] Failed to start ${commandDescription}:`, error);
      reject(new Error(`Failed to start ${commandDescription}: ${error.message}`));
    });
  });
}
