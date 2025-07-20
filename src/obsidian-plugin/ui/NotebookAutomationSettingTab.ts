
import { App, PluginSettingTab, Setting, Notice } from 'obsidian';
import type NotebookAutomationPlugin from '../main';
import { ensureExecutableExists } from '../utils/na-executable';

// URL validation utility function
function isValidUrl(string: string): boolean {
  try {
    const url = new URL(string);
    return ['http:', 'https:'].includes(url.protocol);
  } catch (_) {
    return false;
  }
}

// GUID validation utility function
function isValidGuid(string: string): boolean {
  const guidRegex = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
  return guidRegex.test(string);
}

// File extension validation utility function
function isValidFileExtension(string: string): boolean {
  const extensionRegex = /^\.[a-zA-Z0-9]+$/;
  return extensionRegex.test(string);
}

// Get platform-specific path validation error messages
function getPathValidationErrorMessage(validationType: 'file' | 'directory' | 'path'): string {
  const isWindows = process.platform === 'win32';
  
  if (isWindows) {
    switch (validationType) {
      case 'file':
        return 'Please enter a valid file path (avoid characters: < > " | ? * and reserved names like CON, PRN, etc.)';
      case 'directory':
        return 'Please enter a valid directory path (avoid invalid characters, file extensions, and paths ending with space or dot)';
      case 'path':
        return 'Please enter a valid path (avoid characters: < > " | ? * and reserved names)';
    }
  } else {
    switch (validationType) {
      case 'file':
        return 'Please enter a valid file path (avoid null bytes and paths starting with -)';
      case 'directory':
        return 'Please enter a valid directory path (avoid null bytes, file extensions, and paths starting with -)';
      case 'path':
        return 'Please enter a valid path (avoid null bytes and paths starting with -)';
    }
  }
}

