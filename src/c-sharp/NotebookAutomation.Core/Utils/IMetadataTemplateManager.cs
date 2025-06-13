// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Utils;

/// <summary>
/// Interface for managing loading, parsing, and application of metadata templates from the metadata.yaml file.
/// </summary>

public interface IMetadataTemplateManager
{
    /// <summary>
    /// Loads all templates from the metadata.yaml file into memory.
    /// </summary>
    void LoadTemplates();

    /// <summary>
    /// Gets a template by its type.
    /// </summary>
    /// <param name="templateType">The type of template to retrieve (e.g., "video-reference").</param>
    /// <returns>A copy of the template dictionary, or null if not found.</returns>
    Dictionary<string, object>? GetTemplate(string templateType);

    /// <summary>
    /// Gets all available template types loaded from metadata.yaml.
    /// </summary>
    /// <returns>A list of template type names.</returns>
    List<string> GetTemplateTypes();

    /// <summary>
    /// Gets a template by type and fills in provided values for placeholders.
    /// </summary>
    /// <param name="templateType">The type of template to retrieve (e.g., "video-reference").</param>
    /// <param name="values">A dictionary of values to fill in for template placeholders.</param>
    /// <returns>A filled template dictionary, or null if the template is not found.</returns>
    Dictionary<string, object>? GetFilledTemplate(string templateType, Dictionary<string, string> values);

    /// <summary>
    /// Enhances document metadata with appropriate template fields based on the note type.
    /// </summary>
    /// <param name="metadata">The current document metadata to enhance.</param>
    /// <param name="noteType">The type of document being processed (e.g., "Video Note", "PDF Note").</param>
    /// <returns>Enhanced metadata with template fields added and defaults filled in.</returns>
    Dictionary<string, object> EnhanceMetadataWithTemplate(Dictionary<string, object> metadata, string noteType);
}
