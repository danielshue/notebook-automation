// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Tools.PdfProcessing;

/// <summary>
/// Provides functionality for extracting text and metadata from PDF files and generating markdown notes.
/// </summary>
/// <remarks>
/// <para>
/// This class integrates with the AI summarizer and YAML helper to process PDF files and generate
/// markdown notes. It supports:
/// <list type="bullet">
/// <item><description>Text extraction from PDF pages</description></item>
/// <item><description>Image extraction from PDF pages with markdown references</description></item>
/// <item><description>Metadata extraction (e.g., title, author, keywords, page count, image count)</description></item>
/// <item><description>Course structure detection (module and lesson information)</description></item>
/// <item><description>Markdown note generation with YAML frontmatter</description></item>
/// </list>
/// </para>
/// <para>
/// The class logs detailed diagnostic information during processing and handles errors gracefully.
/// Images are extracted to a subdirectory named "{pdf_filename}_images" (with spaces replaced by underscores) and displayed inline
/// within the extracted text using markdown image notation ![ImageName](filename.ext), creating
/// a natural flow where images appear in roughly the same order as they occur on each page.
/// The extracted text with image references is also saved as "{pdf_filename}.txt"
/// in the same directory as the PDF file for use by downstream AI processing.
/// </para>
/// </remarks>
/// This processor focuses on extracting text and images and defers standard metadata fields (title, hierarchy,
/// share-link, page-count, date-created, status, etc.) to the centralized metadata pipeline and its resolvers.
/// It provides both <c>filePath</c> and <c>_internal_path</c> context keys for resolver robustness.
/// </remarks>
/// <remarks>
/// <para>
/// <b>Image extraction:</b> controlled via <c>_extractImages</c> flag; when enabled, extracted images are saved
/// alongside the generated markdown and referenced using relative paths.
/// </para>
/// var processor = new PdfNoteProcessor(logger, aiSummarizer);
/// var (text, metadata) = await processor.ExtractTextAndMetadataAsync("example.pdf");
/// Console.WriteLine(text);
/// Console.WriteLine(metadata);
/// </code>
/// </example>
/// <param name="logger">Logger for diagnostics.</param>
/// <param name="aiSummarizer">The AISummarizer service for generating AI-powered summaries.</param>
/// <param name="yamlHelper">The YAML helper for processing YAML frontmatter.</param>
/// <param name="hierarchyDetector">The metadata hierarchy detector for extracting metadata from directory structure.</param>
/// <param name="templateManager">The metadata template manager for handling metadata templates.</param>
/// <param name="oneDriveService">Optional service for generating OneDrive share links.</param>
/// <param name="appConfig">Optional application configuration for advanced hierarchy detection.</param>
public class PdfNoteProcessor : DocumentNoteProcessorBase
{
    private readonly IOneDriveService? _oneDriveService;
    private readonly AppConfig? _appConfig;
    private readonly ICourseStructureExtractor _courseStructureExtractor;
    private readonly bool _extractImages;
    /// <summary>
    /// Initializes a new instance of the <see cref="PdfNoteProcessor"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for logging diagnostic and error information.</param>
    /// <param name="aiSummarizer">The AI summarizer service for generating summaries.</param>
    /// <param name="yamlHelper">The YAML helper for processing YAML frontmatter in markdown documents.</param>
    /// <param name="hierarchyDetector">The metadata hierarchy detector for extracting metadata from directory structure.</param>
    /// <param name="templateManager">The metadata template manager for handling metadata templates.</param>
    /// <param name="courseStructureExtractor">The course structure extractor for extracting module and lesson information.</param>
    /// <param name="oneDriveService">Optional service for generating OneDrive share links.</param>
    /// <param name="appConfig">Optional application configuration for metadata management.</param>
    /// <param name="extractImages">Whether to extract images from the PDF. Defaults to false.</param>
    /// <param name="resolverRegistry">Optional field value resolver registry for dynamic field resolution.</param>
    /// <remarks>
    /// This constructor initializes the PDF note processor with optional services for metadata management
    /// and hierarchical detection.
    /// </remarks>
    public PdfNoteProcessor(
        ILogger<PdfNoteProcessor> logger,
        IAISummarizer aiSummarizer,
        IYamlHelper yamlHelper,
        IMetadataHierarchyDetector hierarchyDetector,
        IMetadataTemplateManager templateManager,
        ICourseStructureExtractor courseStructureExtractor,
        MarkdownNoteBuilder markdownNoteBuilder,
        IOneDriveService? oneDriveService = null,
        AppConfig? appConfig = null,
        bool extractImages = false,
        FieldValueResolverRegistry? resolverRegistry = null) : base(logger, aiSummarizer, markdownNoteBuilder, appConfig ?? new AppConfig(), yamlHelper, hierarchyDetector, templateManager, resolverRegistry)
    {
        _oneDriveService = oneDriveService;
        _appConfig = appConfig;
        _courseStructureExtractor = courseStructureExtractor ?? throw new ArgumentNullException(nameof(courseStructureExtractor));
        _extractImages = extractImages;
    }