// File path validation utility function - cross-platform
function isValidFilePath(string: string): boolean {
  if (!string || string.trim().length === 0) return false;
  
  // Detect platform (in Obsidian/Electron context)
  const isWindows = process.platform === 'win32';
  
  if (isWindows) {
    // Windows-specific validation
    // Note: Forward slashes are OK since code normalizes them to backslashes
    // Note: Colons are OK for drive letters (C:, D:, etc.)
    const invalidChars = /[<>"|?*]/; // Removed : and / from invalid chars
    if (invalidChars.test(string)) return false;
    
    // Special validation for colons - only allow in drive letter position
    const colonMatches = string.match(/:/g);
    if (colonMatches) {
      // Allow colons only if they appear as drive letters (position 1) or in UNC paths
      const driveLetterPattern = /^[a-zA-Z]:/;
      const uncPattern = /^\/\/[^/]+\/[^/]+/; // Forward slash UNC
      const uncBackslashPattern = /^\\\\[^\\]+\\[^\\]+/; // Backslash UNC
      
      if (!driveLetterPattern.test(string) && !uncPattern.test(string) && !uncBackslashPattern.test(string)) {
        // If not a drive letter or UNC path, check if colon is in an invalid position
        if (string.indexOf(':') !== 1) return false;
      }
    }
    
    // Check for reserved names on Windows
    const reservedNames = /^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(\.|$)/i;
    const pathParts = string.split(/[/\\]/);
    for (const part of pathParts) {
      if (reservedNames.test(part)) return false;
    }
    
    // Windows path length limit
    if (string.length > 260 && !string.startsWith('\\\\?\\')) return false;
  } else {
    // Unix/Linux/macOS validation
    // Only null byte is truly invalid on Unix systems
    if (string.includes('\0')) return false;
    
    // Check for paths that start with - (could be problematic with command line tools)
    const pathParts = string.split('/');
    for (const part of pathParts) {
      if (part.startsWith('-') && part.length > 1) return false;
    }
    
    // Unix path length limit (typically 4096)
    if (string.length > 4096) return false;
  }
  
  // Common validation for all platforms
  // Forward and backward slashes are both acceptable since path normalization happens in the code
  
  // Avoid paths ending with space or dot (Windows issue, but good practice everywhere)
  const pathParts = string.split(/[/\\]/);
  for (const part of pathParts) {
    if (part.endsWith(' ') || part.endsWith('.')) return false;
  }
  
  return true;
}

// Directory path validation utility function - cross-platform
function isValidDirectoryPath(string: string): boolean {
  if (!string || string.trim().length === 0) return false;
  
  // Use same validation as file path
  if (!isValidFilePath(string)) return false;
  
  // Directory paths shouldn't end with common file extensions
  const fileExtensionRegex = /\.[a-zA-Z0-9]{1,10}$/;
  if (fileExtensionRegex.test(string)) return false;
  
  // Platform-specific directory validation
  const isWindows = process.platform === 'win32';
  
  if (isWindows) {
    // Windows directories can't end with space or dot
    if (string.endsWith(' ') || string.endsWith('.')) return false;
  }
  
  return true;
}

export class NotebookAutomationSettingTab extends PluginSettingTab {
  plugin: NotebookAutomationPlugin;

  constructor(app: App, plugin: NotebookAutomationPlugin) {
    super(app, plugin);
    this.plugin = plugin;
  }

  display(): void {
    this.injectCustomStyles();
    const { containerEl } = this;
    containerEl.empty();
    containerEl.classList.add('notebook-automation-container');
    containerEl.addClass('notebook-automation-settings');
    containerEl.createEl('h2', { text: 'Notebook Automation Settings' });

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
            // Toggle Banners Configuration section visibility
            const bannersSection = document.querySelector('.notebook-automation-banners-section') as HTMLElement;
            if (bannersSection) {
              bannersSection.classList.toggle('notebook-automation-none-display', !value);
            }
            // Update the global CSS rule by re-injecting styles
            this.injectCustomStyles();
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
            // Refresh the config display to show/hide Microsoft Graph section
            const configToDisplay = (window as any).notebookAutomationLoadedConfig;
            const configPath = (window as any).notebookAutomationLoadedConfigPath;
            if (configToDisplay) {
              this.displayLoadedConfig(configToDisplay, configPath);
            }
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

    // Advanced Configuration flag
    new Setting(flagsGroup)
      .setName("Advanced Configuration")
      .setDesc("Show advanced configuration options including timeout settings and detailed technical configurations. When disabled, only basic configuration options are displayed for a cleaner interface. Enable this when you need to customize timeout values, rate limiting, or other advanced technical settings.")
      .addToggle(toggle => {
        toggle.setValue(this.plugin.settings.advancedConfiguration ?? false)
          .onChange(async (value) => {
            this.plugin.settings.advancedConfiguration = value;
            await this.plugin.saveSettings();
            // Refresh the entire settings display to show/hide advanced sections
            this.display();
          });
      });

    // Configuration section (show only if advanced configuration is enabled)
    if (this.plugin.settings.advancedConfiguration) {
      // Config file path setting
      const configFileSetting = new Setting(containerEl)
        .setName('Custom Config File (Optional)')
        .setDesc('Enter the path to a custom config.json file. Priority order: 1) NOTEBOOKAUTOMATION_CONFIG environment variable, 2) default-config.json from plugin directory, 3) this custom path setting. This allows you to override the default configuration if needed.');
    
    configFileSetting.settingEl.addClass("notebook-automation-config-input");
    configFileSetting.controlEl.classList.add('notebook-automation-flex-display');
    configFileSetting.controlEl.classList.add('notebook-automation-config-file-control');
    
    const configPathInput = document.createElement("input");
    configPathInput.type = "text";
    configPathInput.placeholder = "Optional: Path to custom config.json...";
    configPathInput.value = this.plugin.settings.configPath || "";
    configPathInput.classList.add('notebook-automation-config-path-input');
    configPathInput.onchange = async (e: any) => {
      this.plugin.settings.configPath = e.target.value;
      await this.plugin.saveSettings();
    };
    configFileSetting.controlEl.appendChild(configPathInput);

    // Validate & Load button
    const validateBtn = document.createElement("button");
    validateBtn.textContent = "🔍 Validate & Load Config";
    validateBtn.classList.add('notebook-automation-validate-btn');
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
            const configJson = JSON.parse(content);
            new Notice("✅ Config loaded successfully.");
            this.displayLoadedConfig(configJson, path);
          } catch (jsonErr) {
            const configError = "Invalid JSON: " + (jsonErr instanceof Error ? jsonErr.message : String(jsonErr));
            new Notice(configError);
            this.displayLoadedConfig(null, undefined, configError);
          }
        } else {
          const configError = "Config file does not exist or is not a file.";
          new Notice(configError);
          this.displayLoadedConfig(null, undefined, configError);
        }
      } catch (err) {
        const configError = "Error checking file: " + (err instanceof Error ? err.message : String(err));
        new Notice(configError);
        this.displayLoadedConfig(null, undefined, configError);
      }
    };
    configFileSetting.controlEl.appendChild(validateBtn);

    // Check for default-config.json in plugin directory first
    this.checkAndLoadDefaultConfig();

    // Add config status section
    const configStatusDiv = containerEl.createDiv({ cls: "notebook-automation-config-status" });
    configStatusDiv.classList.add('notebook-automation-config-status');

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

    // Display loaded config fields
    const configPath = (window as any).notebookAutomationLoadedConfigPath;
    this.displayLoadedConfig(configToDisplay, configPath);
    }

    // Create version div
    const versionDiv = containerEl.createDiv({ cls: "notebook-automation-version" });
    versionDiv.setText("Notebook Automation version: Loading...");
    
    this.getNaVersion().then(ver => {
      // Convert line feeds to HTML breaks for proper display
      const formattedVersion = ver.replace(/\n/g, '<br>');
      versionDiv.innerHTML = formattedVersion;
    });
  }

  checkAndLoadDefaultConfig() {
    try {
      // @ts-ignore
      const fs = window.require ? window.require('fs') : null;
      // @ts-ignore
      const path = window.require ? window.require('path') : null;
      
      if (!fs || !path) {
        console.log('[Notebook Automation] File system access not available for config auto-loading');
        return;
      }

      let configPath = '';
      
      // First priority: Environment variable NOTEBOOKAUTOMATION_CONFIG
      const envConfigPath = process.env.NOTEBOOKAUTOMATION_CONFIG;
      console.log('[Notebook Automation] Environment variable check - NOTEBOOKAUTOMATION_CONFIG:', envConfigPath);
      console.log('[Notebook Automation] process.env available:', !!process.env);
      console.log('[Notebook Automation] process.env keys:', Object.keys(process.env || {}).filter(k => k.includes('NOTEBOOK')));
      
      if (envConfigPath) {
        try {
          console.log('[Notebook Automation] Checking if env config path exists:', envConfigPath);
          if (fs.existsSync(envConfigPath)) {
            configPath = envConfigPath;
            console.log('[Notebook Automation] Auto-loading config from environment variable NOTEBOOKAUTOMATION_CONFIG:', configPath);
          } else {
            console.log('[Notebook Automation] Environment config path does not exist:', envConfigPath);
          }
        } catch (err) {
          console.log('[Notebook Automation] Error checking environment config path:', err);
        }
      }
      
      // Second priority: Use default-config.json from plugin directory
      if (!configPath) {
        // Get plugin directory
        const pluginDir = this.plugin.manifest?.dir;
        if (pluginDir) {
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
              console.log('[Notebook Automation] Error getting vault root for config auto-loading:', err);
            }
          }

          const defaultConfigPath = path.join(resolvedPluginDir, 'default-config.json');
          if (fs.existsSync(defaultConfigPath) && fs.statSync(defaultConfigPath).isFile()) {
            configPath = defaultConfigPath;
            console.log('[Notebook Automation] Auto-loading default-config.json from plugin directory:', configPath);
          }
        }
      }
      
      // Third priority: Fallback to user-configured path
      if (!configPath && this.plugin.settings.configPath) {
        const userConfigPath = this.plugin.settings.configPath;
        if (fs.existsSync(userConfigPath) && fs.statSync(userConfigPath).isFile()) {
          configPath = userConfigPath;
          console.log('[Notebook Automation] Auto-loading user-configured config path:', configPath);
        }
      }

      // Load the config if we found a path
      if (configPath) {
        const content = fs.readFileSync(configPath, 'utf8');
        try {
          const configJson = JSON.parse(content);
          (window as any).notebookAutomationLoadedConfig = configJson;
          (window as any).notebookAutomationLoadedConfigPath = configPath;
          console.log('[Notebook Automation] Successfully auto-loaded config from:', configPath);
        } catch (jsonErr) {
          console.log('[Notebook Automation] Error parsing config file:', jsonErr);
        }
      } else {
        console.log('[Notebook Automation] No config file found in any of the expected locations for auto-loading');
      }
    } catch (err) {
      console.log('[Notebook Automation] Error auto-loading config:', err);
    }
  }

  displayLoadedConfig(configJson: any, configPath?: string, error?: string) {
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
      (window as any).notebookAutomationLoadedConfigPath = null;
      return;
    }
    
    if (!configJson) return;
    
    (window as any).notebookAutomationLoadedConfig = configJson;
    (window as any).notebookAutomationLoadedConfigPath = configPath || null;
    const fieldsDiv = containerEl.createDiv({ cls: 'notebook-automation-config-fields' });
    
    // Insert before version div if it exists
    if (versionDiv) {
      containerEl.insertBefore(fieldsDiv, versionDiv);
    }

    // Add banners section (show only if banners are enabled)
    if (this.plugin.settings.bannersEnabled) {
      this.addBannersSection(fieldsDiv, configJson);
    }

    // Add logging section (always show when config is loaded)
    this.addLoggingSection(fieldsDiv, configJson);

    // Add paths section (show only if advanced configuration is enabled)
    if (this.plugin.settings.advancedConfiguration) {
      this.addPathsSection(fieldsDiv, configJson);
    }
    
    // Add extensions section (show only if advanced configuration is enabled)
    if (this.plugin.settings.advancedConfiguration) {
      this.addExtensionsSection(fieldsDiv, configJson);
    }
    
    // Add AI service section (show only if advanced configuration is enabled)
    if (this.plugin.settings.advancedConfiguration) {
      this.addAIServiceSection(fieldsDiv, configJson);
    }
    
    // Add Microsoft Graph section (show only if OneDrive Shared Link is enabled and advanced configuration is enabled)
    if (this.plugin.settings.oneDriveSharedLink && this.plugin.settings.advancedConfiguration) {
      this.addMicrosoftGraphSection(fieldsDiv, configJson);
    }
    
    // Add timeout section (show only if advanced configuration is enabled)
    if (this.plugin.settings.advancedConfiguration) {
      this.addTimeoutSection(fieldsDiv, configJson);
    }
    
    // Add other configuration section
    this.addOtherConfigSection(fieldsDiv, configJson);
    
    // Add save button outside the main container
    this.addSaveButton(containerEl, configJson);
  }

  addPathsSection(fieldsDiv: HTMLDivElement, configJson: any) {
    // Add section title above the container
    fieldsDiv.createEl('h4', { text: 'Paths Configuration', cls: 'notebook-automation-ai-header' });
    
    const pathsSection = fieldsDiv.createDiv({ cls: 'notebook-automation-paths-section' });

    // Base Block Template setting (using plugin settings, not config JSON)
    const baseBlockDiv = pathsSection.createDiv({ cls: 'setting-item notebook-automation-custom-setting' });
    const baseBlockInfoDiv = baseBlockDiv.createDiv({ cls: 'setting-item-info' });
    const baseBlockNameDiv = baseBlockInfoDiv.createDiv({ cls: 'setting-item-name' });
    baseBlockNameDiv.setText('Base Block Template File Path');
    const baseBlockDescDiv = baseBlockInfoDiv.createDiv({ cls: 'setting-item-description' });
    baseBlockDescDiv.setText('Filepath to the base block template used in markdown generation on class index pages. (e.g., c:\\notebook\\BaseBlockTemplate.yml)');

    const baseBlockControlDiv = baseBlockDiv.createDiv({ cls: 'setting-item-control notebook-automation-input-control' });
    const baseBlockInput = baseBlockControlDiv.createEl('input', {
      type: 'text',
      cls: 'notebook-automation-path-input'
    });
    baseBlockInput.value = this.plugin.settings.baseBlockTemplateFilename || 'BaseBlockTemplate.yml';
    baseBlockInput.placeholder = 'Enter base block template file path...';

    // Add validation message element for base block template
    const baseBlockValidation = baseBlockControlDiv.createDiv({ cls: 'notebook-automation-base-block-validation' });

    // Initial validation for existing value
    const currentBaseBlockValue = baseBlockInput.value;
    if (currentBaseBlockValue && !isValidFilePath(currentBaseBlockValue)) {
      baseBlockValidation.classList.add('visible');
      baseBlockValidation.textContent = getPathValidationErrorMessage('file');
      baseBlockInput.classList.add('notebook-automation-input-invalid');
    }

    baseBlockInput.oninput = async (e: any) => {
      const inputValue = e.target.value;
      
      // Path validation
      if (inputValue && !isValidFilePath(inputValue)) {
        baseBlockValidation.classList.add('visible');
        baseBlockValidation.textContent = getPathValidationErrorMessage('file');
        baseBlockInput.classList.add('notebook-automation-input-invalid');
      } else {
        baseBlockValidation.classList.remove('visible');
        baseBlockInput.classList.remove('notebook-automation-input-invalid');
      }
      
      // Save to plugin settings
      this.plugin.settings.baseBlockTemplateFilename = inputValue;
      await this.plugin.saveSettings();
    };

    const keyMeta = [
      {
        key: 'onedrive_fullpath_root',
        label: 'OneDrive Root Path',
        desc: 'The full path to the root of your OneDrive folder.',
        icon: '',
        validateDirectoryPath: true
      },
      {
        key: 'notebook_vault_fullpath_root',
        label: 'Notebook Vault Root Path',
        desc: 'The full path to the root of your Obsidian notebook vault.',
        icon: '',
        validateDirectoryPath: true
      },
      {
        key: 'notebook_vault_resources_basepath',
        label: 'Notebook Vault Resources Base Path',
        desc: 'The base path within your vault for resources.',
        icon: '',
        validatePath: true
      },
      {
        key: 'metadata_schema_file',
        label: 'Metadata Schema File',
        desc: 'The path to the metadata-schema.yml file used for notebook automation. This replaces the deprecated metadata_file.',
        icon: '',
        validateFilePath: true
      },
      {
        key: 'onedrive_resources_basepath',
        label: 'OneDrive Resources Base Path',
        desc: 'The base path in OneDrive for education resources.',
        icon: '',
        validatePath: true
      },
      {
        key: 'prompts_path',
        label: 'Prompts Path',
        desc: 'The path to the prompts directory for automation tasks.',
        icon: '',
        validateDirectoryPath: true
      },
      {
        key: 'logging_dir',
        label: 'Logging Directory',
        desc: 'The directory where logs will be written.',
        icon: '',
        validateDirectoryPath: true
      },
    ];

    const paths = configJson.paths || {};
    const updatedPaths: Record<string, string> = { ...paths };

    // Add path configuration fields
    keyMeta.forEach(meta => {
      // Create a custom container instead of using Setting component
      const settingDiv = pathsSection.createDiv({ cls: 'setting-item notebook-automation-custom-setting' });

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

      // Create validation message element for path fields
      let validationMessage: HTMLElement | null = null;
      if (meta.validateFilePath || meta.validateDirectoryPath || meta.validatePath) {
        validationMessage = controlDiv.createDiv({ cls: 'notebook-automation-field-validation' });
        
        // Initial validation for existing values
        const currentValue = input.value;
        if (currentValue) {
          let isValid = true;
          let errorMessage = '';
          
          if (meta.validateFilePath && !isValidFilePath(currentValue)) {
            isValid = false;
            errorMessage = getPathValidationErrorMessage('file');
          } else if (meta.validateDirectoryPath && !isValidDirectoryPath(currentValue)) {
            isValid = false;
            errorMessage = getPathValidationErrorMessage('directory');
          } else if (meta.validatePath && !isValidFilePath(currentValue)) {
            isValid = false;
            errorMessage = getPathValidationErrorMessage('path');
          }
          
          if (!isValid) {
            validationMessage.classList.add('visible');
            validationMessage.textContent = errorMessage;
            input.classList.add('notebook-automation-input-invalid');
          }
        }
      }

      input.oninput = (e: any) => {
        const inputValue = e.target.value;
        
        // Path validation for fields that require it
        if (validationMessage && (meta.validateFilePath || meta.validateDirectoryPath || meta.validatePath)) {
          let isValid = true;
          let errorMessage = '';
          
          if (inputValue) {
            if (meta.validateFilePath && !isValidFilePath(inputValue)) {
              isValid = false;
              errorMessage = getPathValidationErrorMessage('file');
            } else if (meta.validateDirectoryPath && !isValidDirectoryPath(inputValue)) {
              isValid = false;
              errorMessage = getPathValidationErrorMessage('directory');
            } else if (meta.validatePath && !isValidFilePath(inputValue)) {
              isValid = false;
              errorMessage = getPathValidationErrorMessage('path');
            }
          }
          
          if (!isValid) {
            validationMessage.classList.add('visible');
            validationMessage.textContent = errorMessage;
            input.classList.add('notebook-automation-input-invalid');
          } else {
            validationMessage.classList.remove('visible');
            input.classList.remove('notebook-automation-input-invalid');
          }
        }

        updatedPaths[meta.key] = inputValue;
        // Update the global config
        if ((window as any).notebookAutomationLoadedConfig) {
          if (!(window as any).notebookAutomationLoadedConfig.paths) {
            (window as any).notebookAutomationLoadedConfig.paths = {};
          }
          (window as any).notebookAutomationLoadedConfig.paths[meta.key] = inputValue;
        }
      };
    });
  }

  addAIServiceSection(fieldsDiv: HTMLDivElement, configJson: any) {
    // Add section title above the container
    fieldsDiv.createEl('h4', { text: 'AI Service Configuration', cls: 'notebook-automation-ai-header' });
    
    const aiSection = fieldsDiv.createDiv({ cls: 'notebook-automation-ai-section' });

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
          { key: 'endpoint', label: 'Azure OpenAI Endpoint', desc: 'The Azure OpenAI service endpoint URL', type: 'text', validateUrl: true },
          { key: 'deployment', label: 'Deployment Name', desc: 'The deployment name for your Azure OpenAI model', type: 'text' },
          { key: 'model', label: 'Model Name', desc: 'The name of the AI model to use', type: 'text' }
        ],
        openai: [
          { key: 'endpoint', label: 'OpenAI Endpoint', desc: 'The OpenAI API endpoint URL', type: 'text', validateUrl: true },
          { key: 'model', label: 'Model Name', desc: 'The OpenAI model to use (e.g., gpt-4o, gpt-3.5-turbo)', type: 'text' }
        ],
        foundry: [
          { key: 'endpoint', label: 'Foundry Endpoint', desc: 'The Foundry LLM endpoint URL', type: 'text', validateUrl: true },
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

        // Create validation message element for URL fields
        let validationMessage: HTMLElement | null = null;
        if (field.validateUrl) {
          validationMessage = fieldControlDiv.createDiv({ cls: 'notebook-automation-field-validation' });
          
          // Initial validation for existing values
          const currentValue = fieldInput.value;
          if (currentValue && !isValidUrl(currentValue)) {
            validationMessage.classList.add('visible');
            validationMessage.textContent = 'Please enter a valid URL (must start with http:// or https://)';
            fieldInput.classList.add('notebook-automation-input-invalid');
          }
        }

        fieldInput.oninput = (e: any) => {
          const inputValue = e.target.value;
          
          // URL validation for fields that require it
          if (field.validateUrl && validationMessage) {
            if (inputValue && !isValidUrl(inputValue)) {
              validationMessage.classList.add('visible');
              validationMessage.textContent = 'Please enter a valid URL (must start with http:// or https://)';
              fieldInput.classList.add('notebook-automation-input-invalid');
            } else {
              validationMessage.classList.remove('visible');
              fieldInput.classList.remove('notebook-automation-input-invalid');
            }
          }

          if (!updatedAiConfig[provider]) {
            updatedAiConfig[provider] = {};
          }
          updatedAiConfig[provider][field.key] = inputValue;
          
          // Update global config
          if ((window as any).notebookAutomationLoadedConfig) {
            if (!(window as any).notebookAutomationLoadedConfig.aiservice) {
              (window as any).notebookAutomationLoadedConfig.aiservice = {};
            }
            if (!(window as any).notebookAutomationLoadedConfig.aiservice[provider]) {
              (window as any).notebookAutomationLoadedConfig.aiservice[provider] = {};
            }
            (window as any).notebookAutomationLoadedConfig.aiservice[provider][field.key] = inputValue;
          }
        };
      });
    };

    // Initialize provider fields
    updateProviderFields(currentProvider);
    this.addAIProviderValidation(aiSection, currentProvider);

    // Handle provider selection change
    providerSelect.onchange = (e: any) => {
      const selectedProvider = e.target.value;
      updatedAiConfig.provider = selectedProvider;
      updateProviderFields(selectedProvider);
      
      // Validate the new provider immediately with feedback
      this.addAIProviderValidation(aiSection, selectedProvider);
      
      // Show immediate feedback notice
      const validation = NotebookAutomationSettingTab.validateAIProviderEnvironment(selectedProvider);
      if (validation.isValid) {
        new Notice(`✅ ${selectedProvider.toUpperCase()} provider: Environment variable is set`, 3000);
      } else if (validation.missingVar) {
        new Notice(`⚠️ ${selectedProvider.toUpperCase()} provider: Missing ${validation.missingVar} environment variable`, 5000);
      }
      
      // Update global config
      if ((window as any).notebookAutomationLoadedConfig) {
        if (!(window as any).notebookAutomationLoadedConfig.aiservice) {
          (window as any).notebookAutomationLoadedConfig.aiservice = {};
        }
        (window as any).notebookAutomationLoadedConfig.aiservice.provider = selectedProvider;
      }
    };
  }

  addMicrosoftGraphSection(fieldsDiv: HTMLDivElement, configJson: any) {
    // Add section title above the container
    fieldsDiv.createEl('h4', { text: 'Microsoft Graph Configuration', cls: 'notebook-automation-ai-header' });
    
    const graphSection = fieldsDiv.createDiv({ cls: 'notebook-automation-graph-section' });

    const graphConfig = configJson.microsoft_graph || {};
    const updatedGraphConfig: Record<string, any> = { ...graphConfig };

    const graphFields = [
      { key: 'client_id', label: 'Client ID', desc: 'Microsoft Graph application client ID', type: 'text', validateGuid: true },
      { key: 'api_endpoint', label: 'API Endpoint', desc: 'Microsoft Graph API endpoint URL', type: 'text', validateUrl: true },
      { key: 'authority', label: 'Authority', desc: 'Microsoft authentication authority URL', type: 'text', validateUrl: true }
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

      // Create validation message element for URL or GUID fields
      let validationMessage: HTMLElement | null = null;
      if (field.validateUrl || field.validateGuid) {
        validationMessage = fieldControlDiv.createDiv({ cls: 'notebook-automation-field-validation' });
        
        // Initial validation for existing values
        const currentValue = fieldInput.value;
        if (currentValue) {
          if (field.validateUrl && !isValidUrl(currentValue)) {
            validationMessage.classList.add('visible');
            validationMessage.textContent = 'Please enter a valid URL (must start with http:// or https://)';
            fieldInput.classList.add('notebook-automation-input-invalid');
          } else if (field.validateGuid && !isValidGuid(currentValue)) {
            validationMessage.classList.add('visible');
            validationMessage.textContent = 'Please enter a valid GUID (e.g., 12345678-1234-5678-9abc-123456789012)';
            fieldInput.classList.add('notebook-automation-input-invalid');
          }
        }
      }

      fieldInput.oninput = (e: any) => {
        const inputValue = e.target.value;
        
        // Validation for fields that require it
        if (validationMessage) {
          let isValid = true;
          let errorMessage = '';
          
          if (inputValue) {
            if (field.validateUrl && !isValidUrl(inputValue)) {
              isValid = false;
              errorMessage = 'Please enter a valid URL (must start with http:// or https://)';
            } else if (field.validateGuid && !isValidGuid(inputValue)) {
              isValid = false;
              errorMessage = 'Please enter a valid GUID (e.g., 12345678-1234-5678-9abc-123456789012)';
            }
          }
          
          if (!isValid) {
            validationMessage.classList.add('visible');
            validationMessage.textContent = errorMessage;
            fieldInput.classList.add('notebook-automation-input-invalid');
          } else {
            validationMessage.classList.remove('visible');
            fieldInput.classList.remove('notebook-automation-input-invalid');
          }
        }

        updatedGraphConfig[field.key] = inputValue;
        // Update global config
        if ((window as any).notebookAutomationLoadedConfig) {
          if (!(window as any).notebookAutomationLoadedConfig.microsoft_graph) {
            (window as any).notebookAutomationLoadedConfig.microsoft_graph = {};
          }
          (window as any).notebookAutomationLoadedConfig.microsoft_graph[field.key] = inputValue;
        }
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
      cls: 'notebook-automation-path-input',
      attr: { 'data-scopes': 'true' }
    });
    scopesTextarea.value = (updatedGraphConfig.scopes || []).join('\n');
    scopesTextarea.placeholder = 'Enter scopes (one per line)...';
    scopesTextarea.oninput = (e: any) => {
      updatedGraphConfig.scopes = e.target.value.split('\n').filter((scope: string) => scope.trim().length > 0);
      // Update global config
      if ((window as any).notebookAutomationLoadedConfig) {
        if (!(window as any).notebookAutomationLoadedConfig.microsoft_graph) {
          (window as any).notebookAutomationLoadedConfig.microsoft_graph = {};
        }
        (window as any).notebookAutomationLoadedConfig.microsoft_graph.scopes = updatedGraphConfig.scopes;
      }
    };
  }

  addTimeoutSection(fieldsDiv: HTMLDivElement, configJson: any) {
    // Add section title above the container
    fieldsDiv.createEl('h4', { text: 'Timeout Configuration', cls: 'notebook-automation-ai-header' });
    
    const timeoutSection = fieldsDiv.createDiv({ cls: 'notebook-automation-timeout-section' });

    const aiConfig = configJson.aiservice || {};
    const timeoutConfig = aiConfig.timeout || {};
    const timeoutFields = [
      { key: 'request_timeout_seconds', label: 'Request Timeout (seconds)', desc: 'Request timeout in seconds', type: 'number', default: 300 },
      { key: 'max_retry_attempts', label: 'Max Retry Attempts', desc: 'Maximum number of retry attempts for failed requests', type: 'number', default: 3 },
      { key: 'base_retry_delay_seconds', label: 'Base Retry Delay (seconds)', desc: 'Base delay between retry attempts in seconds', type: 'number', default: 2 },
      { key: 'max_retry_delay_seconds', label: 'Max Retry Delay (seconds)', desc: 'Maximum delay between retry attempts in seconds', type: 'number', default: 60 },
      { key: 'max_chunk_parallelism', label: 'Max Chunk Parallelism', desc: 'Maximum number of chunks to process simultaneously', type: 'number', default: 3 },
      { key: 'chunk_rate_limit_ms', label: 'Chunk Rate Limit (ms)', desc: 'Minimum delay between chunk requests in milliseconds', type: 'number', default: 100 },
      { key: 'max_file_parallelism', label: 'Max File Parallelism', desc: 'Maximum number of files to process in parallel', type: 'number', default: 2 },
      { key: 'file_rate_limit_ms', label: 'File Rate Limit (ms)', desc: 'Minimum delay between file processing in milliseconds', type: 'number', default: 200 }
    ];

    timeoutFields.forEach(field => {
      const isNumeric = field.type === 'number';
      const fieldDiv = timeoutSection.createDiv({ 
        cls: `setting-item notebook-automation-custom-setting${isNumeric ? ' numeric-inline' : ''}` 
      });

      const fieldInfoDiv = fieldDiv.createDiv({ cls: 'setting-item-info' });
      const fieldNameDiv = fieldInfoDiv.createDiv({ cls: 'setting-item-name' });
      fieldNameDiv.setText(field.label);
      const fieldDescDiv = fieldInfoDiv.createDiv({ cls: 'setting-item-description' });
      fieldDescDiv.setText(field.desc);

      const fieldControlDiv = fieldDiv.createDiv({ cls: 'setting-item-control notebook-automation-input-control' });
      const fieldInput = fieldControlDiv.createEl('input', {
        type: field.type,
        cls: field.type === 'number' ? 'notebook-automation-numeric-input' : 'notebook-automation-path-input'
      });
      fieldInput.value = timeoutConfig[field.key] !== undefined ? timeoutConfig[field.key].toString() : field.default.toString();
      fieldInput.placeholder = `Enter ${field.label.toLowerCase()}...`;
      
      // Create validation message element for numeric fields
      let validationMessage: HTMLElement | null = null;
      if (field.type === 'number') {
        validationMessage = fieldControlDiv.createDiv({ cls: 'notebook-automation-field-validation' });
      }
      
      // Add numeric validation for number inputs
      if (field.type === 'number') {
        fieldInput.min = '1';
        fieldInput.step = '1';
        fieldInput.oninput = (e: any) => {
          // Only allow numeric input
          const numericValue = e.target.value.replace(/[^0-9]/g, '');
          e.target.value = numericValue;
          const value = parseInt(numericValue) || field.default;
          
          // Validate numeric input
          if (validationMessage) {
            if (numericValue && (value < 1 || value > 999999)) {
              validationMessage.classList.add('visible');
              validationMessage.textContent = 'Please enter a number between 1 and 999,999';
              fieldInput.classList.add('notebook-automation-input-invalid');
            } else {
              validationMessage.classList.remove('visible');
              fieldInput.classList.remove('notebook-automation-input-invalid');
            }
          }
          
          // Update global config
          if ((window as any).notebookAutomationLoadedConfig) {
            if (!(window as any).notebookAutomationLoadedConfig.aiservice) {
              (window as any).notebookAutomationLoadedConfig.aiservice = {};
            }
            if (!(window as any).notebookAutomationLoadedConfig.aiservice.timeout) {
              (window as any).notebookAutomationLoadedConfig.aiservice.timeout = {};
            }
            (window as any).notebookAutomationLoadedConfig.aiservice.timeout[field.key] = value;
          }
        };
      } else {
        fieldInput.oninput = (e: any) => {
          const value = field.type === 'number' ? parseInt(e.target.value) || field.default : e.target.value;
          // Update global config
          if ((window as any).notebookAutomationLoadedConfig) {
            if (!(window as any).notebookAutomationLoadedConfig.aiservice) {
              (window as any).notebookAutomationLoadedConfig.aiservice = {};
            }
            if (!(window as any).notebookAutomationLoadedConfig.aiservice.timeout) {
              (window as any).notebookAutomationLoadedConfig.aiservice.timeout = {};
            }
            (window as any).notebookAutomationLoadedConfig.aiservice.timeout[field.key] = value;
          }
        };
      }
    });
  }

  addLoggingSection(fieldsDiv: HTMLDivElement, configJson: any) {
    // Add section title above the container
    fieldsDiv.createEl('h4', { text: 'Logging Configuration', cls: 'notebook-automation-ai-header' });
    
    const loggingSection = fieldsDiv.createDiv({ cls: 'notebook-automation-logging-section' });

    const loggingConfig = configJson.logging || {};
    const pathsConfig = configJson.paths || {};
    
    // Add logging directory path field
    const logDirDiv = loggingSection.createDiv({ cls: 'setting-item notebook-automation-custom-setting' });
    const logDirInfoDiv = logDirDiv.createDiv({ cls: 'setting-item-info' });
    const logDirNameDiv = logDirInfoDiv.createDiv({ cls: 'setting-item-name' });
    logDirNameDiv.setText('Logging Directory');
    const logDirDescDiv = logDirInfoDiv.createDiv({ cls: 'setting-item-description' });
    logDirDescDiv.setText('Directory path where log files are stored');

    const logDirControlDiv = logDirDiv.createDiv({ cls: 'setting-item-control notebook-automation-input-control' });
    const logDirInput = logDirControlDiv.createEl('input', {
      type: 'text',
      cls: 'notebook-automation-path-input'
    });
    logDirInput.value = pathsConfig.logging_dir || 'd:/source/notebook-automation/logs';
    logDirInput.placeholder = 'Enter logging directory path...';

    // Create validation message element for logging directory
    const logDirValidationMessage = logDirControlDiv.createDiv({ cls: 'notebook-automation-field-validation' });
    
    // Initial validation for existing value
    const initialLogDirValue = logDirInput.value;
    if (initialLogDirValue && !isValidDirectoryPath(initialLogDirValue)) {
      logDirValidationMessage.classList.add('visible');
      logDirValidationMessage.textContent = getPathValidationErrorMessage('directory');
      logDirInput.classList.add('notebook-automation-input-invalid');
    }

    logDirInput.oninput = (e: any) => {
      const inputValue = e.target.value;
      
      // Directory path validation
      if (inputValue && !isValidDirectoryPath(inputValue)) {
        logDirValidationMessage.classList.add('visible');
        logDirValidationMessage.textContent = getPathValidationErrorMessage('directory');
        logDirInput.classList.add('notebook-automation-input-invalid');
      } else {
        logDirValidationMessage.classList.remove('visible');
        logDirInput.classList.remove('notebook-automation-input-invalid');
      }

      // Update global config
      if ((window as any).notebookAutomationLoadedConfig) {
        if (!(window as any).notebookAutomationLoadedConfig.paths) {
          (window as any).notebookAutomationLoadedConfig.paths = {};
        }
        (window as any).notebookAutomationLoadedConfig.paths.logging_dir = inputValue;
      }
    };

    const loggingFields = [
      { key: 'max_file_size_mb', label: 'Max File Size (MB)', desc: 'Maximum size for log files in megabytes', type: 'number', default: 50 },
      { key: 'retained_file_count', label: 'Retained File Count', desc: 'Number of log files to retain', type: 'number', default: 7 }
    ];

    loggingFields.forEach(field => {
      const isNumeric = field.type === 'number';
      const fieldDiv = loggingSection.createDiv({ 
        cls: `setting-item notebook-automation-custom-setting${isNumeric ? ' numeric-inline' : ''}` 
      });

      const fieldInfoDiv = fieldDiv.createDiv({ cls: 'setting-item-info' });
      const fieldNameDiv = fieldInfoDiv.createDiv({ cls: 'setting-item-name' });
      fieldNameDiv.setText(field.label);
      const fieldDescDiv = fieldInfoDiv.createDiv({ cls: 'setting-item-description' });
      fieldDescDiv.setText(field.desc);

      const fieldControlDiv = fieldDiv.createDiv({ cls: 'setting-item-control notebook-automation-input-control' });
      const fieldInput = fieldControlDiv.createEl('input', {
        type: field.type,
        cls: field.type === 'number' ? 'notebook-automation-numeric-input' : 'notebook-automation-path-input'
      });
      fieldInput.value = loggingConfig[field.key] !== undefined ? loggingConfig[field.key].toString() : field.default.toString();
      fieldInput.placeholder = `Enter ${field.label.toLowerCase()}...`;
      
      // Create validation message element for numeric fields
      let validationMessage: HTMLElement | null = null;
      if (field.type === 'number') {
        validationMessage = fieldControlDiv.createDiv({ cls: 'notebook-automation-field-validation' });
      }
      
      // Add numeric validation for number inputs
      if (field.type === 'number') {
        fieldInput.min = '1';
        fieldInput.step = '1';
        fieldInput.oninput = (e: any) => {
          // Only allow numeric input
          const numericValue = e.target.value.replace(/[^0-9]/g, '');
          e.target.value = numericValue;
          const value = parseInt(numericValue) || field.default;
          
          // Validate numeric input
          if (validationMessage) {
            if (numericValue && (value < 1 || value > 999999)) {
              validationMessage.classList.add('visible');
              validationMessage.textContent = 'Please enter a number between 1 and 999,999';
              fieldInput.classList.add('notebook-automation-input-invalid');
            } else {
              validationMessage.classList.remove('visible');
              fieldInput.classList.remove('notebook-automation-input-invalid');
            }
          }
          
          // Update global config
          if ((window as any).notebookAutomationLoadedConfig) {
            if (!(window as any).notebookAutomationLoadedConfig.logging) {
              (window as any).notebookAutomationLoadedConfig.logging = {};
            }
            (window as any).notebookAutomationLoadedConfig.logging[field.key] = value;
          }
        };
      } else {
        fieldInput.oninput = (e: any) => {
          const value = field.type === 'number' ? parseInt(e.target.value) || field.default : e.target.value;
          // Update global config
          if ((window as any).notebookAutomationLoadedConfig) {
            if (!(window as any).notebookAutomationLoadedConfig.logging) {
              (window as any).notebookAutomationLoadedConfig.logging = {};
            }
            (window as any).notebookAutomationLoadedConfig.logging[field.key] = value;
          }
        };
      }
    });

    // Add button to open logging directory (now logDirInput is in scope)
    const buttonDiv = loggingSection.createDiv({ cls: 'setting-item notebook-automation-custom-setting' });
    const buttonInfoDiv = buttonDiv.createDiv({ cls: 'setting-item-info' });
    const buttonNameDiv = buttonInfoDiv.createDiv({ cls: 'setting-item-name' });
    buttonNameDiv.setText('Open Logging Directory');
    const buttonDescDiv = buttonInfoDiv.createDiv({ cls: 'setting-item-description' });
    buttonDescDiv.setText('Open the logging directory in file explorer');

    const buttonControlDiv = buttonDiv.createDiv({ cls: 'setting-item-control notebook-automation-input-control' });
    const openDirButton = buttonControlDiv.createEl('button', {
      cls: 'mod-cta',
      text: 'Open Directory'
    });
    openDirButton.onclick = () => {
      // Get the current logging directory path from the input field
      const currentLogDir = logDirInput.value || 'd:/source/notebook-automation/logs';
      
      try {
        // @ts-ignore
        const { shell } = window.require('electron');
        shell.openPath(currentLogDir);
        new Notice(`Opening logging directory: ${currentLogDir}`);
      } catch (error) {
        console.error('Failed to open logging directory:', error);
        new Notice(`Failed to open logging directory: ${currentLogDir}`);
      }
    };
  }

  addExtensionsSection(fieldsDiv: HTMLDivElement, configJson: any) {
    // Add section title above the container
    fieldsDiv.createEl('h4', { text: 'File Extensions', cls: 'notebook-automation-ai-header' });
    
    const extensionsSection = fieldsDiv.createDiv({ cls: 'notebook-automation-extensions-section' });

    // Video extensions
    const videoExtDiv = extensionsSection.createDiv({ cls: 'setting-item notebook-automation-custom-setting' });
    const videoExtInfoDiv = videoExtDiv.createDiv({ cls: 'setting-item-info' });
    const videoExtNameDiv = videoExtInfoDiv.createDiv({ cls: 'setting-item-name' });
    videoExtNameDiv.setText('Video Extensions');
    const videoExtDescDiv = videoExtInfoDiv.createDiv({ cls: 'setting-item-description' });
    videoExtDescDiv.setText('Supported video file extensions (comma-separated)');

    const videoExtControlDiv = videoExtDiv.createDiv({ cls: 'setting-item-control notebook-automation-input-control' });
    const videoExtInput = videoExtControlDiv.createEl('input', {
      type: 'text',
      cls: 'notebook-automation-path-input'
    });
    const videoExtensions = configJson.video_extensions || [];
    videoExtInput.value = videoExtensions.join(', ');
    videoExtInput.placeholder = 'Enter video extensions (.mp4, .mov, .avi, etc.)...';
    videoExtInput.oninput = (e: any) => {
      const extensions = e.target.value.split(',').map((ext: string) => ext.trim()).filter((ext: string) => ext);
      // Update global config
      if ((window as any).notebookAutomationLoadedConfig) {
        (window as any).notebookAutomationLoadedConfig.video_extensions = extensions;
      }
    };

    // PDF extensions
    const pdfExtDiv = extensionsSection.createDiv({ cls: 'setting-item notebook-automation-custom-setting' });
    const pdfExtInfoDiv = pdfExtDiv.createDiv({ cls: 'setting-item-info' });
    const pdfExtNameDiv = pdfExtInfoDiv.createDiv({ cls: 'setting-item-name' });
    pdfExtNameDiv.setText('PDF Extensions');
    const pdfExtDescDiv = pdfExtInfoDiv.createDiv({ cls: 'setting-item-description' });
    pdfExtDescDiv.setText('Supported PDF file extensions (comma-separated)');

    const pdfExtControlDiv = pdfExtDiv.createDiv({ cls: 'setting-item-control notebook-automation-input-control' });
    const pdfExtInput = pdfExtControlDiv.createEl('input', {
      type: 'text',
      cls: 'notebook-automation-path-input'
    });
    const pdfExtensions = configJson.pdf_extensions || [];
    pdfExtInput.value = pdfExtensions.join(', ');
    pdfExtInput.placeholder = 'Enter PDF extensions (.pdf)...';
    pdfExtInput.oninput = (e: any) => {
      const extensions = e.target.value.split(',').map((ext: string) => ext.trim()).filter((ext: string) => ext);
      // Update global config
      if ((window as any).notebookAutomationLoadedConfig) {
        (window as any).notebookAutomationLoadedConfig.pdf_extensions = extensions;
      }
    };

    // PDF Extract Images toggle
    const pdfExtractDiv = extensionsSection.createDiv({ cls: 'setting-item notebook-automation-custom-setting' });
    const pdfExtractInfoDiv = pdfExtractDiv.createDiv({ cls: 'setting-item-info' });
    const pdfExtractNameDiv = pdfExtractInfoDiv.createDiv({ cls: 'setting-item-name' });
    pdfExtractNameDiv.setText('PDF Extract Images');
    const pdfExtractDescDiv = pdfExtractInfoDiv.createDiv({ cls: 'setting-item-description' });
    pdfExtractDescDiv.setText('Enable image extraction from PDF files during processing');

    const pdfExtractControlDiv = pdfExtractDiv.createDiv({ cls: 'setting-item-control notebook-automation-input-control' });
    const pdfExtractToggle = pdfExtractControlDiv.createEl('input', {
      type: 'checkbox',
      cls: 'notebook-automation-toggle'
    });
    pdfExtractToggle.checked = configJson.pdf_extract_images || false;
    pdfExtractToggle.onchange = (e: any) => {
      const value = e.target.checked;
      // Update global config
      if ((window as any).notebookAutomationLoadedConfig) {
        (window as any).notebookAutomationLoadedConfig.pdf_extract_images = value;
      }
    };
  }

  addBannersSection(fieldsDiv: HTMLDivElement, configJson: any) {
    // Add section title above the container
    fieldsDiv.createEl('h4', { text: 'Banners Configuration', cls: 'notebook-automation-ai-header' });
    
    const bannersSection = fieldsDiv.createDiv({ cls: 'notebook-automation-banners-section' });

    const bannersConfig = configJson.banners || {};

    // Basic banner fields
    const bannerFields = [
      { key: 'default', label: 'Default Banner', desc: 'Default banner image filename', type: 'text' },
      { key: 'format', label: 'Banner Format', desc: 'Banner format (e.g., image)', type: 'text' }
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
        type: field.type,
        cls: 'notebook-automation-path-input'
      });
      fieldInput.value = bannersConfig[field.key] || '';
      fieldInput.placeholder = `Enter ${field.label.toLowerCase()}...`;
      fieldInput.oninput = (e: any) => {
        // Update global config
        if ((window as any).notebookAutomationLoadedConfig) {
          if (!(window as any).notebookAutomationLoadedConfig.banners) {
            (window as any).notebookAutomationLoadedConfig.banners = {};
          }
          (window as any).notebookAutomationLoadedConfig.banners[field.key] = e.target.value;
        }
      };
    });

    // Template banners section
    const templateBannersDiv = bannersSection.createDiv({ cls: 'setting-item notebook-automation-custom-setting' });
    const templateBannersInfoDiv = templateBannersDiv.createDiv({ cls: 'setting-item-info' });
    const templateBannersNameDiv = templateBannersInfoDiv.createDiv({ cls: 'setting-item-name' });
    templateBannersNameDiv.setText('Template Banners');
    const templateBannersDescDiv = templateBannersInfoDiv.createDiv({ cls: 'setting-item-description' });
    templateBannersDescDiv.setText('Banner images for different template types (JSON format)');

    const templateBannersControlDiv = templateBannersDiv.createDiv({ cls: 'setting-item-control notebook-automation-input-control' });
    const templateBannersInput = templateBannersControlDiv.createEl('textarea', {
      cls: 'notebook-automation-path-input',
      attr: { 'data-template-banners': 'true' }
    });
    const templateBanners = bannersConfig.template_banners || {};
    templateBannersInput.value = JSON.stringify(templateBanners, null, 2);
    templateBannersInput.placeholder = 'Enter template banners JSON...';
    templateBannersInput.oninput = (e: any) => {
      try {
        const parsedValue = JSON.parse(e.target.value);
        // Update global config
        if ((window as any).notebookAutomationLoadedConfig) {
          if (!(window as any).notebookAutomationLoadedConfig.banners) {
            (window as any).notebookAutomationLoadedConfig.banners = {};
          }
          (window as any).notebookAutomationLoadedConfig.banners.template_banners = parsedValue;
        }
      } catch (error) {
        // Invalid JSON, ignore for now
      }
    };

    // Filename patterns section
    const filenamePatternsDiv = bannersSection.createDiv({ cls: 'setting-item notebook-automation-custom-setting' });
    const filenamePatternsInfoDiv = filenamePatternsDiv.createDiv({ cls: 'setting-item-info' });
    const filenamePatternsNameDiv = filenamePatternsInfoDiv.createDiv({ cls: 'setting-item-name' });
    filenamePatternsNameDiv.setText('Filename Patterns');
    const filenamePatternsDescDiv = filenamePatternsInfoDiv.createDiv({ cls: 'setting-item-description' });
    filenamePatternsDescDiv.setText('Banner images for specific filename patterns (JSON format)');

    const filenamePatternsControlDiv = filenamePatternsDiv.createDiv({ cls: 'setting-item-control notebook-automation-input-control' });
    const filenamePatternsInput = filenamePatternsControlDiv.createEl('textarea', {
      cls: 'notebook-automation-path-input',
      attr: { 'data-filename-patterns': 'true' }
    });
    const filenamePatterns = bannersConfig.filename_patterns || {};
    filenamePatternsInput.value = JSON.stringify(filenamePatterns, null, 2);
    filenamePatternsInput.placeholder = 'Enter filename patterns JSON...';
    filenamePatternsInput.oninput = (e: any) => {
      try {
        const parsedValue = JSON.parse(e.target.value);
        // Update global config
        if ((window as any).notebookAutomationLoadedConfig) {
          if (!(window as any).notebookAutomationLoadedConfig.banners) {
            (window as any).notebookAutomationLoadedConfig.banners = {};
          }
          (window as any).notebookAutomationLoadedConfig.banners.filename_patterns = parsedValue;
        }
      } catch (error) {
        // Invalid JSON, ignore for now
      }
    };
  }

  addOtherConfigSection(fieldsDiv: HTMLDivElement, configJson: any) {
    // Add section title above the container
    fieldsDiv.createEl('h4', { text: 'Other Configuration', cls: 'notebook-automation-ai-header' });
    
    const otherSection = fieldsDiv.createDiv({ cls: 'notebook-automation-other-section' });

    // Video extensions
    const videoExtDiv = otherSection.createDiv({ cls: 'setting-item notebook-automation-custom-setting' });
    const videoExtInfoDiv = videoExtDiv.createDiv({ cls: 'setting-item-info' });
    const videoExtNameDiv = videoExtInfoDiv.createDiv({ cls: 'setting-item-name' });
    videoExtNameDiv.setText('Video Extensions');
    const videoExtDescDiv = videoExtInfoDiv.createDiv({ cls: 'setting-item-description' });
    videoExtDescDiv.setText('Supported video file extensions (one per line)');

    const videoExtControlDiv = videoExtDiv.createDiv({ cls: 'setting-item-control notebook-automation-input-control' });
    const videoExtTextarea = videoExtControlDiv.createEl('textarea', {
      cls: 'notebook-automation-path-input',
      attr: { 'data-video-ext': 'true' }
    });
    videoExtTextarea.value = (configJson.video_extensions || []).join('\n');
    videoExtTextarea.placeholder = 'Enter video extensions (one per line)...';
    
    // Create validation message element
    const videoExtValidationMessage = videoExtControlDiv.createDiv({ cls: 'notebook-automation-field-validation' });
    
    // Initial validation for existing values
    const initialVideoExtensions = (configJson.video_extensions || []);
    const invalidVideoExts = initialVideoExtensions.filter((ext: string) => ext && !isValidFileExtension(ext));
    if (invalidVideoExts.length > 0) {
      videoExtValidationMessage.classList.add('visible');
      videoExtValidationMessage.textContent = `Invalid extensions: ${invalidVideoExts.join(', ')} (must start with . and contain only letters/numbers)`;
      videoExtTextarea.classList.add('notebook-automation-input-invalid');
    }
    
    videoExtTextarea.oninput = (e: any) => {
      const extensions = e.target.value.split('\n').filter((ext: string) => ext.trim().length > 0).map((ext: string) => ext.trim());
      const invalidExtensions = extensions.filter((ext: string) => !isValidFileExtension(ext));
      
      if (invalidExtensions.length > 0) {
        videoExtValidationMessage.classList.add('visible');
        videoExtValidationMessage.textContent = `Invalid extensions: ${invalidExtensions.join(', ')} (must start with . and contain only letters/numbers)`;
        videoExtTextarea.classList.add('notebook-automation-input-invalid');
      } else {
        videoExtValidationMessage.classList.remove('visible');
        videoExtTextarea.classList.remove('notebook-automation-input-invalid');
      }
      
      // Update global config
      if ((window as any).notebookAutomationLoadedConfig) {
        (window as any).notebookAutomationLoadedConfig.video_extensions = extensions;
      }
    };

    // PDF extensions
    const pdfExtDiv = otherSection.createDiv({ cls: 'setting-item notebook-automation-custom-setting' });
    const pdfExtInfoDiv = pdfExtDiv.createDiv({ cls: 'setting-item-info' });
    const pdfExtNameDiv = pdfExtInfoDiv.createDiv({ cls: 'setting-item-name' });
    pdfExtNameDiv.setText('PDF Extensions');
    const pdfExtDescDiv = pdfExtInfoDiv.createDiv({ cls: 'setting-item-description' });
    pdfExtDescDiv.setText('Supported PDF file extensions (one per line)');

    const pdfExtControlDiv = pdfExtDiv.createDiv({ cls: 'setting-item-control notebook-automation-input-control' });
    const pdfExtTextarea = pdfExtControlDiv.createEl('textarea', {
      cls: 'notebook-automation-path-input',
      attr: { 'data-pdf-ext': 'true' }
    });
    pdfExtTextarea.value = (configJson.pdf_extensions || []).join('\n');
    pdfExtTextarea.placeholder = 'Enter PDF extensions (one per line)...';
    
    // Create validation message element
    const pdfExtValidationMessage = pdfExtControlDiv.createDiv({ cls: 'notebook-automation-field-validation' });
    
    // Initial validation for existing values
    const initialPdfExtensions = (configJson.pdf_extensions || []);
    const invalidPdfExts = initialPdfExtensions.filter((ext: string) => ext && !isValidFileExtension(ext));
    if (invalidPdfExts.length > 0) {
      pdfExtValidationMessage.classList.add('visible');
      pdfExtValidationMessage.textContent = `Invalid extensions: ${invalidPdfExts.join(', ')} (must start with . and contain only letters/numbers)`;
      pdfExtTextarea.classList.add('notebook-automation-input-invalid');
    }
    
    pdfExtTextarea.oninput = (e: any) => {
      const extensions = e.target.value.split('\n').filter((ext: string) => ext.trim().length > 0).map((ext: string) => ext.trim());
      const invalidExtensions = extensions.filter((ext: string) => !isValidFileExtension(ext));
      
      if (invalidExtensions.length > 0) {
        pdfExtValidationMessage.classList.add('visible');
        pdfExtValidationMessage.textContent = `Invalid extensions: ${invalidExtensions.join(', ')} (must start with . and contain only letters/numbers)`;
        pdfExtTextarea.classList.add('notebook-automation-input-invalid');
      } else {
        pdfExtValidationMessage.classList.remove('visible');
        pdfExtTextarea.classList.remove('notebook-automation-input-invalid');
      }
      
      // Update global config
      if ((window as any).notebookAutomationLoadedConfig) {
        (window as any).notebookAutomationLoadedConfig.pdf_extensions = extensions;
      }
    };
  }

  addSaveButton(containerElement: HTMLElement, configJson: any) {
    // Get the path of the currently loaded config file
    const loadedConfigPath = (window as any).notebookAutomationLoadedConfigPath;

    // Show save target information
    const saveInfoDiv = containerElement.createDiv({ cls: 'notebook-automation-save-info' });
    
    if (loadedConfigPath) {
      // @ts-ignore
      const path = window.require ? window.require('path') : null;
      const fileName = path ? path.basename(loadedConfigPath) : loadedConfigPath;
      
      saveInfoDiv.innerHTML = `
        <div style="margin-bottom: 4px;"><strong>Save target:</strong> Currently loaded config</div>
        <div style="font-family: monospace; word-break: break-all;">📁 ${loadedConfigPath}</div>
      `;
    } else {
      saveInfoDiv.innerHTML = `
        <div style="margin-bottom: 4px;"><strong>⚠️ No config file loaded</strong></div>
        <div style="font-style: italic;">Load a config file first to enable saving</div>
      `;
    }

    // Save button for config fields (always on its own line)
    const saveSetting = new Setting(containerElement);
    saveSetting.settingEl.classList.add('notebook-automation-save-setting');
    saveSetting.addButton(btn => {
      btn.setButtonText('💾 Save')
        .setCta()
        .setDisabled(!loadedConfigPath)
        .onClick(async () => {
          if (!loadedConfigPath) {
            new Notice('❌ No config file loaded. Please load a config file first.');
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

            // Ensure directory exists
            const configDir = path.dirname(loadedConfigPath);
            if (!fs.existsSync(configDir)) {
              try {
                fs.mkdirSync(configDir, { recursive: true });
              } catch (mkdirErr) {
                new Notice('Failed to create config directory: ' + (mkdirErr instanceof Error ? mkdirErr.message : String(mkdirErr)));
                return;
              }
            }

            const currentConfig = (window as any).notebookAutomationLoadedConfig || {};

            // Build complete configuration object
            const configToSave = {
              ConfigFilePath: this.plugin.settings.configPath || '',
              DebugEnabled: this.plugin.settings.debug || false,
              paths: currentConfig.paths || {},
              microsoft_graph: currentConfig.microsoft_graph || {},
              aiservice: currentConfig.aiservice || {
                provider: 'azure',
                azure: {},
                openai: {},
                foundry: {},
                timeout: {
                  request_timeout_seconds: 300,
                  max_retry_attempts: 3,
                  base_retry_delay_seconds: 2,
                  max_retry_delay_seconds: 60,
                  max_chunk_parallelism: 3,
                  chunk_rate_limit_ms: 100,
                  max_file_parallelism: 2,
                  file_rate_limit_ms: 200
                }
              },
              video_extensions: currentConfig.video_extensions || [],
              pdf_extensions: currentConfig.pdf_extensions || [],
              pdf_extract_images: this.plugin.settings.pdfExtractImages || false,
              banners: {
                enabled: this.plugin.settings.bannersEnabled || false,
                ...currentConfig.banners
              }
            };

            // Write to the loaded config file
            fs.writeFileSync(loadedConfigPath, JSON.stringify(configToSave, null, 4), 'utf8');
            
            const fileName = path.basename(loadedConfigPath);
            new Notice(`✅ Config saved successfully to ${fileName}`);

            // Update global loaded config
            (window as any).notebookAutomationLoadedConfig = configToSave;

          } catch (err) {
            console.error('[Notebook Automation] Error saving config:', err);
            new Notice('Failed to save config: ' + (err instanceof Error ? err.message : String(err)));
          }
        });
    });
  }

  /**
   * Injects dynamic CSS for toggleable sections based on plugin settings.
   */
  injectCustomStyles() {
    const styleId = 'notebook-automation-dynamic-styles';
    // Remove existing style element to allow updates
    const existingStyle = document.getElementById(styleId);
    if (existingStyle) {
      existingStyle.remove();
    }
    const style = document.createElement('style');
    style.id = styleId;
    style.textContent = `
      /* Dynamic visibility for sections based on plugin settings */
      .notebook-automation-graph-section { display: ${this.plugin.settings.oneDriveSharedLink ? 'block' : 'none'}; }
      .notebook-automation-timeout-section { display: ${this.plugin.settings.advancedConfiguration ? 'block' : 'none'}; }
      .notebook-automation-other-section { display: ${this.plugin.settings.advancedConfiguration ? 'block' : 'none'}; }
      .notebook-automation-paths-section { display: ${this.plugin.settings.advancedConfiguration ? 'block' : 'none'}; }
      .notebook-automation-ai-service-section { display: ${this.plugin.settings.advancedConfiguration ? 'block' : 'none'}; }
      
      /* Always show these sections when config is loaded */
      .notebook-automation-logging-section { display: block; }
      .notebook-automation-extensions-section { display: block; }
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
      const naPath = await ensureExecutableExists(this.plugin);
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

  addAIProviderValidation(aiSection: HTMLDivElement, provider: string) {
    // Remove previous validation warnings
    const existingWarning = aiSection.querySelector('.notebook-automation-env-warning');
    if (existingWarning) {
      existingWarning.remove();
    }

    // Define required environment variables for each provider
    const providerEnvVars: { [key: string]: { varName: string, description: string } } = {
      'openai': { varName: 'OPENAI_API_KEY', description: 'OpenAI API Key' },
      'azure': { varName: 'AZURE_OPENAI_KEY', description: 'Azure OpenAI API Key' },
      'foundry': { varName: 'FOUNDRY_API_KEY', description: 'Foundry API Key' }
    };

    const envInfo = providerEnvVars[provider];
    if (!envInfo) {
      return; // No validation needed for unknown providers
    }

    // Check if the environment variable is set
    const envValue = process.env[envInfo.varName];
    const isEnvVarSet = envValue && envValue.trim() !== '';

    if (!isEnvVarSet) {
      // Create warning div
      const warningDiv = aiSection.createDiv({ cls: 'notebook-automation-env-warning' });
      warningDiv.classList.add('notebook-automation-warning-div');
      
      warningDiv.innerHTML = `
        <div style="display: flex; align-items: center; gap: 8px; margin-bottom: 4px;">
          <span style="font-size: 16px;">⚠️</span>
          <strong style="color: var(--color-red);">Environment Variable Missing</strong>
        </div>
        <div style="font-size: 0.9em; margin-bottom: 8px;">
          The <code>${envInfo.varName}</code> environment variable is not set for the ${provider.toUpperCase()} provider.
          <br>This will cause command execution to fail.
        </div>
        <div style="font-size: 0.85em; font-style: italic;">
          Set <code>${envInfo.varName}</code> in your system environment variables, then restart Obsidian.
        </div>
      `;
    } else {
      // Create success div to show the environment variable is set
      const successDiv = aiSection.createDiv({ cls: 'notebook-automation-env-warning' });
      successDiv.classList.add('notebook-automation-success-div');
      
      successDiv.innerHTML = `
        <div style="display: flex; align-items: center; gap: 8px;">
          <span style="font-size: 16px;">✅</span>
          <strong style="color: var(--color-green);">Environment Variable Set</strong>
        </div>
        <div style="font-size: 0.9em; margin-top: 4px;">
          <code>${envInfo.varName}</code> is configured for the ${provider.toUpperCase()} provider.
        </div>
      `;
    }
  }

  static validateAIProviderEnvironment(provider: string): { isValid: boolean, missingVar?: string, description?: string } {
    const providerEnvVars: { [key: string]: { varName: string, description: string } } = {
      'openai': { varName: 'OPENAI_API_KEY', description: 'OpenAI API Key' },
      'azure': { varName: 'AZURE_OPENAI_KEY', description: 'Azure OpenAI API Key' },
      'foundry': { varName: 'FOUNDRY_API_KEY', description: 'Foundry API Key' }
    };

    const envInfo = providerEnvVars[provider];
    if (!envInfo) {
      return { isValid: true }; // Unknown providers are considered valid
    }

    const envValue = process.env[envInfo.varName];
    const isValid = !!(envValue && envValue.trim() !== '');

    return {
      isValid,
      missingVar: isValid ? undefined : envInfo.varName,
      description: isValid ? undefined : envInfo.description
    };
  }

}
