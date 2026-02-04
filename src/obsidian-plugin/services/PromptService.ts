// Licensed under the MIT License. See LICENSE file in the project root for full license information.

import { App } from 'obsidian';

/**
 * Service interface for prompt template loading and variable substitution.
 * Matches the C# IPromptService interface for 1-to-1 functionality.
 */
export interface IPromptService {
  /**
   * Loads a prompt template from the prompts directory.
   * 
   * @param templateName - Name of the template file (without .md extension).
   * @returns The template content, or null if not found.
   */
  loadTemplate(templateName: string): Promise<string | null>;

  /**
   * Substitutes variables in a template string.
   * 
   * @param template - The template string with {{variable}} placeholders.
   * @param variables - Dictionary of variable names to values.
   * @returns The template with variables substituted.
   */
  substituteVariables(template: string, variables: Record<string, string>): string;

  /**
   * Loads a template and substitutes variables in one step.
   * Equivalent to C# GetPromptAsync method.
   * 
   * @param templateName - Name of the template file (without .md extension).
   * @param variables - Dictionary of variable names to values.
   * @returns The processed template content, or null if template not found.
   */
  loadAndSubstitute(templateName: string, variables?: Record<string, string>): Promise<string | null>;

  /**
   * Alias for loadAndSubstitute to match C# interface.
   * 
   * @param templateName - Name of the template file (without .md extension).
   * @param variables - Dictionary of variable names to values.
   * @returns The processed template content, or null if template not found.
   */
  getPromptAsync(templateName: string, variables?: Record<string, string>): Promise<string | null>;
}

/**
 * Implementation of prompt service for loading and processing templates.
 */
export class PromptService implements IPromptService {
  private fs: any = null;
  private path: any = null;
  private templateCache: Map<string, string> = new Map();

  constructor(private readonly app: App) {
    // Try to load Node.js modules if available
    try {
      // @ts-ignore
      if (typeof require !== 'undefined') {
        // @ts-ignore
        this.fs = require('fs');
        // @ts-ignore
        this.path = require('path');
      }
    } catch (error) {
      console.warn('[PromptService] Node.js fs/path not available:', error);
    }
  }

  async loadTemplate(templateName: string): Promise<string | null> {
    // Check cache first
    if (this.templateCache.has(templateName)) {
      return this.templateCache.get(templateName)!;
    }

    try {
      // Try to load from plugin directory first
      if (this.fs && this.path) {
        // @ts-ignore
        const adapter = this.app.vault.adapter;
        // @ts-ignore
        const vaultRoot = adapter.getBasePath ? adapter.getBasePath() : '';
        
        // Try plugin directory
        const pluginDir = this.path.join(vaultRoot, '.obsidian', 'plugins', 'notebook-automation');
        const templatePath = this.path.join(pluginDir, `${templateName}.md`);
        
        if (this.fs.existsSync(templatePath)) {
          const content = this.fs.readFileSync(templatePath, 'utf8');
          this.templateCache.set(templateName, content);
          console.log(`[PromptService] Loaded template: ${templateName}`);
          return content;
        }
      }

      // Try to load from vault if file system approach doesn't work
      const vaultPaths = [
        `prompts/${templateName}.md`,
        `.obsidian/plugins/notebook-automation/${templateName}.md`,
        `${templateName}.md`
      ];

      for (const vaultPath of vaultPaths) {
        const file = this.app.vault.getAbstractFileByPath(vaultPath);
        if (file && 'read' in file) {
          const content = await this.app.vault.read(file as any);
          this.templateCache.set(templateName, content);
          console.log(`[PromptService] Loaded template from vault: ${vaultPath}`);
          return content;
        }
      }

      console.warn(`[PromptService] Template not found: ${templateName}`);
      return null;
    } catch (error) {
      console.error(`[PromptService] Error loading template ${templateName}:`, error);
      return null;
    }
  }

  substituteVariables(template: string, variables: Record<string, string>): string {
    let result = template;

    // Substitute {{variable}} style placeholders
    for (const [key, value] of Object.entries(variables)) {
      const regex = new RegExp(`{{${key}}}`, 'g');
      result = result.replace(regex, value);
      
      // Also support {{$variable}} style
      const dollarRegex = new RegExp(`{{\\$${key}}}`, 'g');
      result = result.replace(dollarRegex, value);
    }

    // Handle special [yamlfrontmatter] placeholder
    if (variables.yamlfrontmatter) {
      result = result.replace(/\[yamlfrontmatter\]/g, variables.yamlfrontmatter);
    }

    return result;
  }

  async loadAndSubstitute(templateName: string, variables?: Record<string, string>): Promise<string | null> {
    const template = await this.loadTemplate(templateName);
    
    if (!template) {
      return null;
    }

    if (variables && Object.keys(variables).length > 0) {
      return this.substituteVariables(template, variables);
    }

    return template;
  }

  /**
   * Alias for loadAndSubstitute to match C# IPromptService interface.
   * Provides 1-to-1 compatibility with C# GetPromptAsync method.
   * 
   * @param templateName - Name of the template file (without .md extension).
   * @param variables - Dictionary of variable names to values.
   * @returns The processed template content, or null if template not found.
   */
  async getPromptAsync(templateName: string, variables?: Record<string, string>): Promise<string | null> {
    return this.loadAndSubstitute(templateName, variables);
  }

  /**
   * Clears the template cache.
   * Useful for development or when templates are updated.
   */
  clearCache(): void {
    this.templateCache.clear();
    console.log('[PromptService] Template cache cleared');
  }

  /**
   * Preloads common templates into cache.
   * Call this during plugin initialization for better performance.
   */
  async preloadCommonTemplates(): Promise<void> {
    const commonTemplates = [
      'chunk_summary_prompt',
      'final_summary_prompt',
      'video_summary_prompt',
      'pdf_summary_prompt'
    ];

    for (const templateName of commonTemplates) {
      await this.loadTemplate(templateName);
    }

    console.log(`[PromptService] Preloaded ${this.templateCache.size} templates`);
  }
}
