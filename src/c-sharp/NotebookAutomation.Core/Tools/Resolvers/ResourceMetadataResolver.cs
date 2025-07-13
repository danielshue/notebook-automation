// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System.Text.RegularExpressions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;

namespace NotebookAutomation.Core.Tools.Resolvers;

/// <summary>
/// Specialized metadata resolver for resource files (images, documents, media files) with comprehensive file analysis.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="ResourceMetadataResolver"/> provides metadata extraction for various resource file types,
/// including images, documents, and media files. It extracts file system metadata, media-specific properties,
/// and performs content analysis where applicable.
/// </para>
/// <para>
/// <b>Required Context Parameters:</b>
/// <list type="bullet">
/// <item><description><c>filePath</c> (string): Absolute path to the resource file</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Optional Context Parameters:</b>
/// <list type="bullet">
/// <item><description><c>extractImageMetadata</c> (bool): Whether to extract image dimensions and format (default: true)</description></item>
/// <item><description><c>extractFileContent</c> (bool): Whether to analyze file content for metadata (default: true)</description></item>
/// <item><description><c>resourceType</c> (string): Override automatic resource type detection</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Extracted Metadata Fields:</b>
/// <list type="bullet">
/// <item><description><c>file-name</c>: Name of the file without path</description></item>
/// <item><description><c>file-extension</c>: File extension</description></item>
/// <item><description><c>file-size</c>: File size in bytes</description></item>
/// <item><description><c>date-created</c>: File creation date</description></item>
/// <item><description><c>date-modified</c>: File modification date</description></item>
/// <item><description><c>resource-type</c>: Type of resource (image, document, media, etc.)</description></item>
/// <item><description><c>mime-type</c>: MIME type of the file</description></item>
/// <item><description><c>image-width</c>: Width in pixels (images only)</description></item>
/// <item><description><c>image-height</c>: Height in pixels (images only)</description></item>
/// <item><description><c>image-format</c>: Image format (images only)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b> This class is thread-safe for concurrent read operations.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var resolver = new ResourceMetadataResolver(logger);
/// var context = new Dictionary&lt;string, object&gt; { ["filePath"] = "/path/to/image.jpg" };
/// var metadata = resolver.ExtractMetadata(context);
/// var imageWidth = metadata.ContainsKey("image-width") ? metadata["image-width"] : null;
/// </code>
/// </example>
public class ResourceMetadataResolver : IFileTypeMetadataResolver
{
    private readonly ILogger<ResourceMetadataResolver> _logger;
    
