// Licensed under the MIT License. See LICENSE file in the project root for full license information.

import { VideoOperationResult, VideoConsolidationResult } from '../models';
import { IAISummarizer } from './AISummarizer';
import { IMarkdownService } from './MarkdownService';
import { App, TFile, TFolder } from 'obsidian';

/**
 * Service interface for video processing operations.
 */
export interface IVideoService {
  /**
   * Creates markdown notes from video files, extracting metadata and transcripts.
   * 
   * @param inputPath - Path to video file or directory.
   * @param outputPath - Optional output directory for generated notes.
   * @param dryRun - If true, simulates processing without writing files.
   * @param noSummary - If true, skips AI summary generation.
   * @param forceOverwrite - If true, overwrites existing notes.
   * @returns Result with processing statistics.
   */
  createNotes(
    inputPath: string,
    outputPath?: string,
    dryRun?: boolean,
    noSummary?: boolean,
    forceOverwrite?: boolean
  ): Promise<VideoOperationResult>;

  /**
   * Consolidates video transcripts from a folder into a single class-level note.
   * 
   * @param inputPath - Path to folder containing video notes.
   * @param recursive - If true, includes videos from subdirectories.
   * @param force - If true, overwrites existing consolidated note.
   * @param dryRun - If true, simulates without writing the consolidated note.
   * @returns Result with consolidation statistics.
   */
  consolidateTranscripts(
    inputPath: string,
    recursive?: boolean,
    force?: boolean,
    dryRun?: boolean
  ): Promise<VideoConsolidationResult>;

  /**
   * Processes a video transcript file.
   * 
   * @param transcriptPath - Path to the transcript file.
   * @returns Processed transcript content.
   */
  processTranscript(transcriptPath: string): Promise<string>;
}

/**
 * Implementation of video service.
 */
export class VideoService implements IVideoService {
  private fs: any = null;
  private path: any = null;

