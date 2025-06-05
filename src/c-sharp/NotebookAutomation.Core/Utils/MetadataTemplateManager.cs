using NotebookAutomation.Core.Configuration;

namespace NotebookAutomation.Core.Utils;
/// <summary>
/// Manages loading, parsing, and application of metadata templates from the <c>metadata.yaml</c> file.
/// </summary>
/// <remarks>
/// <para>
/// Responsible for loading and parsing the <c>metadata.yaml</c> file, which contains template definitions for various document types used in the notebook vault.
/// Provides methods to retrieve, fill, and apply templates to document metadata.
/// </para>
/// <example>
/// <code>
/// var manager = new MetadataTemplateManager(logger, appConfig, yamlHelper);
/// var template = manager.GetTemplate("video-reference");
/// var filled = manager.GetFilledTemplate("video-reference", new Dictionary&lt;string, string&gt; { ["title"] = "Sample" });
/// </code>
/// </example>
/// </remarks>
public partial class MetadataTemplateManager
{
    private readonly ILogger _logger;
    private readonly string _metadataFilePath;
    private readonly IYamlHelper _yamlHelper;
    private readonly Dictionary<string, Dictionary<string, object>> _templates;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetadataTemplateManager"/> class.
    /// </summary>
    /// <param name="logger">The logger to use for diagnostic and error reporting.</param>
    /// <param name="appConfig">The application configuration.</param>
    /// <param name="yamlHelper">The YAML helper service for parsing metadata.</param>
    public MetadataTemplateManager(ILogger logger, AppConfig appConfig, IYamlHelper yamlHelper)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _yamlHelper = yamlHelper ?? throw new ArgumentNullException(nameof(yamlHelper));

        if (appConfig?.Paths == null)
            throw new ArgumentNullException(nameof(appConfig), "Application config paths must be provided");

        _metadataFilePath = appConfig.Paths.MetadataFile;

        if (string.IsNullOrWhiteSpace(_metadataFilePath))
            throw new ArgumentException("Metadata file path is not configured in appConfig");

        _templates = [];

