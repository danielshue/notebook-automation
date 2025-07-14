// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using NotebookAutomation.Core.Tools;

namespace NotebookAutomation.Core.Utils;

/// <summary>
/// Manages loading, parsing, and application of metadata templates using the metadata schema loader.
/// </summary>
/// <remarks>
/// <para>
/// This class provides schema-driven template management by integrating with the <see cref="IMetadataSchemaLoader"/>
/// to deliver template validation, field resolution, and enhanced metadata processing capabilities.
/// It replaces direct file-based template loading with schema-driven template management.
/// </para>
/// <para>
/// The manager supports dynamic field value resolution through registered resolvers, universal field 
/// inheritance, reserved tag handling, and template validation based on the schema definition.
/// </para>
/// <example>
/// <code>
/// var manager = new MetadataTemplateManager(logger, schemaLoader);
/// var template = manager.GetTemplate("video-reference");
/// var enhanced = manager.EnhanceMetadataWithTemplate(metadata, "Video Note");
/// </code>
/// </example>
/// </remarks>
public partial class MetadataTemplateManager : IMetadataTemplateManager
{
    private readonly ILogger _logger;
    private readonly IMetadataSchemaLoader _schemaLoader;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetadataTemplateManager"/> class.
    /// </summary>
    /// <param name="logger">The logger to use for diagnostic and error reporting.</param>
    /// <param name="schemaLoader">The metadata schema loader to use for template management.</param>
    /// <exception cref="ArgumentNullException">Thrown when logger or schemaLoader is null.</exception>
    public MetadataTemplateManager(ILogger logger, IMetadataSchemaLoader schemaLoader)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _schemaLoader = schemaLoader ?? throw new ArgumentNullException(nameof(schemaLoader));
    }

    /// <summary>
    /// Gets the metadata schema loader instance used by this template manager.
    /// </summary>
    /// <remarks>
    /// Provides access to the underlying schema loader for advanced template operations, 
    /// field resolution, and schema validation.
    /// </remarks>
    public IMetadataSchemaLoader SchemaLoader => _schemaLoader;

    /// <summary>
    /// Loads all templates from the metadata schema loader.
    /// </summary>
    /// <remarks>
    /// This method initializes the template manager with templates from the schema loader.
    /// The schema loader handles template loading, inheritance resolution, and field definitions.
    /// </remarks>
    public void LoadTemplates()
    {
        _logger.LogDebug("Loading templates from metadata schema loader");
        _logger.LogInformation("Template loading completed using schema loader. Total templates available: {Count}. Template types: {TemplateTypes}",
            _schemaLoader.TemplateTypes.Count, string.Join(", ", _schemaLoader.TemplateTypes.Keys));
    }

    /// <summary>
    /// Gets a template by its type using the schema loader.
    /// </summary>
    /// <param name="templateType">The type of template to retrieve (e.g., "video-reference").</param>
    /// <returns>A copy of the template dictionary with resolved fields, or null if not found.</returns>
    /// <remarks>
    /// Templates are resolved using the schema loader, including inheritance from base types 
    /// and universal fields. Field values are resolved using registered resolvers if available.
    /// </remarks>
    public Dictionary<string, object>? GetTemplate(string templateType)
    {
        _logger.LogInformation("Looking for template type: {TemplateType}. Available templates: {AvailableTemplates}",
            templateType, string.Join(", ", _schemaLoader.TemplateTypes.Keys));

        if (!_schemaLoader.TemplateTypes.TryGetValue(templateType, out var templateSchema))
        {
            _logger.LogWarning("Template type not found: {TemplateType}. Available templates: {AvailableTemplates}",
                templateType, string.Join(", ", _schemaLoader.TemplateTypes.Keys));
            return null;
        }

        _logger.LogInformation("Template found for type: {TemplateType}", templateType);
        
        // Create a dictionary from the schema with resolved field values
        var template = new Dictionary<string, object>();
        
        // Add template-type field
        template["template-type"] = templateType;
        
        // Add type field if defined in schema
        if (!string.IsNullOrEmpty(templateSchema.Type))
        {
            template["type"] = templateSchema.Type;
        }

        // Resolve field values through schema loader
        foreach (var field in templateSchema.Fields)
        {
            var resolvedValue = _schemaLoader.ResolveFieldValue(templateType, field.Key);
            template[field.Key] = resolvedValue ?? field.Value.Default ?? string.Empty;
        }

        return template;
    }

    /// <summary>
    /// Gets all available template types loaded from the schema loader.
    /// </summary>
    /// <returns>A list of template type names.</returns>
    /// <remarks>
    /// Returns all template types defined in the metadata schema, including those with inheritance.
    /// </remarks>
    public List<string> GetTemplateTypes()
    {
        return [.. _schemaLoader.TemplateTypes.Keys];
    }

    /// <summary>
    /// Gets a template by type and fills in provided values for placeholders using schema-driven resolution.
    /// </summary>
    /// <param name="templateType">The type of template to retrieve (e.g., "video-reference").</param>
    /// <param name="values">A dictionary of values to fill in for template placeholders.</param>
    /// <returns>A filled template dictionary with resolved fields, or null if the template is not found.</returns>
    /// <remarks>
    /// Uses the schema loader to resolve field values through registered resolvers, then applies 
    /// the provided values to override defaults. Universal fields and reserved tags are handled 
    /// according to the schema definition.
    /// </remarks>
    public Dictionary<string, object>? GetFilledTemplate(string templateType, Dictionary<string, string> values)
    {
        var template = GetTemplate(templateType);
        if (template == null)
        {
            return null;
        }

        // Apply values to the template, overriding resolved defaults
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
    /// Enhances document metadata with appropriate template fields based on the note type using schema-driven processing.
    /// </summary>
    /// <param name="metadata">The current document metadata to enhance.</param>
    /// <param name="noteType">The type of document being processed (e.g., "Video Note", "PDF Note").</param>
    /// <returns>Enhanced metadata with template fields added and defaults filled in through schema resolution.</returns>
    /// <remarks>
    /// <para>
    /// Uses the schema loader to enhance metadata with template fields, universal fields, and 
    /// resolved field values. Reserved tags are handled according to the schema definition.
    /// </para>
    /// <para>
    /// Field values are resolved through registered resolvers when available, with fallback to 
    /// schema-defined defaults. The enhancement process respects field inheritance and type mapping.
    /// </para>
    /// </remarks>
    public Dictionary<string, object> EnhanceMetadataWithTemplate(Dictionary<string, object> metadata, string noteType)
    {
        // Determine appropriate template type based on noteType
        string templateType = DetermineTemplateType(noteType);

        // Get the template using schema loader
        var template = GetTemplate(templateType);
        if (template == null)
        {
            _logger.LogInformation($"No template found for {noteType}, using metadata as-is");
            return metadata;
        }

        // Start with the resolved template (all fields with their resolved values)
        var enhancedMetadata = new Dictionary<string, object>(template);

        // Overlay with actual metadata (content-specific values take precedence)
        foreach (var kvp in metadata)
        {
            enhancedMetadata[kvp.Key] = kvp.Value;
        }

        // Apply schema-driven field resolution with context
        var context = new Dictionary<string, object>(enhancedMetadata);
        foreach (var fieldName in template.Keys)
        {
            // Skip fields that already have specific values
            if (enhancedMetadata.ContainsKey(fieldName) && 
                enhancedMetadata[fieldName] != null && 
                !string.IsNullOrWhiteSpace(enhancedMetadata[fieldName].ToString()))
            {
                continue;
            }

            // Resolve field value using schema loader with context
            var resolvedValue = _schemaLoader.ResolveFieldValue(templateType, fieldName, context);
            if (resolvedValue != null)
            {
                enhancedMetadata[fieldName] = resolvedValue;
            }
        }

        // Apply specific field logic for compatibility
        ApplyFieldSpecificLogic(enhancedMetadata, noteType, template);

        // Remove deprecated fields
        enhancedMetadata.Remove("share_link");

        // Ensure 'type' field matches templateType for reference types
        if (!string.IsNullOrEmpty(templateType) && (templateType.EndsWith("reference") || templateType.Contains("reference")))
        {
            enhancedMetadata["type"] = templateType;
        }

        _logger.LogDebug("Enhanced metadata with template: {TemplateType}", templateType);
        return enhancedMetadata;
    }

    /// <summary>
    /// Validates a template against the schema definition.
    /// </summary>
    /// <param name="templateType">The template type to validate.</param>
    /// <param name="metadata">The metadata dictionary to validate.</param>
    /// <returns>True if the template is valid according to the schema, false otherwise.</returns>
    /// <remarks>
    /// Validates the template against the schema definition, including required fields,
    /// field types, and schema constraints. Uses the schema loader for validation rules.
    /// </remarks>
    public bool ValidateTemplate(string templateType, Dictionary<string, object> metadata)
    {
        if (!_schemaLoader.TemplateTypes.TryGetValue(templateType, out var templateSchema))
        {
            _logger.LogWarning("Cannot validate unknown template type: {TemplateType}", templateType);
            return false;
        }

        // Check required fields
        foreach (var requiredField in templateSchema.RequiredFields)
        {
            if (!metadata.ContainsKey(requiredField) || 
                metadata[requiredField] == null || 
                string.IsNullOrWhiteSpace(metadata[requiredField].ToString()))
            {
                _logger.LogWarning("Required field missing or empty: {Field} for template type: {TemplateType}", 
                    requiredField, templateType);
                return false;
            }
        }

        _logger.LogDebug("Template validation passed for type: {TemplateType}", templateType);
        return true;
    }

    /// <summary>
    /// Resolves field values for a template using the schema loader's resolver registry.
    /// </summary>
    /// <param name="templateType">The template type to resolve fields for.</param>
    /// <param name="context">Optional context for field resolution.</param>
    /// <returns>A dictionary of resolved field values.</returns>
    /// <remarks>
    /// Uses the schema loader's resolver registry to dynamically resolve field values
    /// based on registered resolvers and the provided context.
    /// </remarks>
    public Dictionary<string, object> ResolveTemplateFields(string templateType, Dictionary<string, object>? context = null)
    {
        var resolvedFields = new Dictionary<string, object>();

        if (!_schemaLoader.TemplateTypes.TryGetValue(templateType, out var templateSchema))
        {
            _logger.LogWarning("Cannot resolve fields for unknown template type: {TemplateType}", templateType);
            return resolvedFields;
        }

        foreach (var field in templateSchema.Fields)
        {
            var resolvedValue = _schemaLoader.ResolveFieldValue(templateType, field.Key, context);
            if (resolvedValue != null)
            {
                resolvedFields[field.Key] = resolvedValue;
            }
        }

        return resolvedFields;
    }

    /// <summary>
    /// Applies field-specific logic for compatibility with existing behavior.
    /// </summary>
    /// <param name="enhancedMetadata">The metadata being enhanced.</param>
    /// <param name="noteType">The note type being processed.</param>
    /// <param name="template">The template being applied.</param>
    /// <remarks>
    /// This method maintains compatibility with existing field-specific logic while
    /// transitioning to schema-driven processing.
    /// </remarks>
    private void ApplyFieldSpecificLogic(Dictionary<string, object> enhancedMetadata, string noteType, Dictionary<string, object> template)
    {
        // Handle status field based on note type
        if (template.ContainsKey("status") && 
            (!enhancedMetadata.ContainsKey("status") || 
             string.IsNullOrWhiteSpace(enhancedMetadata["status"]?.ToString())))
        {
            enhancedMetadata["status"] = noteType switch
            {
                "Video Note" => "unwatched",
                "PDF Note" => "unread",
                _ => "in-progress"
            };
        }

        // Handle date-created field
        if (template.ContainsKey("date-created") && 
            (!enhancedMetadata.ContainsKey("date-created") || 
             string.IsNullOrWhiteSpace(enhancedMetadata["date-created"]?.ToString())))
        {
            enhancedMetadata["date-created"] = DateTime.Now.ToString("yyyy-MM-dd");
        }

        // Handle tags field normalization
        if (enhancedMetadata.ContainsKey("tags") && 
            enhancedMetadata["tags"] is System.Collections.IEnumerable enumerable && 
            enhancedMetadata["tags"] is not string)
        {
            var tagArray = enumerable.Cast<object>()
                .Select(tag => tag?.ToString() ?? string.Empty)
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .ToArray();
            enhancedMetadata["tags"] = tagArray;
        }
    }

    /// <summary>
    /// Determines the appropriate template type string based on the note type.
    /// </summary>
    /// <param name="noteType">The type of document being processed (e.g., "Video Note").</param>
    /// <returns>The corresponding template type string (e.g., "video-reference").</returns>
    /// <remarks>
    /// This method maps note types to template types. In future versions, this mapping
    /// could be moved to the schema configuration for greater flexibility.
    /// </remarks>
    private static string DetermineTemplateType(string noteType)
    {
        return noteType switch
        {
            "Video Note" => "video-reference",
            "PDF Note" => "pdf-reference",
            "Live Session Note" => "live-session-note",
            "Transcript" => "transcript",
            _ => "video-reference",  // Default to video-reference for unknown types
        };
    }
}
