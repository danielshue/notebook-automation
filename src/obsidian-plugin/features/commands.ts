
import { TFolder, TFile, Notice } from 'obsidian';
import type NotebookAutomationPlugin from '../main';
import { getRelativeVaultResourcePath, ensureExecutableExists, ensureConfigFilesExist } from '../utils/plugin-assets';

/**
 * Handles notebook automation commands for a given file or folder and action.
 *
 * Determines configuration, calculates relative paths, validates environment, and    case "extract-html-content":
      // Use generate-markdown command with single path parameter
      // CLI will resolve both OneDrive source and vault destination from relative path
      args = ["generate-markdown", "--path", relativePath, "--config", configPath];
      commandDescription = "Extract HTML Content";
      break;opriate automation command.
 *
 * @param plugin The NotebookAutomationPlugin instance.
 * @param file The target file or folder for the command.
 * @param action The action to perform (e.g., 'sync-dir', 'import-summarize-videos').
 */
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
  
  // Calculate the relative path based on action type
  let relPath: string;
  
  if (action === 'sync' || action === 'sync-dir' || action === 'vault-sync') {
    // For sync commands, use the full relative path from vault root
    // The CLI will handle the path mapping based on its internal configuration
    const fullRelativePath = getRelativeVaultResourcePath(file.path, vaultRoot, vaultBase);
    relPath = fullRelativePath;
    console.log(`[Notebook Automation] [DEBUG] Using full relative path for sync: "${relPath}"`);
  } else {
    // For other commands, use the normal relative path calculation
    relPath = getRelativeVaultResourcePath(file.path, vaultRoot, vaultBase);
  }  console.log(`[Notebook Automation] Command '${action}' triggered for: ${file.path}`);
  console.log(`[Notebook Automation] Path calculation - vaultRoot: ${vaultRoot}, vaultBase: ${vaultBase}`);
  console.log(`[Notebook Automation] Relative path for processing: ${relPath}`);
  
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
    // Try to download config files from GitHub release before giving up
    try {
      console.log('[Notebook Automation] No configuration found locally. Attempting to download from GitHub release...');
      const configDownloadResult = await ensureConfigFilesExist(plugin);
      if (configDownloadResult) {
        console.log('[Notebook Automation] Successfully downloaded configuration files from GitHub release');
        // Re-check config availability after download
        hasConfig = true;
      } else {
        console.warn('[Notebook Automation] Failed to download configuration files from GitHub release');
      }
    } catch (error) {
      console.warn('[Notebook Automation] Error downloading configuration files:', error);
    }
  }

  if (!hasConfig) {
    new Notice("❌ No configuration file found. Please set up configuration in plugin settings or check network connectivity for auto-download.");
    return;
  }

  // Handle folder opening commands directly without external executable
  if (action === "open-onedrive-folder") {
    await openOneDriveFolder(plugin, file, relPath);
    new Notice("✅ OneDrive folder opened");
    return;
  }

  // Show immediate feedback that command has started
  if (action === "sync-dir" || action === "vault-sync") {
    new Notice("🔄 OneDrive Directory Sync started - processing in background...");
  } else {
    new Notice(`🔄 ${action} command initiated...`);
  }
  
  // Check AI provider environment variables before execution
  const aiProviderValidation = await validateAIProviderBeforeExecution(plugin);
  if (!aiProviderValidation.isValid) {
    return; // Validation function handles opening settings and showing error
  }
  
  // Execute the command asynchronously without blocking the UI
  executeNotebookAutomationCommand(plugin, action, relPath)
    .then(() => {
      new Notice(`✅ ${action} completed successfully`);
    })
    .catch((error) => {
      console.error(`[Notebook Automation] Error executing command '${action}':`, error);
      new Notice(`❌ Error executing ${action}: ${error instanceof Error ? error.message : String(error)}`);
    });
}

/**
 * Executes a notebook automation command by spawning the external CLI with the correct arguments.
 *
 * Skips execution for folder opening actions (handled elsewhere).
 *
 * @param plugin The NotebookAutomationPlugin instance.
 * @param action The action to perform (e.g., 'sync-dir', 'import-summarize-videos').
 * @param relativePath The relative path to the file or folder for the command.
 * @param opts Optional flags (e.g., force execution).
 * @returns A promise that resolves when the command completes.
 * @throws If no configuration file is available or the action is unknown.
 */
