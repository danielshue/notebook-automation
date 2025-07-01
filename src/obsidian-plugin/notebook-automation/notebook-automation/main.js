"use strict";
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
Object.defineProperty(exports, "__esModule", { value: true });
/**
 * Given a full vault path, strip the notebook_vault_fullpath_root and vault_resources_basepath prefix and return the relative path for OneDrive mapping.
 * @param fullPath The full path to the file/folder in the vault
 * @param vaultRoot The notebook_vault_fullpath_root from config
 * @param vaultBase Optional vault_resources_basepath from config
 * @returns The relative path for OneDrive mapping
 */
function getRelativeVaultResourcePath(fullPath, vaultRoot, vaultBase) {
    // Normalize slashes for cross-platform compatibility
    let normFull = fullPath.replace(/\\/g, '/');
    let normRoot = vaultRoot.replace(/\\/g, '/').replace(/\/$/, '');
    let normBase = (vaultBase || '').replace(/\\/g, '/').replace(/^\//, '').replace(/\/$/, '');
    // Remove vaultRoot if present
    if (normRoot && normFull.startsWith(normRoot)) {
        normFull = normFull.substring(normRoot.length);
        if (normFull.startsWith('/'))
            normFull = normFull.substring(1);
    }
    // Remove vaultBase if present
    if (normBase && normFull.startsWith(normBase)) {
        normFull = normFull.substring(normBase.length);
        if (normFull.startsWith('/'))
            normFull = normFull.substring(1);
    }
    return normFull;
}
/**
 * Utility to resolve the correct executable name for the current platform.
 */
function getNaExecutableName() {
    const platform = (process === null || process === void 0 ? void 0 : process.platform) || ((window === null || window === void 0 ? void 0 : window.process) && window.process.platform);
    if (platform === "win32") {
        return "na.exe";
    }
    return "na";
}
/**
 * Get the full path to the bundled na executable in the plugin directory.
 * @param plugin This plugin instance (for path resolution)
 */
function getNaExecutablePath(plugin) {
    const execName = getNaExecutableName();
    try {
        // @ts-ignore
        const path = window.require ? window.require('path') : null;
        // Try vault root + .obsidian/plugins/<plugin-id>/na
        if (plugin.manifest && plugin.manifest.id && plugin.app && plugin.app.vault && path) {
            // Use process.cwd() as the fallback for vault root
            let vaultRoot = '';
            try {
                if (typeof process !== 'undefined' && process.cwd) {
                    vaultRoot = process.cwd();
                }
            }
            catch (_a) { }
            if (vaultRoot) {
                const pluginId = plugin.manifest.id;
                const pluginDir = path.join(vaultRoot, '.obsidian', 'plugins', pluginId);
                return path.join(pluginDir, execName);
            }
        }
        // Try plugin.manifest.dir (Obsidian 1.4+)
        if (plugin.manifest && plugin.manifest.dir && path) {
            return path.join(plugin.manifest.dir, execName);
        }
        if (typeof __dirname !== 'undefined' && path) {
            return path.join(__dirname, execName);
        }
        // Fallback: just the name (should be in plugin dir)
        return execName;
    }
    catch (_b) {
        return execName;
    }
}
const obsidian_1 = require("obsidian");
const obsidian_2 = require("obsidian");
const DEFAULT_SETTINGS = {
    configPath: "",
    verbose: false,
    debug: false,
    dryRun: false
};
class NotebookAutomationPlugin extends obsidian_2.Plugin {
    constructor() {
        super(...arguments);
        this.settings = DEFAULT_SETTINGS;
    }
    onload() {
        return __awaiter(this, void 0, void 0, function* () {
            yield this.loadSettings();
            this.addRibbonIcon("dice", "Notebook Automation Plugin", () => {
                new obsidian_2.Notice("Hello from Notebook Automation Plugin!");
            });
            this.addSettingTab(new NotebookAutomationSettingTab(this.app, this));
            // Register context menu commands for files and folders
            this.registerEvent(this.app.workspace.on("file-menu", (menu, file) => {
                // Folder context
                if (file instanceof obsidian_1.TFolder) {
                    menu.addSeparator();
                    menu.addItem((item) => {
                        item.setTitle("Notebook Automation: Import & AI Summarize All Videos")
                            .setIcon("video-file")
                            .onClick(() => this.handleNotebookAutomationCommand(file, "import-summarize-videos"));
                    });
                    menu.addItem((item) => {
                        item.setTitle("Notebook Automation: Import & AI Summarize All PDFs")
                            .setIcon("document")
                            .onClick(() => this.handleNotebookAutomationCommand(file, "import-summarize-pdfs"));
                    });
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
                // File context: only for .md files
                if (file instanceof obsidian_1.TFile && file.extension === "md") {
                    menu.addSeparator();
                    menu.addItem((item) => {
                        item.setTitle("Notebook Automation: Reprocess AI Summary (Video)")
                            .setIcon("video-file")
                            .onClick(() => this.handleNotebookAutomationCommand(file, "reprocess-summary-video"));
                    });
                    menu.addItem((item) => {
                        item.setTitle("Notebook Automation: Reprocess AI Summary (PDF)")
                            .setIcon("document")
                            .onClick(() => this.handleNotebookAutomationCommand(file, "reprocess-summary-pdf"));
                    });
                }
            }));
        });
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
    handleNotebookAutomationCommand(file, action) {
        return __awaiter(this, void 0, void 0, function* () {
            var _a, _b, _c, _d;
            // Get config for vault root and base
            let vaultRoot = "";
            let vaultBase = "";
            try {
                // Try to get loaded config from settings tab
                const loaded = window.notebookAutomationLoadedConfig;
                if ((_a = loaded === null || loaded === void 0 ? void 0 : loaded.paths) === null || _a === void 0 ? void 0 : _a.notebook_vault_fullpath_root) {
                    vaultRoot = loaded.paths.notebook_vault_fullpath_root;
                    vaultBase = ((_b = loaded.paths) === null || _b === void 0 ? void 0 : _b.notebook_vault_resources_basepath) || "";
                }
                else {
                    // Fallback: read config.json directly (synchronously)
                    // @ts-ignore
                    const fs = window.require ? window.require('fs') : null;
                    if (fs) {
                        let configPath = this.settings.configPath;
                        if (!configPath)
                            configPath = 'config.json';
                        if (fs.existsSync(configPath)) {
                            const content = fs.readFileSync(configPath, 'utf8');
                            const config = JSON.parse(content);
                            vaultRoot = ((_c = config.paths) === null || _c === void 0 ? void 0 : _c.notebook_vault_fullpath_root) || "";
                            vaultBase = ((_d = config.paths) === null || _d === void 0 ? void 0 : _d.notebook_vault_resources_basepath) || "";
                        }
                    }
                }
            }
            catch (_e) { }
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
            new obsidian_2.Notice(`Notebook Automation: '${action}' for ${relPath}`);
            // TODO: Implement logic for each action
        });
    }
    loadSettings() {
        return __awaiter(this, void 0, void 0, function* () {
            this.settings = Object.assign({}, DEFAULT_SETTINGS, yield this.loadData());
        });
    }
    saveSettings() {
        return __awaiter(this, void 0, void 0, function* () {
            yield this.saveData(this.settings);
        });
    }
}
exports.default = NotebookAutomationPlugin;
class NotebookAutomationSettingTab extends obsidian_2.PluginSettingTab {
    constructor(app, plugin) {
        super(app, plugin);
        this.plugin = plugin;
    }
    getNaVersion() {
        return __awaiter(this, void 0, void 0, function* () {
            // Try to run na --version and parse the first line
            try {
                // @ts-ignore
                const child_process = window.require ? window.require('child_process') : null;
                const path = window.require ? window.require('path') : null;
                if (!child_process || !path)
                    return "Unavailable";
                const naPath = getNaExecutablePath(this.plugin);
                const cmd = `"${naPath}" --version`;
                // eslint-disable-next-line no-console
                console.log(`[Notebook Automation] Running version command: ${cmd}`);
                // eslint-disable-next-line no-console
                console.log(`[Notebook Automation] naPath: ${naPath}`);
                const result = child_process.execSync(cmd).toString();
                const firstLine = result.split(/\r?\n/)[0];
                return firstLine.trim();
            }
            catch (err) {
                // eslint-disable-next-line no-console
                console.log(`[Notebook Automation] Error running na --version:`, err);
                return "Unavailable";
            }
        });
    }
    display() {
        return __awaiter(this, void 0, void 0, function* () {
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
            let configJson = null;
            let configError = null;
            new obsidian_2.Setting(containerEl)
                .setName("Config File")
                .setDesc("Enter the path to the config.json file to use for notebook automation. This can be anywhere accessible to the plugin.")
                .addText(text => {
                text.setPlaceholder("Path to config.json...")
                    .setValue(this.plugin.settings.configPath || "")
                    .onChange((value) => __awaiter(this, void 0, void 0, function* () {
                    this.plugin.settings.configPath = value;
                    yield this.plugin.saveSettings();
                }));
            })
                .addButton(btn => {
                btn.setButtonText("Validate & Load").onClick(() => __awaiter(this, void 0, void 0, function* () {
                    const path = this.plugin.settings.configPath;
                    if (!path) {
                        new obsidian_2.Notice("Please enter a config file path first.");
                        return;
                    }
                    try {
                        // @ts-ignore
                        const fs = window.require ? window.require('fs') : null;
                        if (!fs) {
                            new obsidian_2.Notice("File system access is not available in this environment.");
                            return;
                        }
                        if (fs.existsSync(path) && fs.statSync(path).isFile()) {
                            const content = fs.readFileSync(path, 'utf8');
                            try {
                                configJson = JSON.parse(content);
                                configError = null;
                                new obsidian_2.Notice("Config loaded successfully.");
                                this.displayLoadedConfig(configJson);
                            }
                            catch (jsonErr) {
                                configError = "Invalid JSON: " + ((jsonErr === null || jsonErr === void 0 ? void 0 : jsonErr.message) || jsonErr);
                                new obsidian_2.Notice(configError);
                                this.displayLoadedConfig(null, configError);
                            }
                        }
                        else {
                            configError = "Config file does not exist or is not a file.";
                            new obsidian_2.Notice(configError);
                            this.displayLoadedConfig(null, configError);
                        }
                    }
                    catch (err) {
                        configError = "Error checking file: " + ((err === null || err === void 0 ? void 0 : err.message) || err);
                        new obsidian_2.Notice(configError);
                        this.displayLoadedConfig(null, configError);
                    }
                }));
            });
            // Verbose flag
            new obsidian_2.Setting(containerEl)
                .setName("Verbose Mode")
                .setDesc("Enable verbose output for automation commands.")
                .addToggle(toggle => {
                toggle.setValue(this.plugin.settings.verbose || false)
                    .onChange((value) => __awaiter(this, void 0, void 0, function* () {
                    this.plugin.settings.verbose = value;
                    yield this.plugin.saveSettings();
                }));
            });
            // Debug flag
            new obsidian_2.Setting(containerEl)
                .setName("Debug Mode")
                .setDesc("Enable debug output for troubleshooting.")
                .addToggle(toggle => {
                toggle.setValue(this.plugin.settings.debug || false)
                    .onChange((value) => __awaiter(this, void 0, void 0, function* () {
                    this.plugin.settings.debug = value;
                    yield this.plugin.saveSettings();
                }));
            });
            // Dry-run flag
            new obsidian_2.Setting(containerEl)
                .setName("Dry Run")
                .setDesc("Simulate actions without making changes.")
                .addToggle(toggle => {
                toggle.setValue(this.plugin.settings.dryRun || false)
                    .onChange((value) => __awaiter(this, void 0, void 0, function* () {
                    this.plugin.settings.dryRun = value;
                    yield this.plugin.saveSettings();
                }));
            });
            // If config was previously loaded and valid, show fields
            if (window.notebookAutomationLoadedConfig) {
                this.displayLoadedConfig(window.notebookAutomationLoadedConfig);
            }
        });
    }
    displayLoadedConfig(configJson, error) {
        const { containerEl } = this;
        // Remove previous config fields if any
        const prev = containerEl.querySelector('.notebook-automation-config-fields');
        if (prev)
            prev.remove();
        if (error) {
            const errorDiv = containerEl.createDiv({ cls: 'notebook-automation-config-fields' });
            errorDiv.createEl('p', { text: error, cls: 'mod-warning' });
            window.notebookAutomationLoadedConfig = null;
            return;
        }
        if (!configJson)
            return;
        window.notebookAutomationLoadedConfig = configJson;
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
        keyMeta.forEach(meta => {
            const setting = new obsidian_2.Setting(fieldsDiv)
                .setName(meta.label)
                .setDesc(`${meta.desc} (JSON key: ${meta.key})`)
                .addText(text => {
                text.setValue(paths[meta.key] || '')
                    .setDisabled(true);
            });
        });
    }
}
