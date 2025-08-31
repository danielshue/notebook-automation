import { App, PluginSettingTab, Setting, Notice } from 'obsidian';
import type NotebookAutomationPlugin from '../main';
import { ensureExecutableExists, type DownloadProgressCallback } from '../utils/plugin-assets';

/**
 * Validates whether a string is a well-formed HTTP or HTTPS URL.
 * @param string - The string to validate.
 * @returns True if the string is a valid URL, false otherwise.
 */
// URL validation utility function
function isValidUrl(string: string): boolean {
  try {
    const url = new URL(string);
    return ['http:', 'https:'].includes(url.protocol);
  } catch (_) {
    return false;
  }
}

/**
 * Validates whether a string is a valid GUID (UUID v4/v5).
 * @param string - The string to validate.
 * @returns True if the string is a valid GUID, false otherwise.
 */
// GUID validation utility function
function isValidGuid(string: string): boolean {
  const guidRegex = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;
  return guidRegex.test(string);
}

/**
 * Opens a system dialog for selecting a directory.
 * @returns Promise resolving to the selected directory path or null if cancelled.
 */
function browseForDirectory(): Promise<string | null> {
  return new Promise((resolve) => {
    try {
      // @ts-ignore
      const { dialog } = window.require ? window.require('electron').remote || window.require('@electron/remote') : null;
      if (!dialog) {
        new Notice('Directory browsing not available in this environment.');
        resolve(null);
        return;
      }
      
      const result = dialog.showOpenDialogSync({
        properties: ['openDirectory'],
        title: 'Select Directory'
      });
      
      resolve(result && result.length > 0 ? result[0] : null);
    } catch (err) {
      new Notice('Error opening directory browser: ' + (err instanceof Error ? err.message : String(err)));
      resolve(null);
    }
  });
}

/**
 * Opens a system dialog for selecting a file, optionally filtered by extension.
 * @param filters - Array of file type filters.
 * @returns Promise resolving to the selected file path or null if cancelled.
 */
function browseForFile(filters?: Array<{name: string, extensions: string[]}>): Promise<string | null> {
  return new Promise((resolve) => {
    try {
      // @ts-ignore
      const { dialog } = window.require ? window.require('electron').remote || window.require('@electron/remote') : null;
      if (!dialog) {
        new Notice('File browsing not available in this environment.');
        resolve(null);
        return;
      }
      
      const result = dialog.showOpenDialogSync({
        properties: ['openFile'],
        title: 'Select File',
        filters: filters || [{ name: 'All Files', extensions: ['*'] }]
      });
      
      resolve(result && result.length > 0 ? result[0] : null);
    } catch (err) {
      new Notice('Error opening file browser: ' + (err instanceof Error ? err.message : String(err)));
      resolve(null);
    }
  });
}

/**
 * Validates whether a string is a valid file extension (e.g., ".md").
 * @param string - The string to validate.
 * @returns True if the string is a valid file extension, false otherwise.
 */
function isValidFileExtension(string: string): boolean {
  const extensionRegex = /^\.[a-zA-Z0-9]+$/;
  return extensionRegex.test(string);
}

/**
 * Returns a platform-specific error message for invalid file, directory, or path.
 * @param validationType - Type of validation ('file', 'directory', 'path').
 * @returns Error message string.
 */
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

/**
 * Validates a file path for the current platform (Windows/Unix).
 * @param string - The file path to validate.
 * @returns True if the path is valid, false otherwise.
 */
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

/**
 * Validates a directory path for the current platform (Windows/Unix).
 * @param string - The directory path to validate.
 * @returns True if the path is valid, false otherwise.
 */
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

/**
 * Settings tab UI for Notebook Automation plugin.
 * Handles feature toggles, flags, banners, config file management, and advanced options.
 */
export class NotebookAutomationSettingTab extends PluginSettingTab {
  plugin: NotebookAutomationPlugin;

  /**
   * Creates a new settings tab for the plugin.
   * @param app - The Obsidian app instance.
   * @param plugin - The NotebookAutomationPlugin instance.
   */
  constructor(app: App, plugin: NotebookAutomationPlugin) {
    super(app, plugin);
    this.plugin = plugin;
  }

