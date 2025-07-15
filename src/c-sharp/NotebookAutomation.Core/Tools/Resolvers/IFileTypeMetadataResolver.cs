// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Tools.Resolvers;

/// <summary>
/// Defines the contract for file type-specific metadata resolvers used in the metadata schema integration.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="IFileTypeMetadataResolver"/> interface provides a standardized contract for implementing
/// specialized metadata extraction and resolution logic for different file types (PDF, Video, Markdown, etc.).
/// This interface supports the extensible metadata processing workflow by allowing file type-specific
/// logic to be encapsulated and registered with the <see cref="FieldValueResolverRegistry"/>.
/// </para>
/// <para>
/// <b>Context Requirements:</b> Implementations should document the required context parameters
/// in their XML documentation, including file paths, content data, and any additional metadata
/// needed for proper field value resolution.
/// </para>
/// <para>
/// <b>Thread Safety:</b> Implementations should be thread-safe or document their thread safety requirements.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class PdfMetadataResolver : IFileTypeMetadataResolver
/// {
///     public string FileType => "pdf";
///     
///     public bool CanResolve(string fieldName, Dictionary&lt;string, object&gt;? context = null)
///     {
///         return fieldName == "pdf-page-count" &amp;&amp; context?.ContainsKey("filePath") == true;
///     }
///     
///     public object? Resolve(string fieldName, Dictionary&lt;string, object&gt;? context = null)
///     {
///         // Implementation logic here
///         return null;
///     }
/// }
/// </code>
/// </example>
public interface IFileTypeMetadataResolver : IFieldValueResolver
{
    /// <summary>
    /// Gets the file type this resolver handles (e.g., "pdf", "video", "markdown").
    /// </summary>
    /// <remarks>
    /// This property is used by the registry to categorize and route resolution requests
    /// to the appropriate resolver based on file type.
    /// </remarks>
    string FileType { get; }

    /// <summary>
    /// Determines whether this resolver can resolve the specified field name given the provided context.
    /// </summary>
    /// <param name="fieldName">The field name to check for resolution capability.</param>
    /// <param name="context">Optional context containing file path, content data, and other metadata.</param>
    /// <returns>True if this resolver can resolve the field; otherwise, false.</returns>
    /// <remarks>
    /// <para>
    /// This method allows resolvers to perform capability checks before attempting resolution,
    /// enabling more efficient field resolution routing and better error handling.
    /// </para>
    /// <para>
    /// Implementations should validate that required context parameters are present and
    /// that the field name matches the resolver's capabilities.
    /// </para>
    /// </remarks>
    bool CanResolve(string fieldName, Dictionary<string, object>? context = null);

    /// <summary>
    /// Extracts metadata from the file specified in the context.
    /// </summary>
    /// <param name="context">Context containing file path and any additional parameters needed for metadata extraction.</param>
    /// <returns>A dictionary containing extracted metadata key-value pairs.</returns>
    /// <remarks>
    /// <para>
    /// This method performs comprehensive metadata extraction from the file, returning
    /// all available metadata that can be used for field population. The returned
    /// dictionary should use standardized field names consistent with the metadata schema.
    /// </para>
    /// <para>
    /// <b>Required Context Parameters:</b> Implementations should document required
    /// context parameters in their class documentation.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var context = new Dictionary&lt;string, object&gt; { ["filePath"] = "/path/to/file.pdf" };
    /// var metadata = resolver.ExtractMetadata(context);
    /// </code>
    /// </example>
    Dictionary<string, object> ExtractMetadata(Dictionary<string, object>? context = null);
}
