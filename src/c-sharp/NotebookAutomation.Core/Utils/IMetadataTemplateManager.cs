// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using NotebookAutomation.Core.Tools;

namespace NotebookAutomation.Core.Utils;

/// <summary>
/// Interface for managing loading, parsing, and application of metadata templates using the metadata schema loader.
/// </summary>
/// <remarks>
/// <para>
/// This interface provides schema-driven template management capabilities by integrating with the 
/// <see cref="IMetadataSchemaLoader"/> to provide template validation, field resolution, and enhanced 
/// metadata processing based on the defined schema.
/// </para>
/// <para>
/// The interface supports dynamic field value resolution through registered resolvers, universal field 
/// inheritance, and reserved tag handling as defined in the metadata schema.
/// </para>
/// </remarks>
public interface IMetadataTemplateManager
{
    /// <summary>
    /// Gets the metadata schema loader instance used by this template manager.
    /// </summary>
    /// <remarks>
    /// Provides access to the underlying schema loader for advanced template operations, 
    /// field resolution, and schema validation.
    /// </remarks>
    IMetadataSchemaLoader SchemaLoader { get; }

    /// <summary>
    /// Loads all templates from the metadata schema loader.
    /// </summary>
    /// <remarks>
    /// This method initializes the template manager with templates from the schema loader,
    /// resolving inheritance and field definitions according to the schema.
    /// </remarks>
    void LoadTemplates();

    /// <summary>
    /// Gets a template by its type using the schema loader.
    /// </summary>
    /// <param name="templateType">The type of template to retrieve (e.g., "video-reference").</param>
    /// <returns>A copy of the template dictionary with resolved fields, or null if not found.</returns>
    /// <remarks>
    /// Templates are resolved using the schema loader, including inheritance from base types 
    /// and universal fields. Field values are resolved using registered resolvers if available.
    /// </remarks>
    Dictionary<string, object>? GetTemplate(string templateType);

    /// <summary>
    /// Gets all available template types loaded from the schema loader.
    /// </summary>
    /// <returns>A list of template type names.</returns>
    /// <remarks>
    /// Returns all template types defined in the metadata schema, including those with inheritance.
    /// </remarks>
    List<string> GetTemplateTypes();

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
    Dictionary<string, object>? GetFilledTemplate(string templateType, Dictionary<string, string> values);

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
    Dictionary<string, object> EnhanceMetadataWithTemplate(Dictionary<string, object> metadata, string noteType);

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
    bool ValidateTemplate(string templateType, Dictionary<string, object> metadata);

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
    Dictionary<string, object> ResolveTemplateFields(string templateType, Dictionary<string, object>? context = null);
}
