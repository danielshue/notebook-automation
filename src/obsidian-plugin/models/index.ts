// Licensed under the MIT License. See LICENSE file in the project root for full license information.

/**
 * Core data models and interfaces for the Notebook Automation plugin services.
 */

// ============================================================================
// Tag Service Models
// ============================================================================

/**
 * Result of a tag operation.
 */
export interface TagOperationResult {
  /** Whether the operation completed successfully. */
  success: boolean;
  /** Human-readable summary of the operation. */
  message: string;
  /** Number of files processed. */
  filesProcessed: number;
  /** Number of files modified. */
  filesModified: number;
  /** Number of tags added. */
  tagsAdded: number;
  /** Number of files with errors. */
  filesWithErrors: number;
  /** Whether this was a dry run (no actual changes made). */
  dryRun: boolean;
  /** Error message if the operation failed. */
  errorMessage?: string;
  /** List of files that had errors, with error messages. */
  errorFiles?: string[];
}

/**
 * Result of YAML frontmatter diagnosis.
 */
export interface YamlDiagnosisResult {
  /** Whether the scan completed successfully. */
  success: boolean;
  /** Human-readable summary. */
  message: string;
  /** Number of files scanned. */
  filesScanned: number;
  /** Number of files with YAML issues. */
  filesWithIssues: number;
  /** List of issues found. */
  issues?: YamlIssue[];
}

/**
 * A YAML frontmatter issue found during diagnosis.
 */
export interface YamlIssue {
  /** Path to the file with the issue. */
  filePath: string;
  /** Line number where the issue was found (if available). */
  lineNumber?: number;
  /** Description of the issue. */
  description: string;
  /** Suggested fix for the issue. */
  suggestedFix?: string;
}

// ============================================================================
// PDF Service Models
// ============================================================================

/**
 * Result of a PDF conversion operation.
 */
export interface PdfOperationResult {
  /** Whether the operation completed successfully. */
  success: boolean;
  /** Human-readable summary of the operation. */
  message: string;
  /** Number of PDF files found. */
  filesFound: number;
  /** Number of notes successfully created. */
  notesCreated: number;
  /** Number of files that failed to process. */
  failed: number;
  /** Whether this was a dry run. */
  dryRun: boolean;
  /** Total processing time in milliseconds. */
  processingTime: number;
  /** Total tokens used for AI summaries. */
  totalTokens: number;
  /** Error message if operation failed. */
  errorMessage?: string;
}

// ============================================================================
// Video Service Models
// ============================================================================

/**
 * Result of a video note creation operation.
 */
export interface VideoOperationResult {
  /** Whether the operation completed successfully. */
  success: boolean;
  /** Human-readable summary of the operation. */
  message: string;
  /** Number of video files found. */
  filesFound: number;
  /** Number of notes successfully created. */
  notesCreated: number;
  /** Number of files that failed to process. */
  failed: number;
  /** Whether this was a dry run. */
  dryRun: boolean;
  /** Total processing time in milliseconds. */
  processingTime: number;
  /** Total tokens used for AI summaries. */
  totalTokens: number;
  /** Error message if operation failed. */
  errorMessage?: string;
}

/**
 * Result of a video transcript consolidation operation.
 */
export interface VideoConsolidationResult {
  /** Whether the consolidation completed successfully. */
  success: boolean;
  /** Human-readable summary of the consolidation. */
  message: string;
  /** Path to the output consolidated note. */
  outputPath?: string;
  /** Number of transcripts aggregated into the consolidated note. */
  transcriptsAggregated: number;
  /** Number of videos skipped (no transcript found). */
  skipped: number;
  /** Whether the consolidated note was written (false if dry run or unchanged). */
  wasWritten: boolean;
  /** Whether this was a dry run. */
  dryRun: boolean;
  /** Error message if operation failed. */
  errorMessage?: string;
}

// ============================================================================
// Vault Service Models
// ============================================================================

/**
 * Represents a file or folder in the vault.
 */
export interface VaultItem {
  /** Path to the item relative to vault root. */
  path: string;
  /** Name of the item. */
  name: string;
  /** Whether this is a folder. */
  isFolder: boolean;
  /** Size in bytes (for files). */
  size?: number;
  /** Last modified timestamp. */
  modified?: number;
}

/**
 * Represents a search result in the vault.
 */
export interface SearchResult {
  /** Path to the file. */
  path: string;
  /** Matching lines or content. */
  matches: SearchMatch[];
  /** Score/relevance of the result. */
  score?: number;
}

/**
 * A specific match within a file.
 */
export interface SearchMatch {
  /** Line number (0-indexed). */
  line: number;
  /** The matching text or context. */
  text: string;
  /** Start position in the line. */
  start?: number;
  /** End position in the line. */
  end?: number;
}
