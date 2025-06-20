// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System.Text.RegularExpressions;

using NotebookAutomation.Core.Utils;

namespace NotebookAutomation.Core.Tools.Shared;

/// <summary>
/// Abstract base class for document note processors (PDF, video, etc.).
/// </summary>
/// <remarks>
/// The <c>DocumentNoteProcessorBase</c> class provides shared logic for processing
/// document notes, including AI summary generation, markdown creation, tag extraction,
/// hierarchy detection, template enhancement, and logging. It serves as a foundation
/// for specialized processors that handle specific document types, such as PDFs and videos.
/// </remarks>
/// <param name="logger">The logger instance.</param>
/// <param name="aiSummarizer">The IAISummarizer instance for generating AI-powered summaries.</param>
/// <param name="markdownNoteBuilder">The markdown note builder for generating markdown content.</param>
/// <param name="appConfig">The application configuration.</param>
/// <param name="yamlHelper">Optional YAML helper for processing YAML frontmatter.</param>
/// <param name="hierarchyDetector">Optional hierarchy detector for metadata enhancement.</param>
/// <param name="templateManager">Optional template manager for metadata enhancement.</param>
public abstract class DocumentNoteProcessorBase(
    ILogger logger,
    IAISummarizer aiSummarizer,
    MarkdownNoteBuilder markdownNoteBuilder,
    AppConfig appConfig,
    IYamlHelper? yamlHelper = null,
    IMetadataHierarchyDetector? hierarchyDetector = null,
    IMetadataTemplateManager? templateManager = null)
{
    protected readonly ILogger Logger = logger ?? throw new ArgumentNullException(nameof(logger), "Logger must be provided via DI.");
    protected readonly IAISummarizer Summarizer = aiSummarizer ?? throw new ArgumentNullException(nameof(aiSummarizer), "IAISummarizer must be provided via DI.");
    protected readonly MarkdownNoteBuilder Builder = markdownNoteBuilder ?? throw new ArgumentNullException(nameof(markdownNoteBuilder), "MarkdownNoteBuilder must be provided via DI.");
    protected readonly AppConfig AppConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig), "AppConfig must be provided via DI.");
    protected readonly IYamlHelper? YamlHelper = yamlHelper;
    protected readonly IMetadataHierarchyDetector? HierarchyDetector = hierarchyDetector;
    protected readonly IMetadataTemplateManager? TemplateManager = templateManager;
    /// <summary>
    /// Extracts the main text/content and metadata from the document.
    /// </summary>
    /// <param name="filePath">Path to the document file.</param>
    /// <returns>Tuple of extracted text/content and metadata dictionary.</returns>
    public abstract Task<(string Text, Dictionary<string, object> Metadata)> ExtractTextAndMetadataAsync(string filePath);

    /// <summary>
    /// Generates an AI summary for the given text using OpenAI.
    /// </summary>        /// <param name="text">The extracted text/content.</param>
    /// <param name="variables">Optional variables to substitute in the prompt template.</param>
    /// <param name="promptFileName">Optional name of the prompt template file to use.</param>
    /// <returns>The summary text, or a simulated summary if unavailable.</returns>
    public virtual async Task<string> GenerateAiSummaryAsync(string? text, Dictionary<string, string>? variables = null, string? promptFileName = null)
    {
        Logger.LogDebug("Using AISummarizer to generate summary.");

        // Check for null text
        if (text == null)
        {
            Logger.LogWarning("Null text provided to summarizer");
            return "[No content to summarize]";
        }

        // Log content size
        int textSize = text.Length;
        int estimatedTokens = textSize / 4; // Rough estimate: ~4 characters per token
        Logger.LogInformation(
            $"Text to summarize: {textSize:N0} characters (~{estimatedTokens:N0} estimated tokens)");

        // Enhanced debug logging for yaml-frontmatter
        if (variables != null)
        {
            Logger.LogInformation($"Preparing {variables.Count} variables for prompt template");
            foreach (var kvp in variables)
            {
                var preview = kvp.Value?.Length > 50 ? kvp.Value[..50] + "..." : kvp.Value;
                Logger.LogInformation(
                    $"  Variable {kvp.Key}: {kvp.Value?.Length ?? 0} chars - {preview}");
            }

            if (variables.TryGetValue("yamlfrontmatter", out var yamlValue))
            {
                Logger.LogInformation(
                    $"Found yamlfrontmatter ({yamlValue?.Length:N0} chars): {(yamlValue?.Length > 100 ? yamlValue[..100] + "..." : yamlValue ?? "null")}");
            }
            else
            {
                Logger.LogWarning("YAML frontmatter variable not found in variables dictionary!");
            }
        }
        else
        {
            Logger.LogWarning("No variables provided to summarizer, yaml-frontmatter will not be substituted!");
        }

        if (Summarizer == null)
        {
            Logger.LogWarning("AI summarizer not available - returning simulated summary");
            return "[Simulated AI summary]";
        }

        Logger.LogInformation(
            "Sending content to AI service for summarization (prompt: {PromptFile})",
            promptFileName ?? "default");

        var summary = await Summarizer.SummarizeWithVariablesAsync(text, variables, promptFileName).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(summary))
        {
            Logger.LogWarning("AISummarizer returned an empty summary. Using simulated summary.");
            return "[Simulated AI summary]";
        }

        int summaryLength = summary.Length;
        int summaryEstimatedTokens = summaryLength / 4;
        Logger.LogInformation(
            $"Successfully generated AI summary: {summaryLength:N0} characters (~{summaryEstimatedTokens:N0} estimated tokens)");

        return summary;
    }

    /// <summary>
    /// Generates a markdown note from extracted text and metadata with AI tag extraction and metadata enhancement.
    /// </summary>
    /// <param name="bodyText">The extracted text/content (may contain AI-generated frontmatter).</param>
    /// <param name="metadata">Optional metadata for the note.</param>
    /// <param name="noteType">Type of note (e.g., "PDF Note", "Video Note").</param>
    /// <param name="suppressBody">Whether to suppress the body text and only include frontmatter.</param>
    /// <param name="includeNoteTypeTitle">Whether to include the note type as a title in the markdown.</param>
    /// <returns>The generated markdown content.</returns>
    /// <remarks>
    /// This method performs comprehensive document processing including:
    /// <list type="bullet">
    /// <item><description>Extraction of AI-generated tags and frontmatter from body text</description></item>
    /// <item><description>Hierarchy detection and metadata enhancement</description></item>
    /// <item><description>Template-based metadata enrichment</description></item>
    /// <item><description>Date field cleanup and normalization</description></item>
    /// </list>
    /// </remarks>
    public virtual string GenerateMarkdownNote(
        string bodyText,
        Dictionary<string, object>? metadata = null,
        string noteType = "Document Note",
        bool suppressBody = false,
        bool includeNoteTypeTitle = false)
    {
        // Use default metadata if none provided
        metadata ??= [];

        // If YamlHelper is available, extract AI-generated tags and frontmatter
        if (YamlHelper != null)
        {
            // Debug: Log the original content
            string truncatedBody = bodyText.Length > 200 ? bodyText[..200] + "..." : bodyText;
            Logger.LogDebug($"GenerateMarkdownNote called - Original AI content (first 200 chars): {truncatedBody}");

            // Extract any existing frontmatter from the AI content
            string? contentFrontmatter = YamlHelper.ExtractFrontmatter(bodyText);
            Dictionary<string, object?> contentMetadata = [];

            if (!string.IsNullOrWhiteSpace(contentFrontmatter))
            {
                contentMetadata = YamlHelper.ParseYamlToDictionary(contentFrontmatter)
                    .ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value);
                Logger.LogInformation($"Extracted frontmatter from AI content with {contentMetadata.Count} fields");
            }
            else
            {
                Logger.LogInformation("No frontmatter found in AI content");
            }

            // Remove frontmatter from the content body
            bodyText = YamlHelper.RemoveFrontmatter(bodyText);

            // Debug: Log the cleaned content
            string truncatedCleanBody = bodyText.Length > 200 ? bodyText[..200] + "..." : bodyText;
            Logger.LogDebug($"Cleaned content (first 200 chars): {truncatedCleanBody}");

            // Merge metadata: existing metadata takes precedence, but preserve AI tags if they exist
            var mergedMetadata = new Dictionary<string, object>(metadata?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value!) ?? new Dictionary<string, object>());

            // If AI content has tags and existing metadata doesn't, use AI tags
            if (contentMetadata.TryGetValue("tags", out object? value) && !mergedMetadata.ContainsKey("tags"))
            {
                mergedMetadata["tags"] = value!;
                Logger.LogDebug("Using AI-generated tags from content frontmatter");
            }

            // Merge other non-conflicting AI metadata
            foreach (var kvp in contentMetadata)
            {
                if (kvp.Key != "tags" && !mergedMetadata.ContainsKey(kvp.Key))
                {
                    mergedMetadata[kvp.Key] = kvp.Value!;
                }
            }

            metadata = mergedMetadata;
        }

        // Apply hierarchy detection if available and _internal_path is provided
        if (HierarchyDetector != null && metadata.TryGetValue("_internal_path", out var internalPathObj) && internalPathObj is string internalPath)
        {
            try
            {
                Logger.LogDebug($"Applying hierarchy detection for path: {internalPath}");
                var hierarchyInfo = HierarchyDetector.FindHierarchyInfo(internalPath);
                var nullableMetadata = metadata.ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value);

                // Extract document type from noteType (e.g., "PDF Note" -> "pdf", "Video Note" -> "video")
                string documentType = noteType.Split(' ')[0].ToLowerInvariant();
                var updatedMetadata = HierarchyDetector.UpdateMetadataWithHierarchy(nullableMetadata, hierarchyInfo, documentType);
                metadata = updatedMetadata.ToDictionary(kvp => kvp.Key, kvp => kvp.Value ?? new());
                Logger.LogDebug($"Applied hierarchy detection - program: {hierarchyInfo.GetValueOrDefault("program", "")}, course: {hierarchyInfo.GetValueOrDefault("course", "")}, class: {hierarchyInfo.GetValueOrDefault("class", "")}");
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, $"Error applying hierarchy detection for path: {internalPath}");
            }
        }

        // Remove internal path field as it's only used for hierarchy detection
        metadata.Remove("_internal_path");

        // Apply template enhancements if available
        if (TemplateManager != null)
        {
            try
            {
                // Add template metadata (template-type, etc.)
                metadata = TemplateManager.EnhanceMetadataWithTemplate(metadata, noteType);
                Logger.LogDebug($"Enhanced metadata with template fields for note type: {noteType}");
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Error applying template metadata");
            }
        }

        // Remove all date-related fields from metadata
        var dateFieldsToRemove = metadata.Keys
            .Where(k => k.StartsWith("date-") || k.EndsWith("-date"))
            .ToList();

        foreach (var dateField in dateFieldsToRemove)
        {
            metadata.Remove(dateField);
            Logger.LogDebug($"Removed date field {dateField} from metadata");
        }

        // Log final metadata for debugging
        Logger.LogDebug("Final metadata before markdown generation:");
        foreach (var kvp in metadata)
        {
            Logger.LogDebug($"  {kvp.Key}: {kvp.Value}");
        }

        // Generate the final markdown using the original simple logic
        var frontmatter = metadata ?? new Dictionary<string, object> { { "title", $"Untitled {noteType}" } };

        if (suppressBody)
        {
            return Builder.CreateMarkdownWithFrontmatter(frontmatter);
        }

        string markdownBody;

        // Extract and normalize the title for consistency between YAML and first heading
        string normalizedTitle = ExtractAndNormalizeTitle(frontmatter, bodyText, noteType, includeNoteTypeTitle);

        // Update the frontmatter title with the normalized version
        frontmatter["title"] = normalizedTitle;

        // For the title, use the normalized title consistently
        if (includeNoteTypeTitle)
        {
            markdownBody = $"# {normalizedTitle}\n\n{bodyText}";
            Logger?.LogDebug($"Using normalized title for heading: {normalizedTitle}");
        }
        else
        {
            markdownBody = bodyText;
            Logger?.LogDebug("No title added to markdown body");
        }

        string markdownNote = Builder.BuildNote(frontmatter, markdownBody);

        // Ensure all document notes have a consistent "## Notes" section
        return EnsureNotesSection(markdownNote);
    }

    /// <summary>
    /// Ensures that the generated markdown note contains a "## Notes" section at the end.
    /// This section should never be modified and provides a consistent location for user notes.
    /// </summary>
    /// <param name="markdownNote">The markdown note content to process.</param>
    /// <returns>The markdown note with guaranteed "## Notes" section at the end.</returns>
    /// <remarks>
    /// This method ensures consistency across all document types (PDF, Video, etc.) by
    /// guaranteeing that a "## Notes" section is always present for user annotations.
    /// If the section already exists, the content is preserved as-is.
    /// </remarks>
    protected string EnsureNotesSection(string markdownNote)
    {
        const string notesPattern = "## Notes";

        // Check if Notes section already exists
        if (markdownNote.Contains(notesPattern, StringComparison.OrdinalIgnoreCase))
        {
            Logger.LogDebug("Notes section already exists in markdown content");
            return markdownNote;
        }

        // Add Notes section at the end
        string notesSectionToAdd = markdownNote.EndsWith('\n') ? "\n## Notes\n" : "\n\n## Notes\n";
        string result = markdownNote + notesSectionToAdd;

        Logger.LogDebug("Added '## Notes' section to markdown content");
        return result;
    }

    /// <summary>
    /// Converts a OneDrive file path to the equivalent vault path for hierarchy detection.
    /// </summary>
    /// <param name="oneDrivePath">The original OneDrive file path.</param>
    /// <returns>The converted vault path, or the original path if conversion is not possible.</returns>
    protected string ConvertOneDriveToVaultPath(string oneDrivePath)
    {
        try
        {
            // Get the configured OneDrive resources root and vault root
            string onedriveRoot = AppConfig?.Paths?.OnedriveFullpathRoot ?? "";
            string onedriveResourcesPath = AppConfig?.Paths?.OnedriveResourcesBasepath ?? "";
            string vaultRoot = AppConfig?.Paths?.NotebookVaultFullpathRoot ?? "";

            Logger.LogDebug($"ConvertOneDriveToVaultPath - Input: {oneDrivePath}");
            Logger.LogDebug($"ConvertOneDriveToVaultPath - OnedriveRoot: {onedriveRoot}");
            Logger.LogDebug($"ConvertOneDriveToVaultPath - OnedriveResourcesPath: {onedriveResourcesPath}");
            Logger.LogDebug($"ConvertOneDriveToVaultPath - VaultRoot: {vaultRoot}");

            if (string.IsNullOrEmpty(onedriveRoot) || string.IsNullOrEmpty(vaultRoot))
            {
                Logger.LogWarning("OneDrive or vault root not configured. Using original path for hierarchy detection.");
                return oneDrivePath;
            }

            // Build the full OneDrive resources root path
            string fullOnedriveResourcesRoot = Path.Combine(onedriveRoot, onedriveResourcesPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            // Normalize the path separators for comparison
            fullOnedriveResourcesRoot = Path.GetFullPath(fullOnedriveResourcesRoot);
            Logger.LogDebug($"ConvertOneDriveToVaultPath - FullOnedriveResourcesRoot: {fullOnedriveResourcesRoot}");

            // Check if the OneDrive path is under the resources root
            if (oneDrivePath.StartsWith(fullOnedriveResourcesRoot, StringComparison.OrdinalIgnoreCase))
            {
                // Get the relative path from the OneDrive resources root
                string relativePath = Path.GetRelativePath(fullOnedriveResourcesRoot, oneDrivePath);
                Logger.LogDebug($"ConvertOneDriveToVaultPath - RelativePath: {relativePath}");

                // Combine with vault root to get the equivalent vault path
                string vaultPath = Path.Combine(vaultRoot, relativePath);
                Logger.LogDebug($"ConvertOneDriveToVaultPath - Output VaultPath: {vaultPath}");
                return vaultPath;
            }

            // If not under resources root, return the original path
            Logger.LogWarning($"OneDrive path is not under resources root. Using original path for hierarchy detection: {oneDrivePath}");
            return oneDrivePath;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Error converting OneDrive path to vault path: {oneDrivePath}");
            return oneDrivePath;
        }
    }

    /// <summary>
    /// Extracts and normalizes the title from frontmatter and body text to ensure consistency
    /// between the YAML title and the first heading in the document.
    /// Uses FriendlyTitleHelper for consistent title formatting.
    /// </summary>
    /// <param name="frontmatter">The frontmatter dictionary containing metadata.</param>
    /// <param name="bodyText">The body text content that may contain headings.</param>
    /// <param name="noteType">The default note type to use if no title is found.</param>
    /// <param name="includeNoteTypeTitle">Whether to include the note type as a title.</param>
    /// <returns>A normalized, friendly title string.</returns>
    /// <remarks>
    /// This method prioritizes titles in the following order:
    /// 1. First H1 heading found in the body text (if AI-generated)
    /// 2. Existing title from frontmatter (if valid)
    /// 3. Generated friendly title from file name (if available in metadata)
    /// 4. Default note type
    ///
    /// All titles are normalized using FriendlyTitleHelper for consistency.
    /// </remarks>
    protected virtual string ExtractAndNormalizeTitle(
        Dictionary<string, object> frontmatter,
        string bodyText,
        string noteType,
        bool includeNoteTypeTitle)
    {
        Logger.LogDebug("Extracting and normalizing title from frontmatter and body text");        // First, try to extract the first H1 heading from the body text (common in AI-generated content)
        // ExtractFirstHeading now applies FriendlyTitleHelper internally
        string? firstHeading = ExtractFirstHeading(bodyText);
        if (!string.IsNullOrWhiteSpace(firstHeading))
        {
            Logger.LogDebug($"Found and normalized first heading in body: '{firstHeading}'");
            return firstHeading;
        }

        // Second, try to use the existing title from frontmatter
        if (frontmatter.TryGetValue("title", out var titleObj) && titleObj != null)
        {
            string existingTitle = titleObj.ToString() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(existingTitle) &&
                !existingTitle.StartsWith("Untitled", StringComparison.OrdinalIgnoreCase))
            {
                string normalizedTitle = FriendlyTitleHelper.GetFriendlyTitleFromFileName(existingTitle);
                Logger.LogDebug($"Using existing frontmatter title: '{existingTitle}' -> normalized: '{normalizedTitle}'");
                return normalizedTitle;
            }
        }

        // Third, try to generate a friendly title from the file name if available
        if (frontmatter.TryGetValue("source", out var sourceObj) && sourceObj != null)
        {
            string sourcePath = sourceObj.ToString() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(sourcePath))
            {
                string fileName = Path.GetFileNameWithoutExtension(sourcePath);
                string friendlyTitle = FriendlyTitleHelper.GetFriendlyTitleFromFileName(fileName);
                Logger.LogDebug($"Generated friendly title from source file: '{fileName}' -> '{friendlyTitle}'");
                return friendlyTitle;
            }
        }

        // Fourth, try other common metadata fields that might contain a usable title
        string[] titleFields = ["name", "filename", "document_name"];
        foreach (string field in titleFields)
        {
            if (frontmatter.TryGetValue(field, out var fieldObj) && fieldObj != null)
            {
                string fieldValue = fieldObj.ToString() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(fieldValue))
                {
                    string friendlyTitle = FriendlyTitleHelper.GetFriendlyTitleFromFileName(fieldValue);
                    Logger.LogDebug($"Generated friendly title from {field}: '{fieldValue}' -> '{friendlyTitle}'");
                    return friendlyTitle;
                }
            }
        }

        // Last resort: use the note type with friendly formatting
        string normalizedNoteType = FriendlyTitleHelper.GetFriendlyTitleFromFileName(noteType);
        Logger.LogDebug($"Using note type as fallback title: '{noteType}' -> '{normalizedNoteType}'");
        return normalizedNoteType;
    }
    /// <summary>
    /// Extracts the first H1 heading from markdown text and applies friendly title formatting.
    /// </summary>
    /// <param name="markdownText">The markdown text to search.</param>
    /// <returns>The first H1 heading text without the # symbol and with friendly formatting applied, or null if none found.</returns>
    /// <remarks>
    /// This method looks for lines that start with a single # followed by a space,
    /// which indicates an H1 heading in markdown. It returns the heading text without
    /// the markdown syntax and applies FriendlyTitleHelper for consistent formatting.
    /// This is particularly useful for AI-generated content that may contain raw filename-based headings.
    /// </remarks>
    protected static string? ExtractFirstHeading(string markdownText)
    {
        if (string.IsNullOrWhiteSpace(markdownText))
        {
            return null;
        }

        var lines = markdownText.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();

            // Look for H1 headings (# followed by space)
            if (trimmedLine.StartsWith("# ") && trimmedLine.Length > 2)
            {
                string rawHeading = trimmedLine[2..].Trim(); // Remove "# " and any trailing whitespace

                // Apply FriendlyTitleHelper to clean up the heading (especially useful for AI-generated content)
                string friendlyHeading = FriendlyTitleHelper.GetFriendlyTitleFromFileName(rawHeading);

                return friendlyHeading;
            }
        }

        return null;
    }
}
