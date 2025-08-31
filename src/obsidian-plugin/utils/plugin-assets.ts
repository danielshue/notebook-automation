// Utility functions extracted from main.ts
// All functions are exported for use in other modules

import { Plugin } from 'obsidian';

/**
 * Given a full vault path, strip the notebook_vault_fullpath_root and vault_resources_basepath prefix and return the relative path for OneDrive mapping.
 *
 * @param fullPath The full path to the file or resource in the vault.
 * @param vaultRoot The root path of the Obsidian vault.
 * @param vaultBase The base path for resources within the vault (optional).
 * @returns The relative path for OneDrive mapping.
 */
export function getRelativeVaultResourcePath(fullPath: string, vaultRoot: string, vaultBase?: string): string {
  let normFull = fullPath.replace(/\\/g, '/');
  let normRoot = vaultRoot.replace(/\\/g, '/').replace(/\/$/, '');
  let normBase = (vaultBase || '').replace(/\\/g, '/').replace(/^\//, '').replace(/\/$/, '');
  if (normRoot && normFull.startsWith(normRoot)) {
    normFull = normFull.substring(normRoot.length);
    if (normFull.startsWith('/')) normFull = normFull.substring(1);
  }
  if (normBase && normFull.startsWith(normBase)) {
    normFull = normFull.substring(normBase.length);
    if (normFull.startsWith('/')) normFull = normFull.substring(1);
  }
  return normFull;
}

/**
 * Resolves the correct executable name for the current platform and architecture.
 *
 * @returns The platform-specific executable filename (e.g., na-win-x64.exe).
 */
export function getNaExecutableName(): string {
  // Type guards for Node globals in Obsidian plugin context
  let platform: string | undefined = undefined;
  let arch: string | undefined = undefined;
  if (typeof process !== 'undefined' && process.platform && process.arch) {
    platform = process.platform;
    arch = process.arch;
  } else if (typeof window !== 'undefined' && (window as any).process) {
    platform = (window as any).process.platform;
    arch = (window as any).process.arch;
  }
  let platformName: string;
  let archName: string;
  switch (platform) {
    case "win32": platformName = "win"; break;
    case "darwin": platformName = "macos"; break;
    case "linux": platformName = "linux"; break;
    default: platformName = "win"; break;
  }
  const archString = String(arch);
  if (archString === "x64" || archString === "x86_64" || (archString.includes("64") && !archString.includes("arm"))) {
    archName = "x64";
  } else if (archString === "arm64" || archString === "aarch64" || archString.includes("arm")) {
    archName = "arm64";
  } else {
    archName = "x64";
  }
  const extension = platformName === "win" ? ".exe" : "";
  return `na-${platformName}-${archName}${extension}`;
}

/**
 * Gets the full path to the bundled notebook automation executable in the plugin directory.
 *
 * @param plugin The NotebookAutomationPlugin instance.
 * @returns The full path to the executable, or just the executable name if not found.
 */
export function getNaExecutablePath(plugin: Plugin): string {
  const execName = getNaExecutableName();
  try {
    // @ts-ignore
    const path = window.require ? window.require('path') : null;
    // @ts-ignore
    const fs = window.require ? window.require('fs') : null;
    let vaultRoot = '';
    const adapter = plugin.app?.vault?.adapter;
    // @ts-ignore
    if (adapter && typeof adapter.getBasePath === 'function') {
      try {
        // @ts-ignore
        vaultRoot = adapter.getBasePath();
      } catch {}
    }
    const tryFindExecutable = (dir: string): string | null => {
      if (!fs || !path) return null;
      const exactPath = path.join(dir, execName);
      if (fs.existsSync(exactPath)) return exactPath;
      try {
        const files = fs.readdirSync(dir);
        const naExecutables = files.filter((file: string) =>
          file.startsWith('na-') || file === 'na' || file === 'na.exe'
        );
        if (naExecutables.length > 0) {
          let platform = 'win32';
          if (typeof process !== 'undefined' && process.platform) {
            platform = process.platform;
          } else if (typeof window !== 'undefined' && (window as any).process) {
            platform = (window as any).process.platform;
          }
          const platformName = platform === 'win32' ? 'win' : platform === 'darwin' ? 'macos' : 'linux';
          const platformMatch = naExecutables.find((file: string) => file.includes(platformName));
          if (platformMatch) return path.join(dir, platformMatch);
          return path.join(dir, naExecutables[0]);
        }
      } catch {}
      return null;
    };
    const isValidPluginDir = (dir: string | undefined, pluginId: string | undefined) => {
      if (!dir || dir === '/' || dir === '' || dir.length <= 1) return false;
      if (!pluginId) return false;
      const isAbsolute = path ? path.isAbsolute(dir) : (dir.startsWith('/') || dir.match(/^[A-Za-z]:/));
      if (!isAbsolute && !vaultRoot) return false;
      return true;
    };
    if (plugin.manifest && isValidPluginDir(plugin.manifest.dir, plugin.manifest.id) && path) {
      let resolvedDir = plugin.manifest.dir || '';
      if (resolvedDir && (resolvedDir.startsWith('/.obsidian') || resolvedDir.startsWith('/.') || (resolvedDir.startsWith('/') && !fs?.existsSync?.(resolvedDir)))) {
        resolvedDir = resolvedDir.substring(1);
      }
      const isAbsolute = path.isAbsolute(resolvedDir) && fs?.existsSync?.(resolvedDir);
      if (!isAbsolute && vaultRoot) {
        resolvedDir = path.join(vaultRoot, resolvedDir);
      }
      const foundExecutable = tryFindExecutable(resolvedDir);
      if (foundExecutable) return foundExecutable;
    }
    if (plugin.app && plugin.app.vault && path) {
      if (!vaultRoot) {
        const adapter = plugin.app.vault.adapter;
        // @ts-ignore
        if (adapter && typeof adapter.getBasePath === 'function') {
          try {
            // @ts-ignore
            vaultRoot = adapter.getBasePath();
          } catch {}
        }
      }
      if (vaultRoot) {
        const configDir = plugin.app.vault.configDir || '.obsidian';
        const pluginId = plugin.manifest?.id || 'notebook-automation';
        const pluginDir = path.join(vaultRoot, configDir, 'plugins', pluginId);
        const foundExecutable = tryFindExecutable(pluginDir);
        if (foundExecutable) return foundExecutable;
      }
    }
  // __dirname is not always available in Obsidian plugin context; skip this check for browser/Obsidian
    if (vaultRoot && plugin.manifest?.id) {
      const configDir = plugin.app?.vault?.configDir || '.obsidian';
      const pluginId = plugin.manifest.id;
      const lastResortDir = path ? path.join(vaultRoot, configDir, 'plugins', pluginId) : '';
      if (lastResortDir) {
        const foundExecutable = tryFindExecutable(lastResortDir);
        if (foundExecutable) return foundExecutable;
      }
    }
    return execName;
  } catch {
    return execName;
  }
}

/**
 * Progress callback interface for file downloads.
 */
export interface DownloadProgressCallback {
  (current: number, total: number, fileName: string): void;
}

/**
 * Gets the path to the notebook automation executable, downloading it and required files if necessary.
 *
 * @param plugin The NotebookAutomationPlugin instance.
 * @param progressCallback Optional callback to track download progress.
 * @returns The path to the executable, downloading if not present.
 */
export async function ensureExecutableExists(plugin: Plugin, progressCallback?: DownloadProgressCallback): Promise<string> {
  const existingPath = getNaExecutablePath(plugin);
  const execName = getNaExecutableName();
  const expectedVersion = plugin.manifest?.version || '0.0.0';
  try {
    // @ts-ignore
    const fs = window.require ? window.require('fs') : null;

    let needsDownload = true;
    let finalPath = existingPath;

    if (fs && existingPath !== execName && fs.existsSync(existingPath)) {
      // Validate existing executable
      const currentOk = await isExecutableCurrent(plugin, existingPath, expectedVersion);
      if (currentOk) {
        needsDownload = false;
      } else {
        try { fs.unlinkSync(existingPath); } catch { /* ignore */ }
        needsDownload = true;
      }
    }

    // Always ensure required files exist regardless
    try { await ensureRequiredFilesExist(plugin, progressCallback); } catch { /* ignore */ }

    if (needsDownload) {
      finalPath = await downloadExecutableFromGitHub(plugin, progressCallback);
    }
    return finalPath;
  } catch {
    return existingPath;
  }
}

/**
 * Downloads a single file from GitHub releases to the plugin directory.
 *
 * @param plugin The NotebookAutomationPlugin instance.
 * @param fileName The name of the file to download.
 * @param makeExecutable Whether to make the file executable (for executables).
 * @param progressCallback Optional callback to track download progress.
 * @returns The path to the downloaded file.
 * @throws If required Node.js modules are not available or download fails.
 */
async function downloadFileFromGitHub(plugin: Plugin, fileName: string, makeExecutable: boolean = false, progressCallback?: DownloadProgressCallback): Promise<string> {
  // @ts-ignore
  const fs = window.require ? window.require('fs') : null;
  // @ts-ignore
  const path = window.require ? window.require('path') : null;
  // @ts-ignore
  const https = window.require ? window.require('https') : null;
  
  if (!fs || !path || !https) {
    throw new Error('Required Node.js modules not available');
  }

  // Determine plugin directory
  let pluginDir = '';
  const adapter = plugin.app?.vault?.adapter;
  // @ts-ignore
  if (adapter && typeof adapter.getBasePath === 'function') {
    try {
      // @ts-ignore
      const vaultRoot = adapter.getBasePath();
      if (plugin.manifest?.dir) {
        pluginDir = path.resolve(vaultRoot, plugin.manifest.dir);
      }
    } catch {}
  }
  
  if (!pluginDir) {
    throw new Error('Could not determine plugin directory');
  }

  const filePath = path.join(pluginDir, fileName);
  
  // Check if file already exists
  if (fs.existsSync(filePath)) {
    return filePath;
  }

  const version = plugin.manifest?.version || '0.1.0-beta.2';
  const downloadUrl = `https://github.com/danielshue/notebook-automation/releases/download/v${version}/${fileName}`;
  
  return new Promise((resolve, reject) => {
    const request = https.get(downloadUrl, (response: any) => {
      if (response.statusCode === 302 || response.statusCode === 301) {
        const redirectUrl = response.headers.location;
        const redirectRequest = https.get(redirectUrl, (redirectResponse: any) => {
          if (redirectResponse.statusCode !== 200) {
            reject(new Error(`Failed to download ${fileName}: HTTP ${redirectResponse.statusCode}`));
            return;
          }
          const writeStream = fs.createWriteStream(filePath);
          redirectResponse.pipe(writeStream);
          writeStream.on('finish', () => {
            writeStream.close();
            if (makeExecutable) {
              let isWin = false;
              if (typeof process !== 'undefined' && process.platform) {
                isWin = process.platform === 'win32';
              } else if (typeof window !== 'undefined' && (window as any).process) {
                isWin = (window as any).process.platform === 'win32';
              }
              if (!isWin) {
                try { fs.chmodSync(filePath, 0o755); } catch {}
              }
            }
            resolve(filePath);
          });
          writeStream.on('error', (err: any) => {
            try { fs.unlinkSync(filePath); } catch {}
            reject(err);
          });
        });
        redirectRequest.on('error', (err: any) => reject(err));
      } else if (response.statusCode !== 200) {
        reject(new Error(`Failed to download ${fileName}: HTTP ${response.statusCode}`));
        return;
      } else {
        const writeStream = fs.createWriteStream(filePath);
        response.pipe(writeStream);
        writeStream.on('finish', () => {
          writeStream.close();
          if (makeExecutable) {
            let isWin = false;
            if (typeof process !== 'undefined' && process.platform) {
              isWin = process.platform === 'win32';
            } else if (typeof window !== 'undefined' && (window as any).process) {
              isWin = (window as any).process.platform === 'win32';
            }
            if (!isWin) {
              try { fs.chmodSync(filePath, 0o755); } catch {}
            }
          }
          resolve(filePath);
        });
        writeStream.on('error', (err: any) => {
          try { fs.unlinkSync(filePath); } catch {}
          reject(err);
        });
      }
    });
    request.on('error', (err: any) => reject(err));
  });
}

/**
 * Downloads the appropriate notebook automation executable for the current platform from GitHub releases.
 *
 * @param plugin The NotebookAutomationPlugin instance.
 * @param progressCallback Optional callback to track download progress.
 * @returns The path to the downloaded executable.
 * @throws If required Node.js modules are not available or download fails.
 */
export async function downloadExecutableFromGitHub(plugin: Plugin, progressCallback?: DownloadProgressCallback): Promise<string> {
  const execName = getNaExecutableName();
  try {
    if (progressCallback) {
      progressCallback(1, 1, execName);
    }
    return await downloadFileFromGitHub(plugin, execName, true, progressCallback);
  } catch (error) {
    throw error;
  }
}

/**
 * Gets the complete list of plugin files that should be distributed.
 * This reads from the same configuration used by the build system to ensure consistency.
 *
 * @returns Array of file names that should be available in the plugin directory.
 */
export function getDistributedPluginFiles(): string[] {
  // Core plugin files
  const coreFiles = [
    'main.js',
    'manifest.json', 
    'styles.css'
  ];

  // Configuration and prompt files (matches build-plugin.mjs pluginFiles array)
  const assetFiles = [
    'default-config.json',
    'metadata-schema.yml',
    'BaseBlockTemplate.yml', 
    'chunk_summary_prompt.md',
    'final_summary_prompt.md'
  ];

  return [...coreFiles, ...assetFiles];
}

/**
 * Gets the list of executable file patterns that might be distributed.
 *
 * @returns Array of executable patterns for different platforms.
 */
export function getDistributedExecutablePatterns(): string[] {
  return [
    'na-win-x64.exe',
    'na-win-arm64.exe', 
    'na-linux-x64',
    'na-linux-arm64',
    'na-osx-arm64'  // Note: build uses 'macos' but release uses 'osx'
  ];
}

/**
 * Downloads and parses the asset manifest from GitHub releases.
 *
 * @param plugin The NotebookAutomationPlugin instance.
 * @returns The parsed manifest object, or null if failed.
 */
async function downloadAssetManifest(plugin: Plugin): Promise<{ version: string; files: string[] } | null> {
  try {
    const manifestPath = await downloadFileFromGitHub(plugin, 'asset-manifest.json', false);
    
    // @ts-ignore
    const fs = window.require ? window.require('fs') : null;
    if (!fs || !manifestPath) {
      return null;
    }

    const manifestContent = fs.readFileSync(manifestPath, 'utf8');
    const manifest = JSON.parse(manifestContent);
    
    console.log(`[Notebook Automation] Downloaded asset manifest for version ${manifest.version} with ${manifest.files?.length || 0} files`);
    return manifest;
  } catch (error) {
    console.warn('[Notebook Automation] Could not download or parse asset manifest:', error);
    return null;
  }
}

/**
 * Downloads required configuration and prompt files from GitHub releases if they don't exist.
 * Uses the same file list as the build system to ensure consistency.
 *
 * @param plugin The NotebookAutomationPlugin instance.
 * @param progressCallback Optional callback to track download progress.
 * @returns Array of paths to the downloaded files.
 */
export async function ensureRequiredFilesExist(plugin: Plugin, progressCallback?: DownloadProgressCallback): Promise<string[]> {
  // First try to get the file list from asset manifest
  const manifest = await downloadAssetManifest(plugin);
  let filesToDownload: string[] = [];

  if (manifest && manifest.files) {
    // Filter out core files that BRAT handles, and executables (handled separately)
    const coreFiles = ['main.js', 'manifest.json', 'styles.css'];
    filesToDownload = manifest.files.filter(file => 
      !coreFiles.includes(file) && 
      !file.startsWith('na-') && // Skip executables (handled by ensureExecutableExists)
      file !== 'asset-manifest.json' // Skip the manifest itself
    );
    console.log(`[Notebook Automation] Using manifest-based download for ${filesToDownload.length} files`);
  } else {
    // Fallback to hardcoded list if manifest unavailable
    const allFiles = getDistributedPluginFiles();
    filesToDownload = allFiles.filter(file => 
      !['main.js', 'manifest.json', 'styles.css'].includes(file)
    );
    console.log('[Notebook Automation] Using fallback file list for download');
  }

  const downloadedFiles: string[] = [];
  const totalFiles = filesToDownload.length;
  
  for (let i = 0; i < filesToDownload.length; i++) {
    const fileName = filesToDownload[i];
    try {
      if (progressCallback) {
        progressCallback(i + 1, totalFiles, fileName);
      }
      const filePath = await downloadFileFromGitHub(plugin, fileName, false, progressCallback);
      downloadedFiles.push(filePath);
    } catch (error) {
      console.warn(`[Notebook Automation] Could not download ${fileName}:`, error);
      // Continue with other files even if one fails
    }
  }

  return downloadedFiles;
}

/**
 * Ensures that configuration files exist in the plugin directory, downloading them if missing.
 * This can be called independently when configuration files are needed.
 *
 * @param plugin The NotebookAutomationPlugin instance.
 * @returns True if all required config files are present or successfully downloaded.
 */
export async function ensureConfigFilesExist(plugin: Plugin): Promise<boolean> {
  try {
    // @ts-ignore
    const fs = window.require ? window.require('fs') : null;
    // @ts-ignore
    const path = window.require ? window.require('path') : null;
    
    if (!fs || !path) {
      console.warn('[Notebook Automation] File system modules not available for config check');
      return false;
    }

    // Get plugin directory
    let pluginDir = '';
    const adapter = plugin.app?.vault?.adapter;
    // @ts-ignore
    if (adapter && typeof adapter.getBasePath === 'function') {
      try {
        // @ts-ignore
        const vaultRoot = adapter.getBasePath();
        if (plugin.manifest?.dir) {
          pluginDir = path.resolve(vaultRoot, plugin.manifest.dir);
        }
      } catch {}
    }
    
    if (!pluginDir) {
      console.warn('[Notebook Automation] Could not determine plugin directory for config check');
      return false;
    }

    // Get configuration files from the distributed files list
    const allFiles = getDistributedPluginFiles();
    const configFiles = allFiles.filter(file => 
      file.endsWith('.json') || file.endsWith('.yml') || file.endsWith('.yaml')
    );
    
    const missingFiles = configFiles.filter(file => !fs.existsSync(path.join(pluginDir, file)));

    if (missingFiles.length > 0) {
      console.log(`[Notebook Automation] Missing config files: ${missingFiles.join(', ')}. Downloading...`);
      await ensureRequiredFilesExist(plugin);
      
      // Verify download success
      const stillMissing = configFiles.filter(file => !fs.existsSync(path.join(pluginDir, file)));
      if (stillMissing.length > 0) {
        console.warn(`[Notebook Automation] Still missing after download: ${stillMissing.join(', ')}`);
        return false;
      }
    }

    return true;
  } catch (error) {
    console.warn('[Notebook Automation] Error ensuring config files exist:', error);
    return false;
  }
}

/**
 * Checks if the installed executable is the current version.
 * This runs the executable with --version and compares the reported version.
 *
 * @param plugin The NotebookAutomationPlugin instance.
 * @param execPath The path to the executable.
 * @param expectedPluginVersion The expected plugin version (from manifest).
 * @returns True if the executable is current, false if not or if check fails.
 */
export async function isExecutableCurrent(plugin: Plugin, execPath: string, expectedPluginVersion: string): Promise<boolean> {
  try {
    // @ts-ignore
    const childProcess = window.require ? window.require('child_process') : null;
    // @ts-ignore
    const fs = window.require ? window.require('fs') : null;
    if (!childProcess || !fs) return false;
    if (!fs.existsSync(execPath)) return false;
    // Run with --version (short timeout)
    const output: string = childProcess.execSync(`"${execPath}" --version`, { timeout: 4000, windowsHide: true }).toString();
    // Look for line starting with 'Notebook Automation version '
    const line = output.split(/\r?\n/).find(l => l.toLowerCase().startsWith('notebook automation version')) || '';
    if (!line) return false;
    // Extract semantic plugin version inside 'Notebook Automation version X ('
    const match = line.match(/Notebook Automation version\s+([^\s]+)\s+\(/i);
    if (!match) return false;
    const reported = match[1].trim();
    return reported === expectedPluginVersion;
  } catch {
    return false;
  }
}
