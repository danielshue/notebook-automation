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
    var _a, _b, _c, _d, _e, _f, _g, _h, _j, _k, _l, _m, _o, _p, _q;
    const execName = getNaExecutableName();
    try {
        // @ts-ignore
        const path = window.require ? window.require('path') : null;
        // @ts-ignore
        const fs = window.require ? window.require('fs') : null;
        // Log plugin.manifest.dir and plugin.manifest.id for debugging
        // eslint-disable-next-line no-console
        console.log('[Notebook Automation] plugin.manifest.dir:', (_a = plugin.manifest) === null || _a === void 0 ? void 0 : _a.dir);
        // eslint-disable-next-line no-console
        console.log('[Notebook Automation] plugin.manifest.id:', (_b = plugin.manifest) === null || _b === void 0 ? void 0 : _b.id);
        // Get vault root first - this is essential for building absolute paths
        let vaultRoot = '';
        const adapter = (_d = (_c = plugin.app) === null || _c === void 0 ? void 0 : _c.vault) === null || _d === void 0 ? void 0 : _d.adapter;
        // eslint-disable-next-line no-console
        console.log('[Notebook Automation] adapter exists:', !!adapter);
        // eslint-disable-next-line no-console
        console.log('[Notebook Automation] adapter constructor:', (_e = adapter === null || adapter === void 0 ? void 0 : adapter.constructor) === null || _e === void 0 ? void 0 : _e.name);
        // @ts-ignore - Check for getBasePath method directly instead of constructor name (which can be minified)
        if (adapter && typeof adapter.getBasePath === 'function') {
            try {
                // @ts-ignore
                vaultRoot = adapter.getBasePath();
                // eslint-disable-next-line no-console
                console.log('[Notebook Automation] vaultRoot from getBasePath:', vaultRoot);
            }
            catch (err) {
                // eslint-disable-next-line no-console
                console.log('[Notebook Automation] Error calling getBasePath:', err);
            }
        }
        else {
            // eslint-disable-next-line no-console
            console.log('[Notebook Automation] Could not get vaultRoot - getBasePath method not available');
        }
        // Also try vault.getRoot() for additional validation
        const vaultRootFolder = (_h = (_g = (_f = plugin.app) === null || _f === void 0 ? void 0 : _f.vault) === null || _g === void 0 ? void 0 : _g.getRoot) === null || _h === void 0 ? void 0 : _h.call(_g);
        if (vaultRootFolder) {
            // eslint-disable-next-line no-console
            console.log('[Notebook Automation] vault.getRoot() returned:', vaultRootFolder.path, 'name:', vaultRootFolder.name);
        }
        else {
            // eslint-disable-next-line no-console
            console.log('[Notebook Automation] vault.getRoot() not available or returned null');
        }
        // Log final vaultRoot value
        // eslint-disable-next-line no-console
        console.log('[Notebook Automation] Final vaultRoot value:', vaultRoot);
        const isValidPluginDir = (dir, pluginId) => {
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
            if (resolvedDir.startsWith('/.obsidian') || resolvedDir.startsWith('/.') || (resolvedDir.startsWith('/') && !((_j = fs === null || fs === void 0 ? void 0 : fs.existsSync) === null || _j === void 0 ? void 0 : _j.call(fs, resolvedDir)))) {
                // This is likely a relative path incorrectly prefixed with /
                // Remove the leading / and treat as relative
                resolvedDir = resolvedDir.substring(1);
                // eslint-disable-next-line no-console
                console.log('[Notebook Automation] Detected incorrectly rooted path, treating as relative:', resolvedDir);
            }
            // If manifest.dir is relative, make it absolute by prepending vaultRoot
            const isAbsolute = path.isAbsolute(resolvedDir) && ((_k = fs === null || fs === void 0 ? void 0 : fs.existsSync) === null || _k === void 0 ? void 0 : _k.call(fs, resolvedDir));
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
            }
            else {
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
                    }
                    catch (err) {
                        // eslint-disable-next-line no-console
                        console.log('[Notebook Automation] Error calling getBasePath in fallback:', err);
                    }
                }
            }
            if (vaultRoot) {
                const configDir = plugin.app.vault.configDir || '.obsidian';
                const pluginId = ((_l = plugin.manifest) === null || _l === void 0 ? void 0 : _l.id) || 'notebook-automation';
                const pluginDir = path.join(vaultRoot, configDir, 'plugins', pluginId);
                const resolved = path.join(pluginDir, execName);
                // Check if file exists
                const exists = fs && fs.existsSync && fs.existsSync(resolved);
                // eslint-disable-next-line no-console
                console.log(`[Notebook Automation] Using FileSystemAdapter fallback for naPath: ${resolved} (exists: ${exists})`);
                return resolved;
            }
            else {
                // If we still can't get vaultRoot, try to construct from manifest.dir if it exists
                if (((_m = plugin.manifest) === null || _m === void 0 ? void 0 : _m.dir) && plugin.manifest.dir !== '/' && !path.isAbsolute(plugin.manifest.dir)) {
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
        if (vaultRoot && ((_o = plugin.manifest) === null || _o === void 0 ? void 0 : _o.id)) {
            const configDir = ((_q = (_p = plugin.app) === null || _p === void 0 ? void 0 : _p.vault) === null || _q === void 0 ? void 0 : _q.configDir) || '.obsidian';
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
    }
    catch (_r) {
        // eslint-disable-next-line no-console
        console.log('[Notebook Automation] Exception in getNaExecutablePath, using execName fallback:', execName);
        return execName;
    }
}
const obsidian_1 = require("obsidian");
const obsidian_2 = require("obsidian");
const DEFAULT_SETTINGS = {
    configPath: "",
    verbose: false,
    debug: false,
    dryRun: false,
    force: false
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
            // Check if config is loaded
            if (!this.settings.configPath) {
                new obsidian_2.Notice("Please configure the config file path in plugin settings first.");
                return;
            }
            try {
                yield this.executeNotebookAutomationCommand(action, relPath);
            }
            catch (error) {
                // eslint-disable-next-line no-console
                console.error(`[Notebook Automation] Error executing command:`, error);
                new obsidian_2.Notice(`Error executing command: ${(error === null || error === void 0 ? void 0 : error.message) || error}`);
            }
        });
    }
    /**
     * Execute the actual na CLI command based on the action
     */
    executeNotebookAutomationCommand(action, relativePath) {
        return __awaiter(this, void 0, void 0, function* () {
            var _a, _b;
            // @ts-ignore
            const child_process = window.require ? window.require('child_process') : null;
            if (!child_process) {
                throw new Error("Child process module not available");
            }
            const naPath = getNaExecutablePath(this);
            const configPath = this.settings.configPath;
            // Build command arguments based on action
            let args = [];
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
            new obsidian_2.Notice(`Starting: ${commandDescription} for ${relativePath}`);
            try {
                // Get environment variables from process.env and log them for debugging
                const env = Object.assign({}, process.env);
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
                const exitPromise = new Promise((resolve, reject) => {
                    childProcess.on('close', (code) => {
                        // eslint-disable-next-line no-console
                        console.log(`[Notebook Automation] Process exited with code: ${code}`);
                        // eslint-disable-next-line no-console
                        console.log(`[Notebook Automation] Final STDOUT:`, stdout);
                        // eslint-disable-next-line no-console
                        console.log(`[Notebook Automation] Final STDERR:`, stderr);
                        if (code === 0) {
                            resolve();
                        }
                        else {
                            const error = new Error(`Command failed with exit code ${code}`);
                            error.code = code;
                            error.stdout = stdout;
                            error.stderr = stderr;
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
                yield exitPromise;
                // eslint-disable-next-line no-console
                console.log(`[Notebook Automation] Command completed successfully`);
                // Show success notice
                new obsidian_2.Notice(`✅ ${commandDescription} completed successfully!`);
            }
            catch (error) {
                // eslint-disable-next-line no-console
                console.error(`[Notebook Automation] Command failed:`, error);
                // Get more detailed error information
                const stderr = ((_a = error === null || error === void 0 ? void 0 : error.stderr) === null || _a === void 0 ? void 0 : _a.toString()) || '';
                const stdout = ((_b = error === null || error === void 0 ? void 0 : error.stdout) === null || _b === void 0 ? void 0 : _b.toString()) || '';
                const exitCode = (error === null || error === void 0 ? void 0 : error.code) || (error === null || error === void 0 ? void 0 : error.status) || 'unknown';
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
                }
                else if (stderr && stderr.includes('AZURE_OPENAI_KEY')) {
                    errorMsg = "❗ Azure OpenAI API key is missing. Please set the AZURE_OPENAI_KEY environment variable.";
                }
                else if (stdout) {
                    // Extract the last meaningful line from stdout
                    const lines = stdout.split('\n').filter(line => line.trim().length > 0);
                    const lastLine = lines[lines.length - 1];
                    if (lastLine && lastLine.includes('ERR')) {
                        errorMsg = lastLine.replace(/\[[^\]]*\]/, '').trim(); // Remove timestamp
                    }
                }
                else if (stderr) {
                    errorMsg = stderr;
                }
                else if (error === null || error === void 0 ? void 0 : error.message) {
                    errorMsg = error.message;
                }
                // Show error notice with details
                new obsidian_2.Notice(`❌ ${commandDescription} failed: ${errorMsg}`, 8000); // Show for 8 seconds
                throw error;
            }
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
                const lines = result.split(/\r?\n/).map(line => line.trim()).filter(line => line.length > 0);
                let version = "";
                // Look for a line containing "version" 
                const versionLine = lines.find(line => line.toLowerCase().includes('version'));
                if (versionLine) {
                    // Extract just the version part after "version "
                    const versionMatch = versionLine.match(/version\s+(.+)/i);
                    if (versionMatch && versionMatch[1]) {
                        version = versionMatch[1].trim();
                    }
                    else {
                        // Fallback: try to extract everything after "version"
                        const versionIndex = versionLine.toLowerCase().indexOf('version');
                        if (versionIndex !== -1) {
                            version = versionLine.substring(versionIndex + 7).trim(); // 7 = length of "version"
                        }
                        else {
                            version = versionLine;
                        }
                    }
                }
                else {
                    version = lines[0] || "Unknown version";
                }
                // eslint-disable-next-line no-console
                console.log(`[Notebook Automation] All lines:`, lines);
                // eslint-disable-next-line no-console
                console.log(`[Notebook Automation] Selected version line:`, versionLine);
                // eslint-disable-next-line no-console
                console.log(`[Notebook Automation] Extracted version:`, version);
                return version || "Unknown version";
            }
            catch (err) {
                // eslint-disable-next-line no-console
                console.log(`[Notebook Automation] Error running na --version:`, err);
                // eslint-disable-next-line no-console
                console.log(`[Notebook Automation] Error message:`, err === null || err === void 0 ? void 0 : err.message);
                return `Error: ${(err === null || err === void 0 ? void 0 : err.message) || 'Unknown error'}`;
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
            // Force flag
            new obsidian_2.Setting(containerEl)
                .setName("Force Overwrite")
                .setDesc("Overwrite existing notes even if they already exist.")
                .addToggle(toggle => {
                toggle.setValue(this.plugin.settings.force || false)
                    .onChange((value) => __awaiter(this, void 0, void 0, function* () {
                    this.plugin.settings.force = value;
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