  constructor(
    private readonly app: App,
    private readonly markdownService: IMarkdownService,
    private readonly aiSummarizer?: IAISummarizer
  ) {
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
      console.warn('[VideoService] Node.js fs/path not available:', error);
    }
  }

  async createNotes(
    inputPath: string,
    outputPath?: string,
    dryRun = false,
    noSummary = false,
    forceOverwrite = false
  ): Promise<VideoOperationResult> {
    const startTime = Date.now();
    
    console.log(`[VideoService] createNotes: inputPath=${inputPath}, outputPath=${outputPath}, dryRun=${dryRun}`);

    if (!this.fs) {
      return {
        success: false,
        message: 'File system not available',
        filesFound: 0,
        notesCreated: 0,
        failed: 0,
        dryRun,
        processingTime: Date.now() - startTime,
        totalTokens: 0,
        errorMessage: 'Node.js file system not available'
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

      const stats = this.fs.statSync(fullInputPath);
      const videoFiles: string[] = [];
      const videoExtensions = ['.mp4', '.avi', '.mov', '.mkv', '.webm', '.flv', '.wmv'];

      if (stats.isFile() && videoExtensions.some(ext => fullInputPath.toLowerCase().endsWith(ext))) {
        videoFiles.push(fullInputPath);
      } else if (stats.isDirectory()) {
        const files = this.fs.readdirSync(fullInputPath);
        for (const file of files) {
          if (videoExtensions.some(ext => file.toLowerCase().endsWith(ext))) {
            videoFiles.push(this.path.join(fullInputPath, file));
          }
        }
      }

      filesFound = videoFiles.length;

      // Process each video file
      for (const videoPath of videoFiles) {
        try {
          console.log(`[VideoService] Processing: ${videoPath}`);
          
          // Look for transcript file
          const transcriptPath = await this.findTranscriptFile(videoPath);
          
          if (!transcriptPath) {
            console.warn(`[VideoService] No transcript found for ${videoPath}`);
            failed++;
            continue;
          }

          // Process transcript
          const transcript = await this.processTranscript(transcriptPath);
          
          if (!transcript || transcript.trim().length === 0) {
            console.warn(`[VideoService] Empty transcript for ${videoPath}`);
            failed++;
            continue;
          }

          // Generate summary if requested
          let summary = '';
          if (!noSummary && this.aiSummarizer) {
            try {
              const summaryResult = await this.aiSummarizer.summarizeWithVariables(
                transcript,
                {
                  type: 'video_transcript',
                  source: this.path.basename(videoPath)
                }
              );
              summary = summaryResult || '';
              totalTokens += Math.ceil(summary.length / 4);
            } catch (error) {
              console.warn(`[VideoService] Failed to generate summary for ${videoPath}:`, error);
            }
          }

          if (!dryRun) {
            // Create markdown note
            const outputFileName = this.path.basename(videoPath, this.path.extname(videoPath)) + '.md';
            const outputDir = outputPath || 'Videos';
            const outputFilePath = `${outputDir}/${outputFileName}`;

            const fileStats = this.fs.statSync(videoPath);
            const frontmatter = {
              title: this.path.basename(videoPath, this.path.extname(videoPath)),
              source: videoPath,
              type: 'video',
              size: fileStats.size,
              created: new Date().toISOString()
            };

            let content = '';
            if (summary) {
              content += `## Summary\n\n${summary}\n\n`;
            }
            content += `## Transcript\n\n${transcript}`;

            const file = await this.markdownService.createMarkdownFile(
              outputFilePath,
              content,
              frontmatter,
              forceOverwrite
            );

            if (file) {
              notesCreated++;
              console.log(`[VideoService] Created note: ${outputFilePath}`);
            } else {
              console.warn(`[VideoService] Failed to create note for ${videoPath}`);
              failed++;
            }
          } else {
            notesCreated++;
          }
        } catch (error) {
          console.error(`[VideoService] Error processing ${videoPath}:`, error);
          failed++;
        }
      }

      const processingTime = Date.now() - startTime;
      const success = failed === 0 && filesFound > 0;

      return {
        success,
        message: `Processed ${notesCreated} of ${filesFound} video files`,
        filesFound,
        notesCreated,
        failed,
        dryRun,
        processingTime,
        totalTokens
      };
    } catch (error) {
      console.error('[VideoService] Error in createNotes:', error);
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

  async consolidateTranscripts(
    inputPath: string,
    recursive = false,
    force = false,
    dryRun = false
  ): Promise<VideoConsolidationResult> {
    console.log(`[VideoService] consolidateTranscripts: inputPath=${inputPath}, recursive=${recursive}, dryRun=${dryRun}`);

    try {
      const folder = this.app.vault.getAbstractFileByPath(inputPath);
      
      if (!(folder instanceof TFolder)) {
        return {
          success: false,
          message: `Path is not a folder: ${inputPath}`,
          transcriptsAggregated: 0,
          skipped: 0,
          wasWritten: false,
          dryRun,
          errorMessage: `Path is not a folder: ${inputPath}`
        };
      }

      // Find all markdown files in the folder
      const files = await this.getMarkdownFiles(folder, recursive);
      const transcripts: Array<{file: TFile, content: string}> = [];
      let skipped = 0;

      for (const file of files) {
        try {
          const content = await this.app.vault.cachedRead(file);
          const { frontmatter, body } = this.markdownService.parseFrontmatter(content);
          
          // Check if it's a video note
          if (frontmatter.type === 'video' && body.includes('## Transcript')) {
            // Extract transcript section
            const transcriptMatch = body.match(/## Transcript\n\n([\s\S]+?)(?=\n## |$)/);
            if (transcriptMatch) {
              transcripts.push({
                file,
                content: transcriptMatch[1].trim()
              });
            } else {
              skipped++;
            }
          } else {
            skipped++;
          }
        } catch (error) {
          console.error(`[VideoService] Error reading ${file.path}:`, error);
          skipped++;
        }
      }

      if (transcripts.length === 0) {
        return {
          success: false,
          message: 'No video transcripts found',
          transcriptsAggregated: 0,
          skipped,
          wasWritten: false,
          dryRun,
          errorMessage: 'No video transcripts found'
        };
      }

      // Create consolidated content
      let consolidatedContent = '';
      for (const {file, content} of transcripts) {
        consolidatedContent += `### ${file.basename}\n\n${content}\n\n`;
      }

      const outputPath = `${inputPath}/_consolidated-transcripts.md`;

      if (!dryRun) {
        const frontmatter = {
          title: 'Consolidated Transcripts',
          type: 'consolidated',
          transcripts: transcripts.length,
          created: new Date().toISOString()
        };

        const file = await this.markdownService.createMarkdownFile(
          outputPath,
          consolidatedContent,
          frontmatter,
          force
        );

        return {
          success: !!file,
          message: `Consolidated ${transcripts.length} transcripts`,
          outputPath: file?.path,
          transcriptsAggregated: transcripts.length,
          skipped,
          wasWritten: !!file,
          dryRun
        };
      }

      return {
        success: true,
        message: `Would consolidate ${transcripts.length} transcripts (dry run)`,
        transcriptsAggregated: transcripts.length,
        skipped,
        wasWritten: false,
        dryRun
      };
    } catch (error) {
      console.error('[VideoService] Error in consolidateTranscripts:', error);
      return {
        success: false,
        message: `Error: ${error}`,
        transcriptsAggregated: 0,
        skipped: 0,
        wasWritten: false,
        dryRun,
        errorMessage: String(error)
      };
    }
  }

  async processTranscript(transcriptPath: string): Promise<string> {
    console.log(`[VideoService] processTranscript: transcriptPath=${transcriptPath}`);

    if (!this.fs) {
      throw new Error('File system not available');
    }

    try {
      const content = this.fs.readFileSync(transcriptPath, 'utf8');
      const ext = this.path.extname(transcriptPath).toLowerCase();

      if (ext === '.vtt') {
        return this.parseVTT(content);
      } else if (ext === '.srt') {
        return this.parseSRT(content);
      } else if (ext === '.txt') {
        return content; // Plain text, return as-is
      } else {
        throw new Error(`Unsupported transcript format: ${ext}`);
      }
    } catch (error) {
      console.error(`[VideoService] Error processing transcript ${transcriptPath}:`, error);
      throw error;
    }
  }

  private parseVTT(content: string): string {
    // Remove WEBVTT header and cue identifiers
    let lines = content.split('\n');
    const textLines: string[] = [];
    let skipNextLine = false;

    for (const line of lines) {
      const trimmed = line.trim();
      
      // Skip WEBVTT header, NOTE lines, and timestamps
      if (trimmed.startsWith('WEBVTT') || trimmed.startsWith('NOTE') || trimmed.startsWith('Kind:') || trimmed.startsWith('Language:')) {
        continue;
      }
      
      // Skip timestamp lines (format: 00:00:00.000 --> 00:00:00.000)
      if (trimmed.includes('-->')) {
        skipNextLine = false;
        continue;
      }
      
      // Skip empty lines and cue identifiers
      if (trimmed === '' || /^\d+$/.test(trimmed)) {
        continue;
      }
      
      // Add text lines
      textLines.push(trimmed);
    }

    return textLines.join(' ').replace(/\s+/g, ' ').trim();
  }

  private parseSRT(content: string): string {
    // Remove sequence numbers and timestamps
    const lines = content.split('\n');
    const textLines: string[] = [];

    for (const line of lines) {
      const trimmed = line.trim();
      
      // Skip sequence numbers, timestamps, and empty lines
      if (trimmed === '' || /^\d+$/.test(trimmed) || trimmed.includes('-->')) {
        continue;
      }
      
      textLines.push(trimmed);
    }

    return textLines.join(' ').replace(/\s+/g, ' ').trim();
  }

  private async findTranscriptFile(videoPath: string): Promise<string | null> {
    if (!this.fs) return null;

    const baseName = this.path.basename(videoPath, this.path.extname(videoPath));
    const dirName = this.path.dirname(videoPath);
    const extensions = ['.vtt', '.srt', '.txt'];

    for (const ext of extensions) {
      const transcriptPath = this.path.join(dirName, baseName + ext);
      if (this.fs.existsSync(transcriptPath)) {
        return transcriptPath;
      }
    }

    return null;
  }

  private async getMarkdownFiles(folder: TFolder, recursive: boolean): Promise<TFile[]> {
    const files: TFile[] = [];

    for (const child of folder.children) {
      if (child instanceof TFile && child.extension === 'md') {
        files.push(child);
      } else if (recursive && child instanceof TFolder) {
        const subFiles = await this.getMarkdownFiles(child, true);
        files.push(...subFiles);
      }
    }

    return files;
  }
}
