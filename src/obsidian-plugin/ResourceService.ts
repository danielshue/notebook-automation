// Placeholder for user notifications (to be replaced with Obsidian API notification system)
function notifyUser(message: string) {
  // eslint-disable-next-line no-console
  console.log(`[NOTIFY] ${message}`);
}
// Simple logging utility (can be replaced with a more robust logger)
function logInfo(message: string) {
  // eslint-disable-next-line no-console
  console.info(`[INFO] ${message}`);
}
function logError(message: string) {
  // eslint-disable-next-line no-console
  console.error(`[ERROR] ${message}`);
}
/**
 * Copies a resource file from OneDrive into the Vault.
 * @param oneDriveFilePath Absolute path to the file in OneDrive.
 * @param vaultTargetFolder Absolute path to the target folder in the Vault.
 * @returns Promise that resolves when the file is copied.
 */
export function importResourceFile(oneDriveFilePath: string, vaultTargetFolder: string): Promise<void> {
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
import { exec } from 'child_process';
/**
 * Opens a resource file using the system default application.
 * On desktop, uses the OS open command. On mobile, this is a placeholder.
 * @param filePath Absolute path to the resource file.
 * @returns Promise that resolves when the file is opened or rejects on error.
 */
export function openResourceFile(filePath: string): Promise<void> {
  return new Promise((resolve, reject) => {
    logInfo(`Opening resource file: ${filePath}`);
    // Platform-specific open command
    let cmd: string;
    if (process.platform === 'darwin') {
      cmd = `open "${filePath}"`;
    } else if (process.platform === 'win32') {
      cmd = `start "" "${filePath}"`;
    } else if (process.platform === 'linux') {
      cmd = `xdg-open "${filePath}"`;
    } else {
      logError('Open operation not supported on this platform.');
      return reject(new Error('Open operation not supported on this platform.'));
    }
    exec(cmd, (error) => {
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
import * as fs from 'fs';

/**
 * Lists resource files in the given OneDrive folder.
 * @param oneDriveFolderPath The absolute path to the OneDrive folder.
 * @returns Array of resource file names (videos, PDFs, spreadsheets, HTML).
 */
export function listResourceFiles(oneDriveFolderPath: string): string[] {
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

import { PluginConfig } from './config';
import * as path from 'path';

/**
 * Maps a Vault-relative folder path to the corresponding OneDrive folder path.
 * @param vaultFolderPath The absolute path to the folder in the Vault.
 * @param config The loaded plugin configuration.
 * @returns The absolute path to the corresponding folder in OneDrive.
 */
export function mapVaultToOneDriveFolder(vaultFolderPath: string, config: PluginConfig): string {
  if (!vaultFolderPath.startsWith(config.vaultRoot)) {
    throw new Error('Vault folder path does not start with configured vault root.');
  }
  const relative = path.relative(config.vaultRoot, vaultFolderPath);
  return path.join(config.oneDriveRoot, relative);
}
