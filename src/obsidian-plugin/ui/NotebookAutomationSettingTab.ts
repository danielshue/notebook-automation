
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

  /**
   * Formats a validation error message with improved styling and icons.
   */
  formatValidationError(title: string, message: string, icon: string = 'alert-triangle'): string {
    return `
      <div style="display: flex; align-items: flex-start; gap: 8px; margin-bottom: 4px;">
        <div style="flex-shrink: 0; margin-top: 2px;">
          <span style="font-size: 14px; color: var(--text-error);">⚠️</span>
        </div>
        <div style="flex: 1;">
          <div style="font-weight: 600; color: var(--text-error); margin-bottom: 4px; font-size: 0.9em;">
            ${title}
          </div>
          <div style="font-size: 0.8em; line-height: 1.4; color: var(--text-muted);">
            ${message}
          </div>
        </div>
      </div>
    `;
  }

  /**
   * Formats a validation success message with improved styling and icons.
   */
  formatValidationSuccess(title: string, message: string): string {
    return `
      <div style="display: flex; align-items: flex-start; gap: 8px; margin-bottom: 4px;">
        <div style="flex-shrink: 0; margin-top: 2px;">
          <span style="font-size: 14px; color: #4ade80;">✅</span>
        </div>
        <div style="flex: 1;">
          <div style="font-weight: 600; color: #22c55e; margin-bottom: 4px; font-size: 0.9em;">
            ${title}
          </div>
          <div style="font-size: 0.8em; line-height: 1.4; color: var(--text-muted);">
            ${message}
          </div>
        </div>
      </div>
    `;
  }

  display(): void {
    this.injectCustomStyles();
    const { containerEl } = this;
    containerEl.empty();
    containerEl.classList.add('notebook-automation-container');
    containerEl.addClass('notebook-automation-settings');

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

    // Banners Enabled flag
    new Setting(flagsGroup)
      .setName("Banners Enabled")
      .setDesc("Enable banner images in generated content. When enabled, the automation will include banner images at the top of generated markdown files based on the configured banner settings and filename patterns.")
      .addToggle(toggle => {
        toggle.setValue(this.plugin.settings.bannersEnabled || false)
          .onChange(async (value) => {
            this.plugin.settings.bannersEnabled = value;
            await this.plugin.saveSettings();
            // Update the global CSS rule by re-injecting styles
            this.injectCustomStyles();
            
            // If banners are enabled and no config is loaded, create a minimal config to show the configuration section
            if (value && !(window as any).notebookAutomationLoadedConfig) {
              const minimalConfig = {
                banners: {
                  enabled: true,
                  default: "gies-banner.png",
                  format: "image"
                }
              };
              this.displayLoadedConfig(minimalConfig, undefined);
            } else if (!value) {
              // If banners are disabled, refresh the display to hide the banners section
              const currentConfig = (window as any).notebookAutomationLoadedConfig;
              const currentPath = (window as any).notebookAutomationLoadedConfigPath;
              if (currentConfig) {
                this.displayLoadedConfig(currentConfig, currentPath);
              }
            }
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

    // Add banners section header and controls (independent of advanced configuration)
    const bannersHeaderEl = containerEl.createEl('h3', {
      text: 'Banners Configuration',
      cls: 'notebook-automation-section-header notebook-automation-banners-header'
    });

    // Create banners section container
    const bannersContainer = containerEl.createDiv({ cls: 'notebook-automation-banners-section' });
    
    // Add expanded description for banner functionality
    const bannerDescriptionDiv = bannersContainer.createDiv({ cls: 'notebook-automation-section-description' });
    bannerDescriptionDiv.innerHTML = `
      <p>Configure banner images for generated markdown files. This functionality integrates with the 
      <a href="https://github.com/noatpad/obsidian-banners" target="_blank">Obsidian Banner Plugin</a> 
      to display attractive header images at the top of your notes.</p>
      
      <p><strong>Image Requirements:</strong> Banner images must be stored within your Obsidian vault. 
      When specifying banner filenames, the system uses wiki-style links to resolve the file path 
      (e.g., "gies-banner.png" will be resolved to [[gies-banner.png]] format internally).</p>
      
      <p><strong>How it works:</strong> When generating markdown files from videos, PDFs, or other content, 
      the plugin will automatically add the appropriate banner image based on content type, filename patterns, 
      or fallback to the default banner you specify below.</p>
    `;
    
    // Add banner format setting first
    const bannerFormatSetting = new Setting(bannersContainer)
      .setName('Banner Format')
      .setDesc('Choose how banners are added to generated files. "Image" mode integrates with the <a href="https://github.com/noatpad/obsidian-banners" target="_blank">Obsidian Banner Plugin</a> to display header images at the top of created index files. "Markdown" mode inserts custom markdown content into each generated file based on the template and filename patterns you configure below.');
    
    bannerFormatSetting.settingEl.addClass('notebook-automation-custom-setting');
    
    // Create dropdown for banner format
    const bannerFormatSelect = bannerFormatSetting.controlEl.createEl('select', {
      cls: 'notebook-automation-provider-select'
    });
    
    // Add format options
    const formatOptions = [
      { value: 'image', text: 'Image' },
      { value: 'markdown', text: 'Markdown' }
    ];
    
    formatOptions.forEach(option => {
      const optionEl = bannerFormatSelect.createEl('option', { 
        value: option.value, 
        text: option.text 
      });
    });
    
    // Load current value or set default
    const currentFormat = (this.plugin.settings as any).bannerFormat || 'image';
    bannerFormatSelect.value = currentFormat;
    
    // Add default banner setting (conditionally visible)
    const defaultBannerSetting = new Setting(bannersContainer)
      .setName('Default Image Banner')
      .setDesc('Default banner image filename to use when no specific banner is configured for a content type. This image should exist in your Obsidian vault. Enter just the filename (e.g., "gies-banner.png") - the system will automatically resolve it using wiki-link format. This banner will be used as a fallback when no content-specific or filename-pattern banners are defined.');
    
    defaultBannerSetting.settingEl.addClass('notebook-automation-custom-setting');
    defaultBannerSetting.settingEl.id = 'defaultBannerContainer';
    const defaultBannerInput = defaultBannerSetting.controlEl.createEl('input', {
      type: 'text',
      cls: 'notebook-automation-path-input',
      placeholder: 'e.g., gies-banner.png'
    });
    
    // Load value from plugin settings or set default
    defaultBannerInput.value = (this.plugin.settings as any).defaultBanner || 'gies-banner.png';
    defaultBannerInput.oninput = async (e: any) => {
      (this.plugin.settings as any).defaultBanner = e.target.value;
      await this.plugin.saveSettings();
    };
    
    // Create container for markdown-specific settings
    const markdownSettingsContainer = bannersContainer.createDiv({ 
      cls: 'notebook-automation-markdown-banner-settings' 
    });
    
    // Function to update settings visibility and content based on format
    const updateFormatSettings = (format: string) => {
      console.log('updateFormatSettings called with format:', format);
      markdownSettingsContainer.empty();
      
      if (format === 'markdown') {
        // Hide default banner setting for markdown format
        console.log('Hiding default banner setting for markdown format');
        defaultBannerSetting.settingEl.style.display = 'none';
        defaultBannerSetting.settingEl.addClass('notebook-automation-hidden');
        // Add markdown-specific banner settings
        this.addMarkdownBannerSettings(markdownSettingsContainer);
      } else {
        // Show default banner setting for image format
        console.log('Showing default banner setting for image format');
        defaultBannerSetting.settingEl.style.display = '';
        defaultBannerSetting.settingEl.removeClass('notebook-automation-hidden');
      }
    };
    
    // Initial setup
    updateFormatSettings(currentFormat);
    
    // Handle format selection change
    bannerFormatSelect.onchange = async (e: any) => {
      const selectedFormat = e.target.value;
      (this.plugin.settings as any).bannerFormat = selectedFormat;
      await this.plugin.saveSettings();
      
      // Update format-specific settings visibility
      updateFormatSettings(selectedFormat);
    };

    // Configuration section (show only if advanced configuration is enabled)
    if (this.plugin.settings.advancedConfiguration) {
      // Add section header
      containerEl.createEl('h3', { 
        text: 'Advanced Configuration',
        cls: 'notebook-automation-section-header'
      });
      
      // Config file path setting
      const configFileSetting = new Setting(containerEl)
        .setName('Custom Config File (Optional)');
    
      configFileSetting.settingEl.addClass("notebook-automation-config-input");
      
      // Create custom description with proper HTML formatting
      const descriptionDiv = configFileSetting.settingEl.createDiv({ cls: 'setting-item-description' });
      const process = window.require ? window.require('process') : null;
      const isWindows = process?.platform === 'win32';
      
      if (isWindows) {
        descriptionDiv.innerHTML = `
          If you want to use different configurations, you can override the plugin's default configuration file that used (default-config.json).
          Enter a file path to your custom your configuration file "e.g. config.json" to be used for this plugin. Configuration settings have the 
          following priority for loading:
          <br><br>
          • NOTEBOOKAUTOMATION_CONFIG environment variable (NOTEBOOKAUTOMATION_CONFIG="C:\\Users\\YourName\\notebook\\config.json")
          <br><br>
          • Custom file path to configuration file ("C:\\Users\\YourName\\school-work\\my_config.json")
          <br><br>
          • Plugin Directory defaults file "default-config.json"
        `;
      } else {
        descriptionDiv.innerHTML = `
          If you want to use different configurations, you can override the plugin's default configuration file that used (default-config.json).
          Enter a file path to your custom your configuration file "e.g. config.json" to be used for this plugin. Configuration settings have the 
          following priority for loading:
          <br><br>
          • NOTEBOOKAUTOMATION_CONFIG environment variable (NOTEBOOKAUTOMATION_CONFIG="~/notebook/config.json")
          <br><br>
          • Custom file path to configuration file ("~/school-work/my_config.json")
          <br><br>
          • Plugin Directory defaults file "default-config.json"
        `;
      }
      
      // Create container for input and button below the description
      const configPathContainer = containerEl.createDiv({
        cls: 'notebook-automation-config-path-container'
      });
      
      const configPathInput = configPathContainer.createEl("input", {
        type: "text",
        placeholder: "Optional: Path to custom config.json...",
        cls: 'notebook-automation-config-path-input'
      });
      configPathInput.value = this.plugin.settings.configPath || "";
      configPathInput.onchange = async (e: any) => {
        this.plugin.settings.configPath = e.target.value;
        await this.plugin.saveSettings();
      };

      // Validate & Load button
      const validateBtn = configPathContainer.createEl("button", {
        text: "🔍 Validate & Load Config",
        cls: 'notebook-automation-validate-btn'
      });
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

    // Check for default-config.json in plugin directory first
    this.checkAndLoadDefaultConfig();

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

    // Always show these three sections at the bottom in this order:
    // 1. Configuration Status, 2. Save Configuration, 3. Version Information
    
    // Remove any existing instances of these sections to prevent duplicates
    const existingConfigStatus = containerEl.querySelector('.notebook-automation-config-status-section');
    if (existingConfigStatus) existingConfigStatus.remove();
    
    const existingSaveSection = containerEl.querySelector('.notebook-automation-save-section');
    if (existingSaveSection) existingSaveSection.remove();
    
    const existingVersionSection = containerEl.querySelector('.notebook-automation-version-section');
    if (existingVersionSection) existingVersionSection.remove();
    
    // Also remove any existing version divs that might be floating around
    const existingVersionDiv = containerEl.querySelector('.notebook-automation-version');
    if (existingVersionDiv) existingVersionDiv.remove();
    
    // Configuration File Section (combined status and save)
    containerEl.createEl("h3", { text: "Configuration File", cls: "notebook-automation-section-header" });
    const configFileContainer = containerEl.createDiv({ 
      cls: "notebook-automation-settings-group notebook-automation-config-status-section" 
    });

    // Environment variable detection and current config display
    // @ts-ignore
    const process = window.require ? window.require('process') : null;
    const envConfigPath = process?.env?.NOTEBOOKAUTOMATION_CONFIG;

    // Get the path of the currently loaded config file
    const loadedConfigPath = (window as any).notebookAutomationLoadedConfigPath;

    // Check if we're using a non-default config file and determine default config path
    let isNonDefaultConfig = false;
    let defaultConfigPath = '';
    
    // Always determine the default config path
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
      
      defaultConfigPath = path.join(resolvedPluginDir, 'default-config.json');
      
      // Check if we're using a non-default config
      if (loadedConfigPath) {
        // We have a loaded config - check if it's not the default
        const normalizedLoadedPath = path.resolve(loadedConfigPath);
        const normalizedDefaultPath = path.resolve(defaultConfigPath);
        isNonDefaultConfig = normalizedLoadedPath !== normalizedDefaultPath;
      } else if (envConfigPath) {
        // No loaded config but environment config is set - we're using environment config
        isNonDefaultConfig = true;
      }
      // If no loaded config and no environment config, we'll use default config (isNonDefaultConfig stays false)
    }

    // Determine current config status and paths
    let currentConfigPath = loadedConfigPath;
    let configStatus = '';
    let configDescription = '';
    
    if (envConfigPath) {
      // @ts-ignore
      const fs = window.require ? window.require('fs') : null;
      const envFileExists = fs ? fs.existsSync(envConfigPath) : false;
      
      if (envFileExists) {
        currentConfigPath = envConfigPath;
        configStatus = '✅ Environment Configuration';
        configDescription = 'Using NOTEBOOKAUTOMATION_CONFIG environment variable';
      } else {
        configStatus = '⚠️ Environment Config Missing';
        configDescription = 'NOTEBOOKAUTOMATION_CONFIG is set but file does not exist';
        currentConfigPath = envConfigPath; // Show the missing path
      }
    } else if (loadedConfigPath) {
      configStatus = '✅ Custom Configuration';
      configDescription = 'Using loaded configuration file';
    } else {
      configStatus = 'ℹ️ Default Configuration';
      configDescription = 'Using plugin default configuration';
      // Set the default config path
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
        currentConfigPath = path.join(resolvedPluginDir, 'default-config.json');
      }
    }

    // Display current config status
    const statusDiv = configFileContainer.createDiv({ cls: 'notebook-automation-config-status' });
    statusDiv.innerHTML = `
      <div style="display: flex; align-items: center; gap: 8px; margin-bottom: 8px;">
        <strong>${configStatus}</strong>
      </div>
      <div class="notebook-automation-file-path">${currentConfigPath || 'No config file available'}</div>
    `;

    // Add checkbox for updating default config (show when not using default config) and save button
    let updateDefaultCheckbox: HTMLInputElement | null = null;
    
    // Save button (always enabled)
    const saveSetting = new Setting(configFileContainer);
    saveSetting.settingEl.classList.add('notebook-automation-save-setting');
    
    // Add checkbox for default config update if needed
    if (isNonDefaultConfig && defaultConfigPath) {
      saveSetting
        .setName('Also update default configuration file')
        .addToggle(toggle => {
          const checkboxEl = toggle.toggleEl.querySelector('input[type="checkbox"]') as HTMLInputElement;
          if (checkboxEl) {
            updateDefaultCheckbox = checkboxEl;
          }
          toggle.setValue(false)
            .onChange(async (value) => {
              // Value is automatically handled by the toggle
            });
        });
    }
    
    // Add save button
    saveSetting.addButton(btn => {
      btn.setButtonText('Save')
        .setCta()
        .onClick(async () => {
          // If no loaded config path, use default config
          let targetPath = loadedConfigPath;
          if (!targetPath) {
            // First check for NOTEBOOKAUTOMATION_CONFIG environment variable
            if (envConfigPath) {
              targetPath = envConfigPath;
            } else {
              // Fallback to plugin directory default config
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
                targetPath = path.join(resolvedPluginDir, 'default-config.json');
              }
            }
          }
          
          if (!targetPath) {
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

            // Function to ensure directory exists and write config
            const writeConfigFile = (filePath: string, config: any) => {
              const configDir = path.dirname(filePath);
              if (!fs.existsSync(configDir)) {
                try {
                  fs.mkdirSync(configDir, { recursive: true });
                } catch (mkdirErr) {
                  throw new Error('Failed to create config directory: ' + (mkdirErr instanceof Error ? mkdirErr.message : String(mkdirErr)));
                }
              }
              fs.writeFileSync(filePath, JSON.stringify(config, null, 4), 'utf8');
            };

            // Write to the target config file
            writeConfigFile(targetPath, configToSave);
            
            const fileName = path.basename(targetPath);
            let successMessage = `✅ Config saved successfully to ${fileName}`;

            // Also write to default config if checkbox is checked
            if (updateDefaultCheckbox && updateDefaultCheckbox.checked && defaultConfigPath) {
              try {
                writeConfigFile(defaultConfigPath, configToSave);
                const defaultFileName = path.basename(defaultConfigPath);
                successMessage += ` and ${defaultFileName}`;
              } catch (defaultErr) {
                console.error('[Notebook Automation] Error saving to default config:', defaultErr);
                new Notice(`⚠️ Saved to custom config but failed to save to default: ${defaultErr instanceof Error ? defaultErr.message : String(defaultErr)}`);
                return;
              }
            }

            new Notice(successMessage);

            // Update global loaded config
            (window as any).notebookAutomationLoadedConfig = configToSave;

          } catch (err) {
            console.error('[Notebook Automation] Error saving config:', err);
            new Notice('Failed to save config: ' + (err instanceof Error ? err.message : String(err)));
          }
        });
    });

    // Add version information at the very bottom
    const versionContainer = containerEl.createDiv({ 
      cls: "notebook-automation-settings-group notebook-automation-version-section" 
    });

    // Add version information at the very bottom
    const versionDiv = versionContainer.createDiv({ cls: "notebook-automation-version" });
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
      
      // Second priority: User-configured custom path
      if (!configPath && this.plugin.settings.configPath) {
        const userConfigPath = this.plugin.settings.configPath;
        if (fs.existsSync(userConfigPath) && fs.statSync(userConfigPath).isFile()) {
          configPath = userConfigPath;
          console.log('[Notebook Automation] Auto-loading user-configured config path:', configPath);
        }
      }
      
      // Third priority: Use default-config.json from plugin directory
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

    // Add logging section (show only if advanced configuration is enabled)
    if (this.plugin.settings.advancedConfiguration) {
      this.addLoggingSection(fieldsDiv, configJson);
    }
    
    // Add other configuration section (only if advanced configuration is enabled)
    if (this.plugin.settings.advancedConfiguration) {
      this.addOtherConfigSection(fieldsDiv, configJson);
    }
    
    // Save button is handled by the main display method, not here
  }

  addPathsSection(fieldsDiv: HTMLDivElement, configJson: any) {
    // Add section title above the container
    fieldsDiv.createEl('h3', { text: 'Paths Configuration', cls: 'notebook-automation-section-header' });
    
    // Add section description
    const pathsDescriptionDiv = fieldsDiv.createDiv({ cls: 'notebook-automation-section-description' });
    pathsDescriptionDiv.innerHTML = `
      <p>Configure file paths and directories used by the notebook automation system. These settings define where the plugin looks for templates, where it saves generated content, and how it organizes your automated workflows.</p>
      
      <p><strong>Key features:</strong> Template file locations for consistent formatting, output directory management for organized content structure, and working directory settings for processing operations.</p>
    `;
    
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
        baseBlockValidation.innerHTML = this.formatValidationError(
          'Invalid File Path',
          getPathValidationErrorMessage('file'),
          'file-x'
        );
        baseBlockInput.classList.add('notebook-automation-input-invalid');
      }

      baseBlockInput.oninput = async (e: any) => {
        const inputValue = e.target.value;
        
        // Path validation
        if (inputValue && !isValidFilePath(inputValue)) {
          baseBlockValidation.classList.add('visible');
          baseBlockValidation.innerHTML = this.formatValidationError(
            'Invalid File Path',
            getPathValidationErrorMessage('file'),
            'file-x'
          );
          baseBlockInput.classList.add('notebook-automation-input-invalid');
        } else {
          baseBlockValidation.classList.remove('visible');
          baseBlockInput.classList.remove('notebook-automation-input-invalid');
        }
        
        // Save to plugin settings
        this.plugin.settings.baseBlockTemplateFilename = inputValue;
        await this.plugin.saveSettings();
      };    const keyMeta = [
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
          let errorTitle = 'Invalid Input';
          
          if (meta.validateFilePath && !isValidFilePath(currentValue)) {
            isValid = false;
            errorMessage = getPathValidationErrorMessage('file');
            errorTitle = 'Invalid File Path';
          } else if (meta.validateDirectoryPath && !isValidDirectoryPath(currentValue)) {
            isValid = false;
            errorMessage = getPathValidationErrorMessage('directory');
            errorTitle = 'Invalid Directory Path';
          } else if (meta.validatePath && !isValidFilePath(currentValue)) {
            isValid = false;
            errorMessage = getPathValidationErrorMessage('path');
            errorTitle = 'Invalid Path';
          }
          
          if (!isValid) {
            validationMessage.classList.add('visible');
            validationMessage.innerHTML = this.formatValidationError(errorTitle, errorMessage);
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
          let errorTitle = 'Invalid Input';
          
          if (inputValue) {
            if (meta.validateFilePath && !isValidFilePath(inputValue)) {
              isValid = false;
              errorMessage = getPathValidationErrorMessage('file');
              errorTitle = 'Invalid File Path';
            } else if (meta.validateDirectoryPath && !isValidDirectoryPath(inputValue)) {
              isValid = false;
              errorMessage = getPathValidationErrorMessage('directory');
              errorTitle = 'Invalid Directory Path';
            } else if (meta.validatePath && !isValidFilePath(inputValue)) {
              isValid = false;
              errorMessage = getPathValidationErrorMessage('path');
              errorTitle = 'Invalid Path';
            }
          }
          
          if (!isValid) {
            validationMessage.classList.add('visible');
            validationMessage.innerHTML = this.formatValidationError(errorTitle, errorMessage);
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
    fieldsDiv.createEl('h3', { text: 'AI Service Configuration', cls: 'notebook-automation-section-header' });
    
    // Add section description
    const aiDescriptionDiv = fieldsDiv.createDiv({ cls: 'notebook-automation-section-description' });
    aiDescriptionDiv.innerHTML = `
      <p>Configure AI services for automated content processing, summarization, and analysis. These settings connect the plugin to AI providers for generating summaries, extracting key insights, and creating structured content from your materials.</p>
      
      <p><strong>Supported providers:</strong> <a href="https://azure.microsoft.com/en-us/products/ai-services/openai-service" target="_blank">Azure OpenAI</a> for enterprise-grade AI, <a href="https://openai.com/api/" target="_blank">OpenAI</a> for direct API access, and <a href="https://www.ibm.com/products/watsonx-ai" target="_blank">IBM watsonx.ai</a> (Foundry) for comprehensive AI workflows.</p>
    `;
    
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

    // Create validation message element right under the provider dropdown
    const providerValidationDiv = providerControlDiv.createDiv({ cls: 'notebook-automation-field-validation' });

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
            validationMessage.innerHTML = this.formatValidationError(
              'Invalid URL',
              'Please enter a valid URL starting with http:// or https://. Example: https://api.openai.com/v1 or https://your-resource.openai.azure.com/'
            );
            fieldInput.classList.add('notebook-automation-input-invalid');
          }
        }

        fieldInput.oninput = (e: any) => {
          const inputValue = e.target.value;
          
          // URL validation for fields that require it
          if (field.validateUrl && validationMessage) {
            if (inputValue && !isValidUrl(inputValue)) {
              validationMessage.classList.add('visible');
              validationMessage.innerHTML = this.formatValidationError(
                'Invalid URL',
                'Please enter a valid URL starting with http:// or https://. Example: https://api.openai.com/v1 or https://your-resource.openai.azure.com/'
              );
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
    this.addAIProviderValidation(providerValidationDiv, currentProvider);

    // Handle provider selection change
    providerSelect.onchange = (e: any) => {
      const selectedProvider = e.target.value;
      updatedAiConfig.provider = selectedProvider;
      updateProviderFields(selectedProvider);
      
      // Validate the new provider immediately with feedback
      this.addAIProviderValidation(providerValidationDiv, selectedProvider);
      
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
    fieldsDiv.createEl('h3', { text: 'Microsoft Graph Configuration', cls: 'notebook-automation-section-header' });
    
    // Add section description
    const graphDescriptionDiv = fieldsDiv.createDiv({ cls: 'notebook-automation-section-description' });
    graphDescriptionDiv.innerHTML = `
      <p>Configure Microsoft Graph API integration for accessing OneDrive, SharePoint, and other Microsoft 365 services. This enables the plugin to process shared files and documents directly from your organization's cloud storage.</p>
      
      <p><strong>Requirements:</strong> A registered <a href="https://docs.microsoft.com/en-us/graph/auth-register-app-v2" target="_blank">Azure AD application</a> with appropriate permissions for Microsoft Graph API access. Used primarily for OneDrive shared link processing and collaborative document workflows.</p>
    `;
    
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
            validationMessage.innerHTML = this.formatValidationError(
              'Invalid URL',
              'Please enter a valid URL starting with http:// or https://. Example: https://graph.microsoft.com/v1.0 or https://login.microsoftonline.com/your-tenant-id'
            );
            fieldInput.classList.add('notebook-automation-input-invalid');
          } else if (field.validateGuid && !isValidGuid(currentValue)) {
            validationMessage.classList.add('visible');
            validationMessage.innerHTML = this.formatValidationError(
              'Invalid GUID Format',
              'Please enter a valid GUID in the format: 12345678-1234-5678-9abc-123456789012. You can find your application\'s Client ID in the Azure portal under App registrations.'
            );
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
          let errorTitle = 'Invalid Input';
          
          if (inputValue) {
            if (field.validateUrl && !isValidUrl(inputValue)) {
              isValid = false;
              errorMessage = 'Please enter a valid URL starting with http:// or https://. Example: https://graph.microsoft.com/v1.0 or https://login.microsoftonline.com/your-tenant-id';
              errorTitle = 'Invalid URL';
            } else if (field.validateGuid && !isValidGuid(inputValue)) {
              isValid = false;
              errorMessage = 'Please enter a valid GUID in the format: 12345678-1234-5678-9abc-123456789012. You can find your application\'s Client ID in the Azure portal under App registrations.';
              errorTitle = 'Invalid GUID Format';
            }
          }
          
          if (!isValid) {
            validationMessage.classList.add('visible');
            validationMessage.innerHTML = this.formatValidationError(errorTitle, errorMessage);
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
    fieldsDiv.createEl('h3', { text: 'Timeout Configuration', cls: 'notebook-automation-section-header' });
    
    // Add section description
    const timeoutDescriptionDiv = fieldsDiv.createDiv({ cls: 'notebook-automation-section-description' });
    timeoutDescriptionDiv.innerHTML = `
      <p>Configure timeout settings and rate limiting for AI service requests and file processing operations. These settings help manage system performance, prevent API overload, and ensure reliable processing of large document collections.</p>
      
      <p><strong>Key controls:</strong> Request timeouts to prevent hanging operations, retry mechanisms for handling temporary failures, parallelism limits for optimal resource usage, and rate limiting to respect API quotas and prevent throttling.</p>
    `;
    
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
              validationMessage.innerHTML = this.formatValidationError(
                'Invalid Number',
                `Please enter a valid number between 1 and 999,999. This setting controls ${field.label.toLowerCase()} and must be within reasonable limits for proper system operation.`
              );
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
    fieldsDiv.createEl('h3', { text: 'Logging Configuration', cls: 'notebook-automation-section-header' });
    
    // Add section description
    const loggingDescriptionDiv = fieldsDiv.createDiv({ cls: 'notebook-automation-section-description' });
    loggingDescriptionDiv.innerHTML = `
      <p>Configure logging behavior for debugging, monitoring, and troubleshooting automation processes. These settings control what information is captured, where logs are stored, and how detailed the logging output should be.</p>
      
      <p><strong>Log management:</strong> Directory configuration for organized log storage, log level settings to control verbosity from basic info to detailed debugging, and retention policies for managing disk space usage.</p>
    `;
    
    const loggingSection = fieldsDiv.createDiv({ cls: 'notebook-automation-logging-section' });

    const loggingConfig = configJson.logging || {};
    const pathsConfig = configJson.paths || {};
    
    // Add logging directory path field with inline button
    const logDirDiv = loggingSection.createDiv({ cls: 'setting-item notebook-automation-custom-setting' });
    const logDirInfoDiv = logDirDiv.createDiv({ cls: 'setting-item-info' });
    const logDirNameDiv = logDirInfoDiv.createDiv({ cls: 'setting-item-name' });
    logDirNameDiv.setText('Logging Directory');
    const logDirDescDiv = logDirInfoDiv.createDiv({ cls: 'setting-item-description' });
    logDirDescDiv.setText('Directory path where log files are stored');

    const logDirControlDiv = logDirDiv.createDiv({ cls: 'setting-item-control notebook-automation-input-control' });
    
    // Create container for input and button
    const logDirInputContainer = logDirControlDiv.createDiv({ cls: 'notebook-automation-input-button-container' });
    
    const logDirInput = logDirInputContainer.createEl('input', {
      type: 'text',
      cls: 'notebook-automation-path-input notebook-automation-path-with-button'
    });
    logDirInput.value = pathsConfig.logging_dir || 'd:/source/notebook-automation/logs';
    logDirInput.placeholder = 'Enter logging directory path...';

    // Add the Open Directory button inline
    const openDirButton = logDirInputContainer.createEl('button', {
      cls: 'notebook-automation-inline-button',
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

    // Create validation message element for logging directory
    const logDirValidationMessage = logDirControlDiv.createDiv({ cls: 'notebook-automation-field-validation' });
    
    // Initial validation for existing value
    const initialLogDirValue = logDirInput.value;
    if (initialLogDirValue && !isValidDirectoryPath(initialLogDirValue)) {
      logDirValidationMessage.classList.add('visible');
      logDirValidationMessage.innerHTML = this.formatValidationError(
        'Invalid Logging Directory',
        getPathValidationErrorMessage('directory') + ' This directory will be used to store application logs and debug information.'
      );
      logDirInput.classList.add('notebook-automation-input-invalid');
    }

    logDirInput.oninput = (e: any) => {
      const inputValue = e.target.value;
      
      // Directory path validation
      if (inputValue && !isValidDirectoryPath(inputValue)) {
        logDirValidationMessage.classList.add('visible');
        logDirValidationMessage.innerHTML = this.formatValidationError(
          'Invalid Logging Directory',
          getPathValidationErrorMessage('directory') + ' This directory will be used to store application logs and debug information.'
        );
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
              validationMessage.innerHTML = this.formatValidationError(
                'Invalid Number',
                `Please enter a valid number between 1 and 999,999. This controls ${field.label.toLowerCase()} for log file management and must be within reasonable limits.`
              );
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
  }

  addExtensionsSection(fieldsDiv: HTMLDivElement, configJson: any) {
    // Add section title above the container
    fieldsDiv.createEl('h3', { text: 'File Extensions', cls: 'notebook-automation-section-header' });
    
    // Add section description
    const extensionsDescriptionDiv = fieldsDiv.createDiv({ cls: 'notebook-automation-section-description' });
    extensionsDescriptionDiv.innerHTML = `
      <p>Define which file types the automation system can process. These extensions determine what files are recognized and handled by the various processing modules.</p>
      
      <p><strong>Supported types:</strong> Video files for transcript generation and content extraction, PDF documents for text and image extraction, and additional formats for specialized processing workflows.</p>
    `;
    
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

    // PDF Extract Images toggle - custom layout with inline toggle
    const pdfExtractContainer = extensionsSection.createDiv({ cls: 'notebook-automation-custom-setting pdf-extract-setting' });
    
    // Create title row with toggle
    const titleRow = pdfExtractContainer.createDiv({ cls: 'pdf-extract-title-row' });
    const titleText = titleRow.createSpan({ cls: 'setting-item-name', text: 'PDF Extract Images' });
    const toggleContainer = titleRow.createDiv({ cls: 'pdf-extract-toggle-container' });
    
    // Create toggle
    const pdfExtractToggle = toggleContainer.createEl('input', { 
      type: 'checkbox',
      cls: 'pdf-extract-toggle'
    });
    pdfExtractToggle.checked = configJson.pdf_extract_images || false;
    pdfExtractToggle.addEventListener('change', (e) => {
      const value = (e.target as HTMLInputElement).checked;
      // Update global config
      if ((window as any).notebookAutomationLoadedConfig) {
        (window as any).notebookAutomationLoadedConfig.pdf_extract_images = value;
      }
    });
    
    // Create description row
    const descRow = pdfExtractContainer.createDiv({ 
      cls: 'setting-item-description',
      text: 'Extract images from PDF files during processing. When enabled, the automation will extract and save images found in PDF documents alongside the generated markdown notes. This is useful for preserving diagrams, charts, and other visual content from academic papers and documents.'
    });
  }

  addBannersSection(fieldsDiv: HTMLDivElement, configJson: any) {
    // Add section title above the container
    fieldsDiv.createEl('h3', { text: 'Banners Configuration', cls: 'notebook-automation-section-header' });
    
    const bannersSection = fieldsDiv.createDiv({ cls: 'notebook-automation-banners-section' });

    const bannersConfig = configJson.banners || {};

    // Basic banner fields
    const bannerFields = [
      { key: 'default', label: 'Default Image Banner', desc: 'Default banner image filename', type: 'text' },
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
    fieldsDiv.createEl('h3', { text: 'Other Configuration', cls: 'notebook-automation-section-header' });
    
    // Add section description
    const otherDescriptionDiv = fieldsDiv.createDiv({ cls: 'notebook-automation-section-description' });
    otherDescriptionDiv.innerHTML = `
      <p>Additional configuration options for specialized features and advanced automation workflows. These settings provide fine-grained control over specific processing behaviors and experimental features.</p>
      
      <p><strong>Advanced options:</strong> Specialized file type handling, custom processing parameters, experimental feature toggles, and integration settings for third-party tools and services.</p>
    `;
    
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
      videoExtValidationMessage.innerHTML = this.formatValidationError(
        'Invalid Video Extensions',
        `The following extensions are invalid: <strong>${invalidVideoExts.join(', ')}</strong><br><br>File extensions must start with a dot (.) and contain only letters and numbers. Examples: .mp4, .mov, .avi, .mkv`
      );
      videoExtTextarea.classList.add('notebook-automation-input-invalid');
    }
    
    videoExtTextarea.oninput = (e: any) => {
      const extensions = e.target.value.split('\n').filter((ext: string) => ext.trim().length > 0).map((ext: string) => ext.trim());
      const invalidExtensions = extensions.filter((ext: string) => !isValidFileExtension(ext));
      
      if (invalidExtensions.length > 0) {
        videoExtValidationMessage.classList.add('visible');
        videoExtValidationMessage.innerHTML = this.formatValidationError(
          'Invalid Video Extensions',
          `The following extensions are invalid: <strong>${invalidExtensions.join(', ')}</strong><br><br>File extensions must start with a dot (.) and contain only letters and numbers. Examples: .mp4, .mov, .avi, .mkv`
        );
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
      pdfExtValidationMessage.innerHTML = this.formatValidationError(
        'Invalid PDF Extensions',
        `The following extensions are invalid: <strong>${invalidPdfExts.join(', ')}</strong><br><br>File extensions must start with a dot (.) and contain only letters and numbers. Typical PDF extension: .pdf`
      );
      pdfExtTextarea.classList.add('notebook-automation-input-invalid');
    }
    
    pdfExtTextarea.oninput = (e: any) => {
      const extensions = e.target.value.split('\n').filter((ext: string) => ext.trim().length > 0).map((ext: string) => ext.trim());
      const invalidExtensions = extensions.filter((ext: string) => !isValidFileExtension(ext));
      
      if (invalidExtensions.length > 0) {
        pdfExtValidationMessage.classList.add('visible');
        pdfExtValidationMessage.innerHTML = this.formatValidationError(
          'Invalid PDF Extensions',
          `The following extensions are invalid: <strong>${invalidExtensions.join(', ')}</strong><br><br>File extensions must start with a dot (.) and contain only letters and numbers. Typical PDF extension: .pdf`
        );
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

  /**
   * Adds markdown-specific banner configuration settings.
   */
  addMarkdownBannerSettings(container: HTMLElement) {
    // Template Banners setting
    const templateBannersSetting = new Setting(container)
      .setName('Template Banners')
      .setDesc('JSON configuration for banners based on content templates. Define banners for different content types like "main", "program", "course", "assignment". Example: {"main": "main-header.png", "course": "course-header.png"}');
    
    templateBannersSetting.settingEl.addClass('notebook-automation-custom-setting');
    const templateBannersTextarea = templateBannersSetting.controlEl.createEl('textarea', {
      cls: 'notebook-automation-path-input',
      attr: { 'data-template-banners': 'true' }
    });
    
    // Load current template banners or set default
    const currentTemplateBanners = (this.plugin.settings as any).templateBanners || {};
    const defaultTemplateBanners = {
      "main": "main-header.png",
      "program": "program-header.png", 
      "course": "course-header.png",
      "assignment": "assignment-header.png"
    };
    
    // Use default values if current is empty
    const templateBannersToShow = Object.keys(currentTemplateBanners).length === 0 
      ? defaultTemplateBanners 
      : currentTemplateBanners;
      
    templateBannersTextarea.value = JSON.stringify(templateBannersToShow, null, 2);
    templateBannersTextarea.placeholder = 'Enter template banners JSON...';
    
    templateBannersTextarea.oninput = async (e: any) => {
      try {
        const parsedValue = JSON.parse(e.target.value);
        (this.plugin.settings as any).templateBanners = parsedValue;
        await this.plugin.saveSettings();
        templateBannersTextarea.classList.remove('notebook-automation-input-invalid');
      } catch (error) {
        templateBannersTextarea.classList.add('notebook-automation-input-invalid');
      }
    };

    // Filename Patterns setting
    const filenamePatternsetting = new Setting(container)
      .setName('Filename Patterns')
      .setDesc('JSON configuration for banners based on filename patterns. Use wildcards (*) to match filenames. Example: {"*index*": "index-banner.png", "assignment-*": "assignment-banner.png", "*final*": "final-project-banner.png"}');
    
    filenamePatternsetting.settingEl.addClass('notebook-automation-custom-setting');
    const filenamePatternsTextarea = filenamePatternsetting.controlEl.createEl('textarea', {
      cls: 'notebook-automation-path-input',
      attr: { 'data-filename-patterns': 'true' }
    });
    
    // Load current filename patterns or set default
    const currentFilenamePatterns = (this.plugin.settings as any).filenamePatterns || {};
    const defaultFilenamePatterns = {
      "*index*": "index-banner.png",
      "*readme*": "readme-banner.png",
      "assignment-*": "assignment-banner.png",
      "*final*": "final-project-banner.png"
    };
    
    // Use default values if current is empty
    const filenamePatternsToShow = Object.keys(currentFilenamePatterns).length === 0 
      ? defaultFilenamePatterns 
      : currentFilenamePatterns;
      
    filenamePatternsTextarea.value = JSON.stringify(filenamePatternsToShow, null, 2);
    filenamePatternsTextarea.placeholder = 'Enter filename patterns JSON...';
    
    filenamePatternsTextarea.oninput = async (e: any) => {
      try {
        const parsedValue = JSON.parse(e.target.value);
        (this.plugin.settings as any).filenamePatterns = parsedValue;
        await this.plugin.saveSettings();
        filenamePatternsTextarea.classList.remove('notebook-automation-input-invalid');
      } catch (error) {
        filenamePatternsTextarea.classList.add('notebook-automation-input-invalid');
      }
    };
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
      .notebook-automation-banners-section { display: block; }
      .notebook-automation-banners-header { display: block; }
      .notebook-automation-timeout-section { display: ${this.plugin.settings.advancedConfiguration ? 'block' : 'none'}; }
      .notebook-automation-other-section { display: ${this.plugin.settings.advancedConfiguration ? 'block' : 'none'}; }
      .notebook-automation-paths-section { display: ${this.plugin.settings.advancedConfiguration ? 'block' : 'none'}; }
      .notebook-automation-ai-service-section { display: ${this.plugin.settings.advancedConfiguration ? 'block' : 'none'}; }
      .notebook-automation-logging-section { display: ${this.plugin.settings.advancedConfiguration ? 'block' : 'none'}; }
      
      /* Always show these sections when config is loaded */
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

  addAIProviderValidation(validationContainer: HTMLDivElement, provider: string) {
    // Clear previous validation content
    validationContainer.empty();
    validationContainer.classList.remove('visible');

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
      // Show warning validation
      validationContainer.classList.add('visible');
      validationContainer.innerHTML = this.formatValidationError(
        'Environment Variable Missing',
        `The <code>${envInfo.varName}</code> environment variable is not set for the ${provider.toUpperCase()} provider. This will cause command execution to fail.<br><br><strong>Solution:</strong> Set <code>${envInfo.varName}</code> in your system environment variables, then restart Obsidian.`
      );
    } else {
      // Show success validation  
      validationContainer.classList.add('visible');
      validationContainer.innerHTML = this.formatValidationSuccess(
        'Environment Variable Set',
        `<code>${envInfo.varName}</code> is configured for the ${provider.toUpperCase()} provider.`
      );
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
