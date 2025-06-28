"use strict";
var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    var desc = Object.getOwnPropertyDescriptor(m, k);
    if (!desc || ("get" in desc ? !m.__esModule : desc.writable || desc.configurable)) {
      desc = { enumerable: true, get: function() { return m[k]; } };
    }
    Object.defineProperty(o, k2, desc);
}) : (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    o[k2] = m[k];
}));
var __setModuleDefault = (this && this.__setModuleDefault) || (Object.create ? (function(o, v) {
    Object.defineProperty(o, "default", { enumerable: true, value: v });
}) : function(o, v) {
    o["default"] = v;
});
var __importStar = (this && this.__importStar) || (function () {
    var ownKeys = function(o) {
        ownKeys = Object.getOwnPropertyNames || function (o) {
            var ar = [];
            for (var k in o) if (Object.prototype.hasOwnProperty.call(o, k)) ar[ar.length] = k;
            return ar;
        };
        return ownKeys(o);
    };
    return function (mod) {
        if (mod && mod.__esModule) return mod;
        var result = {};
        if (mod != null) for (var k = ownKeys(mod), i = 0; i < k.length; i++) if (k[i] !== "default") __createBinding(result, mod, k[i]);
        __setModuleDefault(result, mod);
        return result;
    };
})();
Object.defineProperty(exports, "__esModule", { value: true });
exports.importResourceFile = importResourceFile;
exports.openResourceFile = openResourceFile;
exports.listResourceFiles = listResourceFiles;
exports.mapVaultToOneDriveFolder = mapVaultToOneDriveFolder;
// Placeholder for user notifications (to be replaced with Obsidian API notification system)
function notifyUser(message) {
    // eslint-disable-next-line no-console
    console.log(`[NOTIFY] ${message}`);
}
// Simple logging utility (can be replaced with a more robust logger)
function logInfo(message) {
    // eslint-disable-next-line no-console
    console.info(`[INFO] ${message}`);
}
function logError(message) {
    // eslint-disable-next-line no-console
    console.error(`[ERROR] ${message}`);
}
/**
 * Copies a resource file from OneDrive into the Vault.
 * @param oneDriveFilePath Absolute path to the file in OneDrive.
 * @param vaultTargetFolder Absolute path to the target folder in the Vault.
 * @returns Promise that resolves when the file is copied.
 */
function importResourceFile(oneDriveFilePath, vaultTargetFolder) {
    return new Promise((resolve, reject) => {
        logInfo(`Importing file from OneDrive: ${oneDriveFilePath} to Vault: ${vaultTargetFolder}`);
        if (!fs.existsSync(oneDriveFilePath)) {
            logError('Source file does not exist: ' + oneDriveFilePath);
            return reject(new Error('Source file does not exist: ' + oneDriveFilePath));
        }
        if (!fs.existsSync(vaultTargetFolder)) {
            fs.mkdirSync(vaultTargetFolder, { recursive: true });
        }
        const fileName = path.basename(oneDriveFilePath);
        const destPath = path.join(vaultTargetFolder, fileName);
        fs.copyFile(oneDriveFilePath, destPath, (err) => {
            if (err) {
                logError(`Failed to copy file: ${err.message}`);
                return reject(err);
            }
            logInfo(`Successfully imported file: ${fileName}`);
            notifyUser(`Imported file: ${fileName}`);
            resolve();
        });
    });
}
const child_process_1 = require("child_process");
/**
 * Opens a resource file using the system default application.
 * On desktop, uses the OS open command. On mobile, this is a placeholder.
 * @param filePath Absolute path to the resource file.
 * @returns Promise that resolves when the file is opened or rejects on error.
 */
function openResourceFile(filePath) {
    return new Promise((resolve, reject) => {
        logInfo(`Opening resource file: ${filePath}`);
        // Platform-specific open command
        let cmd;
        if (process.platform === 'darwin') {
            cmd = `open "${filePath}"`;
        }
        else if (process.platform === 'win32') {
            cmd = `start "" "${filePath}"`;
        }
        else if (process.platform === 'linux') {
            cmd = `xdg-open "${filePath}"`;
        }
        else {
            logError('Open operation not supported on this platform.');
            return reject(new Error('Open operation not supported on this platform.'));
        }
        (0, child_process_1.exec)(cmd, (error) => {
            if (error) {
                logError(`Failed to open file: ${error.message}`);
                return reject(error);
            }
            logInfo(`Successfully opened file: ${filePath}`);
            notifyUser(`Opened file: ${filePath}`);
            resolve();
        });
    });
}
const fs = __importStar(require("fs"));
/**
 * Lists resource files in the given OneDrive folder.
 * @param oneDriveFolderPath The absolute path to the OneDrive folder.
 * @returns Array of resource file names (videos, PDFs, spreadsheets, HTML).
 */
function listResourceFiles(oneDriveFolderPath) {
    logInfo(`Listing resource files in OneDrive folder: ${oneDriveFolderPath}`);
    if (!fs.existsSync(oneDriveFolderPath)) {
        logError('OneDrive folder does not exist: ' + oneDriveFolderPath);
        throw new Error('OneDrive folder does not exist: ' + oneDriveFolderPath);
    }
    const supportedExtensions = ['.mp4', '.mov', '.avi', '.pdf', '.xls', '.xlsx', '.csv', '.htm', '.html'];
    const files = fs.readdirSync(oneDriveFolderPath);
    if (files.length === 0) {
        logError('No files found in the OneDrive folder.');
        throw new Error('No files found in the OneDrive folder.');
    }
    const resourceFiles = files.filter(file => supportedExtensions.includes(path.extname(file).toLowerCase()));
    if (resourceFiles.length === 0) {
        logError('No supported resource files found in the OneDrive folder.');
        throw new Error('No supported resource files found in the OneDrive folder.');
    }
    logInfo(`Found resource files: ${resourceFiles.join(', ')}`);
    return resourceFiles;
}
const path = __importStar(require("path"));
/**
 * Maps a Vault-relative folder path to the corresponding OneDrive folder path.
 * @param vaultFolderPath The absolute path to the folder in the Vault.
 * @param config The loaded plugin configuration.
 * @returns The absolute path to the corresponding folder in OneDrive.
 */
function mapVaultToOneDriveFolder(vaultFolderPath, config) {
    if (!vaultFolderPath.startsWith(config.vaultRoot)) {
        throw new Error('Vault folder path does not start with configured vault root.');
    }
    const relative = path.relative(config.vaultRoot, vaultFolderPath);
    return path.join(config.oneDriveRoot, relative);
}