export async function executeNotebookAutomationCommand(plugin: NotebookAutomationPlugin, action: string, relativePath: string, opts?: { force?: boolean }) {
  // Skip external command execution for folder opening actions
  if (action === "open-onedrive-folder" || action === "open-local-folder") {
    return; // These are handled directly in handleNotebookAutomationCommand
  }
  
  // @ts-ignore
  const child_process = window.require ? window.require('child_process') : null;
  if (!child_process) {
    throw new Error("Child process module not available");
  }
  
  const naPath = await ensureExecutableExists(plugin);
  
  // Use same priority logic as startup: env variable, user settings, loaded config, then default-config.json
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
  
  // Second priority: User-configured path from plugin settings
  if (!configPath && plugin.settings.configPath) {
    const userConfigPath = plugin.settings.configPath;
    try {
      // @ts-ignore
      const fs = window.require ? window.require('fs') : null;
      if (fs && fs.existsSync(userConfigPath)) {
        configPath = userConfigPath;
        console.log('[Notebook Automation] Using config from user plugin settings:', configPath);
      } else {
        console.log('[Notebook Automation] User-configured config path does not exist:', userConfigPath);
      }
    } catch (err) {
      console.log('[Notebook Automation] Error checking user-configured config path:', err);
    }
  }
  
  // Third priority: Use loaded config path from window (fallback for when settings aren't available)
  if (!configPath) {
    const loadedConfigPath = (window as any).notebookAutomationLoadedConfigPath;
    if (loadedConfigPath) {
      try {
        // @ts-ignore
        const fs = window.require ? window.require('fs') : null;
        if (fs && fs.existsSync(loadedConfigPath)) {
          configPath = loadedConfigPath;
          console.log('[Notebook Automation] Using loaded config path from startup:', configPath);
        }
      } catch (err) {
        console.log('[Notebook Automation] Error checking loaded config path:', err);
      }
    }
  }
  
  // Fourth priority: Use default-config.json from plugin directory
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
  
  if (!configPath) {
    throw new Error("No configuration file available. Please set up configuration in plugin settings.");
  }
  
  // Build command arguments based on action
  let args: string[] = [];
  let commandDescription = "";
  
  // Extract directory path for commands that need it
  const dirPath = relativePath.includes('/') ? relativePath.substring(0, relativePath.lastIndexOf('/')) : '.';
  
  switch (action) {
    case "sync-dir":
    case "vault-sync":
      args = ["vault", "vault-sync", relativePath, "--config", configPath];
      commandDescription = "Sync Directory with OneDrive";
      break;
    case "import-summarize-videos":
      args = ["video-notes", "--path", relativePath, "--config", configPath];
      commandDescription = "Import & AI Summarize Videos";
      break;
    case "import-summarize-pdfs":
      args = ["pdf-notes", "--path", relativePath, "--config", configPath];
      commandDescription = "Import & AI Summarize PDFs";
      break;
    case "import-summarize-html-epub-txt":
      args = ["generate-markdown", "--path", relativePath, "--config", configPath];
      commandDescription = "Import & AI Summarize HTML/EPUB/TXT";
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
      args = ["video-notes", "--path", relativePath, "--reprocess", "--config", configPath];
      commandDescription = "Reprocess Video Summary";
      break;
    case "reprocess-summary-pdf":
      args = ["pdf-notes", "--path", relativePath, "--reprocess", "--config", configPath];
      commandDescription = "Reprocess PDF Summary";
      break;
    case "reprocess-summary-html-epub-txt":
      args = ["generate-markdown", "--path", relativePath, "--config", configPath];
      commandDescription = "Reprocess HTML/EPUB/TXT Summary";
      break;
    case "ensure-metadata":
      args = ["vault", "ensure-metadata", relativePath, "--config", configPath];
      commandDescription = "Ensure Metadata Consistency";
      break;
    case "extract-html-content":
      // Use generate-markdown command with --extract-from-markdown mode
      // Pass the relative path to the markdown file itself
      // CLI will read the frontmatter to find onedrive_relative_path and process accordingly
      args = ["generate-markdown", "--path", relativePath, "--extract-from-markdown", "--config", configPath];
      commandDescription = "Extract HTML Content";
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
  
  // Add flags specific to sync operations only
  if (action === "sync-dir" || action === "vault-sync") {
    if (plugin.settings.unidirectionalSync) {
      args.push("--unidirectional");
    }
    if (plugin.settings.recursiveDirectorySync) {
      args.push("--recursive");
    }
    // Add document types if the feature is enabled
    if (plugin.settings.enableDocumentPlaceholders) {
      args.push("--create-placeholders", "videos", "pdf", "html");
    }
  }
  
  // Only add --force if explicitly requested by the caller (in addition to settings)
  if (opts?.force) {
    args.push("--force");
  }
  
  console.log(`[Notebook Automation] Executing: ${naPath} ${args.map(arg => arg.includes(' ') ? `"${arg}"` : arg).join(' ')}`);
  
  // Return immediately, allowing UI to continue
  return new Promise<void>((resolve, reject) => {
    // Use setTimeout to defer the heavy work to next tick, avoiding UI blocking
    setTimeout(() => {
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
    }, 0);
  });
}

/**
 * Validates that the required environment variables for the selected AI provider are set before command execution.
 *
 * @param plugin The NotebookAutomationPlugin instance.
 * @returns An object indicating if the environment is valid for execution.
 */
async function validateAIProviderBeforeExecution(plugin: NotebookAutomationPlugin): Promise<{ isValid: boolean }> {
  try {
    // Load the current configuration to determine the AI provider
    const loadedConfig = (window as any).notebookAutomationLoadedConfig;
    let configToCheck = loadedConfig;

    // If no config is loaded in the settings, try to load it using the same priority logic
    if (!configToCheck) {
      configToCheck = await loadConfigForValidation(plugin);
    }

    if (!configToCheck || !configToCheck.aiservice) {
      // No AI service configuration found, assume it's okay
      return { isValid: true };
    }

    const provider = configToCheck.aiservice.provider;
    if (!provider) {
      // No provider specified, assume it's okay
      return { isValid: true };
    }

    // Import the validation function from the settings tab
    const { NotebookAutomationSettingTab } = await import('../ui/NotebookAutomationSettingTab');
    const validation = NotebookAutomationSettingTab.validateAIProviderEnvironment(provider);

    if (!validation.isValid) {
      // Show error notice
      new Notice(`❌ Missing environment variable: ${validation.missingVar}`, 8000);
      
      // Open settings and scroll to AI section
      await openSettingsAndScrollToAIProvider(plugin, provider, validation);
      return { isValid: false };
    }

    return { isValid: true };
  } catch (error) {
    console.error('[Notebook Automation] Error validating AI provider:', error);
    // If validation fails, allow execution to proceed (fail-safe)
    return { isValid: true };
  }
}

/**
 * Loads the configuration for validation purposes, using the same priority as command execution.
 *
 * @param plugin The NotebookAutomationPlugin instance.
 * @returns The loaded configuration object, or null if not found.
 */
async function loadConfigForValidation(plugin: NotebookAutomationPlugin): Promise<any> {
  try {
    // @ts-ignore
    const fs = window.require ? window.require('fs') : null;
    // @ts-ignore
    const path = window.require ? window.require('path') : null;
    
    if (!fs || !path) {
      return null;
    }

    let configPath = '';
    
    // Use same priority logic as executeNotebookAutomationCommand
    const envConfigPath = process.env.NOTEBOOKAUTOMATION_CONFIG;
    if (envConfigPath && fs.existsSync(envConfigPath)) {
      configPath = envConfigPath;
    } else {
      // Check for default-config.json
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
          configPath = defaultConfigPath;
        } else if (plugin.settings.configPath && fs.existsSync(plugin.settings.configPath)) {
          configPath = plugin.settings.configPath;
        }
      }
    }

    if (configPath) {
      const content = fs.readFileSync(configPath, 'utf8');
      return JSON.parse(content);
    }

    return null;
  } catch (error) {
    console.error('[Notebook Automation] Error loading config for validation:', error);
    return null;
  }
}

