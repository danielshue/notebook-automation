// Licensed under the MIT License. See LICENSE file in the project root for full license information.

import { PdfOperationResult } from '../models';
import { IAISummarizer } from './AISummarizer';

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
 * Requires pdf-parse or pdfjs-dist npm package for PDF text extraction.
 */
export class PdfService implements IPdfService {
  constructor(
    private readonly aiSummarizer?: IAISummarizer
  ) {}

  async convert(
    inputPath: string,
    outputPath?: string,
    dryRun = false,
    noSummary = false,
    forceOverwrite = false
  ): Promise<PdfOperationResult> {
    const startTime = Date.now();
    
    console.log(`[PdfService] convert: inputPath=${inputPath}, outputPath=${outputPath}, dryRun=${dryRun}`);

    // TODO: Implement PDF conversion logic
    // 1. Find PDF files at inputPath
    // 2. For each PDF:
    //    - Extract text using pdf-parse or pdfjs-dist
    //    - Generate summary using aiSummarizer if !noSummary
    //    - Create markdown note with frontmatter
    //    - Save to outputPath or default location
    
    const processingTime = Date.now() - startTime;

    return {
      success: false,
      message: 'Not yet implemented',
      filesFound: 0,
      notesCreated: 0,
      failed: 0,
      dryRun,
      processingTime,
      totalTokens: 0,
      errorMessage: 'This feature is not yet implemented. Requires pdf-parse or pdfjs-dist package.'
    };
  }

  async extractText(filePath: string): Promise<string> {
    console.log(`[PdfService] extractText: filePath=${filePath}`);

    // TODO: Implement PDF text extraction
    // Option 1: Use pdf-parse (Node.js)
    // const fs = require('fs');
    // const pdf = require('pdf-parse');
    // const dataBuffer = fs.readFileSync(filePath);
    // const data = await pdf(dataBuffer);
    // return data.text;

    // Option 2: Use pdfjs-dist (works in both Node.js and browser)
    // const pdfjsLib = require('pdfjs-dist/legacy/build/pdf.js');
    // const loadingTask = pdfjsLib.getDocument(filePath);
    // const pdf = await loadingTask.promise;
    // let text = '';
    // for (let i = 1; i <= pdf.numPages; i++) {
    //   const page = await pdf.getPage(i);
    //   const content = await page.getTextContent();
    //   const pageText = content.items.map((item: any) => item.str).join(' ');
    //   text += pageText + '\n';
    // }
    // return text;

    throw new Error('PDF text extraction not yet implemented. Install pdf-parse or pdfjs-dist package.');
  }
}
