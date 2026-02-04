// Licensed under the MIT License. See LICENSE file in the project root for full license information.

import { App, TFile, TFolder } from 'obsidian';
import { VaultItem, SearchResult, SearchMatch } from '../models';

/**
 * Service interface for vault browsing and search operations.
 */
export interface IVaultService {
  /**
   * Browses the vault and returns items at the specified path.
   * 
   * @param path - Path to browse (relative to vault root).
   * @returns Array of vault items (files and folders).
   */
  browseVault(path: string): Promise<VaultItem[]>;

  /**
   * Searches the vault for files matching the query.
   * 
   * @param query - Search query string.
   * @returns Array of search results.
   */
  searchVault(query: string): Promise<SearchResult[]>;
}

/**
 * Implementation of vault service using Obsidian's API.
 */
export class VaultService implements IVaultService {
  constructor(private readonly app: App) {}

  async browseVault(path: string): Promise<VaultItem[]> {
    const items: VaultItem[] = [];
    
    try {
      const folder = path === '' || path === '/' 
        ? this.app.vault.getRoot() 
        : this.app.vault.getAbstractFileByPath(path);

      if (!folder) {
        console.warn(`[VaultService] Path not found: ${path}`);
        return [];
      }

      if (!(folder instanceof TFolder)) {
        console.warn(`[VaultService] Path is not a folder: ${path}`);
        return [];
      }

      for (const child of folder.children) {
        if (child instanceof TFile) {
          items.push({
            path: child.path,
            name: child.name,
            isFolder: false,
            size: child.stat.size,
            modified: child.stat.mtime
          });
        } else if (child instanceof TFolder) {
          items.push({
            path: child.path,
            name: child.name,
            isFolder: true,
            modified: child.stat?.mtime
          });
        }
      }

      return items;
    } catch (error) {
      console.error(`[VaultService] Error browsing vault at ${path}:`, error);
      return [];
    }
  }

  async searchVault(query: string): Promise<SearchResult[]> {
    const results: SearchResult[] = [];

    if (!query || query.trim().length === 0) {
      return results;
    }

    const searchQuery = query.toLowerCase();
    
    try {
      // Get all markdown files
      const files = this.app.vault.getMarkdownFiles();

      for (const file of files) {
        const content = await this.app.vault.cachedRead(file);
        const lines = content.split('\n');
        const matches: SearchMatch[] = [];

        // Search through each line
        for (let i = 0; i < lines.length; i++) {
          const line = lines[i];
          const lowerLine = line.toLowerCase();
          
          if (lowerLine.includes(searchQuery)) {
            const startPos = lowerLine.indexOf(searchQuery);
            matches.push({
              line: i,
              text: line,
              start: startPos,
              end: startPos + query.length
            });
          }
        }

        if (matches.length > 0) {
          results.push({
            path: file.path,
            matches,
            score: matches.length
          });
        }
      }

      // Sort by score (number of matches)
      results.sort((a, b) => (b.score || 0) - (a.score || 0));

      return results;
    } catch (error) {
      console.error('[VaultService] Error searching vault:', error);
      return [];
    }
  }
}
