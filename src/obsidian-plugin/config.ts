
import * as fs from 'fs';
import * as path from 'path';

export interface PluginConfig {
  vaultRoot: string;
  oneDriveRoot: string;
}

/**
 * Reads and validates the plugin configuration from config.json.
 * @param configPath Path to the config.json file.
 * @returns The validated PluginConfig object.
 * @throws Error if the config file is missing or invalid.
 */
export function readConfig(configPath: string): PluginConfig {
  if (!fs.existsSync(configPath)) {
    throw new Error(`Config file not found at: ${configPath}`);
  }
  const raw = fs.readFileSync(configPath, 'utf-8');
  let parsed: any;
  try {
    parsed = JSON.parse(raw);
  } catch (e) {
    throw new Error('Invalid JSON in config file.');
  }
  if (!parsed.vaultRoot || !parsed.oneDriveRoot) {
    throw new Error('Config must include both vaultRoot and oneDriveRoot.');
  }
  return {
    vaultRoot: path.resolve(parsed.vaultRoot),
    oneDriveRoot: path.resolve(parsed.oneDriveRoot)
  };
}
