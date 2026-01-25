// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace NotebookAutomation.Cli.Services.Copilot;

/// <summary>
/// Parses file attachment syntax (@file) from user input and prepares
/// file content for inclusion in Copilot messages.
/// </summary>
public partial class FileAttachmentParser
{
    private readonly ILogger<FileAttachmentParser> logger;

    /// <summary>
    /// Maximum file size to attach (10 MB).
    /// </summary>
    public const long MaxFileSizeBytes = 10 * 1024 * 1024;

    /// <summary>
    /// File extensions that are supported for text content.
    /// </summary>
    private static readonly HashSet<string> SupportedTextExtensions =
    [
        ".md", ".txt", ".json", ".xml", ".yaml", ".yml",
        ".cs", ".py", ".js", ".ts", ".html", ".css",
        ".java", ".c", ".cpp", ".h", ".hpp",
        ".rb", ".go", ".rs", ".swift", ".kt",
        ".sh", ".bash", ".ps1", ".cmd", ".bat",
        ".sql", ".graphql", ".proto",
        ".toml", ".ini", ".cfg", ".conf",
        ".log", ".csv", ".tsv",
        ".dockerfile", ".gitignore", ".editorconfig"
    ];

    /// <summary>
    /// Initializes a new instance of the <see cref="FileAttachmentParser"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public FileAttachmentParser(ILogger<FileAttachmentParser> logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Parse the input and extract file references.
    /// </summary>
    /// <param name="input">The user input containing potential @file references.</param>
    /// <param name="workingDirectory">The current working directory for resolving relative paths.</param>
    /// <returns>Parse result with extracted attachments and cleaned message.</returns>
    public async Task<FileAttachmentParseResult> ParseAsync(
        string input,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return new FileAttachmentParseResult(input, [], []);
        }

        workingDirectory ??= Directory.GetCurrentDirectory();
        var attachments = new List<FileAttachment>();
        var errors = new List<FileAttachmentError>();
        var processedInput = input;

        // Find all @file references
        var matches = FileReferenceRegex().Matches(input);

        foreach (Match match in matches)
        {
            var reference = match.Value;
            var filePath = ExtractFilePath(reference);

            // Resolve the full path
            var fullPath = ResolvePath(filePath, workingDirectory);

            // Validate and process the file
            var result = await ProcessFileAsync(fullPath, reference, cancellationToken);

            if (result.Attachment != null)
            {
                attachments.Add(result.Attachment);

                // Replace the reference with a placeholder if needed
                // Or keep it as-is for context
            }

            if (result.Error != null)
            {
                errors.Add(result.Error);
            }
        }

        logger.LogDebug(
            "Parsed {AttachmentCount} file attachments from input, {ErrorCount} errors",
            attachments.Count,
            errors.Count);

