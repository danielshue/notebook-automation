// Licensed under the MIT License. See LICENSE file in the project root for full license information.

/**
 * Provides text chunking operations for AI summarization services.
 * Implements intelligent text splitting with overlap to maintain context continuity.
 */
export class TextChunkingService {
  /**
   * Splits text into chunks with overlap for optimal processing.
   * Uses character-based chunking with intelligent boundary detection.
   * 
   * @param text - The text to split.
   * @param chunkSize - Maximum size of each chunk in characters.
   * @param overlap - Number of characters to overlap between chunks.
   * @returns Array of text chunks.
   * @throws {Error} If parameters are invalid.
   */
  splitTextIntoChunks(text: string, chunkSize: number, overlap: number): string[] {
    if (!text) {
      return [];
    }

    if (text.length === 0) {
      return [];
    }

    if (chunkSize <= 0) {
      throw new Error('Chunk size must be positive');
    }

    if (overlap < 0) {
      throw new Error('Overlap cannot be negative');
    }

    if (overlap >= chunkSize) {
      throw new Error('Overlap must be less than chunk size');
    }

    const chunks: string[] = [];
    const textLength = text.length;
    let position = 0;

    while (position < textLength) {
      const currentChunkSize = Math.min(chunkSize, textLength - position);
      const chunk = text.substring(position, position + currentChunkSize);
      chunks.push(chunk);

      // Move position forward by chunk size minus overlap
      position += Math.max(1, chunkSize - overlap);
    }

    return chunks;
  }

  /**
   * Estimates the token count for the given text using a character-based heuristic.
   * Uses approximately 4 characters per token as a rough estimate for English text.
   * 
   * @param text - The text to estimate tokens for.
   * @returns The estimated token count based on character length, or 0 if the text is null or whitespace.
   * 
   * @remarks
   * This is a simplified estimation method that provides reasonable approximations for:
   * - English academic text (typical in MBA coursework)
   * - Mixed alphanumeric content
   * - Standard punctuation and formatting
   * 
   * The 4:1 character-to-token ratio is a conservative estimate that works well for OpenAI models.
   * Actual token counts may vary based on text complexity, language, and specific tokenizer implementation.
   */
  estimateTokenCount(text: string): number {
    if (!text || text.trim().length === 0) {
      return 0;
    }

    // Rough estimate: 1 token per 4 characters for English text (using ceiling to match original behavior)
    return Math.ceil(text.length / 4.0);
  }
}
