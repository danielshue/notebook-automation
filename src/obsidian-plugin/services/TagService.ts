// Licensed under the MIT License. See LICENSE file in the project root for full license information.

import { App, TFile, TFolder, CachedMetadata } from 'obsidian';
import { TagOperationResult, YamlDiagnosisResult } from '../models';

/**
 * Service interface for tag management operations in the Obsidian vault.
 * Provides high-level API for tag operations designed for integration with the plugin.
 */
export interface ITagService {
  /**
   * Adds nested tags to markdown files based on frontmatter fields.
   * 
   * @param path - Path to process (file or folder).
   * @param dryRun - If true, simulates changes without modifying files.
   * @returns Result containing statistics and any errors.
   */
  addNestedTags(path: string, dryRun?: boolean): Promise<TagOperationResult>;

  /**
   * Consolidates duplicate and similar tags across files.
   * 
   * @param path - Path to process, or null for entire vault.
   * @param dryRun - If true, simulates changes without modifying files.
   * @returns Result containing statistics and any errors.
   */
  consolidateTags(path?: string, dryRun?: boolean): Promise<TagOperationResult>;

  /**
   * Restructures tags according to the configured hierarchy.
   * 
   * @param path - Path to process, or null for entire vault.
   * @param dryRun - If true, simulates changes without modifying files.
   * @returns Result containing statistics and any errors.
   */
  restructureTags(path?: string, dryRun?: boolean): Promise<TagOperationResult>;

  /**
   * Updates or adds a frontmatter key-value pair in markdown files.
   * 
   * @param path - Path to file or directory to process.
   * @param key - Frontmatter key to update.
   * @param value - New value for the key.
   * @param dryRun - If true, simulates changes without modifying files.
   * @returns Result containing statistics and any errors.
   */
  updateFrontmatter(path: string, key: string, value: string, dryRun?: boolean): Promise<TagOperationResult>;

  /**
   * Diagnoses YAML frontmatter issues in markdown files.
   * 
   * @param path - Path to scan, or null for entire vault.
   * @returns Diagnosis result containing files with issues and suggested fixes.
   */
  diagnoseYaml(path?: string): Promise<YamlDiagnosisResult>;

  /**
   * Gets tags from a specific file.
   * 
   * @param filePath - Path to the file.
   * @returns Array of tag strings.
   */
  getTags(filePath: string): Promise<string[]>;

  /**
   * Adds a tag to a file.
   * 
   * @param filePath - Path to the file.
   * @param tag - Tag to add.
   */
  addTag(filePath: string, tag: string): Promise<void>;

  /**
   * Removes a tag from a file.
   * 
   * @param filePath - Path to the file.
   * @param tag - Tag to remove.
   */
  removeTag(filePath: string, tag: string): Promise<void>;
}

/**
 * Implementation of tag service using Obsidian's API.
 */
export class TagService implements ITagService {
  constructor(private readonly app: App) {}

  async addNestedTags(path: string, dryRun = false): Promise<TagOperationResult> {
    // TODO: Implement nested tag addition based on frontmatter fields
    console.log(`[TagService] addNestedTags: path=${path}, dryRun=${dryRun}`);
    
    return {
      success: false,
      message: 'Not yet implemented',
      filesProcessed: 0,
      filesModified: 0,
      tagsAdded: 0,
      filesWithErrors: 0,
      dryRun,
      errorMessage: 'This feature is not yet implemented'
    };
  }

  async consolidateTags(path?: string, dryRun = false): Promise<TagOperationResult> {
    // TODO: Implement tag consolidation
    console.log(`[TagService] consolidateTags: path=${path}, dryRun=${dryRun}`);
    
    return {
      success: false,
      message: 'Not yet implemented',
      filesProcessed: 0,
      filesModified: 0,
      tagsAdded: 0,
      filesWithErrors: 0,
      dryRun,
      errorMessage: 'This feature is not yet implemented'
    };
  }

  async restructureTags(path?: string, dryRun = false): Promise<TagOperationResult> {
    // TODO: Implement tag restructuring
    console.log(`[TagService] restructureTags: path=${path}, dryRun=${dryRun}`);
    
    return {
      success: false,
      message: 'Not yet implemented',
      filesProcessed: 0,
      filesModified: 0,
      tagsAdded: 0,
      filesWithErrors: 0,
      dryRun,
      errorMessage: 'This feature is not yet implemented'
    };
  }