    /// <summary>
    /// Extracts text and metadata from a PDF file.
    /// </summary>
    /// <param name="pdfPath">Path to the PDF file.</param>
    /// <returns>Tuple of extracted text and metadata dictionary.</returns>
    /// <remarks>
    /// <para>
    /// This method reads the PDF file, extracts text from its pages, and collects metadata such as:
    /// <list type="bullet">
    /// <item><description>Page count</description></item>
    /// <item><description>Title, author, subject, and keywords</description></item>
    /// <item><description>File size and creation date</description></item>
    /// <item><description>Course structure information (module and lesson)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The extracted text and metadata are returned as a tuple. If the file does not exist or an error occurs,
    /// the method logs the issue and returns empty results.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var (text, metadata) = await processor.ExtractTextAndMetadataAsync("example.pdf");
    /// Console.WriteLine(text);
    /// Console.WriteLine(metadata);
    /// </code>
    /// </example>
    public override async Task<(string Text, Dictionary<string, object> Metadata)> ExtractTextAndMetadataAsync(string pdfPath)
    {
        var metadata = new Dictionary<string, object?>();
        if (!File.Exists(pdfPath))
        {
            Logger.LogError($"PDF file not found: {pdfPath}");
            return (string.Empty, metadata.ToDictionary(kvp => kvp.Key, kvp => kvp.Value!));
        }

        string extractedText = string.Empty;
        try
        {
            Logger.LogDebug($"Starting PDF content extraction: {pdfPath}");
            extractedText = await Task.Run(() =>
            {
                var sb = new StringBuilder();
                using (PdfDocument document = PdfDocument.Open(pdfPath))
                {
                    Logger.LogDebug($"Opened PDF document with {document.NumberOfPages} pages: {pdfPath}");
                    sb.AppendLine();

                    int pageCount = 0;
                    foreach (Page page in document.GetPages())
                    {
                        pageCount++;
                        if (pageCount % 10 == 0 || pageCount == 1 || pageCount == document.NumberOfPages)
                        {
                            Logger.LogDebug($"Extracting text from page {pageCount}/{document.NumberOfPages} for {pdfPath}");
                        }

                        // Extract text and images interleaved from page
                        ExtractPageContentWithImages(page, pageCount, pdfPath, sb);
                    }

                    // Count total valid images across all pages (only if image extraction is enabled)
                    int totalImages = 0;
                    if (_extractImages)
                    {
                        try
                        {
                            foreach (Page page in document.GetPages())
                            {
                                var validImages = page.GetImages().Where(IsValidImage).Count();
                                totalImages += validImages;
                            }
                            // Image count can be useful for downstream prompts; include as an optional field
                            metadata["image_count"] = totalImages;
                            Logger.LogDebug($"PDF contains {totalImages} valid images across {document.NumberOfPages} pages");
                        }
                        catch (Exception ex)
                        {
                            Logger.LogWarning(ex, $"Failed to count images in PDF: {pdfPath}");
                            metadata["image_count"] = 0;
                        }
                    }
                    else
                    {
                        metadata["image_count"] = 0;
                        Logger.LogDebug($"Image extraction disabled, setting image count to 0");
                    }

                    // "generated" field removed as requested
                    // Title will be derived by TitleResolver based on file name; ignore PDF info title
                    // Defer other PDF properties to resolvers/schema where applicable
                }

                // Extract module and lesson information
                Logger.LogDebug($"Extracting course structure information from file path {pdfPath}");
                _courseStructureExtractor.ExtractModuleAndLesson(pdfPath, metadata);

                // Defer hierarchy and file property enrichment to the centralized pipeline and resolvers

                return sb.ToString();
            }).ConfigureAwait(false); int extractedCharCount = extractedText.Length;
            Logger.LogDebug($"Extracted {extractedCharCount:N0} characters of text from PDF: {pdfPath}");

            // Do not generate or set OneDrive shared link here; let the schema-driven resolver populate it
            // to avoid precedence conflicts and duplicate network calls.

            // Save extracted text with image references next to the PDF file
            try
            {
                string pdfDirectory = Path.GetDirectoryName(pdfPath) ?? string.Empty;
                string pdfFileName = Path.GetFileNameWithoutExtension(pdfPath);
                string textFilePath = Path.Combine(pdfDirectory, $"{pdfFileName}.txt");
                string markdownFilePath = Path.Combine(pdfDirectory, $"{pdfFileName}.md");

                await File.WriteAllTextAsync(textFilePath, extractedText).ConfigureAwait(false);
                Logger.LogDebug($"Saved extracted text to: {textFilePath}");

                // Also save as markdown file
                await File.WriteAllTextAsync(markdownFilePath, extractedText).ConfigureAwait(false);
                Logger.LogDebug($"Saved extracted text as markdown to: {markdownFilePath}");

                // Add the text file path to metadata for reference
                metadata["extracted_text_file"] = textFilePath;
                metadata["extracted_markdown_file"] = markdownFilePath;
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, $"Failed to save extracted text file for PDF: {pdfPath}");
            }

            // Remove any unwanted fields
            metadata.Remove("aliases"); metadata.Remove("pdf-link");
            metadata.Remove("permalink");
            metadata.Remove("yaml-frontmatter"); // Prevent duplication

            // Provide internal path hint and absolute file path for pipeline/resolvers
            metadata["_internal_path"] = pdfPath;
            metadata["filePath"] = pdfPath;

            return (extractedText, metadata.ToDictionary(kvp => kvp.Key, kvp => kvp.Value!));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Failed to extract text from PDF: {pdfPath}");
            return (string.Empty, metadata.ToDictionary(kvp => kvp.Key, kvp => kvp.Value!));
        }
    }

    // BuildYamlFrontmatter method removed; metadata composition is handled by the centralized pipeline

    /// <summary>
    /// Generates a markdown note from extracted PDF text and metadata.
    /// </summary>
    /// <param name="pdfText">The extracted PDF text.</param>
    /// <param name="metadata">Optional metadata for the note.</param>
    /// <returns>The generated markdown content.</returns>
    public string GenerateMarkdownNote(string pdfText, Dictionary<string, object>? metadata = null)
    {
        // Use base implementation for consistent formatting, include the title from metadata
        return GenerateMarkdownNote(pdfText, metadata, "PDF Note", includeNoteTypeTitle: true);
    }

    /// <summary>
    /// Generates an AI summary for the PDF content with proper variable substitution.
    /// </summary>
    /// <param name="pdfText">The extracted PDF text.</param>
    /// <param name="metadata">The PDF metadata dictionary.</param>
    /// <param name="promptFileName">Optional prompt template file name.</param>
    /// <returns>The AI-generated summary text.</returns>
    public async Task<string> GeneratePdfSummaryAsync(string pdfText, Dictionary<string, object> metadata, string? promptFileName = null)
    {
        // Create variables dictionary for the AI summarizer
        var variables = new Dictionary<string, string>();
        string effectivePrompt = promptFileName ?? "final_summary_prompt";

        Logger.LogDebug($"Preparing variables for AI summarization: {effectivePrompt}");

        // Track character counts for detailed progress reporting
        int textLength = pdfText?.Length ?? 0;
        int estimatedTokens = textLength / 4; // Rough estimate: 4 chars per token
        Logger.LogDebug(
            $"PDF content to summarize: {textLength:N0} characters (~{estimatedTokens:N0} estimated tokens)",
            effectivePrompt);

        // Add title if available
        if (metadata.TryGetValue("title", out var titleObj) && titleObj != null)
        {
            variables["title"] = titleObj.ToString() ?? "Untitled PDF";
            Logger.LogDebug($"Added title to variables: {variables["title"]} effectivePrompt:{effectivePrompt}");
        }

        // Avoid passing yamlfrontmatter; rely on the metadata pipeline and template manager

        // Make a copy to avoid modifying the original metadata
        _ = new Dictionary<string, object>(metadata);

        Logger.LogDebug(
            $"Starting AI summarization process with prompt template: {effectivePrompt}");
        Logger.LogDebug(
            $"AI summary generation beginning - this may take some time for large documents: {effectivePrompt}");

        // Use the summarizer directly
        string? result = null;
        try
        {
            if (Summarizer != null)
            {
                Logger.LogDebug($"Sending content to AI summarizer: {effectivePrompt}");
                result = await Summarizer.SummarizeWithVariablesAsync(
                    pdfText ?? string.Empty,
                    variables,
                    effectivePrompt).ConfigureAwait(false);
            }
            else
            {
                Logger.LogDebug($"AI summarizer service not available: {effectivePrompt}");
                result = "[Simulated AI summary - summarizer service unavailable]";
            }

            // Log the result statistics
            int summaryLength = result?.Length ?? 0;
            int compressionRatio = textLength > 0 ? (int)(100 - ((double)summaryLength / textLength * 100)) : 0;
            Logger.LogDebug($"AI summary generation complete: {summaryLength:N0} characters ({compressionRatio}% reduction): {effectivePrompt}");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error generating AI summary for PDF: {EffectivePrompt}", effectivePrompt);
            result = "[Error generating AI summary]";
        }
        return result ?? string.Empty;
    }

    /// <summary>
    /// Extracts text and images from a PDF page in the order they appear, creating an interleaved content flow with inline image display.
    /// </summary>
    /// <param name="page">The PDF page to extract content from.</param>
    /// <param name="pageNumber">The current page number.</param>
    /// <param name="pdfPath">The path to the PDF file.</param>
    /// <param name="contentBuilder">The StringBuilder to append content to.</param>
    private void ExtractPageContentWithImages(Page page, int pageNumber, string pdfPath, StringBuilder contentBuilder)
    {
        try
        {
            // Extract text content
            string pageText = page.Text;

            // If image extraction is disabled, just add the text
            if (!_extractImages)
            {
                contentBuilder.AppendLine(pageText);
                return;
            }

            // Get images from the page and filter out invalid ones
            var allImages = page.GetImages().ToList();
            var images = allImages.Where(IsValidImage).ToList();

            Logger.LogDebug($"Found {allImages.Count} total images on page {pageNumber}, {images.Count} are valid");

            if (!images.Any())
            {
                // No valid images, just add the text
                contentBuilder.AppendLine(pageText);
                return;
            }
            // Create directory for images if it doesn't exist
            string pdfDirectory = Path.GetDirectoryName(pdfPath) ?? string.Empty;
            string pdfFileName = Path.GetFileNameWithoutExtension(pdfPath);
            string imageFolderName = $"{pdfFileName.Replace(" ", "_")}_images";
            string imageDirectory = Path.Combine(pdfDirectory, imageFolderName);

            if (!Directory.Exists(imageDirectory))
            {
                Directory.CreateDirectory(imageDirectory);
                Logger.LogDebug($"Created image directory: {imageDirectory}");
            }

            // Split text into sections and interleave with images
            var textLines = pageText.Split('\n');
            int totalLines = textLines.Length;
            int imageCount = 0;

            // Calculate rough positions to insert images throughout the text
            var imagePositions = CalculateImagePositions(totalLines, images.Count);

            int currentImageIndex = 0;
            for (int lineIndex = 0; lineIndex < textLines.Length; lineIndex++)
            {
                // Add the current line of text
                contentBuilder.AppendLine(textLines[lineIndex]);

                // Check if we should insert an image after this line
                if (currentImageIndex < imagePositions.Count &&
                    lineIndex >= imagePositions[currentImageIndex])
                {
                    imageCount++;
                    try
                    {
                        var image = images[currentImageIndex];

                        // Generate image filename
                        string imageFileName = $"page_{pageNumber:D3}_image_{imageCount:D2}";
                        string imageExtension = DetermineImageExtension(image);
                        string fullImageFileName = $"{imageFileName}.{imageExtension}";
                        string imagePath = Path.Combine(imageDirectory, fullImageFileName);

                        // Save the image and only add reference if successful
                        if (SaveImageBytes(image, imagePath))
                        {
                            // Add markdown image reference inline with text with folder path
                            string relativeImagePath = $"{imageFolderName}/{fullImageFileName}";
                            contentBuilder.AppendLine();
                            contentBuilder.AppendLine($"![{imageFileName}]({relativeImagePath})");
                            contentBuilder.AppendLine();

                            Logger.LogDebug($"Extracted image: {fullImageFileName} from page {pageNumber} at line {lineIndex}");
                        }
                        else
                        {
                            Logger.LogWarning($"Failed to save image {imageCount} from page {pageNumber}, skipping reference");
                        }

                        currentImageIndex++;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning(ex, $"Failed to extract image {imageCount} from page {pageNumber} of {pdfPath}");
                        currentImageIndex++; // Still move to next image to avoid infinite loop
                    }
                }
            }

            // If there are remaining images that didn't get placed, add them at the end
            while (currentImageIndex < images.Count)
            {
                imageCount++;
                try
                {
                    var image = images[currentImageIndex];

                    // Generate image filename
                    string imageFileName = $"page_{pageNumber:D3}_image_{imageCount:D2}";
                    string imageExtension = DetermineImageExtension(image);
                    string fullImageFileName = $"{imageFileName}.{imageExtension}";
                    string imagePath = Path.Combine(imageDirectory, fullImageFileName);

                    // Save the image and only add reference if successful
                    if (SaveImageBytes(image, imagePath))
                    {
                        // Add markdown image reference at the end with folder path
                        string relativeImagePath = $"{imageFolderName}/{fullImageFileName}";
                        contentBuilder.AppendLine();
                        contentBuilder.AppendLine($"![{imageFileName}]({relativeImagePath})");
                        contentBuilder.AppendLine();

                        Logger.LogDebug($"Extracted remaining image: {fullImageFileName} from page {pageNumber}");
                    }
                    else
                    {
                        Logger.LogWarning($"Failed to save remaining image {imageCount} from page {pageNumber}, skipping reference");
                    }

                    currentImageIndex++;
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, $"Failed to extract remaining image {imageCount} from page {pageNumber} of {pdfPath}");
                    currentImageIndex++; // Still move to next image to avoid infinite loop
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, $"Failed to extract content from page {pageNumber} of {pdfPath}");
            // Fallback: just add the text without images
            try
            {
                contentBuilder.AppendLine(page.Text);
            }
            catch (Exception textEx)
            {
                Logger.LogError(textEx, $"Failed to extract even basic text from page {pageNumber} of {pdfPath}");
            }
        }
    }

    /// <summary>
    /// Calculates optimal positions to insert images throughout the text to create a natural flow.
    /// </summary>
    /// <param name="totalLines">Total number of text lines on the page.</param>
    /// <param name="imageCount">Number of images to distribute.</param>
    /// <returns>A list of line indices where images should be inserted.</returns>
    private static List<int> CalculateImagePositions(int totalLines, int imageCount)
    {
        var positions = new List<int>();

        if (imageCount == 0 || totalLines == 0)
        {
            return positions;
        }

        if (imageCount == 1)
        {
            // Single image goes in the middle
            positions.Add(totalLines / 2);
        }
        else
        {
            // Distribute images evenly throughout the text
            double interval = (double)totalLines / (imageCount + 1);

            for (int i = 1; i <= imageCount; i++)
            {
                int position = (int)(interval * i);
                // Ensure position is within bounds
                position = Math.Max(0, Math.Min(position, totalLines - 1));
                positions.Add(position);
            }
        }

        return positions.OrderBy(p => p).ToList();
    }

    /// <summary>
    /// Determines the appropriate file extension for an image based on its format.
    /// </summary>
    /// <param name="image">The PDF image object.</param>
    /// <returns>The file extension (without dot) for the saved image content.</returns>
    private static string DetermineImageExtension(IPdfImage image)
    {
        // Try to determine format based on the image properties
        // PdfPig supports extracting as PNG which is more reliable
        return "png";
    }

    /// <summary>
    /// Saves image bytes to the specified file path using PdfPig's image extraction methods.
    /// </summary>
    /// <param name="image">The PDF image object.</param>
    /// <param name="imagePath">The file path to save the image to.</param>
    /// <returns><see langword="true"/> if the image was successfully saved; otherwise, <see langword="false"/>.</returns>
    private bool SaveImageBytes(IPdfImage image, string imagePath)
    {
        try
        {
            byte[]? imageBytes = null;

            // Try to get PNG bytes first (most reliable format from PdfPig)
            if (image.TryGetPng(out var pngBytes))
            {
                imageBytes = pngBytes;
                Logger.LogDebug($"Extracted image as PNG: {imagePath}");
            }
            // Fallback to raw bytes if PNG extraction fails
            else if (image.RawBytes.Count > 0)
            {
                imageBytes = image.RawBytes.ToArray();
                Logger.LogDebug($"Using raw bytes for image: {imagePath}");
            }

            if (imageBytes != null && imageBytes.Length > 0)
            {
                // Validate that we have a reasonable image size (at least 100 bytes)
                if (imageBytes.Length >= 100)
                {
                    File.WriteAllBytes(imagePath, imageBytes);
                    Logger.LogDebug($"Saved image: {imagePath} ({imageBytes.Length} bytes)");
                    return true;
                }
                else
                {
                    Logger.LogWarning($"Image too small, likely invalid: {imagePath} ({imageBytes.Length} bytes)");
                    return false;
                }
            }
            else
            {
                Logger.LogWarning($"Image has no extractable bytes: {imagePath}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Failed to save image: {imagePath}");
            return false;
        }
    }

    /// <summary>
    /// Validates whether an image from a PDF page is extractable and valid.
    /// </summary>
    /// <param name="image">The PDF image to validate.</param>
    /// <returns><see langword="true"/> if the image is valid and extractable; otherwise, <see langword="false"/>.</returns>
    private bool IsValidImage(IPdfImage image)
    {
        try
        {
            // Check if we can extract PNG bytes
            if (image.TryGetPng(out var pngBytes) && pngBytes.Length >= 100)
            {
                return true;
            }

            // Check if raw bytes are available and reasonable size
            if (image.RawBytes.Count >= 100)
            {
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Failed to validate image");
            return false;
        }
    }
}
