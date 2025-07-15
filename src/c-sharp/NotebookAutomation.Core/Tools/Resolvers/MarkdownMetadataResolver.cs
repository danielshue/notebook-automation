// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System.Text.RegularExpressions;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace NotebookAutomation.Core.Tools.Resolvers;

/// <summary>
/// Specialized metadata resolver for Markdown files with YAML frontmatter extraction and content analysis.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="MarkdownMetadataResolver"/> provides comprehensive metadata extraction for Markdown files,
/// including YAML frontmatter parsing, content analysis, and structural metadata detection. It supports
/// common Markdown metadata fields such as title, tags, creation date, word count, and heading structure.
/// </para>
/// <para>
/// <b>Required Context Parameters:</b>
/// <list type="bullet">
/// <item><description><c>filePath</c> (string): Absolute path to the Markdown file</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Optional Context Parameters:</b>
/// <list type="bullet">
/// <item><description><c>content</c> (string): Pre-loaded file content to avoid file I/O</description></item>
/// <item><description><c>extractContent</c> (bool): Whether to extract content metadata (default: true)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Extracted Metadata Fields:</b>
/// <list type="bullet">
/// <item><description><c>title</c>: Document title from frontmatter or first heading</description></item>
/// <item><description><c>tags</c>: List of tags from frontmatter</description></item>
/// <item><description><c>date-created</c>: Creation date from frontmatter or file system</description></item>
/// <item><description><c>date-modified</c>: Last modification date from file system</description></item>
/// <item><description><c>word-count</c>: Approximate word count of content</description></item>
/// <item><description><c>heading-count</c>: Number of headings in the document</description></item>
/// <item><description><c>frontmatter</c>: Complete YAML frontmatter as dictionary</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b> This class is thread-safe for concurrent read operations.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var resolver = new MarkdownMetadataResolver(logger);
/// var context = new Dictionary&lt;string, object&gt; { ["filePath"] = "/path/to/file.md" };
/// var metadata = resolver.ExtractMetadata(context);
/// var title = metadata.ContainsKey("title") ? metadata["title"] : null;
/// </code>
/// </example>
public class MarkdownMetadataResolver : IFileTypeMetadataResolver
{
    private readonly ILogger<MarkdownMetadataResolver> _logger;
    private readonly IDeserializer _yamlDeserializer;

