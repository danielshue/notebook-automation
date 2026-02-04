// Licensed under the MIT License. See LICENSE file in the project root for full license information.

import { App, TFile } from 'obsidian';

/**
 * Service interface for markdown generation operations.
 */
export interface IMarkdownService {
  /**
   * Generates markdown content with YAML frontmatter.
   * 
   * @param content - The markdown content body.
   * @param frontmatter - Key-value pairs for the YAML frontmatter.
   * @returns Complete markdown document with frontmatter.
   */
  generateWithFrontmatter(content: string, frontmatter: Record<string, any>): string;

  /**
   * Creates a markdown file in the vault.
   * 
   * @param path - Path where the file should be created.
   * @param content - The markdown content body.
   * @param frontmatter - Key-value pairs for the YAML frontmatter.
   * @param overwrite - If true, overwrites existing file.
   * @returns The created file, or null if creation failed.
   */
  createMarkdownFile(
    path: string,
    content: string,
    frontmatter?: Record<string, any>,
    overwrite?: boolean
  ): Promise<TFile | null>;

  /**
   * Parses YAML frontmatter from markdown content.
   * 
   * @param content - The markdown content with frontmatter.
   * @returns Object containing frontmatter and body content.
   */
  parseFrontmatter(content: string): { frontmatter: Record<string, any>; body: string };
}

/**
 * Implementation of markdown service.
 */
export class MarkdownService implements IMarkdownService {
  constructor(private readonly app: App) {}

  generateWithFrontmatter(content: string, frontmatter: Record<string, any>): string {
    const yamlLines: string[] = ['---'];

    for (const [key, value] of Object.entries(frontmatter)) {
      if (value === null || value === undefined) {
        continue;
      }

      if (Array.isArray(value)) {
        yamlLines.push(`${key}:`);
        for (const item of value) {
          yamlLines.push(`  - ${this.escapeYamlValue(item)}`);
        }
      } else if (typeof value === 'object') {
        // Simple object serialization
        yamlLines.push(`${key}:`);
        for (const [subKey, subValue] of Object.entries(value)) {
          yamlLines.push(`  ${subKey}: ${this.escapeYamlValue(subValue)}`);
        }
      } else {
        yamlLines.push(`${key}: ${this.escapeYamlValue(value)}`);
      }
    }

    yamlLines.push('---');
    yamlLines.push('');

    return yamlLines.join('\n') + content;
  }

  async createMarkdownFile(
    path: string,
    content: string,
    frontmatter?: Record<string, any>,
    overwrite = false
  ): Promise<TFile | null> {
    try {
      const fullContent = frontmatter 
        ? this.generateWithFrontmatter(content, frontmatter)
        : content;

      const existingFile = this.app.vault.getAbstractFileByPath(path);

      if (existingFile) {
        if (!overwrite) {
          console.warn(`[MarkdownService] File already exists: ${path}`);
          return null;
        }

        if (existingFile instanceof TFile) {
          await this.app.vault.modify(existingFile, fullContent);
          return existingFile;
        }
      }

      // Create parent folders if they don't exist
      const parentPath = path.substring(0, path.lastIndexOf('/'));
      if (parentPath && !this.app.vault.getAbstractFileByPath(parentPath)) {
        await this.createFoldersRecursively(parentPath);
      }

      const file = await this.app.vault.create(path, fullContent);
      return file;
    } catch (error) {
      console.error(`[MarkdownService] Error creating markdown file at ${path}:`, error);
      return null;
    }
  }

  parseFrontmatter(content: string): { frontmatter: Record<string, any>; body: string } {
    const frontmatterRegex = /^---\n([\s\S]*?)\n---\n([\s\S]*)$/;
    const match = content.match(frontmatterRegex);

    if (!match) {
      return { frontmatter: {}, body: content };
    }

    const frontmatterText = match[1];
    const body = match[2];
    const frontmatter: Record<string, any> = {};

    // Simple YAML parsing (handles basic key: value pairs and arrays)
    const lines = frontmatterText.split('\n');
    let currentKey: string | null = null;
    let currentArray: any[] | null = null;

    for (const line of lines) {
      const trimmedLine = line.trim();
      
      if (!trimmedLine) {
        continue;
      }

      if (trimmedLine.startsWith('- ')) {
        // Array item
        if (currentArray) {
          currentArray.push(this.parseYamlValue(trimmedLine.substring(2)));
        }
      } else if (trimmedLine.includes(':')) {
        // Key-value pair
        const colonIndex = trimmedLine.indexOf(':');
        const key = trimmedLine.substring(0, colonIndex).trim();
        const value = trimmedLine.substring(colonIndex + 1).trim();

        if (value === '') {
          // Start of an array or object
          currentKey = key;
          currentArray = [];
          frontmatter[key] = currentArray;
        } else {
          currentKey = null;
          currentArray = null;
          frontmatter[key] = this.parseYamlValue(value);
        }
      }
    }

    return { frontmatter, body };
  }

  private escapeYamlValue(value: any): string {
    if (typeof value === 'string') {
      // Escape special YAML characters
      if (value.includes(':') || value.includes('#') || value.includes('[') || value.includes(']') || value.includes('\\') || value.includes('"')) {
        // Escape backslashes first, then double quotes
        const escaped = value.replace(/\\/g, '\\\\').replace(/"/g, '\\"');
        return `"${escaped}"`;
      }
      return value;
    }
    return String(value);
  }

  private parseYamlValue(value: string): any {
    const trimmed = value.trim();
    
    // Remove quotes if present
    if ((trimmed.startsWith('"') && trimmed.endsWith('"')) || 
        (trimmed.startsWith("'") && trimmed.endsWith("'"))) {
      return trimmed.substring(1, trimmed.length - 1);
    }

    // Parse numbers
    if (!isNaN(Number(trimmed))) {
      return Number(trimmed);
    }

    // Parse booleans
    if (trimmed === 'true') return true;
    if (trimmed === 'false') return false;
    if (trimmed === 'null') return null;

    return trimmed;
  }

  private async createFoldersRecursively(path: string): Promise<void> {
    const parts = path.split('/').filter(p => p.length > 0);
    let currentPath = '';

    for (const part of parts) {
      currentPath = currentPath ? `${currentPath}/${part}` : part;
      const folder = this.app.vault.getAbstractFileByPath(currentPath);
      
      if (!folder) {
        await this.app.vault.createFolder(currentPath);
      }
    }
  }
}
