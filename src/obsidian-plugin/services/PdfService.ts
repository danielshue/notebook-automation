// Licensed under the MIT License. See LICENSE file in the project root for full license information.

import { PdfOperationResult } from '../models';
import { IAISummarizer } from './AISummarizer';
import { IMarkdownService } from './MarkdownService';
import { App, TFile, TFolder } from 'obsidian';

/**
 * Service interface for PDF processing operations.
 */
export interface IPdfService {
  /**
   * Converts PDF files to markdown notes, extracting text and optionally generating AI summaries.
   * 
   * @param inputPath - Path to PDF file or directory.
   * @param outputPath - Optional output directory for generated notes.
   * @param dryRun - If true, simulates processing without writing files.
   * @param noSummary - If true, skips AI summary generation.
   * @param forceOverwrite - If true, overwrites existing notes.
   * @returns Result with processing statistics.
   */
  convert(
    inputPath: string,
    outputPath?: string,
    dryRun?: boolean,
    noSummary?: boolean,
    forceOverwrite?: boolean
  ): Promise<PdfOperationResult>;

  /**
   * Extracts text from a PDF file.
   * 
   * @param filePath - Path to the PDF file.
   * @returns Extracted text content.
   */
  extractText(filePath: string): Promise<string>;
}

/**
 * Implementation of PDF service.
 * Uses pdf-parse npm package for PDF text extraction.
 */
export class PdfService implements IPdfService {
  private pdfParse: any = null;
  private fs: any = null;
  private path: any = null;

  constructor(
    private readonly app: App,
    private readonly markdownService: IMarkdownService,
    private readonly aiSummarizer?: IAISummarizer
  ) {
    // Try to load pdf-parse and Node.js modules if available
    try {
      // @ts-ignore
      if (typeof require !== 'undefined') {
        this.pdfParse = require('pdf-parse');
        // @ts-ignore
        this.fs = require('fs');
        // @ts-ignore
        this.path = require('path');
      }
    } catch (error) {
      console.warn('[PdfService] pdf-parse not available:', error);
    }
  }

  async convert(
    inputPath: string,
    outputPath?: string,
    dryRun = false,
    noSummary = false,
    forceOverwrite = false
  ): Promise<PdfOperationResult> {
    const startTime = Date.now();
    
    console.log(`[PdfService] convert: inputPath=${inputPath}, outputPath=${outputPath}, dryRun=${dryRun}`);

    if (!this.pdfParse || !this.fs) {
      return {
        success: false,
        message: 'PDF parsing not available',
        filesFound: 0,
        notesCreated: 0,
        failed: 0,
        dryRun,
        processingTime: Date.now() - startTime,
        totalTokens: 0,
        errorMessage: 'pdf-parse library not available. This requires Node.js file system access.'
      };
    }

    let filesFound = 0;
    let notesCreated = 0;
    let failed = 0;
    let totalTokens = 0;

    try {
      // Get absolute path
      // @ts-ignore
      const adapter = this.app.vault.adapter;
      // @ts-ignore
      const vaultRoot = adapter.getBasePath ? adapter.getBasePath() : '';
      const fullInputPath = this.path.isAbsolute(inputPath) 
        ? inputPath 
        : this.path.join(vaultRoot, inputPath);

      // Check if path exists
      if (!this.fs.existsSync(fullInputPath)) {
        return {
          success: false,
          message: `Path not found: ${inputPath}`,
          filesFound: 0,
          notesCreated: 0,
          failed: 0,
          dryRun,
          processingTime: Date.now() - startTime,
          totalTokens: 0,
          errorMessage: `Path not found: ${inputPath}`
        };
      }

      // Determine if it's a file or directory
      const stats = this.fs.statSync(fullInputPath);
      const pdfFiles: string[] = [];

      if (stats.isFile() && fullInputPath.toLowerCase().endsWith('.pdf')) {
        pdfFiles.push(fullInputPath);
      } else if (stats.isDirectory()) {
        // Find all PDF files in directory
        const files = this.fs.readdirSync(fullInputPath);
        for (const file of files) {
          if (file.toLowerCase().endsWith('.pdf')) {
            pdfFiles.push(this.path.join(fullInputPath, file));
          }
        }
      }

      filesFound = pdfFiles.length;

      // Process each PDF file
      for (const pdfPath of pdfFiles) {
        try {
          console.log(`[PdfService] Processing: ${pdfPath}`);
          
          // Extract text from PDF
          const text = await this.extractText(pdfPath);
          
          if (!text || text.trim().length === 0) {
            console.warn(`[PdfService] No text extracted from ${pdfPath}`);
            failed++;
            continue;
          }

          // Generate summary if requested
          let summary = '';
          if (!noSummary && this.aiSummarizer) {
            try {
              const summaryResult = await this.aiSummarizer.summarizeWithVariables(
                text,
                {
                  type: 'pdf_document',
                  source: this.path.basename(pdfPath)
                }
              );
              summary = summaryResult || '';
              // Estimate tokens used (rough estimate: summary length / 4)
              totalTokens += Math.ceil(summary.length / 4);
            } catch (error) {
              console.warn(`[PdfService] Failed to generate summary for ${pdfPath}:`, error);
            }
          }

          if (!dryRun) {
            // Create markdown note
            const outputFileName = this.path.basename(pdfPath, '.pdf') + '.md';
            const outputDir = outputPath || 'PDFs';
            const outputFilePath = `${outputDir}/${outputFileName}`;

            const frontmatter = {
              title: this.path.basename(pdfPath, '.pdf'),
              source: pdfPath,
              type: 'pdf',
              created: new Date().toISOString()
            };

            let content = '';
            if (summary) {
              content += `## Summary\n\n${summary}\n\n`;
            }
            content += `## Extracted Text\n\n${text}`;

            const file = await this.markdownService.createMarkdownFile(
              outputFilePath,
              content,
              frontmatter,
              forceOverwrite
            );

            if (file) {
              notesCreated++;
              console.log(`[PdfService] Created note: ${outputFilePath}`);
            } else {
              console.warn(`[PdfService] Failed to create note for ${pdfPath}`);
              failed++;
            }
          } else {
            // Dry run - just count as created
            notesCreated++;
          }
        } catch (error) {
          console.error(`[PdfService] Error processing ${pdfPath}:`, error);
          failed++;
        }
      }

      const processingTime = Date.now() - startTime;
      const success = failed === 0 && filesFound > 0;

      return {
        success,
        message: `Processed ${notesCreated} of ${filesFound} PDF files`,
        filesFound,
        notesCreated,
        failed,
        dryRun,
        processingTime,
        totalTokens
      };
    } catch (error) {
      console.error('[PdfService] Error in convert:', error);
      return {
        success: false,
        message: `Error: ${error}`,
        filesFound,
        notesCreated,
        failed,
        dryRun,
        processingTime: Date.now() - startTime,
        totalTokens,
        errorMessage: String(error)
      };
    }
  }

  async extractText(filePath: string): Promise<string> {
    console.log(`[PdfService] extractText: filePath=${filePath}`);

    if (!this.pdfParse || !this.fs) {
      throw new Error('pdf-parse library not available. This requires Node.js file system access.');
    }

    try {
      const dataBuffer = this.fs.readFileSync(filePath);
      const data = await this.pdfParse(dataBuffer);
      return data.text || '';
    } catch (error) {
      console.error(`[PdfService] Error extracting text from ${filePath}:`, error);
      throw new Error(`Failed to extract text from PDF: ${error}`);
    }
  }
}
