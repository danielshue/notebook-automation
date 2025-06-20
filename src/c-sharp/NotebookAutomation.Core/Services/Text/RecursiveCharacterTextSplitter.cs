// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Services.Text;

/// <summary>
/// A sophisticated text splitter that recursively splits text based on a hierarchy of separators,
/// preserving semantic boundaries and ensuring better context maintenance between chunks.
/// </summary>
/// <remarks>
/// <para>
/// This implementation is inspired by the RecursiveCharacterTextSplitter concept from LangChain
/// but adapted specifically for C# and the NotebookAutomation project. The splitter works by:
/// <list type="bullet">
/// <item><description>Splitting on the strongest separators first (e.g., triple newlines, headers)</description></item>
/// <item><description>Progressively moving to weaker separators (e.g., single spaces) if necessary</description></item>
/// <item><description>Preserving special patterns like markdown headers, code blocks, and lists</description></item>
/// </list>
/// </para>
/// <para>
/// The splitter is optimized for handling markdown and code content, ensuring semantic boundaries
/// are maintained while splitting text into manageable chunks.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var splitter = new RecursiveCharacterTextSplitter(logger);
/// var chunks = splitter.SplitText("This is a sample text with multiple paragraphs.");
/// foreach (var chunk in chunks)
/// {
///     Console.WriteLine(chunk);
/// }
/// </code>
/// </example>
public partial class RecursiveCharacterTextSplitter
{
    private readonly ILogger logger;
    private readonly int chunkSize;
    private readonly int chunkOverlap;
    private readonly List<string> separators;
    private readonly bool keepSeparator;
    private readonly List<(Regex Pattern, int Priority)> specialPatterns;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecursiveCharacterTextSplitter"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic information.</param>
    /// <param name="chunkSize">Maximum size of each chunk in estimated tokens.</param>
    /// <param name="chunkOverlap">Number of tokens to overlap between chunks.</param>
    /// <param name="separators">Optional list of separators to use for splitting, in order of priority.</param>
    /// <param name="keepSeparator">Whether to keep the separator with the chunk.</param>
    /// <remarks>
    /// <para>
    /// This constructor initializes the splitter with default or custom settings for chunk size,
    /// overlap, and separator hierarchy. It validates input parameters and ensures the chunk overlap
    /// is smaller than the chunk size.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var splitter = new RecursiveCharacterTextSplitter(logger, chunkSize: 3000, chunkOverlap: 500);
    /// </code>
    /// </example>
    public RecursiveCharacterTextSplitter(
        ILogger logger,
        int chunkSize = 3000,
        int chunkOverlap = 500,
        List<string>? separators = null,
        bool keepSeparator = true)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.chunkSize = chunkSize > 0 ? chunkSize : throw new ArgumentException("Chunk size must be positive", nameof(chunkSize));
        this.chunkOverlap = chunkOverlap >= 0 ? chunkOverlap : throw new ArgumentException("Chunk overlap must be non-negative", nameof(chunkOverlap));

        if (this.chunkOverlap >= this.chunkSize)
        {
            throw new ArgumentException("Chunk overlap must be smaller than chunk size", nameof(chunkOverlap));
        }

        // Default separator hierarchy, from strongest to weakest
        this.separators = separators ??
        [
            "\n\n\n",   // Triple line break (major section)
            "\n\n",     // Double line break (paragraph)
            "\n",       // Single line break
            ". ",       // End of sentence
            "! ",       // End of exclamatory sentence
            "? ",       // End of question
            ";",        // Semi-colon
            ",",        // Comma
            " ",        // Space (word boundary)
            string.Empty // Character level (last resort)
        ];

        this.keepSeparator = keepSeparator;