    // Regex patterns for markdown parsing
    private static readonly Regex FrontmatterPattern = new(@"^---\s*\n(.*?)\n---\s*\n", RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex HeadingPattern = new(@"^#+\s+(.+)$", RegexOptions.Multiline | RegexOptions.Compiled);
    private static readonly Regex WordCountPattern = new(@"\b\w+\b", RegexOptions.Compiled);

    /// <summary>
    /// Initializes a new instance of the <see cref="MarkdownMetadataResolver"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for diagnostic and error information.</param>
    /// <remarks>
    /// The resolver is initialized with a YAML deserializer configured for kebab-case naming conventions
    /// to support standard Markdown frontmatter formats.
    /// </remarks>
    public MarkdownMetadataResolver(ILogger<MarkdownMetadataResolver> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(HyphenatedNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    /// <summary>
    /// Gets the file type this resolver handles.
    /// </summary>
    public string FileType => "markdown";

    /// <summary>
    /// Determines whether this resolver can resolve the specified field name given the provided context.
    /// </summary>
    /// <param name="fieldName">The field name to check for resolution capability.</param>
    /// <param name="context">Optional context containing file path and content data.</param>
    /// <returns>True if this resolver can resolve the field; otherwise, false.</returns>
    /// <remarks>
    /// This resolver can handle standard markdown metadata fields including title, tags, dates, word-count,
    /// heading-count, and any field present in the YAML frontmatter. Requires either filePath or content
    /// in the context.
    /// </remarks>
    public bool CanResolve(string fieldName, Dictionary<string, object>? context = null)
    {
        if (context == null)
            return false;

        // Check if we have either file path or content
        bool hasSource = context.ContainsKey("filePath") || context.ContainsKey("content");
        if (!hasSource)
            return false;

        // Standard markdown metadata fields this resolver can handle
        var supportedFields = new HashSet<string>
        {
            "title", "tags", "date-created", "date-modified", "word-count",
            "heading-count", "frontmatter", "author", "description", "keywords"
        };

        return supportedFields.Contains(fieldName);
    }

    /// <summary>
    /// Resolves the value for a specific field using markdown content and metadata analysis.
    /// </summary>
    /// <param name="fieldName">The field name to resolve.</param>
    /// <param name="context">Context containing file path, content, and resolution parameters.</param>
    /// <returns>The resolved field value or null if not found.</returns>
    /// <remarks>
    /// <para>
    /// This method extracts the requested field from the markdown file's frontmatter or content.
    /// For performance, it only extracts the specific field requested rather than parsing all metadata.
    /// </para>
    /// <para>
    /// If the field is not found in frontmatter, it attempts to derive it from content analysis
    /// (e.g., title from first heading, word count from content).
    /// </para>
    /// </remarks>
    public object? Resolve(string fieldName, Dictionary<string, object>? context = null)
    {
        if (!CanResolve(fieldName, context))
            return null;

        try
        {
            var content = GetContent(context!);
            if (string.IsNullOrEmpty(content))
                return null;

            // Extract frontmatter
            var frontmatter = ExtractFrontmatter(content);

            // First check if field exists in frontmatter
            if (frontmatter?.ContainsKey(fieldName) == true)
                return frontmatter[fieldName];

            // Handle derived fields
            return fieldName switch
            {
                "title" => GetTitle(content, frontmatter),
                "word-count" => GetWordCount(content),
                "heading-count" => GetHeadingCount(content),
                "date-created" => GetDateCreated(context!),
                "date-modified" => GetDateModified(context!),
                "frontmatter" => frontmatter,
                _ => null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving field '{FieldName}' for markdown file", fieldName);
            return null;
        }
    }

    /// <summary>
    /// Extracts comprehensive metadata from the markdown file.
    /// </summary>
    /// <param name="context">Context containing file path and any additional parameters needed for metadata extraction.</param>
    /// <returns>A dictionary containing extracted metadata key-value pairs.</returns>
    /// <remarks>
    /// <para>
    /// This method performs a complete metadata extraction, including all frontmatter fields
    /// and derived content metadata. It provides a comprehensive view of the markdown file's metadata.
    /// </para>
    /// <para>
    /// The extraction process includes YAML frontmatter parsing, content analysis for word count
    /// and heading structure, and file system metadata for creation and modification dates.
    /// </para>
    /// </remarks>
    public Dictionary<string, object> ExtractMetadata(Dictionary<string, object>? context = null)
    {
        var metadata = new Dictionary<string, object>();

        if (context == null)
            return metadata;

        try
        {
            var content = GetContent(context);
            if (string.IsNullOrEmpty(content))
                return metadata;

            // Extract frontmatter
            var frontmatter = ExtractFrontmatter(content);
            if (frontmatter != null)
            {
                foreach (var kvp in frontmatter)
                {
                    metadata[kvp.Key] = kvp.Value;
                }
            }

            // Add derived metadata
            metadata["word-count"] = GetWordCount(content);
            metadata["heading-count"] = GetHeadingCount(content);

            // Add file system metadata if file path is available
            if (context.ContainsKey("filePath") && context["filePath"] is string filePath)
            {
                if (File.Exists(filePath))
                {
                    var fileInfo = new FileInfo(filePath);
                    metadata["date-created"] = fileInfo.CreationTimeUtc.ToString("yyyy-MM-dd");
                    metadata["date-modified"] = fileInfo.LastWriteTimeUtc.ToString("yyyy-MM-dd");
                    metadata["file-size"] = fileInfo.Length;
                }

                // Ensure title is set
                if (!metadata.ContainsKey("title"))
                {
                    metadata["title"] = GetTitle(content, frontmatter) ?? Path.GetFileNameWithoutExtension(filePath);
                }
            }
            else
            {
                // Ensure title is set even without file path
                if (!metadata.ContainsKey("title"))
                {
                    metadata["title"] = GetTitle(content, frontmatter) ?? "Untitled";
                }
            }

            _logger.LogDebug("Extracted {Count} metadata fields from markdown file", metadata.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting metadata from markdown file");
        }

        return metadata;
    }

    /// <summary>
    /// Gets the content from context, either from pre-loaded content or by reading the file.
    /// </summary>
    private string? GetContent(Dictionary<string, object> context)
    {
        // Check for pre-loaded content first
        if (context.ContainsKey("content") && context["content"] is string content)
            return content;

        // Read from file if path is provided
        if (context.ContainsKey("filePath") && context["filePath"] is string filePath)
        {
            if (File.Exists(filePath))
            {
                return File.ReadAllText(filePath);
            }
        }

        return null;
    }

    /// <summary>
    /// Extracts YAML frontmatter from markdown content.
    /// </summary>
    private Dictionary<string, object>? ExtractFrontmatter(string content)
    {
        var match = FrontmatterPattern.Match(content);
        if (!match.Success)
            return null;

        try
        {
            var yamlContent = match.Groups[1].Value;
            return _yamlDeserializer.Deserialize<Dictionary<string, object>>(yamlContent);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse YAML frontmatter");
            return null;
        }
    }

    /// <summary>
    /// Gets the title from frontmatter or first heading.
    /// </summary>
    private string? GetTitle(string content, Dictionary<string, object>? frontmatter)
    {
        // Check frontmatter first
        if (frontmatter?.ContainsKey("title") == true)
            return frontmatter["title"]?.ToString();

        // Look for first heading
        var headingMatch = HeadingPattern.Match(content);
        if (headingMatch.Success)
            return headingMatch.Groups[1].Value.Trim();

        return null;
    }

    /// <summary>
    /// Counts words in the content (excluding frontmatter).
    /// </summary>
    private int GetWordCount(string content)
    {
        // Remove frontmatter
        var contentWithoutFrontmatter = FrontmatterPattern.Replace(content, "");

        // Count words using regex
        var matches = WordCountPattern.Matches(contentWithoutFrontmatter);
        return matches.Count;
    }

    /// <summary>
    /// Counts headings in the content.
    /// </summary>
    private int GetHeadingCount(string content)
    {
        var matches = HeadingPattern.Matches(content);
        return matches.Count;
    }

    /// <summary>
    /// Gets creation date from file system or context.
    /// </summary>
    private string? GetDateCreated(Dictionary<string, object> context)
    {
        if (context.ContainsKey("filePath") && context["filePath"] is string filePath)
        {
            if (File.Exists(filePath))
            {
                var fileInfo = new FileInfo(filePath);
                return fileInfo.CreationTimeUtc.ToString("yyyy-MM-dd");
            }
        }
        return null;
    }

    /// <summary>
    /// Gets modification date from file system or context.
    /// </summary>
    private string? GetDateModified(Dictionary<string, object> context)
    {
        if (context.ContainsKey("filePath") && context["filePath"] is string filePath)
        {
            if (File.Exists(filePath))
            {
                var fileInfo = new FileInfo(filePath);
                return fileInfo.LastWriteTimeUtc.ToString("yyyy-MM-dd");
            }
        }
        return null;
    }
}