  async updateFrontmatter(path: string, key: string, value: string, dryRun = false): Promise<TagOperationResult> {
    console.log(`[TagService] updateFrontmatter: path=${path}, key=${key}, value=${value}, dryRun=${dryRun}`);
    
    try {
      const file = this.app.vault.getAbstractFileByPath(path);
      
      if (!file) {
        return {
          success: false,
          message: `File not found: ${path}`,
          filesProcessed: 0,
          filesModified: 0,
          tagsAdded: 0,
          filesWithErrors: 1,
          dryRun,
          errorMessage: `File not found: ${path}`
        };
      }

      if (file instanceof TFile && file.extension === 'md') {
        if (!dryRun) {
          await this.app.fileManager.processFrontMatter(file, (frontmatter) => {
            frontmatter[key] = value;
          });
        }
        
        return {
          success: true,
          message: `Updated frontmatter ${key}=${value} in ${path}`,
          filesProcessed: 1,
          filesModified: dryRun ? 0 : 1,
          tagsAdded: 0,
          filesWithErrors: 0,
          dryRun
        };
      } else if (file instanceof TFolder) {
        // TODO: Process all markdown files in folder
        return {
          success: false,
          message: 'Folder processing not yet implemented',
          filesProcessed: 0,
          filesModified: 0,
          tagsAdded: 0,
          filesWithErrors: 0,
          dryRun,
          errorMessage: 'Folder processing not yet implemented'
        };
      }

      return {
        success: false,
        message: `Not a markdown file: ${path}`,
        filesProcessed: 0,
        filesModified: 0,
        tagsAdded: 0,
        filesWithErrors: 1,
        dryRun,
        errorMessage: `Not a markdown file: ${path}`
      };
    } catch (error) {
      return {
        success: false,
        message: `Error updating frontmatter: ${error}`,
        filesProcessed: 0,
        filesModified: 0,
        tagsAdded: 0,
        filesWithErrors: 1,
        dryRun,
        errorMessage: String(error)
      };
    }
  }

  async diagnoseYaml(path?: string): Promise<YamlDiagnosisResult> {
    // TODO: Implement YAML diagnosis
    console.log(`[TagService] diagnoseYaml: path=${path}`);
    
    return {
      success: false,
      message: 'Not yet implemented',
      filesScanned: 0,
      filesWithIssues: 0,
      issues: []
    };
  }

  async getTags(filePath: string): Promise<string[]> {
    const file = this.app.vault.getAbstractFileByPath(filePath);
    
    if (!(file instanceof TFile)) {
      return [];
    }

    const metadata = this.app.metadataCache.getFileCache(file);
    
    if (!metadata) {
      return [];
    }

    // Get tags from frontmatter
    const frontmatterTags = metadata.frontmatter?.tags || [];
    const tags = Array.isArray(frontmatterTags) ? frontmatterTags : [frontmatterTags];
    
    // Get tags from content (inline tags)
    const inlineTags = metadata.tags?.map(tag => tag.tag) || [];
    
    // Combine and deduplicate
    return [...new Set([...tags, ...inlineTags])];
  }

  async addTag(filePath: string, tag: string): Promise<void> {
    const file = this.app.vault.getAbstractFileByPath(filePath);
    
    if (!(file instanceof TFile) || file.extension !== 'md') {
      throw new Error(`Not a markdown file: ${filePath}`);
    }

    await this.app.fileManager.processFrontMatter(file, (frontmatter) => {
      if (!frontmatter.tags) {
        frontmatter.tags = [];
      } else if (!Array.isArray(frontmatter.tags)) {
        frontmatter.tags = [frontmatter.tags];
      }
      
      if (!frontmatter.tags.includes(tag)) {
        frontmatter.tags.push(tag);
      }
    });
  }

  async removeTag(filePath: string, tag: string): Promise<void> {
    const file = this.app.vault.getAbstractFileByPath(filePath);
    
    if (!(file instanceof TFile) || file.extension !== 'md') {
      throw new Error(`Not a markdown file: ${filePath}`);
    }

    await this.app.fileManager.processFrontMatter(file, (frontmatter) => {
      if (Array.isArray(frontmatter.tags)) {
        frontmatter.tags = frontmatter.tags.filter((t: string) => t !== tag);
      }
    });
  }
}