        LoadTemplates();
    }

    /// <summary>
    /// Loads all templates from the <c>metadata.yaml</c> file into memory.
    /// </summary>
    /// <remarks>
    /// Parses each YAML document in the file and stores templates by their <c>template-type</c> key.
    /// Logs errors and warnings for missing files or parse failures.
    /// </remarks>
    public void LoadTemplates()
    {
        try
        {
            if (!File.Exists(_metadataFilePath))
            {
                _logger.LogErrorWithPath("Metadata file does not exist at path: {filePath}", _metadataFilePath);
                return;
            }

            _logger.LogInformationWithPath("Loading templates from metadata file: {filePath}", _metadataFilePath);
            string content = File.ReadAllText(_metadataFilePath);

            // The metadata.yaml file has multiple YAML documents separated by ---
            var documents = SplitYamlDocuments(content);

            _templates.Clear();

            foreach (var document in documents)
            {
                try
                {
                    var template = _yamlHelper.ParseYamlToDictionary(document);

                    // Normalize tags to string arrays if present
                    if (template.TryGetValue("tags", out object? value) && value is System.Collections.IEnumerable enumerable && value is not string)
                    {
                        var tagArray = enumerable.Cast<object>()
                            .Select(tag => tag?.ToString() ?? string.Empty)
                            .Where(tag => !string.IsNullOrWhiteSpace(tag))
                            .ToArray();
                        template["tags"] = tagArray;
                    }
                    if (template.Count > 0 && template.TryGetValue("template-type", out object? templateTypeValue))
                    {
                        string templateType = templateTypeValue.ToString() ?? string.Empty;
                        if (!string.IsNullOrEmpty(templateType))
                        {
                            _templates[templateType] = template;
                            _logger.LogDebug("Loaded template: {TemplateType}", templateType);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Failed to parse template document: {Error}", ex.Message);
                }
            }

            _logger.LogInformation("Loaded {Count} templates from metadata.yaml", _templates.Count);
        }
        catch (Exception ex)
        {
            _logger.LogErrorWithPath(ex, "Failed to load templates from metadata file: {filePath}", _metadataFilePath);
            throw;
        }
    }

    /// <summary>
    /// Gets a template by its type.
    /// </summary>
    /// <param name="templateType">The type of template to retrieve (e.g., <c>video-reference</c>).</param>
    /// <returns>A copy of the template dictionary, or <c>null</c> if not found.</returns>
    /// <example>
    /// <code>
    /// var template = manager.GetTemplate("pdf-reference");
    /// </code>
    /// </example>
    public Dictionary<string, object>? GetTemplate(string templateType)
    {
        if (_templates.TryGetValue(templateType, out var template))
        {
            return new Dictionary<string, object>(template);
        }

        _logger.LogWarning("Template type not found: {TemplateType}", templateType);
        return null;
    }

    /// <summary>
    /// Gets all available template types loaded from <c>metadata.yaml</c>.
    /// </summary>
    /// <returns>A list of template type names.</returns>
    public List<string> GetTemplateTypes()
    {
        return [.. _templates.Keys];
    }

    /// <summary>
    /// Gets a template by type and fills in provided values for placeholders.
    /// </summary>
    /// <param name="templateType">The type of template to retrieve (e.g., <c>video-reference</c>).</param>
    /// <param name="values">A dictionary of values to fill in for template placeholders.</param>
    /// <returns>A filled template dictionary, or <c>null</c> if the template is not found.</returns>
    /// <example>
    /// <code>
    /// var filled = manager.GetFilledTemplate("video-reference", new Dictionary&lt;string, string&gt; { ["title"] = "Sample" });
    /// </code>
    /// </example>
    public Dictionary<string, object>? GetFilledTemplate(string templateType, Dictionary<string, string> values)
    {
        var template = GetTemplate(templateType);
        if (template == null)
            return null;

        // Apply values to the template
        foreach (var kvp in values)
        {
            if (!string.IsNullOrEmpty(kvp.Value))
            {
                template[kvp.Key] = kvp.Value;
            }
        }

        return template;
    }

    /// <summary>
    /// Enhances document metadata with appropriate template fields based on the note type.
    /// </summary>
    /// <param name="metadata">The current document metadata to enhance.</param>
    /// <param name="noteType">The type of document being processed (e.g., <c>Video Note</c>, <c>PDF Note</c>).</param>
    /// <returns>Enhanced metadata with template fields added and defaults filled in.</returns>
    /// <remarks>
    /// Fields from the template are added if missing or empty in the original metadata. Special handling is applied for certain fields.
    /// </remarks>
    /// <example>
    /// <code>
    /// var enhanced = manager.EnhanceMetadataWithTemplate(metadata, "Video Note");
    /// </code>
    /// </example>
    public Dictionary<string, object> EnhanceMetadataWithTemplate(Dictionary<string, object> metadata, string noteType)
    {
        // Determine appropriate template type based on noteType
        string templateType = DetermineTemplateType(noteType);

        // Get the template if available
        var template = GetTemplate(templateType);
        if (template == null)
        {
            _logger.LogInformation("No template found for {NoteType}, using default fields only", noteType);
            return metadata;  // Return original metadata if no template found
        }

        // Start with a copy of the template (all fields present)
        var enhancedMetadata = new Dictionary<string, object>(template);

        // Overlay with actual metadata (content-specific values take precedence)
        foreach (var kvp in metadata)
        {
            enhancedMetadata[kvp.Key] = kvp.Value;
        }

        // Calculate derived fields and set defaults for option fields
        foreach (var key in template.Keys)
        {
            // If the field is missing or has an empty/null value, fill with calculated or template default
            bool shouldFillFromTemplate = false;

            if (!enhancedMetadata.TryGetValue(key, out object? existingValue))
            {
                shouldFillFromTemplate = true;
            }
            else
            {
                if (existingValue == null)
                {
                    shouldFillFromTemplate = true;
                }
                else if (existingValue is string stringValue && string.IsNullOrWhiteSpace(stringValue))
                {
                    shouldFillFromTemplate = true;
                }
                // For arrays, only fill if null or empty
                else if (existingValue is System.Collections.ICollection collection && collection.Count == 0)
                {
                    shouldFillFromTemplate = true;
                }
            }

            if (shouldFillFromTemplate)
            {
                switch (key)
                {
                    case "video-size":
                        // Since size_bytes field has been removed, set video-size to empty or template default
                        enhancedMetadata["video-size"] = template[key]?.ToString() ?? string.Empty;
                        break;
                    case "status":
                        // Use template default, or set based on note type
                        if (noteType == "Video Note")
                            enhancedMetadata["status"] = template[key]?.ToString() ?? "unwatched";
                        else if (noteType == "PDF Note")
                            enhancedMetadata["status"] = template[key]?.ToString() ?? "unread";
                        else
                            enhancedMetadata["status"] = template[key]?.ToString() ?? "in-progress";
                        break;
                    case "date-created":
                        // Use file creation date if available
                        if (enhancedMetadata.TryGetValue("date-created", out var dateCreatedVal) && !string.IsNullOrWhiteSpace(Convert.ToString(dateCreatedVal)))
                            enhancedMetadata["date-created"] = Convert.ToString(dateCreatedVal) ?? string.Empty;
                        else
                            enhancedMetadata["date-created"] = DateTime.Now.ToString("yyyy-MM-dd");
                        break;
                    default:
                        // Preserve the original value for complex types (arrays, objects)
                        // For string types, ensure no null assignment
                        var templateValue = template[key];
                        if (templateValue is string stringValue)
                        {
                            enhancedMetadata[key] = stringValue ?? string.Empty;
                        }
                        else if (templateValue != null)
                        {
                            // Special handling for tags - convert to string array
                            if (key == "tags" && templateValue is System.Collections.IEnumerable enumerable && templateValue is not string)
                            {
                                var tagArray = enumerable.Cast<object>()
                                    .Select(tag => tag?.ToString() ?? string.Empty)
                                    .Where(tag => !string.IsNullOrWhiteSpace(tag))
                                    .ToArray();
                                enhancedMetadata[key] = tagArray;
                            }
                            else
                            {
                                // Preserve other complex types as-is
                                enhancedMetadata[key] = templateValue;
                            }
                        }
                        else
                        {
                            enhancedMetadata[key] = string.Empty;
                        }
                        break;
                }
            }
        }

        // Remove share_link from metadata as it's no longer needed
        enhancedMetadata.Remove("share_link");

        // Ensure 'type' field matches templateType if required by tests
        // Only for known reference types (video-reference, pdf-reference, etc.)
        if (!string.IsNullOrEmpty(templateType) && (templateType.EndsWith("reference") || templateType.Contains("reference")))
        {
            enhancedMetadata["type"] = templateType;
        }

        // DEBUG: Log before returning to verify 'type' field
        _logger.LogDebug("[DEBUG] Final enhancedMetadata before return in EnhanceMetadataWithTemplate:");
        foreach (var kvp in enhancedMetadata)
        {
            _logger.LogDebug("[DEBUG]   {Key}: {Value}", kvp.Key, kvp.Value);
        }

        _logger.LogDebug("Enhanced metadata with template: {TemplateType}", templateType); return enhancedMetadata;
    }

    /// <summary>
    /// Determines the appropriate template type string based on the note type.
    /// </summary>
    /// <param name="noteType">The type of document being processed (e.g., <c>Video Note</c>).</param>
    /// <returns>The corresponding template type string (e.g., <c>video-reference</c>).</returns>
    private static string DetermineTemplateType(string noteType)
    {
        return noteType switch
        {
            "Video Note" => "video-reference",
            "PDF Note" => "pdf-reference",
            "Live Session Note" => "live-session-note",
            "Transcript" => "transcript",
            _ => "video-reference"  // Default to video-reference for unknown types
        };
    }

    /// <summary>
    /// Splits a YAML file with multiple documents separated by <c>---</c> into individual document strings.
    /// </summary>
    /// <param name="content">The full YAML file content.</param>
    /// <returns>An array of individual YAML document strings.</returns>
    /// <remarks>
    /// Uses a regular expression to split only at line beginnings for document separators.
    /// </remarks>
    private static string[] SplitYamlDocuments(string content)
    {
        // Split on --- but only at line beginnings (not in the middle of a line)
        // This regex matches the YAML document separator pattern (--- at the start of a line)
        var regex = YamlDocumentSeparatorRegex();

        // First, split the content into segments
        var segments = regex.Split(content);

        // Filter out empty segments and trim each segment
        return [.. segments
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())];
    }

    /// <summary>
    /// Gets a compiled regex that matches YAML document separators (lines containing only '---').
    /// </summary>
    /// <remarks>
    /// Used to split a YAML file into individual documents by matching lines that consist solely of '---',
    /// which is the standard YAML document separator.
    /// </remarks>
    /// <returns>A compiled <see cref="Regex"/> for splitting YAML documents.</returns>
    [GeneratedRegex(@"^---\s*$", RegexOptions.Multiline)]
    private static partial Regex YamlDocumentSeparatorRegex();
}