        return new FileAttachmentParseResult(processedInput, attachments, errors);
    }

    /// <summary>
    /// Extract the file path from a reference like @filename or @"path with spaces".
    /// </summary>
    private static string ExtractFilePath(string reference)
    {
        // Remove the @ prefix
        var path = reference[1..];

        // Handle quoted paths
        if (path.StartsWith('"') && path.EndsWith('"'))
        {
            path = path[1..^1];
        }
        else if (path.StartsWith('\'') && path.EndsWith('\''))
        {
            path = path[1..^1];
        }

        return path;
    }

    /// <summary>
    /// Resolve a relative path to an absolute path.
    /// </summary>
    private static string ResolvePath(string filePath, string workingDirectory)
    {
        if (Path.IsPathRooted(filePath))
        {
            return Path.GetFullPath(filePath);
        }

        return Path.GetFullPath(Path.Combine(workingDirectory, filePath));
    }

    /// <summary>
    /// Process a file and create an attachment.
    /// </summary>
    private async Task<(FileAttachment? Attachment, FileAttachmentError? Error)> ProcessFileAsync(
        string fullPath,
        string originalReference,
        CancellationToken cancellationToken)
    {
        try
        {
            // Check if file exists
            if (!File.Exists(fullPath))
            {
                // Check if it's a directory
                if (Directory.Exists(fullPath))
                {
                    return (null, new FileAttachmentError(
                        originalReference,
                        fullPath,
                        FileAttachmentErrorType.IsDirectory,
                        $"'{originalReference}' is a directory, not a file"));
                }

                return (null, new FileAttachmentError(
                    originalReference,
                    fullPath,
                    FileAttachmentErrorType.NotFound,
                    $"File not found: {originalReference}"));
            }

            // Check file size
            var fileInfo = new FileInfo(fullPath);
            if (fileInfo.Length > MaxFileSizeBytes)
            {
                return (null, new FileAttachmentError(
                    originalReference,
                    fullPath,
                    FileAttachmentErrorType.TooLarge,
                    $"File too large ({fileInfo.Length / 1024 / 1024:F1} MB). Maximum size is {MaxFileSizeBytes / 1024 / 1024} MB"));
            }

            // Check if we can read it as text
            var extension = Path.GetExtension(fullPath).ToLowerInvariant();
            var isTextFile = SupportedTextExtensions.Contains(extension) ||
                             string.IsNullOrEmpty(extension);

            if (!isTextFile)
            {
                // For non-text files, we'll create a reference but not include content
                logger.LogWarning(
                    "File {Path} has unsupported extension {Extension}, will be referenced but content not included",
                    fullPath,
                    extension);

                return (new FileAttachment(
                    Path: fullPath,
                    OriginalReference: originalReference,
                    Content: null,
                    ContentType: GetContentType(extension),
                    IsTextContent: false,
                    SizeBytes: fileInfo.Length), null);
            }

            // Read text content
            var content = await File.ReadAllTextAsync(fullPath, cancellationToken);

            logger.LogDebug("Successfully read file {Path} ({Size} bytes)", fullPath, fileInfo.Length);

            return (new FileAttachment(
                Path: fullPath,
                OriginalReference: originalReference,
                Content: content,
                ContentType: GetContentType(extension),
                IsTextContent: true,
                SizeBytes: fileInfo.Length), null);
        }
        catch (UnauthorizedAccessException)
        {
            return (null, new FileAttachmentError(
                originalReference,
                fullPath,
                FileAttachmentErrorType.AccessDenied,
                $"Access denied: {originalReference}"));
        }
        catch (IOException ex)
        {
            return (null, new FileAttachmentError(
                originalReference,
                fullPath,
                FileAttachmentErrorType.ReadError,
                $"Error reading file: {ex.Message}"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error processing file {Path}", fullPath);
            return (null, new FileAttachmentError(
                originalReference,
                fullPath,
                FileAttachmentErrorType.Unknown,
                $"Unexpected error: {ex.Message}"));
        }
    }

    /// <summary>
    /// Get MIME content type for file extension.
    /// </summary>
    private static string GetContentType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".md" => "text/markdown",
            ".txt" => "text/plain",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".yaml" or ".yml" => "text/yaml",
            ".html" => "text/html",
            ".css" => "text/css",
            ".js" => "text/javascript",
            ".ts" => "text/typescript",
            ".cs" => "text/x-csharp",
            ".py" => "text/x-python",
            ".java" => "text/x-java",
            ".csv" => "text/csv",
            ".sql" => "text/x-sql",
            _ => "text/plain"
        };
    }

    /// <summary>
    /// Format file attachments for inclusion in a prompt.
    /// </summary>
    /// <param name="attachments">The file attachments to format.</param>
    /// <returns>Formatted string to include in the prompt.</returns>
    public static string FormatAttachmentsForPrompt(IEnumerable<FileAttachment> attachments)
    {
        var builder = new StringBuilder();

        foreach (var attachment in attachments)
        {
            if (attachment.IsTextContent && attachment.Content != null)
            {
                builder.AppendLine();
                builder.AppendLine($"--- File: {Path.GetFileName(attachment.Path)} ---");
                builder.AppendLine($"Path: {attachment.Path}");
                builder.AppendLine($"Content-Type: {attachment.ContentType}");
                builder.AppendLine("```");
                builder.AppendLine(attachment.Content);
                builder.AppendLine("```");
                builder.AppendLine($"--- End of {Path.GetFileName(attachment.Path)} ---");
            }
            else
            {
                builder.AppendLine();
                builder.AppendLine($"[Referenced file: {attachment.Path} ({attachment.SizeBytes} bytes, {attachment.ContentType})]");
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Regex pattern for file references.
    /// Matches:
    /// - @filename.ext
    /// - @"path with spaces.ext"
    /// - @'path with spaces.ext'
    /// - @./relative/path.ext
    /// - @/absolute/path.ext
    /// </summary>
    [GeneratedRegex(@"@(?:""[^""]+"")|(?:'[^']+')|(?:[\w./\-\\]+(?:\.\w+)?)")]
    private static partial Regex FileReferenceRegex();
}

/// <summary>
/// Result of parsing file attachments from input.
/// </summary>
public record FileAttachmentParseResult(
    string ProcessedInput,
    IReadOnlyList<FileAttachment> Attachments,
    IReadOnlyList<FileAttachmentError> Errors)
{
    /// <summary>
    /// Gets a value indicating whether any attachments were found.
    /// </summary>
    public bool HasAttachments => Attachments.Count > 0;

    /// <summary>
    /// Gets a value indicating whether any errors occurred.
    /// </summary>
    public bool HasErrors => Errors.Count > 0;
}

/// <summary>
/// A file attachment to include with a message.
/// </summary>
public record FileAttachment(
    string Path,
    string OriginalReference,
    string? Content,
    string ContentType,
    bool IsTextContent,
    long SizeBytes);

/// <summary>
/// Error that occurred while processing a file attachment.
/// </summary>
public record FileAttachmentError(
    string OriginalReference,
    string ResolvedPath,
    FileAttachmentErrorType ErrorType,
    string Message);

/// <summary>
/// Types of errors that can occur during file attachment processing.
/// </summary>
public enum FileAttachmentErrorType
{
    /// <summary>File was not found.</summary>
    NotFound,

    /// <summary>Path points to a directory, not a file.</summary>
    IsDirectory,

    /// <summary>File is too large to attach.</summary>
    TooLarge,

    /// <summary>Access to the file was denied.</summary>
    AccessDenied,

    /// <summary>Error reading the file.</summary>
    ReadError,

    /// <summary>Unknown error.</summary>
    Unknown
}
