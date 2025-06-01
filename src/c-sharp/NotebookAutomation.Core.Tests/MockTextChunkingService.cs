// Enable nullable reference types for this file
#nullable enable
using System;
using System.Collections.Generic;

using NotebookAutomation.Core.Services;

namespace NotebookAutomation.Core.Tests
{
    /// <summary>
    /// A mock implementation of ITextChunkingService for testing AISummarizer.
    /// </summary>
    /// <remarks>
    /// This implementation allows tests to control the chunking behavior and verify that
    /// AISummarizer correctly delegates to the chunking service.
    /// </remarks>
    public class MockTextChunkingService : ITextChunkingService
    {
        /// <summary>
        /// Gets or sets the list of chunks to return when SplitTextIntoChunks is called.
        /// </summary>
        public List<string> PredefinedChunks { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the token count to return when EstimateTokenCount is called.
        /// </summary>
        public int TokenCountToReturn { get; set; } = 0;

        /// <summary>
        /// Gets or sets a flag to track if SplitTextIntoChunks was called.
        /// </summary>
        public bool SplitTextWasCalled { get; private set; }

        /// <summary>
        /// Gets or sets a flag to track if EstimateTokenCount was called.
        /// </summary>
        public bool EstimateTokenWasCalled { get; private set; }

        /// <summary>
        /// Gets the input text passed to SplitTextIntoChunks.
        /// </summary>
        public string? LastInputText { get; private set; }

        /// <summary>
        /// Gets the chunk size passed to SplitTextIntoChunks.
        /// </summary>
        public int LastChunkSize { get; private set; }

        /// <summary>
        /// Gets the overlap passed to SplitTextIntoChunks.
        /// </summary>
        public int LastOverlap { get; private set; }

        /// <summary>
        /// Splits text into chunks using predefined chunks if available, otherwise creates
        /// chunks based on the provided chunk size.
        /// </summary>
        /// <param name="text">The input text to chunk.</param>
        /// <param name="chunkSize">The maximum size of each chunk in characters.</param>
        /// <param name="overlap">The overlap between adjacent chunks in characters.</param>
        /// <returns>A list of text chunks.</returns>
        /// <exception cref="ArgumentNullException">Thrown if text is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if chunkSize is less than or equal to zero, or if overlap is negative.</exception>
        public List<string> SplitTextIntoChunks(string text, int chunkSize = 8000, int overlap = 500)
        {
            // Always record the method was called
            SplitTextWasCalled = true;
            LastInputText = text;
            LastChunkSize = chunkSize;
            LastOverlap = overlap;

            // Check for null text
            if (text == null)
                throw new ArgumentNullException(nameof(text), "Text cannot be null");

            // Check for invalid chunk size
            if (chunkSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(chunkSize), "Chunk size must be greater than zero");

            // Check for negative overlap
            if (overlap < 0)
                throw new ArgumentOutOfRangeException(nameof(overlap), "Overlap must be non-negative");

            if (string.IsNullOrEmpty(text))
                return new List<string>();

            if (overlap >= chunkSize)
                throw new ArgumentException("Overlap must be less than chunk size", nameof(overlap));

            // Return predefined chunks if available
            if (PredefinedChunks.Count > 0)
            {
                return PredefinedChunks;
            }

            // Otherwise create a simple chunking
            var result = new List<string>();
            for (int i = 0; i < text.Length; i += chunkSize - overlap)
            {
                if (i + chunkSize > text.Length)
                {
                    result.Add(text.Substring(i));
                }
                else
                {
                    result.Add(text.Substring(i, chunkSize));
                }
            }

            return result;
        }

        /// <summary>
        /// Estimates token count based on the configured value or defaults to 1 token per 4 characters.
        /// </summary>
        /// <param name="text">The text to estimate tokens for.</param>
        /// <returns>The estimated token count.</returns>
        public int EstimateTokenCount(string text)
        {
            EstimateTokenWasCalled = true;

            if (string.IsNullOrWhiteSpace(text))
                return 0;

            return TokenCountToReturn > 0
                ? TokenCountToReturn
                : (int)Math.Ceiling(text.Length / 4.0); // Default behavior
        }
    }
}