  /**
   * Formats a validation error message with improved styling and icons.
   * @param title - The error title.
   * @param message - The error message.
   * @param icon - Optional icon name (default: 'alert-triangle').
   * @returns HTML string for the error message.
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
   * @param title - The success title.
   * @param message - The success message.
   * @returns HTML string for the success message.
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

  /**
   * Renders the settings tab UI and all configuration sections.
   */
  display(): void {
    this.injectCustomStyles();
    const { containerEl } = this;
    containerEl.empty();
    containerEl.classList.add('notebook-automation-container');
    containerEl.addClass('notebook-automation-settings');

    // 1. Feature toggles section
    containerEl.createEl("h3", { text: "Features", cls: "notebook-automation-section-header" });
    const featureGroup = containerEl.createDiv({ cls: "notebook-automation-settings-group" });

    // AI Video Summary
    new Setting(featureGroup)
      .setName("AI Video Summary")
      .setDesc("Enables AI-powered video summarization features in context menus. Right-click folders to import and summarize all videos, or reprocess existing video summaries with AI analysis.")
      .addToggle(toggle => {
        toggle.setValue(this.plugin.settings.enableVideoSummary ?? true)
          .onChange(async (value) => {
            this.plugin.settings.enableVideoSummary = value;
            await this.plugin.saveSettings();
          });
      });

    // AI PDF Summary
    new Setting(featureGroup)
      .setName("AI PDF Summary")
      .setDesc("Enables AI-powered PDF document summarization features in context menus. Right-click folders to import and summarize all PDFs, or reprocess existing PDF summaries with AI analysis.")
      .addToggle(toggle => {
        toggle.setValue(this.plugin.settings.enablePdfSummary ?? true)
          .onChange(async (value) => {
            this.plugin.settings.enablePdfSummary = value;
            await this.plugin.saveSettings();
          });
      });

    // AI HTML/EPUB/TXT Summary
    new Setting(featureGroup)
      .setName("AI HTML/EPUB/TXT Summary")
      .setDesc("Enables AI-powered HTML, EPUB, and TXT document summarization features in context menus. Right-click folders to import and summarize all HTML/EPUB/TXT files, or reprocess existing summaries with AI analysis.")
      .addToggle(toggle => {
        toggle.setValue(this.plugin.settings.enableHtmlEpubTxtSummary ?? true)
          .onChange(async (value) => {
            this.plugin.settings.enableHtmlEpubTxtSummary = value;
            await this.plugin.saveSettings();
          });
      });

    // Index Creation
    new Setting(featureGroup)
      .setName("Index Creation")
      .setDesc("Enables automatic index generation for organizing notebook structure. Right-click folders to build comprehensive indexes with file summaries and navigation links.")
      .addToggle(toggle => {
        toggle.setValue(this.plugin.settings.enableIndexCreation ?? true)
          .onChange(async (value) => {
            this.plugin.settings.enableIndexCreation = value;
            await this.plugin.saveSettings();
          });
      });

    // Ensure Metadata
    new Setting(featureGroup)
      .setName("Ensure Metadata")
      .setDesc("Enables metadata consistency management to maintain proper YAML frontmatter across your notebook. Right-click folders to automatically ensure all markdown files have consistent metadata fields.")
      .addToggle(toggle => {
        toggle.setValue(this.plugin.settings.enableEnsureMetadata ?? true)
          .onChange(async (value) => {
            this.plugin.settings.enableEnsureMetadata = value;
            await this.plugin.saveSettings();
          });
      });

    // Document Placeholders
    new Setting(featureGroup)
      .setName("Document Placeholders")
      .setDesc("Automatically create placeholder markdown files for documents (videos, PDFs, HTML) when synchronizing directories with OneDrive. Generates structured notes with metadata for easy organization and note-taking. This will allow you to later go back and run the AI Summary and OneDrive shared Link on the folders and files.")
      .addToggle(toggle => {
        toggle.setValue(this.plugin.settings.enableDocumentPlaceholders ?? true)
          .onChange(async (value) => {
            this.plugin.settings.enableDocumentPlaceholders = value;
            await this.plugin.saveSettings();
          });
      });

    // Command flags section
    containerEl.createEl("h3", { text: "Flags", cls: "notebook-automation-section-header" });
    const flagsGroup = containerEl.createDiv({ cls: "notebook-automation-settings-group" });

    // Verbose flag
    new Setting(flagsGroup)
      .setName("Verbose Mode")
      .setDesc("Enable detailed output during command execution with progress updates and processing details. Useful for monitoring long-running tasks and understanding operations.")
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
      .setDesc("Enable comprehensive debug logging for technical troubleshooting including API calls, configuration parsing, and error traces. Generates significantly more output than verbose mode for diagnosing issues.")
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
      .setDesc("Simulate all operations without making actual changes to files or folders. Allows you to preview what the automation would do without risk of unwanted modifications.")
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
      .setDesc("Override safety checks and force operations to proceed even when normally skipped or blocked. Use with caution as this can overwrite existing work or ignore important safety mechanisms.")
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
      .setDesc("Enable banner images or markdown content at the top of generated index pages. Uses configured banner settings and filename patterns to determine appropriate content.")
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
              // Note: Configuration fields will be refreshed by the main display method
            } else if (!value) {
              // If banners are disabled, refresh the display to hide the banners section
              const currentConfig = (window as any).notebookAutomationLoadedConfig;
              const currentPath = (window as any).notebookAutomationLoadedConfigPath;
              // Note: Configuration fields will be refreshed by the main display method
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
            // Note: Configuration fields will be refreshed by the main display method
          });
      });

    // Unidirectional Sync flag
    new Setting(flagsGroup)
      .setName("Unidirectional Sync")
      .setDesc("Enable unidirectional synchronization mode for directory sync operations (default: ON for safety). When enabled, synchronization will only flow from OneDrive to Vault (OneDrive → Vault), preventing any changes from being pushed back to OneDrive. This is the recommended safe mode. Disable this for bidirectional sync where changes can flow in both directions.")
      .addToggle(toggle => {
        toggle.setValue(this.plugin.settings.unidirectionalSync ?? true)
          .onChange(async (value) => {
            this.plugin.settings.unidirectionalSync = value;
            await this.plugin.saveSettings();
          });
      });

    // Recursive Directory Sync flag
    new Setting(flagsGroup)
      .setName("Recursive Directory Sync")
      .setDesc("Enable recursive directory scanning for sync operations. When enabled, directory synchronization will process the entire directory tree including all subdirectories and nested folders. When disabled, only the immediate children (first level) of the target directory will be synchronized. This affects how deep the sync operation goes into the folder hierarchy when synchronizing between OneDrive and your vault. When Document Placeholders is also enabled, recursive mode will create placeholder markdown files for documents found in all subdirectories, not just the immediate level.")
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
      <p>Configure banner images for generated markdown files that integrate with the 
      <a href="https://github.com/noatpad/obsidian-banners" target="_blank">Obsidian Banner Plugin</a>. 
      Images must be stored within your vault and are automatically selected based on content type or filename patterns.</p>
    `;
    
    // Add banner format setting first
    const bannerFormatSetting = new Setting(bannersContainer)
      .setName('Banner Format')
      .setDesc('Choose how banners are added to generated files. "Image" mode integrates with the Obsidian Banner Plugin to display header images at the top of created index files. "Markdown" mode inserts custom markdown content into each generated file based on the template and filename patterns you configure below.');
    
    // Create custom description with HTML rendering to replace the basic one
    const bannerFormatDesc = bannerFormatSetting.descEl;
    bannerFormatDesc.innerHTML = 'Choose how banners are added to generated files. "Image" mode integrates with the <a href="https://github.com/noatpad/obsidian-banners" target="_blank">Obsidian Banner Plugin</a> to display header images at the top of created index files. "Markdown" mode inserts custom markdown content into each generated file based on the template and filename patterns you configure below.';
    
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
        html_extensions: [],
        banners: {}
      };
    }

    // Show informational message if Advanced Configuration is not enabled but config is loaded
    const loadedConfig = (window as any).notebookAutomationLoadedConfig;
    if (loadedConfig && !this.plugin.settings.advancedConfiguration) {
      const infoDiv = containerEl.createDiv({ cls: 'notebook-automation-config-info' });
      infoDiv.createEl('h4', { text: 'Configuration Loaded Successfully', cls: 'notebook-automation-info-title' });
      infoDiv.createEl('p', { 
        text: 'Configuration file has been loaded, but no editable fields are currently displayed. Enable "Advanced Configuration" in the Flags section above to see and edit the configuration fields.',
        cls: 'notebook-automation-info-message' 
      });
    }

    // Always show config fields (create default structure if no config loaded)
    let bottomConfigToDisplay = (window as any).notebookAutomationLoadedConfig;
    if (!bottomConfigToDisplay) {
      // Create a default config structure to show empty fields
      bottomConfigToDisplay = {
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
        html_extensions: [],
        banners: {}
      };
    }
    
  // Anchor for advanced configuration sections (they will be inserted BEFORE this anchor)
  const advancedAnchor = containerEl.createDiv();
  advancedAnchor.id = 'na-advanced-anchor';
  advancedAnchor.style.display = 'none';

    // --- Reinsert Custom Config File section here so it appears at bottom before status & save ---
    containerEl.createEl('h3', { 
      text: 'Custom Config File (Optional)',
      cls: 'notebook-automation-section-header'
    });
    const customConfigDescriptionDiv = containerEl.createDiv({ cls: 'notebook-automation-section-description' });
    const nodeProcess = window.require ? window.require('process') : null;
    const isWindows = nodeProcess?.platform === 'win32';
    if (isWindows) {
      customConfigDescriptionDiv.innerHTML = `
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
      customConfigDescriptionDiv.innerHTML = `
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
    const configPathContainer = containerEl.createDiv({
      cls: 'notebook-automation-config-path-container notebook-automation-input-button-container'
    });
    const configPathInput = configPathContainer.createEl("input", {
      type: "text",
      placeholder: "Optional: Path to custom config.json...",
      cls: 'notebook-automation-config-path-input notebook-automation-path-with-button'
    });
    configPathInput.value = this.plugin.settings.configPath || "";
    configPathInput.onchange = async (e: any) => {
      this.plugin.settings.configPath = e.target.value;
      await this.plugin.saveSettings();
    };
    const browseConfigButton = configPathContainer.createEl("button", {
      text: "Browse",
      cls: 'notebook-automation-inline-button'
    });
    browseConfigButton.onclick = async () => {
      const selectedPath = await browseForFile([
        { name: 'JSON Files', extensions: ['json'] },
        { name: 'All Files', extensions: ['*'] }
      ]);
      if (selectedPath) {
        configPathInput.value = selectedPath;
        this.plugin.settings.configPath = selectedPath;
        await this.plugin.saveSettings();
        new Notice(`Selected custom config file: ${selectedPath}`);
      }
    };
    const validateBtn = configPathContainer.createEl("button", {
      text: "Load",
      cls: 'notebook-automation-validate-btn'
    });
    validateBtn.onclick = async () => {
      const path = this.plugin.settings.configPath;
      if (!path) {
        new Notice("Please enter a config file path first.");
        return;
      }
      const prevError = containerEl.querySelector('.notebook-automation-config-fields');
      if (prevError) prevError.remove();
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
            this.refreshConfigurationFileStatus();
          } catch (jsonErr) {
            const configError = "Invalid JSON: " + (jsonErr instanceof Error ? jsonErr.message : String(jsonErr));
            new Notice(configError);
            this.displayLoadedConfig(null, undefined, configError);
            this.refreshConfigurationFileStatus();
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

  // Configuration File Section (status display only) - Positioned at bottom before Save section
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
        configStatus = '✅ Environment Variable';
        configDescription = 'Using NOTEBOOKAUTOMATION_CONFIG environment variable';
      } else {
        configStatus = '⚠️ Environment Config Missing';
        configDescription = 'NOTEBOOKAUTOMATION_CONFIG is set but file does not exist';
        currentConfigPath = envConfigPath; // Show the missing path
      }
    } else if (loadedConfigPath) {
      // Check if the loaded config is the default plugin directory config
      // @ts-ignore
      const path = window.require ? window.require('path') : null;
      if (path && defaultConfigPath) {
        const normalizedLoadedPath = path.resolve(loadedConfigPath);
        const normalizedDefaultPath = path.resolve(defaultConfigPath);
        
        if (normalizedLoadedPath === normalizedDefaultPath) {
          configStatus = '✅ Plugin Directory';
          configDescription = 'Using default-config.json from plugin directory';
        } else {
          configStatus = '✅ Custom Configuration';
          configDescription = 'Using custom configuration file';
        }
      } else {
        configStatus = '✅ Custom Configuration';
        configDescription = 'Using loaded configuration file';
      }
    } else {
      configStatus = 'ℹ️ Plugin Directory';
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

    // Create standalone Save Configuration section - positioned for better visibility
    containerEl.createEl('h3', { 
      text: 'Save Configuration',
      cls: 'notebook-automation-section-header'
    });
    const saveContainer = containerEl.createDiv({ 
      cls: "notebook-automation-settings-group notebook-automation-save-section" 
    });

    // Environment variable detection for Save section
    // @ts-ignore
    const processForSave = window.require ? window.require('process') : null;
    const envConfigPathForSave = processForSave?.env?.NOTEBOOKAUTOMATION_CONFIG;
    const loadedConfigPathForSave = (window as any).notebookAutomationLoadedConfigPath;
    
    // Check if we're using a non-default config file for Save section
    let isNonDefaultConfigForSave = false;
    let defaultConfigPathForSave = '';
    
    // @ts-ignore
    const pathForSave = window.require ? window.require('path') : null;
    if (pathForSave && this.plugin.manifest?.dir) {
      const adapter = this.plugin.app?.vault?.adapter;
      let resolvedPluginDir = this.plugin.manifest.dir;
      // @ts-ignore
      if (adapter && typeof adapter.getBasePath === 'function') {
        try {
          // @ts-ignore
          const vaultRoot = adapter.getBasePath();
          resolvedPluginDir = pathForSave.resolve(vaultRoot, this.plugin.manifest.dir);
        } catch (err) {
          // Fallback to original path
        }
      }
      
      defaultConfigPathForSave = pathForSave.join(resolvedPluginDir, 'default-config.json');
      
      // Check if we're using a non-default config
      if (loadedConfigPathForSave) {
        const normalizedLoadedPath = pathForSave.resolve(loadedConfigPathForSave);
        const normalizedDefaultPath = pathForSave.resolve(defaultConfigPathForSave);
        isNonDefaultConfigForSave = normalizedLoadedPath !== normalizedDefaultPath;
      } else if (envConfigPathForSave) {
        isNonDefaultConfigForSave = true;
      }
    }

    // Add checkbox for updating default config and save button
    let updateDefaultCheckboxStandalone: HTMLInputElement | null = null;
    
    // Save button setting
    const saveSettingStandalone = new Setting(saveContainer);
    saveSettingStandalone.settingEl.classList.add('notebook-automation-save-setting');
    
    // Add checkbox for default config update if needed
    if (isNonDefaultConfigForSave && defaultConfigPathForSave) {
      saveSettingStandalone
        .setName('Also update default configuration file')
        .addToggle(toggle => {
          const checkboxEl = toggle.toggleEl.querySelector('input[type="checkbox"]') as HTMLInputElement;
          if (checkboxEl) {
            updateDefaultCheckboxStandalone = checkboxEl;
          }
          toggle.setValue(false)
            .onChange(async (value) => {
              // Value is automatically handled by the toggle
            });
        });
    }
    
    // Add save button
    saveSettingStandalone.addButton(btn => {
      btn.setButtonText('Save')
        .setCta()
        .onClick(async () => {
          // If no loaded config path, use default config
          let targetPath = loadedConfigPathForSave;
          if (!targetPath) {
            // First check for NOTEBOOKAUTOMATION_CONFIG environment variable
            if (envConfigPathForSave) {
              targetPath = envConfigPathForSave;
            } else {
              // Fallback to plugin directory default config
              if (pathForSave && this.plugin.manifest?.dir) {
                const adapter = this.plugin.app?.vault?.adapter;
                let resolvedPluginDir = this.plugin.manifest.dir;
                // @ts-ignore
                if (adapter && typeof adapter.getBasePath === 'function') {
                  try {
                    // @ts-ignore
                    const vaultRoot = adapter.getBasePath();
                    resolvedPluginDir = pathForSave.resolve(vaultRoot, this.plugin.manifest.dir);
                  } catch (err) {
                    // Fallback to original path
                  }
                }
                targetPath = pathForSave.join(resolvedPluginDir, 'default-config.json');
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
            if (!fs || !pathForSave) {
              new Notice('File system access is not available in this environment.');
              return;
            }

            const currentConfig = (window as any).notebookAutomationLoadedConfig || {};

            // Build complete configuration object
            const configToSave = {
              ConfigFilePath: this.plugin.settings.configPath || '',
              paths: currentConfig.paths || {},
              microsoft_graph: currentConfig.microsoft_graph || {},
              aiservice: currentConfig.aiservice || {
                provider: 'azure',
                azure: {},
                openai: {},
                foundry: {},
                timeout: {},
                retry_policy: {}
              },
              video_extensions: currentConfig.video_extensions || [],
              pdf_extensions: currentConfig.pdf_extensions || [],
              html_extensions: currentConfig.html_extensions || [],
              banners: currentConfig.banners || {}
            };

            // Create directory if it doesn't exist
            const targetDir = pathForSave.dirname(targetPath);
            if (!fs.existsSync(targetDir)) {
              fs.mkdirSync(targetDir, { recursive: true });
            }

            // Write main config file
            fs.writeFileSync(targetPath, JSON.stringify(configToSave, null, 2));
            new Notice(`✅ Configuration saved to: ${targetPath}`);

            // Update checkbox for also updating default
            const shouldUpdateDefault = updateDefaultCheckboxStandalone?.checked ?? false;
            if (shouldUpdateDefault && defaultConfigPathForSave && targetPath !== defaultConfigPathForSave) {
              try {
                const defaultDir = pathForSave.dirname(defaultConfigPathForSave);
                if (!fs.existsSync(defaultDir)) {
                  fs.mkdirSync(defaultDir, { recursive: true });
                }
                fs.writeFileSync(defaultConfigPathForSave, JSON.stringify(configToSave, null, 2));
                new Notice(`✅ Also updated default config: ${defaultConfigPathForSave}`);
              } catch (defaultErr) {
                new Notice(`⚠️ Failed to update default config: ${defaultErr instanceof Error ? defaultErr.message : String(defaultErr)}`);
              }
            }

            // Update global state
            (window as any).notebookAutomationLoadedConfig = configToSave;
            (window as any).notebookAutomationLoadedConfigPath = targetPath;
            
            // Refresh the configuration status display
            this.refreshConfigurationFileStatus();

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
    versionDiv.setText("Notebook Automation version: Verifying plugin files...");
    
    this.getNaVersion(versionDiv).then(ver => {
      // Convert line feeds to HTML breaks for proper display
      const formattedVersion = ver.replace(/\n/g, '<br>');
      versionDiv.innerHTML = formattedVersion;
      
      // Trigger a refresh of configuration status after version is loaded
      this.refreshConfigurationFileStatus();
    });

    // Finally render (or re-render) advanced configuration fields now that bottom sections exist.
    const bottomConfigPath = (window as any).notebookAutomationLoadedConfigPath;
    this.displayLoadedConfig(bottomConfigToDisplay, bottomConfigPath);
  }

  /**
   * Checks for and loads the default configuration file if present.
   */
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

  /**
   * Displays loaded configuration fields in the settings tab.
   * @param configJson - The loaded config JSON object.
   * @param configPath - Path to the config file.
   * @param error - Optional error message.
   */
  displayLoadedConfig(configJson: any, configPath?: string, error?: string) {
    const { containerEl } = this;
    this.injectCustomStyles();
    
    // Remove previous config fields if any
    const prev = containerEl.querySelector('.notebook-automation-config-fields');
    if (prev) prev.remove();

    // Find the Configuration File section to insert content before it (config fields should appear before the save section)
    const configFileSection = containerEl.querySelector('.notebook-automation-config-status-section');
    
    if (error) {
      const errorDiv = containerEl.createDiv({ cls: 'notebook-automation-config-fields' });
      const errorContainer = errorDiv.createDiv({ cls: 'notebook-automation-config-error' });
      errorContainer.createEl('h4', { text: 'Configuration Load Error', cls: 'notebook-automation-error-title' });
      errorContainer.createEl('p', { text: error, cls: 'notebook-automation-error-message' });
      if (configFileSection) {
        containerEl.insertBefore(errorDiv, configFileSection);
      } else {
        containerEl.appendChild(errorDiv);
      }
      (window as any).notebookAutomationLoadedConfig = null;
      (window as any).notebookAutomationLoadedConfigPath = null;
      return;
    }
    
    if (!configJson) return;
    
    (window as any).notebookAutomationLoadedConfig = configJson;
    (window as any).notebookAutomationLoadedConfigPath = configPath || null;
    const fieldsDiv = containerEl.createDiv({ cls: 'notebook-automation-config-fields' });
    
    // Preferred insertion: before dedicated anchor if present
    const anchor = containerEl.querySelector('#na-advanced-anchor');
    if (anchor && anchor.parentElement === containerEl) {
      containerEl.insertBefore(fieldsDiv, anchor);
    } else {
      // Fallback: append near top (will still precede bottom sections if anchor missing)
      containerEl.appendChild(fieldsDiv);
    }

    // Check if any sections will be displayed
    let sectionsDisplayed = false;

    // Add paths section (show only if advanced configuration is enabled)
    if (this.plugin.settings.advancedConfiguration) {
      this.addPathsSection(fieldsDiv, configJson);
      sectionsDisplayed = true;
    }
    
    // Add extensions section (show only if advanced configuration is enabled)
    if (this.plugin.settings.advancedConfiguration) {
      this.addExtensionsSection(fieldsDiv, configJson);
      sectionsDisplayed = true;
    }
    
    // Add language preferences section (show only if advanced configuration is enabled)
    if (this.plugin.settings.advancedConfiguration) {
      this.addLanguagePreferencesSection(fieldsDiv, configJson);
      sectionsDisplayed = true;
    }
    
    // Add AI service section (show only if advanced configuration is enabled)
    if (this.plugin.settings.advancedConfiguration) {
      this.addAIServiceSection(fieldsDiv, configJson);
      sectionsDisplayed = true;
    }
    
    // Add Microsoft Graph section (show only if OneDrive Shared Link is enabled and advanced configuration is enabled)
    if (this.plugin.settings.oneDriveSharedLink && this.plugin.settings.advancedConfiguration) {
      this.addMicrosoftGraphSection(fieldsDiv, configJson);
      sectionsDisplayed = true;
    }
    
    // Add timeout section (show only if advanced configuration is enabled)
    if (this.plugin.settings.advancedConfiguration) {
      this.addTimeoutSection(fieldsDiv, configJson);
      sectionsDisplayed = true;
    }

    // Add logging section (show only if advanced configuration is enabled)
    if (this.plugin.settings.advancedConfiguration) {
      this.addLoggingSection(fieldsDiv, configJson);
    }
    
    // Note: Video and PDF extensions are now handled in the File Extensions section
    
    // Save button is handled by the main display method, not here
  }

  /**
   * Adds the paths configuration section to the settings tab.
   * @param fieldsDiv - The container div for fields.
   * @param configJson - The config JSON object.
   */
  addPathsSection(fieldsDiv: HTMLDivElement, configJson: any) {
    // Add section title above the container
    fieldsDiv.createEl('h3', { text: 'Paths Configuration', cls: 'notebook-automation-section-header' });
    
    // Add section description
    const pathsDescriptionDiv = fieldsDiv.createDiv({ cls: 'notebook-automation-section-description' });
    pathsDescriptionDiv.innerHTML = `
      <p>Configure file paths and directories used by the notebook automation system. These settings define template locations, output directories, and processing workspaces for organized automated workflows.</p>
    `;
    
    const pathsSection = fieldsDiv.createDiv({ cls: 'notebook-automation-paths-section' });

    const keyMeta = [
      {
        key: 'onedrive_fullpath_root',
        label: 'OneDrive Root Path',
        desc: 'Absolute path to your local OneDrive folder (e.g., C:\\Users\\YourName\\OneDrive). Used for syncing content between OneDrive and your vault.',
        icon: '',
        validateDirectoryPath: true
      },
      {
        key: 'onedrive_resources_basepath',
        label: 'OneDrive Resources Base Path',
        desc: 'Path within OneDrive (relative to OneDrive root) where educational resources are located. Used to locate course materials for automation.',
        icon: '',
        validatePath: true
      },
      {
        key: 'notebook_vault_fullpath_root',
        label: 'Notebook Vault Root Path',
        desc: 'Absolute path to your Obsidian vault root directory. Use the "Current Vault" button to auto-populate with the active vault path.',
        icon: '',
        validateDirectoryPath: true
      },
      {
        key: 'notebook_vault_resources_basepath',
        label: 'Notebook Vault Resources Base Path',
        desc: 'Relative path within your vault for storing resources and generated files. Leave empty to use vault root directly.',
        icon: '',
        validatePath: true
      },
      {
        key: 'metadata_schema_file',
        label: 'Metadata Schema File',
        desc: 'Path to YAML schema file defining metadata structure for content generation. Supports absolute or relative paths (resolved from plugin directory).',
        icon: '',
        validateFilePath: true
      },
      {
        key: 'base_block_template_filename',
        label: 'Base Block Template File Path',
        desc: 'Path to the YAML template file for class index page generation. Supports absolute paths or relative paths (resolved from plugin directory).',
        icon: '',
        validateFilePath: true
      },
      {
        key: 'prompts_path',
        label: 'Prompts Path',
        desc: 'Path to directory containing AI prompt templates for content generation. Supports absolute or relative paths (resolved from plugin directory).',
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
      descDiv.innerHTML = `${meta.desc} (JSON key: <code>${meta.key}</code>)`;

      // Create control section (input)
      const controlDiv = settingDiv.createDiv({ cls: 'setting-item-control notebook-automation-input-control' });
      
      // Check if this field needs special buttons
      const isVaultPath = meta.key === 'notebook_vault_fullpath_root';
      const needsBrowse = meta.validateDirectoryPath || meta.validateFilePath;
      
      // Create input container for fields that need buttons
      const inputContainer = (isVaultPath || needsBrowse) 
        ? controlDiv.createDiv({ cls: 'notebook-automation-input-button-container' })
        : controlDiv;
        
      const input = inputContainer.createEl('input', {
        type: 'text',
        cls: needsBrowse || isVaultPath ? 'notebook-automation-path-input notebook-automation-path-with-button' : 'notebook-automation-path-input'
      });
      // Special handling for base_block_template_filename to use plugin settings as fallback
      if (meta.key === 'base_block_template_filename') {
        input.value = updatedPaths[meta.key] || this.plugin.settings.baseBlockTemplateFilename || 'BaseBlockTemplate.yml';
      } else {
        input.value = updatedPaths[meta.key] || '';
      }
      input.placeholder = `Enter ${meta.label.toLowerCase()}...`;

      // Add browse button for directory/file fields
      if (needsBrowse) {
        const browseButton = inputContainer.createEl('button', {
          cls: 'notebook-automation-inline-button',
          text: meta.validateDirectoryPath ? 'Browse' : 'Browse'
        });
        browseButton.onclick = async () => {
          let selectedPath: string | null = null;
          
          if (meta.validateDirectoryPath) {
            selectedPath = await browseForDirectory();
          } else if (meta.validateFilePath) {
            // Determine file filters based on the field
            let filters = [{ name: 'All Files', extensions: ['*'] }];
            if (meta.key === 'metadata_schema_file' || meta.key === 'base_block_template_filename') {
              filters = [
                { name: 'YAML Files', extensions: ['yml', 'yaml'] },
                { name: 'All Files', extensions: ['*'] }
              ];
            }
            selectedPath = await browseForFile(filters);
          }
          
          if (selectedPath) {
            input.value = selectedPath;
            updatedPaths[meta.key] = selectedPath;
            
            // Special handling for base_block_template_filename to save to plugin settings
            if (meta.key === 'base_block_template_filename') {
              this.plugin.settings.baseBlockTemplateFilename = selectedPath;
              this.plugin.saveSettings();
            }
            
            // Update the global config
            if ((window as any).notebookAutomationLoadedConfig) {
              if (!(window as any).notebookAutomationLoadedConfig.paths) {
                (window as any).notebookAutomationLoadedConfig.paths = {};
              }
              (window as any).notebookAutomationLoadedConfig.paths[meta.key] = selectedPath;
            }
            
            // Trigger input validation
            input.dispatchEvent(new Event('input'));
            
            new Notice(`Selected ${meta.validateDirectoryPath ? 'directory' : 'file'}: ${selectedPath}`);
          }
        };
      }

      // Add the Current Vault button for vault path
      if (isVaultPath) {
        const currentVaultButton = inputContainer.createEl('button', {
          cls: 'notebook-automation-inline-button',
          text: 'Current Vault'
        });
        currentVaultButton.onclick = () => {
          // Get the current vault root path
          const adapter = this.app?.vault?.adapter;
          if (adapter && typeof (adapter as any).getBasePath === 'function') {
            try {
              const vaultRoot = (adapter as any).getBasePath();
              input.value = vaultRoot;
              updatedPaths[meta.key] = vaultRoot;
              
              // Update the global config
              if ((window as any).notebookAutomationLoadedConfig) {
                if (!(window as any).notebookAutomationLoadedConfig.paths) {
                  (window as any).notebookAutomationLoadedConfig.paths = {};
                }
                (window as any).notebookAutomationLoadedConfig.paths[meta.key] = vaultRoot;
              }
              
              // Trigger input validation
              input.dispatchEvent(new Event('input'));
              
              new Notice(`Current vault path populated: ${vaultRoot}`);
            } catch (error) {
              console.error('Failed to get vault root path:', error);
              new Notice('Failed to get current vault path');
            }
          } else {
            new Notice('Unable to get current vault path');
          }
        };
      }

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
        
        // Special handling for base_block_template_filename to save to plugin settings
        if (meta.key === 'base_block_template_filename') {
          this.plugin.settings.baseBlockTemplateFilename = inputValue;
          this.plugin.saveSettings();
        }
        
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

  /**
   * Adds the AI service configuration section to the settings tab.
   * @param fieldsDiv - The container div for fields.
   * @param configJson - The config JSON object.
   */
  addAIServiceSection(fieldsDiv: HTMLDivElement, configJson: any) {
    // Add section title above the container
    fieldsDiv.createEl('h3', { text: 'AI Service Configuration', cls: 'notebook-automation-section-header' });
    
    // Add section description
    const aiDescriptionDiv = fieldsDiv.createDiv({ cls: 'notebook-automation-section-description' });
    aiDescriptionDiv.innerHTML = `
      <p>Configure AI services for automated content processing, summarization, and analysis. Supported providers include Azure OpenAI for enterprise-grade AI, OpenAI for direct API access, and Microsoft Azure AI Foundry Local for comprehensive AI workflows.</p>
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
          { key: 'endpoint', label: 'Azure OpenAI Endpoint', desc: 'Azure OpenAI service endpoint URL (format: https://your-resource-name.openai.azure.com/). Found in Azure portal under your OpenAI resource\'s Keys and Endpoint section.', type: 'text', validateUrl: true },
          { key: 'deployment', label: 'Deployment Name', desc: 'Custom deployment name from Azure OpenAI Studio (e.g., gpt-4, my-gpt-35-turbo). This is your deployment name, not the base model name.', type: 'text' },
          { key: 'model', label: 'Model Name', desc: 'Base model name for your Azure deployment (e.g., gpt-4, gpt-35-turbo, gpt-4o). Should match the model version in your deployment.', type: 'text' }
        ],
        openai: [
          { key: 'endpoint', label: 'OpenAI Endpoint', desc: 'OpenAI API endpoint URL, typically https://api.openai.com/v1. Use default unless using a proxy or alternative service.', type: 'text', validateUrl: true },
          { key: 'model', label: 'Model Name', desc: 'OpenAI model name for content generation (e.g., gpt-4o, gpt-4, gpt-3.5-turbo). Choose based on quality needs and cost considerations.', type: 'text' }
        ],
        foundry: [
          { key: 'endpoint', label: 'Foundry Endpoint', desc: 'Microsoft Azure AI Foundry Local endpoint URL for your LLM service. See Azure AI Foundry Local documentation for setup details.', type: 'text', validateUrl: true },
          { key: 'model', label: 'Model Name', desc: 'Model name available in your Azure AI Foundry Local instance. Contact your administrator for available models.', type: 'text' }
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
          cls: field.validateUrl ? 'notebook-automation-path-input notebook-automation-path-with-button' : 'notebook-automation-path-input'
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

  /**
   * Adds the Microsoft Graph configuration section to the settings tab.
   * @param fieldsDiv - The container div for fields.
   * @param configJson - The config JSON object.
   */
  addMicrosoftGraphSection(fieldsDiv: HTMLDivElement, configJson: any) {
    // Add section title above the container
    fieldsDiv.createEl('h3', { text: 'Microsoft Graph Configuration', cls: 'notebook-automation-section-header' });
    
    // Add section description
    const graphDescriptionDiv = fieldsDiv.createDiv({ cls: 'notebook-automation-section-description' });
    graphDescriptionDiv.innerHTML = `
      <p>Configure Microsoft Graph API integration for accessing OneDrive, SharePoint, and other Microsoft 365 services. Requires a registered Azure AD application with appropriate permissions for shared file processing and collaborative workflows.</p>
    `;
    
    const graphSection = fieldsDiv.createDiv({ cls: 'notebook-automation-graph-section' });

    const graphConfig = configJson.microsoft_graph || {};
    const updatedGraphConfig: Record<string, any> = { ...graphConfig };

    const graphFields = [
      { key: 'client_id', label: 'Client ID', desc: 'Application (Client) ID from your Azure AD app registration (GUID format). Found in Azure portal under App registrations → Your App → Overview.', type: 'text', validateGuid: true },
      { key: 'api_endpoint', label: 'API Endpoint', desc: 'Microsoft Graph API base URL, typically https://graph.microsoft.com/v1.0. Use beta endpoint only if requiring preview features.', type: 'text', validateUrl: true },
      { key: 'authority', label: 'Authority', desc: 'Microsoft authentication authority URL (format: https://login.microsoftonline.com/your-tenant-id). Find tenant ID in Azure portal under Azure Active Directory → Overview.', type: 'text', validateUrl: true }
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
        cls: (field.validateUrl || field.validateGuid) ? 'notebook-automation-path-input notebook-automation-path-with-button' : 'notebook-automation-path-input'
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
    scopesDescDiv.setText('Microsoft Graph API permission scopes, one per line (e.g., Files.Read, Files.ReadWrite). Add only minimum required scopes for your use case.');

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

  /**
   * Adds the timeout configuration section to the settings tab.
   * @param fieldsDiv - The container div for fields.
   * @param configJson - The config JSON object.
   */
  addTimeoutSection(fieldsDiv: HTMLDivElement, configJson: any) {
    // Add section title above the container
    fieldsDiv.createEl('h3', { text: 'Timeout Configuration', cls: 'notebook-automation-section-header' });
    
    // Add section description
    const timeoutDescriptionDiv = fieldsDiv.createDiv({ cls: 'notebook-automation-section-description' });
    timeoutDescriptionDiv.innerHTML = `
      <p>Configure timeout settings and rate limiting for AI service requests and file processing operations. These settings manage performance, prevent API overload, and ensure reliable processing of large document collections.</p>
    `;
    
    const timeoutSection = fieldsDiv.createDiv({ cls: 'notebook-automation-timeout-section' });

    const aiConfig = configJson.aiservice || {};
    const timeoutConfig = aiConfig.timeout || {};
    const timeoutFields = [
      { key: 'request_timeout_seconds', label: 'Request Timeout (seconds)', desc: 'Maximum seconds to wait for AI service responses (default: 300). Increase for complex tasks, decrease to fail faster.', type: 'number', default: 300 },
      { key: 'max_retry_attempts', label: 'Max Retry Attempts', desc: 'Number of retry attempts for failed requests (default: 3). Higher values increase reliability but may delay error reporting.', type: 'number', default: 3 },
      { key: 'base_retry_delay_seconds', label: 'Base Retry Delay (seconds)', desc: 'Initial delay in seconds before first retry (default: 2). System uses exponential backoff for subsequent retries.', type: 'number', default: 2 },
      { key: 'max_retry_delay_seconds', label: 'Max Retry Delay (seconds)', desc: 'Maximum delay in seconds between retries, regardless of backoff (default: 60). Prevents extremely long waits between attempts.', type: 'number', default: 60 },
      { key: 'max_chunk_parallelism', label: 'Max Chunk Parallelism', desc: 'Maximum content chunks to process in parallel (default: 3). Reduce if experiencing rate limit errors or memory issues.', type: 'number', default: 3 },
      { key: 'chunk_rate_limit_ms', label: 'Chunk Rate Limit (ms)', desc: 'Minimum milliseconds between processing chunks (default: 100). Increase to avoid API rate limits.', type: 'number', default: 100 },
      { key: 'max_file_parallelism', label: 'Max File Parallelism', desc: 'Maximum files to process simultaneously (default: 2). Balance between speed and resource usage.', type: 'number', default: 2 },
      { key: 'file_rate_limit_ms', label: 'File Rate Limit (ms)', desc: 'Minimum milliseconds between processing files (default: 200). Helps manage system load and API limits.', type: 'number', default: 200 }
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

  /**
   * Adds the logging configuration section to the settings tab.
   * @param fieldsDiv - The container div for fields.
   * @param configJson - The config JSON object.
   */
  addLoggingSection(fieldsDiv: HTMLDivElement, configJson: any) {
    // Add section title above the container
    fieldsDiv.createEl('h3', { text: 'Logging Configuration', cls: 'notebook-automation-section-header' });
    
    // Add section description
    const loggingDescriptionDiv = fieldsDiv.createDiv({ cls: 'notebook-automation-section-description' });
    loggingDescriptionDiv.innerHTML = `
      <p>Configure logging behavior for debugging, monitoring, and troubleshooting automation processes. Settings control what information is captured, where logs are stored, and how detailed the output should be.</p>
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
    // Add browse button for logging directory
    const browseLogDirButton = logDirInputContainer.createEl('button', {
      cls: 'notebook-automation-inline-button',
      text: 'Browse'
    });
    browseLogDirButton.onclick = async () => {
      const selectedPath = await browseForDirectory();
      
      if (selectedPath) {
        logDirInput.value = selectedPath;
        
        // Update the global config
        if ((window as any).notebookAutomationLoadedConfig) {
          if (!(window as any).notebookAutomationLoadedConfig.paths) {
            (window as any).notebookAutomationLoadedConfig.paths = {};
          }
          (window as any).notebookAutomationLoadedConfig.paths.logging_dir = selectedPath;
        }
        
        // Trigger validation
        logDirInput.dispatchEvent(new Event('input'));
        
        new Notice(`Selected logging directory: ${selectedPath}`);
      }
    };

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
      { key: 'max_file_size_mb', label: 'Max File Size (MB)', desc: 'Maximum log file size in MB before rotation (default: 50). Larger files mean fewer files but slower to search.', type: 'number', default: 50 },
      { key: 'retained_file_count', label: 'Retained File Count', desc: 'Number of log files to keep before deleting oldest (default: 7). Balance between history retention and disk space.', type: 'number', default: 7 }
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

  /**
   * Adds the file extensions configuration section to the settings tab.
   * @param fieldsDiv - The container div for fields.
   * @param configJson - The config JSON object.
   */
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
    videoExtDescDiv.setText('Video file extensions to process, comma-separated with dots (e.g., .mp4, .mov, .avi). System will generate transcripts and summaries for these formats.');

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
    pdfExtDescDiv.setText('PDF file extensions to process, comma-separated (typically .pdf). System will extract text and optionally images for processing.');

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

    // HTML extensions
    const htmlExtDiv = extensionsSection.createDiv({ cls: 'setting-item notebook-automation-custom-setting' });
    const htmlExtInfoDiv = htmlExtDiv.createDiv({ cls: 'setting-item-info' });
    const htmlExtNameDiv = htmlExtInfoDiv.createDiv({ cls: 'setting-item-name' });
    htmlExtNameDiv.setText('HTML Extensions');
    const htmlExtDescDiv = htmlExtInfoDiv.createDiv({ cls: 'setting-item-description' });
    htmlExtDescDiv.setText('HTML and eBook file extensions to process, comma-separated with dots (e.g., .html, .htm, .epub). System will extract content and generate markdown for these formats.');

    const htmlExtControlDiv = htmlExtDiv.createDiv({ cls: 'setting-item-control notebook-automation-input-control' });
    const htmlExtInput = htmlExtControlDiv.createEl('input', {
      type: 'text',
      cls: 'notebook-automation-path-input'
    });
    // Use plugin settings as source with fallback to default
    const htmlExtensions = this.plugin.settings.htmlExtensions || ".html,.htm,.epub";
    htmlExtInput.value = htmlExtensions;
    htmlExtInput.placeholder = 'Enter HTML/eBook extensions (.html, .htm, .epub)...';
    htmlExtInput.oninput = async (e: any) => {
      const extensions = (e.target as HTMLInputElement).value;
      // Save to plugin settings
      this.plugin.settings.htmlExtensions = extensions;
      await this.plugin.saveSettings();
      
      // Also update global config for immediate use
      if ((window as any).notebookAutomationLoadedConfig) {
        // Parse extensions and update config
        const extensionList = extensions.split(',').map((ext: string) => ext.trim()).filter((ext: string) => ext);
        (window as any).notebookAutomationLoadedConfig.html_extensions = extensionList;
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
    pdfExtractToggle.checked = this.plugin.settings.pdfExtractImages || false;
    pdfExtractToggle.addEventListener('change', async (e) => {
      const value = (e.target as HTMLInputElement).checked;
      // Save to plugin settings only (not config file)
      this.plugin.settings.pdfExtractImages = value;
      await this.plugin.saveSettings();
    });
    
    // Create description row
    const descRow = pdfExtractContainer.createDiv({ 
      cls: 'setting-item-description',
      text: 'Extract and save images from PDF files alongside generated markdown notes. Preserves diagrams, charts, and visual content from documents.'
    });
  }

  /**
   * Adds the language preferences configuration section to the settings tab.
   * @param fieldsDiv - The container div for fields.
   * @param configJson - The config JSON object.
   */
  addLanguagePreferencesSection(fieldsDiv: HTMLDivElement, configJson: any) {
    // Add section title above the container
    fieldsDiv.createEl('h3', { text: 'Language Preferences', cls: 'notebook-automation-section-header' });
    
    // Add section description
    const languageDescriptionDiv = fieldsDiv.createDiv({ cls: 'notebook-automation-section-description' });
    languageDescriptionDiv.innerHTML = `
      <p>Configure preferred languages for transcript selection when multiple language-specific transcripts are available for video files. The system will select transcripts in the order specified below.</p>
      
      <p><strong>Language codes:</strong> Use standard language codes like "en" (English), "en-us" (US English), "fr" (French), "es" (Spanish), "de" (German), "zh-cn" (Chinese Simplified), etc.</p>
    `;
    
    const languageSection = fieldsDiv.createDiv({ cls: 'notebook-automation-language-section' });

    // Language preferences
    const langPrefDiv = languageSection.createDiv({ cls: 'setting-item notebook-automation-custom-setting' });
    const langPrefInfoDiv = langPrefDiv.createDiv({ cls: 'setting-item-info' });
    const langPrefNameDiv = langPrefInfoDiv.createDiv({ cls: 'setting-item-name' });
    langPrefNameDiv.setText('Preferred Transcript Languages');
    const langPrefDescDiv = langPrefInfoDiv.createDiv({ cls: 'setting-item-description' });
    langPrefDescDiv.setText('Language codes in priority order, comma-separated (e.g., en, en-us, fr, es). When multiple language-specific transcripts exist for a video file, the system will select the first available language from this list. If none of the preferred languages are available, the system will use the first language-specific transcript found.');

    const langPrefControlDiv = langPrefDiv.createDiv({ cls: 'setting-item-control notebook-automation-input-control' });
    const langPrefInput = langPrefControlDiv.createEl('input', {
      type: 'text',
      cls: 'notebook-automation-path-input'
    });
    const preferredLanguages = configJson.preferred_transcript_languages || ['en'];
    langPrefInput.value = preferredLanguages.join(', ');
    langPrefInput.placeholder = 'Enter language codes (en, en-us, fr, es, etc.)...';

    // Create validation message element for language codes
    const langValidationMessage = langPrefControlDiv.createDiv({ cls: 'notebook-automation-field-validation' });
    
    // Validation function for language codes
    const validateLanguageCodes = (input: string): { isValid: boolean; errorMessage?: string } => {
      if (!input.trim()) {
        return { isValid: false, errorMessage: 'At least one language code is required.' };
      }
      
      const codes = input.split(',').map(code => code.trim()).filter(code => code);
      const invalidCodes: string[] = [];
      
      for (const code of codes) {
        // Validate language code format: 2-3 letters, or 2-3 letters followed by hyphen and 2-3 letters
        const languageCodeRegex = /^[a-zA-Z]{2,3}(-[a-zA-Z]{2,3})?$/;
        if (!languageCodeRegex.test(code)) {
          invalidCodes.push(code);
        }
      }
      
      if (invalidCodes.length > 0) {
        return { 
          isValid: false, 
          errorMessage: `Invalid language codes: ${invalidCodes.join(', ')}. Use standard codes like "en", "en-us", "fr", "es", "de", "zh-cn".` 
        };
      }
      
      return { isValid: true };
    };

    // Initial validation for existing value
    const initialValidation = validateLanguageCodes(langPrefInput.value);
    if (!initialValidation.isValid) {
      langValidationMessage.classList.add('visible');
      langValidationMessage.innerHTML = this.formatValidationError(
        'Invalid Language Codes',
        initialValidation.errorMessage || 'Please check the language code format.'
      );
      langPrefInput.classList.add('notebook-automation-input-invalid');
    }

    langPrefInput.oninput = (e: any) => {
      const inputValue = e.target.value;
      
      // Validate language codes
      const validation = validateLanguageCodes(inputValue);
      if (!validation.isValid) {
        langValidationMessage.classList.add('visible');
        langValidationMessage.innerHTML = this.formatValidationError(
          'Invalid Language Codes',
          validation.errorMessage || 'Please check the language code format.'
        );
        langPrefInput.classList.add('notebook-automation-input-invalid');
      } else {
        langValidationMessage.classList.remove('visible');
        langPrefInput.classList.remove('notebook-automation-input-invalid');
      }

      const languages = inputValue.split(',').map((lang: string) => lang.trim()).filter((lang: string) => lang);
      
      // Update global config
      if ((window as any).notebookAutomationLoadedConfig) {
        (window as any).notebookAutomationLoadedConfig.preferred_transcript_languages = languages;
      }
    };
  }

  /**
   * Adds the banners configuration section to the settings tab.
   * @param fieldsDiv - The container div for fields.
   * @param configJson - The config JSON object.
   */
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

  /**
   * Adds markdown-specific banner configuration settings.
   */
  addMarkdownBannerSettings(container: HTMLElement) {
    // Template Banners setting
    const templateBannersSetting = new Setting(container)
      .setName('Template Banners')
      .setDesc('JSON configuration mapping content templates to banner images (e.g., {"main": "main-header.png", "course": "course-header.png"}). Define banners for different content types.');
    
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
      .setDesc('JSON configuration mapping filename patterns to banner images using wildcards (e.g., {"*index*": "index-banner.png", "assignment-*": "assignment-banner.png"}). Patterns match against file names.');
    
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

  /**
   * Loads and applies configuration after plugin files have been downloaded.
   * This ensures configuration is applied with all necessary files in place.
   */
  async loadAndApplyConfig(): Promise<void> {
    try {
      console.log('[Notebook Automation] Loading configuration after file download completion...');
      
      // Load the configuration using the existing method
      this.checkAndLoadDefaultConfig();
      
      // Refresh the UI with the loaded configuration
      const loadedConfig = (window as any).notebookAutomationLoadedConfig;
      const loadedConfigPath = (window as any).notebookAutomationLoadedConfigPath;
      
      if (loadedConfig) {
        console.log('[Notebook Automation] Configuration loaded and applied after file download:', loadedConfigPath);
        this.displayLoadedConfig(loadedConfig, loadedConfigPath);
        console.log('[Notebook Automation] UI updated with loaded configuration');
      } else {
        console.log('[Notebook Automation] No configuration found to apply after file download - using default settings');
      }
    } catch (error) {
      console.warn('[Notebook Automation] Error loading and applying config after file download:', error);
    }
  }

  /**
   * Gets the current version of the Notebook Automation plugin.
   * @param versionElement Optional element to update with download progress.
   * @returns Promise resolving to the version string.
   */
  async getNaVersion(versionElement?: HTMLElement): Promise<string> {
    try {
      // @ts-ignore
      const child_process = window.require ? window.require('child_process') : null;
      if (!child_process) {
        return "Unknown (Node.js not available)";
      }
      
      // Create progress callback if element provided
      const progressCallback: DownloadProgressCallback | undefined = versionElement 
        ? (current: number, total: number, fileName: string) => {
            if (versionElement) {
              versionElement.innerHTML = `📥 Downloading ${current} of ${total} plugin files: ${fileName}`;
            }
          }
        : undefined;
      
      const naPath = await ensureExecutableExists(this.plugin, progressCallback);
      
      // Update to show config loading after download complete
      if (versionElement) {
        versionElement.innerHTML = "⚙️ Loading configuration...";
      }
      
      // Load and apply configuration after files are downloaded
      await this.loadAndApplyConfig();
      
      // Update to show version loading after config is applied
      if (versionElement) {
        versionElement.innerHTML = "🔍 Getting CLI version information...";
      }
      
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

  /**
   * Adds validation messaging for the selected AI provider environment.
   * @param validationContainer - The container for validation messages.
   * @param provider - The selected AI provider.
   */
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

  /**
   * Validates the environment for the selected AI provider.
   * @param provider - The AI provider to validate.
   * @returns Object with isValid, missingVar, and description fields.
   */
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

  /**
   * Refreshes the Configuration File status section to show the current loaded config
   */
  refreshConfigurationFileStatus() {
    const { containerEl } = this;
    
    // Find the existing config status section
    const statusSection = containerEl.querySelector('.notebook-automation-config-status-section');
    if (!statusSection) return;
    
    // Find the status div within the section
    const statusDiv = statusSection.querySelector('.notebook-automation-config-status');
    if (!statusDiv) return;
    
    // Environment variable detection and current config display
    // @ts-ignore
    const process = window.require ? window.require('process') : null;
    const envConfigPath = process?.env?.NOTEBOOKAUTOMATION_CONFIG;

    // Get the path of the currently loaded config file
    const loadedConfigPath = (window as any).notebookAutomationLoadedConfigPath;

    // Check if we're using a non-default config file and determine default config path
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
    }

    // Determine current config status and paths
    let currentConfigPath = loadedConfigPath;
    let configStatus = '';
    
    if (envConfigPath) {
      // @ts-ignore
      const fs = window.require ? window.require('fs') : null;
      const envFileExists = fs ? fs.existsSync(envConfigPath) : false;
      
      if (!envFileExists) {
        configStatus = '❌ Environment Variable';
      } else if (loadedConfigPath) {
        configStatus = '✅ Environment Variable';
        currentConfigPath = loadedConfigPath;
      } else {
        configStatus = 'ℹ️ Environment Variable';
        currentConfigPath = envConfigPath;
      }
    } else if (loadedConfigPath) {
      // Check if this is a custom config (not the default)
      if (path && defaultConfigPath) {
        try {
          const normalizedLoadedPath = path.resolve(loadedConfigPath);
          const normalizedDefaultPath = path.resolve(defaultConfigPath);
          if (normalizedLoadedPath === normalizedDefaultPath) {
            configStatus = '✅ Plugin Directory';
          } else {
            configStatus = '✅ Custom Configuration';
          }
        } catch (err) {
          configStatus = '✅ Custom Configuration';
        }
      } else {
        configStatus = '✅ Custom Configuration';
      }
    } else {
      configStatus = 'ℹ️ Plugin Directory';
      // Set the default config path
      if (path && defaultConfigPath) {
        currentConfigPath = defaultConfigPath;
      }
    }

    // Update the status display
    statusDiv.innerHTML = `
      <div style="display: flex; align-items: center; gap: 8px; margin-bottom: 8px;">
        <strong>${configStatus}</strong>
      </div>
      <div class="notebook-automation-file-path">${currentConfigPath || 'No config file available'}</div>
    `;
  }

}
