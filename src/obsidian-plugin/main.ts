
import { App, Notice, Plugin, TAbstractFile, TFile, TFolder } from 'obsidian';

import { NotebookAutomationSettingTab } from './ui/NotebookAutomationSettingTab';
import { executeNotebookAutomationCommand } from './features/commands';
import { registerContextMenus } from './features/contextMenus';
import { registerCommands } from './features/registerCommands';
import { DEFAULT_SETTINGS, NotebookAutomationSettings } from './config/settings';

/**
 * Main plugin class for Notebook Automation in Obsidian.
 *
 * Handles plugin lifecycle, settings management, configuration loading, and registration of commands and context menus.
 */
export default class NotebookAutomationPlugin extends Plugin {
  /**
   * Current plugin settings, loaded from disk or defaults.
   */
  settings: NotebookAutomationSettings = DEFAULT_SETTINGS;

  /**
   * Called when the plugin is loaded by Obsidian.
   * Loads settings, configuration, and registers UI and commands.
   */
  async onload() {
    await this.loadSettings();

    // Load configuration into window for commands to use
    await this.loadConfigurationForCommands();

    // Add the settings tab to the Obsidian settings UI
    this.addSettingTab(new NotebookAutomationSettingTab(this.app, this));

    // Register context menus for files and folders
    registerContextMenus(this);

    // Register Command Palette commands
    registerCommands(this);
  }

  /**
   * Loads the configuration file into the window global for use by commands.
   *
   * Priority order:
   * 1. NOTEBOOKAUTOMATION_CONFIG environment variable
   * 2. User-configured path from plugin settings
   * 3. default-config.json from plugin directory
   *
   * Sets `window.notebookAutomationLoadedConfig` and `window.notebookAutomationLoadedConfigPath`.
   */
  async loadConfigurationForCommands() {
    try {
      // @ts-ignore
      const fs = window.require ? window.require('fs') : null;
      // @ts-ignore
      const path = window.require ? window.require('path') : null;
      
      if (!fs || !path) {
        console.log('[Notebook Automation] File system access not available for config loading');
        return;
      }

      let configPath = '';
      
      // First priority: Environment variable
      const envConfigPath = process.env.NOTEBOOKAUTOMATION_CONFIG;
      if (envConfigPath && fs.existsSync(envConfigPath) && fs.statSync(envConfigPath).isFile()) {
        configPath = envConfigPath;
        console.log('[Notebook Automation] Loading config from environment variable:', configPath);
      }
      
      // Second priority: User-configured path from settings
      if (!configPath && this.settings.configPath) {
        const userConfigPath = this.settings.configPath;
        if (fs.existsSync(userConfigPath) && fs.statSync(userConfigPath).isFile()) {
          configPath = userConfigPath;
          console.log('[Notebook Automation] Loading config from user settings:', configPath);
        } else {
          console.log('[Notebook Automation] User-configured config path does not exist:', userConfigPath);
        }
      }
      
      // Third priority: default-config.json from plugin directory
      if (!configPath) {
        const pluginDir = this.manifest?.dir;
        if (pluginDir) {
          let resolvedPluginDir = pluginDir;
          const adapter = this.app?.vault?.adapter;
          // @ts-ignore
          if (adapter && typeof adapter.getBasePath === 'function') {
            try {
              // @ts-ignore
              const vaultRoot = adapter.getBasePath();
              if (vaultRoot && !path.isAbsolute(pluginDir)) {
                resolvedPluginDir = path.join(vaultRoot, pluginDir);
              }
            } catch (err) {
              console.log('[Notebook Automation] Error getting vault root for config loading:', err);
            }
          }

          const defaultConfigPath = path.join(resolvedPluginDir, 'default-config.json');
          if (fs.existsSync(defaultConfigPath) && fs.statSync(defaultConfigPath).isFile()) {
            configPath = defaultConfigPath;
            console.log('[Notebook Automation] Loading default config from plugin directory:', configPath);
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
          console.log('[Notebook Automation] Successfully loaded config during startup from:', configPath);
        } catch (jsonErr) {
          console.log('[Notebook Automation] Error parsing config file during startup:', jsonErr);
        }
      } else {
        // Try to download default config file from GitHub release
        try {
          console.log('[Notebook Automation] No config file found during startup. Attempting to download from GitHub release...');
          const { ensureConfigFilesExist } = await import('./utils/plugin-assets');
          const configDownloadResult = await ensureConfigFilesExist(this);
          if (configDownloadResult) {
            console.log('[Notebook Automation] Successfully downloaded config files from GitHub release during startup');
            // Try loading again after download
            await this.loadConfigurationForCommands();
          } else {
            console.log('[Notebook Automation] No config file found during startup - please configure a config file path in plugin settings');
          }
        } catch (error) {
          console.log('[Notebook Automation] Error downloading config files during startup:', error);
          console.log('[Notebook Automation] No config file found during startup - please configure a config file path in plugin settings');
        }
      }
    } catch (err) {
      console.log('[Notebook Automation] Error loading config during startup:', err);
    }
  }

  /**
   * Loads plugin settings from disk, merging with defaults.
   */
  async loadSettings() {
    this.settings = Object.assign({}, DEFAULT_SETTINGS, await this.loadData());
  }

  /**
   * Saves current plugin settings to disk.
   */
  async saveSettings() {
    await this.saveData(this.settings);
  }
}
