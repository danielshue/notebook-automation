// Utility functions extracted from main.ts
// All functions are exported for use in other modules

import { Plugin } from 'obsidian';

/**
 * Given a full vault path, strip the notebook_vault_fullpath_root and vault_resources_basepath prefix and return the relative path for OneDrive mapping.
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
 * Utility to resolve the correct executable name for the current platform and architecture.
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
 * Get the full path to the bundled na executable in the plugin directory.
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
 * Gets the path to the notebook automation executable, downloading it if necessary
 */
export async function ensureExecutableExists(plugin: Plugin): Promise<string> {
  const existingPath = getNaExecutablePath(plugin);
  const execName = getNaExecutableName();
  try {
    // @ts-ignore
    const fs = window.require ? window.require('fs') : null;
    if (fs && existingPath !== execName && fs.existsSync(existingPath)) {
      return existingPath;
    }
    return await downloadExecutableFromGitHub(plugin);
  } catch (error) {
    return existingPath;
  }
}

/**
 * Downloads the appropriate executable for the current platform from GitHub releases
 */
export async function downloadExecutableFromGitHub(plugin: Plugin): Promise<string> {
  const execName = getNaExecutableName();
  try {
    // @ts-ignore
    const fs = window.require ? window.require('fs') : null;
    // @ts-ignore
    const path = window.require ? window.require('path') : null;
    // @ts-ignore
    const https = window.require ? window.require('https') : null;
    if (!fs || !path || !https) throw new Error('Required Node.js modules not available');
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
    if (!pluginDir) throw new Error('Could not determine plugin directory');
    const execPath = path.join(pluginDir, execName);
    if (fs.existsSync(execPath)) return execPath;
    const version = plugin.manifest?.version || '0.1.0-beta.2';
    const downloadUrl = `https://github.com/danielshue/notebook-automation/releases/download/v${version}/${execName}`;
    return new Promise((resolve, reject) => {
      const request = https.get(downloadUrl, (response: any) => {
        if (response.statusCode === 302 || response.statusCode === 301) {
          const redirectUrl = response.headers.location;
          const redirectRequest = https.get(redirectUrl, (redirectResponse: any) => {
            if (redirectResponse.statusCode !== 200) {
              reject(new Error(`Failed to download executable: HTTP ${redirectResponse.statusCode}`));
              return;
            }
            const writeStream = fs.createWriteStream(execPath);
            redirectResponse.pipe(writeStream);
            writeStream.on('finish', () => {
              writeStream.close();
              let isWin = false;
              if (typeof process !== 'undefined' && process.platform) {
                isWin = process.platform === 'win32';
              } else if (typeof window !== 'undefined' && (window as any).process) {
                isWin = (window as any).process.platform === 'win32';
              }
              if (!isWin) {
                try { fs.chmodSync(execPath, 0o755); } catch {}
              }
              resolve(execPath);
            });
            writeStream.on('error', (err: any) => {
              fs.unlinkSync(execPath);
              reject(err);
            });
          });
          redirectRequest.on('error', (err: any) => reject(err));
        } else if (response.statusCode !== 200) {
          reject(new Error(`Failed to download executable: HTTP ${response.statusCode}`));
          return;
        } else {
          const writeStream = fs.createWriteStream(execPath);
          response.pipe(writeStream);
          writeStream.on('finish', () => {
            writeStream.close();
            let isWin = false;
            if (typeof process !== 'undefined' && process.platform) {
              isWin = process.platform === 'win32';
            } else if (typeof window !== 'undefined' && (window as any).process) {
              isWin = (window as any).process.platform === 'win32';
            }
            if (!isWin) {
              try { fs.chmodSync(execPath, 0o755); } catch {}
            }
            resolve(execPath);
          });
          writeStream.on('error', (err: any) => {
            fs.unlinkSync(execPath);
            reject(err);
          });
        }
      });
      request.on('error', (err: any) => reject(err));
    });
  } catch (error) {
    throw error;
  }
}