    // File type mappings
    private static readonly Dictionary<string, string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        { ".jpg", "image/jpeg" }, { ".jpeg", "image/jpeg" }, { ".png", "image/png" },
        { ".gif", "image/gif" }, { ".bmp", "image/bmp" }, { ".tiff", "image/tiff" },
        { ".svg", "image/svg+xml" }, { ".webp", "image/webp" }
    };

    private static readonly Dictionary<string, string> DocumentExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        { ".pdf", "application/pdf" }, { ".doc", "application/msword" },
        { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
        { ".xls", "application/vnd.ms-excel" }, { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
        { ".ppt", "application/vnd.ms-powerpoint" }, { ".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation" },
        { ".txt", "text/plain" }, { ".rtf", "application/rtf" }
    };

    private static readonly Dictionary<string, string> MediaExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        { ".mp4", "video/mp4" }, { ".avi", "video/x-msvideo" }, { ".mkv", "video/x-matroska" },
        { ".mov", "video/quicktime" }, { ".wmv", "video/x-ms-wmv" }, { ".flv", "video/x-flv" },
        { ".mp3", "audio/mpeg" }, { ".wav", "audio/wav" }, { ".flac", "audio/flac" },
        { ".aac", "audio/aac" }, { ".ogg", "audio/ogg" }, { ".wma", "audio/x-ms-wma" }
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourceMetadataResolver"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for diagnostic and error information.</param>
    public ResourceMetadataResolver(ILogger<ResourceMetadataResolver> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the file type this resolver handles.
    /// </summary>
    public string FileType => "resource";

    /// <summary>
    /// Determines whether this resolver can resolve the specified field name given the provided context.
    /// </summary>
    /// <param name="fieldName">The field name to check for resolution capability.</param>
    /// <param name="context">Optional context containing file path and resource data.</param>
    /// <returns>True if this resolver can resolve the field; otherwise, false.</returns>
    /// <remarks>
    /// This resolver can handle standard resource metadata fields including file properties,
    /// image dimensions, and resource type classification. Requires filePath in the context.
    /// </remarks>
    public bool CanResolve(string fieldName, Dictionary<string, object>? context = null)
    {
        if (context == null || !context.ContainsKey("filePath"))
            return false;

        // Standard resource metadata fields this resolver can handle
        var supportedFields = new HashSet<string>
        {
            "file-name", "file-extension", "file-size", "date-created", "date-modified",
            "resource-type", "mime-type", "image-width", "image-height", "image-format"
        };

        return supportedFields.Contains(fieldName);
    }

    /// <summary>
    /// Resolves the value for a specific field using resource file analysis.
    /// </summary>
    /// <param name="fieldName">The field name to resolve.</param>
    /// <param name="context">Context containing file path and resolution parameters.</param>
    /// <returns>The resolved field value or null if not found.</returns>
    /// <remarks>
    /// This method analyzes the resource file to extract the requested metadata field.
    /// For image files, it can extract dimensions and format information. For all files,
    /// it extracts basic file system metadata.
    /// </remarks>
    public object? Resolve(string fieldName, Dictionary<string, object>? context = null)
    {
        if (!CanResolve(fieldName, context))
            return null;

        var filePath = context!["filePath"] as string;
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            return null;

        try
        {
            var fileInfo = new FileInfo(filePath);
            
            return fieldName switch
            {
                "file-name" => fileInfo.Name,
                "file-extension" => fileInfo.Extension,
                "file-size" => fileInfo.Length,
                "date-created" => fileInfo.CreationTimeUtc.ToString("yyyy-MM-dd"),
                "date-modified" => fileInfo.LastWriteTimeUtc.ToString("yyyy-MM-dd"),
                "resource-type" => GetResourceType(fileInfo.Extension),
                "mime-type" => GetMimeType(fileInfo.Extension),
                "image-width" => GetImageDimensions(filePath)?.Width,
                "image-height" => GetImageDimensions(filePath)?.Height,
                "image-format" => GetImageFormat(filePath),
                _ => null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving field '{FieldName}' for resource file '{FilePath}'", fieldName, filePath);
            return null;
        }
    }

    /// <summary>
    /// Extracts comprehensive metadata from the resource file.
    /// </summary>
    /// <param name="context">Context containing file path and any additional parameters needed for metadata extraction.</param>
    /// <returns>A dictionary containing extracted metadata key-value pairs.</returns>
    /// <remarks>
    /// <para>
    /// This method performs a complete metadata extraction for the resource file,
    /// including file system properties, resource type classification, and type-specific
    /// metadata (e.g., image dimensions for image files).
    /// </para>
    /// <para>
    /// The extraction process adapts based on the file type, providing relevant metadata
    /// for each resource type while maintaining consistent field naming conventions.
    /// </para>
    /// </remarks>
    public Dictionary<string, object> ExtractMetadata(Dictionary<string, object>? context = null)
    {
        var metadata = new Dictionary<string, object>();

        if (context == null || !context.ContainsKey("filePath"))
            return metadata;

        var filePath = context["filePath"] as string;
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            return metadata;

        try
        {
            var fileInfo = new FileInfo(filePath);
            
            // Basic file metadata
            metadata["file-name"] = fileInfo.Name;
            metadata["file-extension"] = fileInfo.Extension;
            metadata["file-size"] = fileInfo.Length;
            metadata["date-created"] = fileInfo.CreationTimeUtc.ToString("yyyy-MM-dd");
            metadata["date-modified"] = fileInfo.LastWriteTimeUtc.ToString("yyyy-MM-dd");
            
            // Resource type and MIME type
            var resourceType = GetResourceType(fileInfo.Extension);
            metadata["resource-type"] = resourceType;
            metadata["mime-type"] = GetMimeType(fileInfo.Extension);
            
            // Type-specific metadata
            if (resourceType == "image")
            {
                var extractImageMetadata = !context.ContainsKey("extractImageMetadata") || 
                                         (context["extractImageMetadata"] is bool extract && extract);
                
                if (extractImageMetadata)
                {
                    var dimensions = GetImageDimensions(filePath);
                    if (dimensions.HasValue)
                    {
                        metadata["image-width"] = dimensions.Value.Width;
                        metadata["image-height"] = dimensions.Value.Height;
                    }
                    
                    var format = GetImageFormat(filePath);
                    if (format != null)
                    {
                        metadata["image-format"] = format;
                    }
                }
            }
            
            _logger.LogDebug("Extracted {Count} metadata fields from resource file '{FilePath}'", 
                           metadata.Count, filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting metadata from resource file '{FilePath}'", filePath);
        }

        return metadata;
    }

    /// <summary>
    /// Determines the resource type based on file extension.
    /// </summary>
    private string GetResourceType(string extension)
    {
        if (ImageExtensions.ContainsKey(extension))
            return "image";
        if (DocumentExtensions.ContainsKey(extension))
            return "document";
        if (MediaExtensions.ContainsKey(extension))
            return "media";
        
        return "unknown";
    }

    /// <summary>
    /// Gets the MIME type for the file extension.
    /// </summary>
    private string GetMimeType(string extension)
    {
        if (ImageExtensions.TryGetValue(extension, out var imageMime))
            return imageMime;
        if (DocumentExtensions.TryGetValue(extension, out var docMime))
            return docMime;
        if (MediaExtensions.TryGetValue(extension, out var mediaMime))
            return mediaMime;
        
        return "application/octet-stream";
    }

    /// <summary>
    /// Gets image dimensions if the file is an image.
    /// </summary>
    private (int Width, int Height)? GetImageDimensions(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        if (!ImageExtensions.ContainsKey(extension))
            return null;

        // Skip SVG files as they don't have fixed dimensions
        if (extension.Equals(".svg", StringComparison.OrdinalIgnoreCase))
            return null;

        try
        {
            using var image = SixLabors.ImageSharp.Image.Load(filePath);
            return (image.Width, image.Height);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read image dimensions from '{FilePath}'", filePath);
            return null;
        }
    }

    /// <summary>
    /// Gets the image format if the file is an image.
    /// </summary>
    private string? GetImageFormat(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        if (!ImageExtensions.ContainsKey(extension))
            return null;

        try
        {
            using var image = SixLabors.ImageSharp.Image.Load(filePath);
            return image.Metadata.DecodedImageFormat?.Name;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read image format from '{FilePath}'", filePath);
            return null;
        }
    }
}