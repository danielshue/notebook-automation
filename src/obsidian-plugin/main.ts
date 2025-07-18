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
    this.addSettingTab(new NotebookAutomationSettingTab(this.app, this));
    
    // Register context menus
    registerContextMenus(this);
    
    // Register Command Palette commands
    registerCommands(this);
  }

  async loadSettings() {
    this.settings = Object.assign({}, DEFAULT_SETTINGS, await this.loadData());
  }

  async saveSettings() {
    await this.saveData(this.settings);
  }
}