        // Initialize special patterns for markdown and code content
        specialPatterns =
        [

            // Markdown headers (keep them as separate chunks if possible)
            (MarkdownHeaderRegex(), 10),

            // Code blocks (try to keep complete code blocks together)
            (CodeBlockRegex(), 20),

            // Bulleted lists (try to keep list items together)
            (BulletedListRegex(), 15),

            // Numbered lists (try to keep list items together)
            (NumberedListRegex(), 15)
        ];
    }

    /// <summary>
    /// Creates a recursive text splitter optimized for markdown content.
    /// </summary>
    /// <param name="logger">Logger for diagnostic information.</param>
    /// <param name="chunkSize">Maximum size of each chunk in estimated tokens.</param>
    /// <param name="chunkOverlap">Number of tokens to overlap between chunks.</param>
    /// <returns>A RecursiveCharacterTextSplitter configured for markdown content.</returns>
    /// <remarks>
    /// <para>
    /// This factory method creates a splitter with a separator hierarchy optimized for markdown content,
    /// including headers, paragraph breaks, and other markdown-specific patterns.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var markdownSplitter = RecursiveCharacterTextSplitter.CreateForMarkdown(logger);
    /// </code>
    /// </example>
    public static RecursiveCharacterTextSplitter CreateForMarkdown(
        ILogger logger,
        int chunkSize = 3000,
        int chunkOverlap = 500)
    {
        var separators = new List<string>
        {
            "\n## ",    // Heading level 2
            "\n### ",   // Heading level 3
            "\n#### ",  // Heading level 4
            "\n##### ", // Heading level 5
            "\n###### ", // Heading level 6
            "\n\n",     // Paragraph break
            "\n",       // Line break
            " ",        // Word separator
            string.Empty,          // Character level
        };

        return new RecursiveCharacterTextSplitter(logger, chunkSize, chunkOverlap, separators);
    }

    /// <summary>
    /// Creates a recursive text splitter optimized for code content.
    /// </summary>
    /// <param name="logger">Logger for diagnostic information.</param>
    /// <param name="chunkSize">Maximum size of each chunk in estimated tokens.</param>
    /// <param name="chunkOverlap">Number of tokens to overlap between chunks.</param>
    /// <returns>A RecursiveCharacterTextSplitter configured for code content.</returns>
    public static RecursiveCharacterTextSplitter CreateForCode(
        ILogger logger,
        int chunkSize = 3000,
        int chunkOverlap = 500)
    {
        // For code, we want to split preferentially at class and function boundaries
        var separators = new List<string>
        {
            "\n\n\n",   // Triple line break (major section)
            "\n\n",     // Double line break (possible function/class separator)
            "\n",       // Line break
            ";",        // Statement end in many languages
            "{",        // Beginning of block
            "}",        // End of block
            " ",        // Space
            string.Empty,          // Character level
        };

        return new RecursiveCharacterTextSplitter(logger, chunkSize, chunkOverlap, separators);
    }

    /// <summary>
    /// Splits the text into chunks recursively respecting the defined separators.
    /// </summary>
    /// <param name="text">The text to split.</param>
    /// <returns>A list of text chunks that respect the configured size constraints.</returns>
    public List<string> SplitText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            logger.LogWarning("Empty text provided to SplitText");
            return [];
        }

        if (EstimateTokenCount(text) <= chunkSize)
        {
            logger.LogDebug(
                "Text fits in a single chunk ({TextLength} chars, ~{EstimatedTokens} tokens)",
                text.Length, EstimateTokenCount(text));
            return [text];
        }

        logger.LogInformation(
            "Splitting text of {TextLength} chars into chunks (max tokens: {MaxTokens}, overlap: {OverlapTokens})",
            text.Length, chunkSize, chunkOverlap);

        // First try to split based on special patterns
        var specialChunks = TrySplitBySpecialPatterns(text);
        if (specialChunks.Count > 0)
        {
            logger.LogDebug("Successfully split text using special patterns into {Count} initial segments", specialChunks.Count);
            return MergeOrSplitToFinalChunks(specialChunks);
        }

        // If no special patterns matched or the text is still too large,
        // proceed with recursive splitting
        return SplitTextRecursive(text);
    }

    /// <summary>
    /// Tries to split the text based on special patterns like markdown headers and code blocks.
    /// </summary>
    /// <param name="text">The text to split.</param>
    /// <returns>A list of chunks or an empty list if no special patterns were applicable.</returns>
    private List<string> TrySplitBySpecialPatterns(string text)
    {
        var result = new List<string>();

        // Sort patterns by priority (higher numbers first)
        var sortedPatterns = specialPatterns.OrderByDescending(p => p.Priority).ToList();

        foreach (var (pattern, _) in sortedPatterns)
        {
            var matches = pattern.Matches(text);
            if (matches.Count == 0)
            {
                continue;
            }

            logger.LogDebug(
                "Found {MatchCount} matches for pattern: {Pattern}",
                matches.Count, pattern.ToString());

            // Extract the matched segments and the text between them
            int lastEnd = 0;
            foreach (Match match in matches)
            {
                // Add text before this match if it exists
                if (match.Index > lastEnd)
                {
                    var between = text[lastEnd..match.Index];
                    if (!string.IsNullOrWhiteSpace(between))
                    {
                        result.Add(between);
                    }
                }

                // Add the matched text
                result.Add(match.Value);
                lastEnd = match.Index + match.Length;
            }

            // Add any remaining text after the last match
            if (lastEnd < text.Length)
            {
                var remaining = text[lastEnd..];
                if (!string.IsNullOrWhiteSpace(remaining))
                {
                    result.Add(remaining);
                }
            }

            // If we found matches for this pattern, return the result
            if (result.Count > 0)
            {
                return result;
            }
        }

        // No special patterns matched
        return result;
    }

    /// <summary>
    /// Recursively splits the text using the configured separators.
    /// </summary>
    /// <param name="text">The text to split.</param>
    /// <returns>A list of text chunks that respect the size constraints.</returns>
    private List<string> SplitTextRecursive(string text)
    {
        var finalChunks = new List<string>();

        // Try each separator in order
        foreach (var separator in separators)
        {
            logger.LogDebug(
                "Attempting to split with separator: '{Separator}'",
                separator.Replace("\n", "\\n"));

            // Skip empty separator unless it's our last resort
            if (string.IsNullOrEmpty(separator) && separator != separators.Last())
            {
                continue;
            }

            // Split on this separator
            var splits = string.IsNullOrEmpty(separator)
                ? SplitByCharacters(text, chunkSize)
                : SplitBySeparator(text, separator);

            // If we have multiple splits, process them
            if (splits.Count > 1)
            {
                logger.LogDebug(
                    "Split into {Count} segments using separator: '{Separator}'",
                    splits.Count, separator.Replace("\n", "\\n"));

                // Process each split
                foreach (var split in splits)
                {
                    // If this split is still too long, recursively split it
                    // using the next separator in the list
                    if (EstimateTokenCount(split) > chunkSize)
                    {
                        // Try the next separator level
                        var nextIndex = separators.IndexOf(separator) + 1;
                        if (nextIndex < separators.Count)
                        {
                            var subSeparators = separators.GetRange(nextIndex, separators.Count - nextIndex);
                            var subSplitter = new RecursiveCharacterTextSplitter(
                                logger, chunkSize, chunkOverlap, subSeparators, keepSeparator);
                            finalChunks.AddRange(subSplitter.SplitText(split));
                        }
                        else
                        {
                            // We're at the last separator, just add it even if it's too long
                            finalChunks.Add(split);
                        }
                    }
                    else
                    {
                        finalChunks.Add(split);
                    }
                }

                // We successfully split with this separator
                break;
            }
        }

        // If we couldn't split further, just return the original text
        if (finalChunks.Count == 0 && !string.IsNullOrEmpty(text))
        {
            finalChunks.Add(text);
        }

        // Apply chunk overlap, ensuring chunks are within size constraints
        return ApplyChunkOverlap(finalChunks);
    }

    /// <summary>
    /// Applies overlap between chunks to maintain context.
    /// </summary>
    /// <param name="chunks">The initial chunks without overlap.</param>
    /// <returns>Chunks with overlap applied.</returns>
    private List<string> ApplyChunkOverlap(List<string> chunks)
    {
        if (chunks.Count <= 1 || chunkOverlap <= 0)
        {
            return chunks;
        }

        var result = new List<string>();

        for (int i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];

            // For all chunks except the first one, add overlap from the previous chunk
            if (i > 0)
            {
                var prevChunk = chunks[i - 1];
                var overlapText = GetOverlapText(prevChunk);
                chunk = overlapText + chunk;
            }

            // For all chunks except the last one, the next chunk will include overlap
            // from this one, so we don't need to modify this chunk further
            result.Add(chunk);
        }

        return result;
    }

    /// <summary>
    /// Gets the overlap text from the end of a chunk.
    /// </summary>
    /// <param name="text">The source text.</param>
    /// <returns>The text to use for overlap.</returns>
    private string GetOverlapText(string text)
    {
        // Simple approach: take characters from the end
        // A more sophisticated approach would consider token boundaries
        int estimatedCharsPerToken = 4; // Rough estimate
        int overlapChars = Math.Min(chunkOverlap * estimatedCharsPerToken, text.Length);

        return text[^overlapChars..];
    }

    /// <summary>
    /// Merges small chunks and splits large chunks to create final chunks that fit
    /// the size constraints.
    /// </summary>
    /// <param name="initialChunks">Initial chunks that may not respect size constraints.</param>
    /// <returns>Final chunks respecting size constraints.</returns>
    private List<string> MergeOrSplitToFinalChunks(List<string> initialChunks)
    {
        var finalChunks = new List<string>();
        var currentChunk = new StringBuilder();
        int currentSize = 0;

        foreach (var chunk in initialChunks)
        {
            var chunkTokens = EstimateTokenCount(chunk);

            // If this chunk alone exceeds the limit, we need to split it further
            if (chunkTokens > chunkSize)
            {
                // First, add any accumulated content
                if (currentSize > 0)
                {
                    finalChunks.Add(currentChunk.ToString());
                    currentChunk.Clear();
                    currentSize = 0;
                }

                // Then recursively split this chunk
                finalChunks.AddRange(SplitTextRecursive(chunk));
            }

            // If adding this chunk would exceed the limit, finalize current chunk and start a new one
            else if (currentSize + chunkTokens > chunkSize)
            {
                finalChunks.Add(currentChunk.ToString());
                currentChunk.Clear();
                currentChunk.Append(chunk);
                currentSize = chunkTokens;
            }

            // Otherwise, add this chunk to the current accumulated chunk
            else
            {
                if (currentSize > 0)
                {
                    currentChunk.Append(' ');
                }

                currentChunk.Append(chunk);
                currentSize += chunkTokens;
            }
        }

        // Add any remaining content
        if (currentSize > 0)
        {
            finalChunks.Add(currentChunk.ToString());
        }

        // Apply chunk overlap
        return ApplyChunkOverlap(finalChunks);
    }

    /// <summary>
    /// Splits text by a specific separator.
    /// </summary>
    /// <param name="text">The text to split.</param>
    /// <param name="separator">The separator to use.</param>
    /// <returns>List of splits.</returns>
    private List<string> SplitBySeparator(string text, string separator)
    {
        var splits = new List<string>();

        // Handle special case for empty separator
        if (string.IsNullOrEmpty(separator))
        {
            splits.Add(text);
            return splits;
        }

        var segments = text.Split([separator], StringSplitOptions.None);

        for (int i = 0; i < segments.Length; i++)
        {
            var segment = segments[i];

            // If we should keep the separator and it's not the last segment
            if (keepSeparator && i < segments.Length - 1)
            {
                segment += separator;
            }

            // Only add non-empty segments
            if (!string.IsNullOrEmpty(segment))
            {
                splits.Add(segment);
            }
        }

        return splits;
    }

    /// <summary>
    /// Splits text into chunks of specified size at the character level.
    /// This is used as a last resort when other separators don't work.
    /// </summary>
    /// <param name="text">The text to split.</param>
    /// <param name="maxTokens">Maximum tokens per chunk.</param>
    /// <returns>List of character-level chunks.</returns>
    private static List<string> SplitByCharacters(string text, int maxTokens)
    {
        var chunks = new List<string>();
        var estimatedCharsPerToken = 4; // Rough estimate
        var charsPerChunk = maxTokens * estimatedCharsPerToken;

        for (int i = 0; i < text.Length; i += charsPerChunk)
        {
            var length = Math.Min(charsPerChunk, text.Length - i);
            chunks.Add(text.Substring(i, length));
        }

        return chunks;
    }

    /// <summary>
    /// Estimates token count in a string using a simple heuristic.
    /// </summary>
    /// <param name="text">Text to estimate token count for.</param>
    /// <returns>Estimated token count.</returns>
    private static int EstimateTokenCount(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        // Split by whitespace to get a rough word count
        string[] words = text.Split(new char[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);

        // Count punctuation and special characters separately as they often become individual tokens
        int punctuationCount = text.Count(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c));

        // Calculate a weighted token count:
        // - Most words become ~1 token
        // - Numbers and short words (<3 chars) are often fractional tokens
        // - Special characters and punctuation often become separate tokens
        int shortWordCount = words.Count(w => w.Length < 3);
        int normalWordCount = words.Length - shortWordCount;

        // Apply weightings to different elements (these weights are approximations)
        double estimatedTokens = (normalWordCount * 1.0) + (shortWordCount * 0.5) + (punctuationCount * 0.5);

        // Apply a safety factor of 1.2 to avoid underestimation
        return (int)(estimatedTokens * 1.2);
    }

    /// <summary>
    /// Matches markdown headers (e.g., "# Header", "## Subheader") with levels 1 to 6.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This regex is used to identify markdown headers in text and split them into separate chunks.
    /// It matches headers with leading hashes followed by a space and text.
    /// </para>
    /// </remarks>
    [GeneratedRegex(@"^\s*(#{1,6})\s+.+$", RegexOptions.Multiline)]
    private static partial Regex MarkdownHeaderRegex();

    /// <summary>
    /// Matches code blocks enclosed in triple backticks (e.g., "```code```"), including multiline blocks.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This regex is used to identify code blocks in markdown or text files and keep them as separate chunks.
    /// It matches content enclosed within triple backticks.
    /// </para>
    /// </remarks>
    [GeneratedRegex(@"```[\s\S]*?```", RegexOptions.Multiline)]
    private static partial Regex CodeBlockRegex();

    /// <summary>
    /// Matches bulleted list items (e.g., "- Item", "* Item", "+ Item") in text.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This regex is used to identify bulleted list items in text and keep them together as separate chunks.
    /// It matches lines starting with a bullet character followed by text.
    /// </para>
    /// </remarks>
    [GeneratedRegex(@"^\s*[-*+]\s+.+$", RegexOptions.Multiline)]
    private static partial Regex BulletedListRegex();

    /// <summary>
    /// Matches numbered list items (e.g., "1. Item", "2. Item") in text.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This regex is used to identify numbered list items in text and keep them together as separate chunks.
    /// It matches lines starting with a number followed by a period and text.
    /// </para>
    /// </remarks>
    [GeneratedRegex(@"^\s*\d+\.\s+.+$", RegexOptions.Multiline)]
    private static partial Regex NumberedListRegex();
}
