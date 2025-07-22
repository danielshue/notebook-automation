import { App, Notice, Plugin, TAbstractFile, TFile, TFolder } from 'obsidian';

import { NotebookAutomationSettingTab } from './ui/NotebookAutomationSettingTab';
import { executeNotebookAutomationCommand } from './features/commands';
import { registerContextMenus } from './features/contextMenus';
import { registerCommands } from './features/registerCommands';
import { DEFAULT_SETTINGS, NotebookAutomationSettings } from './config/settings';

export default class NotebookAutomationPlugin extends Plugin {
  settings: NotebookAutomationSettings = DEFAULT_SETTINGS;

  async onload() {
    await this.loadSettings();
    
    // Load configuration into window for commands to use
    await this.loadConfigurationForCommands();
    
    this.addSettingTab(new NotebookAutomationSettingTab(this.app, this));
    
    // Register context menus
    registerContextMenus(this);
    
    // Register Command Palette commands
    registerCommands(this);
  }

  /**
   * Load configuration into window global for use by commands
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
        console.log('[Notebook Automation] No config file found during startup - please configure a config file path in plugin settings');
      }
    } catch (err) {
      console.log('[Notebook Automation] Error loading config during startup:', err);
    }
  }

  async loadSettings() {
    this.settings = Object.assign({}, DEFAULT_SETTINGS, await this.loadData());
  }

  async saveSettings() {
    await this.saveData(this.settings);
  }
}