/**
 * Opens the plugin settings tab and scrolls to the AI provider section, highlighting any missing environment variables.
 *
 * @param plugin The NotebookAutomationPlugin instance.
 * @param provider The AI provider name.
 * @param validation Validation result with missing variable info.
 */
async function openSettingsAndScrollToAIProvider(plugin: NotebookAutomationPlugin, provider: string, validation: { missingVar?: string, description?: string }): Promise<void> {
  try {
    // Open the plugin settings
    // @ts-ignore
    const settingTab = plugin.app.setting.openTabById(plugin.manifest.id);
    if (settingTab) {
      // Wait a bit for the settings to render
      setTimeout(() => {
        // Find and scroll to the AI provider section
        const aiSection = document.querySelector('.notebook-automation-ai-section');
        if (aiSection) {
          aiSection.scrollIntoView({ behavior: 'smooth', block: 'center' });
          
          // Highlight the AI section temporarily
          const originalBorder = (aiSection as HTMLElement).style.border;
          (aiSection as HTMLElement).style.border = '2px solid var(--color-red)';
          (aiSection as HTMLElement).style.borderRadius = '4px';
          
          setTimeout(() => {
            (aiSection as HTMLElement).style.border = originalBorder;
          }, 3000);

          // Show additional error message in the settings
          const errorDiv = document.createElement('div');
          errorDiv.style.cssText = `
            margin-top: 8px;
            padding: 12px;
            background-color: var(--background-modifier-error);
            border: 1px solid var(--color-red);
            border-radius: 4px;
            font-size: 0.9em;
          `;
          errorDiv.innerHTML = `
            <div style="display: flex; align-items: center; gap: 8px; margin-bottom: 8px;">
              <span style="font-size: 16px;">❌</span>
              <strong style="color: var(--color-red);">Command Execution Blocked</strong>
            </div>
            <div>
              Cannot execute commands with the <strong>${provider.toUpperCase()}</strong> provider because the required 
              environment variable <code>${validation.missingVar}</code> is not set.
            </div>
            <div style="margin-top: 8px; font-size: 0.85em; font-style: italic;">
              Set this environment variable in your system settings, then restart Obsidian.
            </div>
          `;
          
          aiSection.appendChild(errorDiv);
          
          // Remove the error message after 10 seconds
          setTimeout(() => {
            if (errorDiv.parentNode) {
              errorDiv.parentNode.removeChild(errorDiv);
            }
          }, 10000);
        }
      }, 100);
    }
  } catch (error) {
    console.error('[Notebook Automation] Error opening settings:', error);
  }
}

