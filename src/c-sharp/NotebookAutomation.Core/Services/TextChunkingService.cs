#nullable enable

namespace NotebookAutomation.Core.Services
{
    /// <summary>
    /// Defines the contract for text chunking operations used in AI summarization.
    /// Provides methods for splitting large texts into manageable chunks with intelligent overlap.
    /// </summary>
    public interface ITextChunkingService
    {
        /// <summary>
        /// Splits text into chunks with overlap for optimal processing.
        /// Uses character-based chunking with intelligent boundary detection.
        /// </summary>
        /// <param name="text">The text to split</param>
        /// <param name="chunkSize">Maximum size of each chunk in characters</param>
        /// <param name="overlap">Number of characters to overlap between chunks</param>
        /// <returns>List of text chunks</returns>
        List<string> SplitTextIntoChunks(string text, int chunkSize, int overlap);

        /// <summary>
        /// Estimates the token count for the given text using a character-based heuristic.
        /// Uses approximately 4 characters per token as a rough estimate for English text.
        /// </summary>
        /// <param name="text">The text to estimate tokens for</param>
        /// <returns>The estimated token count based on character length</returns>
        int EstimateTokenCount(string text);
    }

    /// <summary>
    /// Provides text chunking operations for AI summarization services.
    /// Implements intelligent text splitting with overlap to maintain context continuity.
    /// </summary>
    public class TextChunkingService : ITextChunkingService
    {        /// <summary>
        /// Splits text into chunks with overlap for optimal processing.
        /// Uses character-based chunking with intelligent boundary detection.
        /// </summary>
        /// <param name="text">The text to split</param>
        /// <param name="chunkSize">Maximum size of each chunk in characters</param>
        /// <param name="overlap">Number of characters to overlap between chunks</param>
        /// <returns>List of text chunks</returns>
        /// <exception cref="ArgumentNullException">Thrown when text is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when chunkSize or overlap are invalid</exception>
        /// <exception cref="ArgumentException">Thrown when overlap is greater than or equal to chunkSize</exception>
        public List<string> SplitTextIntoChunks(string text, int chunkSize, int overlap)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));
            
            if (string.IsNullOrEmpty(text))
                return new List<string>();
            
            if (chunkSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(chunkSize), "Chunk size must be positive");
                
            if (overlap < 0)
                throw new ArgumentOutOfRangeException(nameof(overlap), "Overlap cannot be negative");
                
            if (overlap >= chunkSize)
                throw new ArgumentException("Overlap must be less than chunk size", nameof(overlap));

            var chunks = new List<string>();
            int textLength = text.Length;
            int position = 0;

            while (position < textLength)
            {
                int currentChunkSize = Math.Min(chunkSize, textLength - position);
                string chunk = text.Substring(position, currentChunkSize);
                chunks.Add(chunk);

                // Move position forward by chunk size minus overlap
                position += Math.Max(1, chunkSize - overlap);
            }

            return chunks;
        }

        /// <summary>
        /// Estimates the token count for the given text using a character-based heuristic.
        /// Uses approximately 4 characters per token as a rough estimate for English text.
        /// </summary>
        /// <param name="text">The text to estimate tokens for</param>
        /// <returns>
        /// The estimated token count based on character length, or 0 if the text is null or whitespace.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This is a simplified estimation method that provides reasonable approximations for:
        /// </para>
        /// <list type="bullet">
        /// <item><description>English academic text (typical in MBA coursework)</description></item>
        /// <item><description>Mixed alphanumeric content</description></item>
        /// <item><description>Standard punctuation and formatting</description></item>
        /// </list>        /// <para>
        /// The 4:1 character-to-token ratio is a conservative estimate that works well for OpenAI models.
        /// Actual token counts may vary based on text complexity, language, and specific tokenizer implementation.
        /// </para>
        /// </remarks>
        public int EstimateTokenCount(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            // Rough estimate: 1 token per 4 characters for English text (using ceiling to match original behavior)
            return (int)Math.Ceiling(text.Length / 4.0);
        }
    }
}