/**
 * Opens the OneDrive folder location in the system file manager.
 *
 * @param plugin The NotebookAutomationPlugin instance.
 * @param file The file or folder reference.
 * @param relativePath The relative path to open in OneDrive.
 */
async function openOneDriveFolder(plugin: any, file: any, relativePath: string): Promise<void> {
  try {
    // Try to get the loaded config from settings tab first (same as main command logic)
    let config = (window as any).notebookAutomationLoadedConfig;
    
    // If no config loaded in window, try the validation function
    if (!config) {
      config = await loadConfigForValidation(plugin);
    }
    
    if (!config || !config.paths || !config.paths.onedrive_fullpath_root) {
      new Notice("OneDrive root path not configured");
      console.log('[Notebook Automation] Config check failed:', {
        configExists: !!config,
        pathsExists: !!(config && config.paths),
        onedrivePathExists: !!(config && config.paths && config.paths.onedrive_fullpath_root)
      });
      return;
    }

    const { remote } = require('electron');
    const path = require('path');
    
    console.log('[Notebook Automation] Using config for OneDrive:', {
      onedrive_root: config.paths.onedrive_fullpath_root,
      onedrive_base: config.paths.onedrive_resources_basepath,
      relative_path: relativePath
    });
    
    // Get the directory containing the file
    const fileDir = path.dirname(relativePath);
    
    // Normalize and combine paths using proper separators
    let oneDriveFolder = path.normalize(config.paths.onedrive_fullpath_root);
    
    if (config.paths.onedrive_resources_basepath) {
      // Remove leading/trailing separators and normalize
      const basePath = config.paths.onedrive_resources_basepath.replace(/^[\\\/]+|[\\\/]+$/g, '');
      oneDriveFolder = path.join(oneDriveFolder, basePath);
    }
    
    // Add the file directory if it's not just "."
    if (fileDir && fileDir !== '.') {
      oneDriveFolder = path.join(oneDriveFolder, fileDir);
    }
    
    console.log('[Notebook Automation] Opening OneDrive folder:', oneDriveFolder);
    
    // Use Electron's shell to open the folder
    await remote.shell.openPath(path.resolve(oneDriveFolder));
    
  } catch (error) {
    console.error('Error opening OneDrive folder:', error);
    new Notice(`Failed to open OneDrive folder: ${error instanceof Error ? error.message : 'Unknown error'}`);
  }
}
