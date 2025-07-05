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
/// <example>
/// <code>
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
    private string _yamlFrontmatter = string.Empty; // Temporarily store YAML frontmatter    /// <summary>
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
        bool extractImages = false) : base(logger, aiSummarizer, markdownNoteBuilder, appConfig ?? new AppConfig(), yamlHelper, hierarchyDetector, templateManager)
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
        var metadata = new Dictionary<string, object?>(); if (!File.Exists(pdfPath))
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
                    }                    // Collect metadata after reading pages
                    metadata["page-count"] = document.NumberOfPages;// Count total valid images across all pages (only if image extraction is enabled)
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
                    var info = document.Information;
                    if (!string.IsNullOrWhiteSpace(info?.Title))
                    {
                        metadata["title"] = info.Title;
                    }

                    if (!string.IsNullOrWhiteSpace(info?.Author))
                    {
                        metadata["authors"] = new string[] { info.Author }; // Using authors (string array) as requested
                    }

                    if (!string.IsNullOrWhiteSpace(info?.Subject))
                    {
                        metadata["subject"] = info.Subject;
                    }

                    if (!string.IsNullOrWhiteSpace(info?.Keywords))
                    {
                        metadata["keywords"] = info.Keywords;
                    }
                }                // Extract module and lesson information
                Logger.LogDebug($"Extracting course structure information from file path {pdfPath}");
                _courseStructureExtractor.ExtractModuleAndLesson(pdfPath, metadata);

                // Extract hierarchy information using injected MetadataHierarchyDetector
                Logger.LogDebug($"Extracting hierarchy information from file path {pdfPath}");

                // Convert OneDrive path to equivalent vault path for hierarchy detection
                Logger.LogDebug($"BEFORE CONVERSION: OneDrive path = {pdfPath}");
                string vaultPath = ConvertOneDriveToVaultPath(pdfPath);
                Logger.LogDebug($"AFTER CONVERSION: Vault path = {vaultPath}");
                Logger.LogDebug($"Detecting hierarchy information from vault path: {vaultPath} (converted from OneDrive path: {pdfPath})"); var hierarchyInfo = HierarchyDetector?.FindHierarchyInfo(vaultPath);
                if (HierarchyDetector != null && hierarchyInfo != null)
                {
                    HierarchyDetector.UpdateMetadataWithHierarchy(metadata, hierarchyInfo, "pdf-reference");
                }

                // Add file information for PDF
                var fileInfo = new FileInfo(pdfPath);
                metadata["pdf-size"] = $"{fileInfo.Length / 1024.0 / 1024.0:F2} MB";
                metadata["date-created"] = DateTime.Now.ToString("yyyy-MM-dd");
                metadata["pdf-uploaded"] = fileInfo.CreationTime.ToString("yyyy-MM-dd");

                // Add template-type for PDF
                metadata["template-type"] = "pdf-reference";
                metadata["type"] = "note/case-study";
                metadata["status"] = "unread";
                metadata["comprehension"] = 0;
                metadata["auto-generated-state"] = "writable";                // Add the file path for later use
                metadata["onedrive_fullpath_file_reference"] = pdfPath; return sb.ToString();
            }).ConfigureAwait(false); int extractedCharCount = extractedText.Length;
            Logger.LogDebug($"Extracted {extractedCharCount:N0} characters of text from PDF: {pdfPath}");

            // Generate OneDrive shared link if service is available
            if (_oneDriveService != null)
            {
                try
                {
                    string? sharedLink = await _oneDriveService.GetShareLinkAsync(pdfPath);
                    if (!string.IsNullOrEmpty(sharedLink))
                    {
                        metadata["onedrive-shared-link"] = sharedLink;
                        Logger.LogDebug($"Generated OneDrive shared link for PDF: {pdfPath}");
                    }
                    else
                    {
                        metadata["onedrive-shared-link"] = string.Empty;
                        Logger.LogDebug($"No OneDrive shared link generated for PDF: {pdfPath}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, $"Failed to generate OneDrive shared link for PDF: {pdfPath}");
                    metadata["onedrive-shared-link"] = string.Empty;
                }
            }
            else
            {
                metadata["onedrive-shared-link"] = string.Empty;
                Logger.LogDebug("OneDrive service not available, setting empty shared link");
            }

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

            // Ensure all required fields are in the metadata dictionary
            // These will be used both for frontmatter and for the AI summarizer
            if (!metadata.ContainsKey("template-type"))
            {
                metadata["template-type"] = "pdf-reference";
            }

            if (!metadata.ContainsKey("auto-generated-state"))
            {
                metadata["auto-generated-state"] = "writable";
            }

            if (!metadata.ContainsKey("module"))
            {
                metadata["module"] = string.Empty;
            }

            if (!metadata.ContainsKey("lesson"))
            {
                metadata["lesson"] = string.Empty;
            }

            if (!metadata.ContainsKey("comprehension"))
            {
                metadata["comprehension"] = 0;
            }

            if (!metadata.ContainsKey("completion-date"))
            {
                metadata["completion-date"] = string.Empty;
            }

            if (!metadata.ContainsKey("date-review"))
            {
                metadata["date-review"] = string.Empty;
            }

            if (!metadata.ContainsKey("onedrive-shared-link"))
            {
                metadata["onedrive-shared-link"] = string.Empty;
            }

            if (!metadata.ContainsKey("publisher"))
            {
                metadata["publisher"] = "University of Illinois at Urbana-Champaign";
            }

            // Make sure we have the author field from authors if available
            if (metadata.TryGetValue("authors", out var authors) && authors != null)
            {
                metadata["authors"] = authors; // For consistency in output
            }

            // Build YAML frontmatter without the --- separators
            string yamlContent = BuildYamlFrontmatter(metadata.ToDictionary(kvp => kvp.Key, kvp => kvp.Value!));

            // Store in a temporary field for use by GeneratePdfSummaryAsync
            _yamlFrontmatter = yamlContent;

            // Remove any unwanted fields
            metadata.Remove("aliases"); metadata.Remove("pdf-link");
            metadata.Remove("permalink");
            metadata.Remove("yaml-frontmatter"); // Prevent duplication

            return (extractedText, metadata.ToDictionary(kvp => kvp.Key, kvp => kvp.Value!));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Failed to extract text from PDF: {pdfPath}");
            return (string.Empty, metadata.ToDictionary(kvp => kvp.Key, kvp => kvp.Value!));
        }
    }

    /// <summary>
    /// Builds a YAML frontmatter string using the PDF metadata and following the template from metadata.yaml.
    /// </summary>
    /// <param name="metadata">The PDF metadata dictionary.</param>
    /// <returns>A YAML frontmatter string suitable for use in the prompt template.</returns>
    private string BuildYamlFrontmatter(Dictionary<string, object> metadata)
    {
        try
        {
            // Create a dictionary with the expected YAML frontmatter structure
            var yamlData = new Dictionary<string, object>
            {
                ["template-type"] = "pdf-reference",
                ["auto-generated-state"] = "writable",
                ["type"] = "note/case-study",  //TODO: Not every PDF is a case study
            };

            // Add title if available
            if (metadata.TryGetValue("title", out var title) && title != null)
            {
                yamlData["title"] = title?.ToString() ?? "Untitled PDF";
            }

            // Add author if available - map from authors field
            if (metadata.TryGetValue("authors", out var authors) && authors != null)
            {
                yamlData["authors"] = authors;
            }

            // Add page count if available
            if (metadata.TryGetValue("page-count", out var pageCount) && pageCount != null)
            {
                yamlData["page-count"] = pageCount;
            }

            // Add program, course, class, module, lesson if available
            if (metadata.TryGetValue("program", out var program) && program != null)
            {
                yamlData["program"] = program?.ToString() ?? string.Empty;
            }

            if (metadata.TryGetValue("course", out var course) && course != null)
            {
                yamlData["course"] = course?.ToString() ?? string.Empty;
            }

            if (metadata.TryGetValue("class", out var className) && className != null)
            {
                yamlData["class"] = className?.ToString() ?? string.Empty;
            }

            if (metadata.TryGetValue("module", out var module) && module != null)
            {
                yamlData["module"] = module?.ToString() ?? string.Empty;
            }
            else
            {
                yamlData["module"] = string.Empty;  // Ensure module is always included
            }

            if (metadata.TryGetValue("lesson", out var lesson) && lesson != null)
            {
                yamlData["lesson"] = lesson?.ToString() ?? string.Empty;
            }
            else
            {
                yamlData["lesson"] = string.Empty;  // Ensure lesson is always included
            }

            // Add fixed values
            yamlData["comprehension"] = 0;

            // Add date fields
            yamlData["date-created"] = DateTime.Now.ToString("yyyy-MM-dd");

            // Add empty date review/completion fields
            yamlData["completion-date"] = string.Empty;
            yamlData["date-review"] = string.Empty;

            // Add file information
            if (metadata.TryGetValue("onedrive_fullpath_file_reference", out var filePath) && filePath != null)
            {
                yamlData["onedrive_fullpath_file_reference"] = filePath?.ToString() ?? string.Empty;
            }

            if (metadata.TryGetValue("onedrive-shared-link", out var shareLink) && shareLink != null)
            {
                yamlData["onedrive-shared-link"] = shareLink?.ToString() ?? string.Empty;
            }
            else
            {
                yamlData["onedrive-shared-link"] = string.Empty;  // Ensure onedrive-shared-link is always included
            }

            if (metadata.TryGetValue("pdf-size", out var pdfSize) && pdfSize != null)
            {
                yamlData["pdf-size"] = pdfSize?.ToString() ?? string.Empty;
            }

            if (metadata.TryGetValue("pdf-uploaded", out var pdfUploaded) && pdfUploaded != null)
            {
                yamlData["pdf-uploaded"] = pdfUploaded?.ToString() ?? string.Empty;
            }

            // Set publisher if not already set
            if (!yamlData.ContainsKey("publisher"))
            {
                yamlData["publisher"] = "University of Illinois at Urbana-Champaign"; //TODO: This should not be hardcoded but instad come from the metaedata.yaml file.
            }

            // Set status as unread by default
            yamlData["status"] = "unread";

            // Add empty tags field for AI to populate
            yamlData["tags"] = new string[] { };

            // Add resources_root if available
            if (metadata.TryGetValue("onedrive_fullpath_root", out var resourcesRoot) && resourcesRoot != null)
            {
                yamlData["onedrive_fullpath_root"] = resourcesRoot?.ToString() ?? string.Empty;
            }

            // Explicitly remove unwanted fields if they exist
            // (These shouldn't be in our data, but just in case)
            yamlData.Remove("aliases");
            yamlData.Remove("pdf-link");
            yamlData.Remove("permalink");

            // Serialize to YAML - without the --- separators
            var serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            string yamlString = serializer.Serialize(yamlData);

            int yamlLength = yamlString.Length;
            int fields = yamlData.Count;
            Logger.LogDebug($"Generated YAML frontmatter for PDF: {yamlLength} chars, {fields} fields");
            Logger.LogDebug("Generated YAML frontmatter for PDF without separators");
            return yamlString;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to build YAML frontmatter for PDF");
            return string.Empty;
        }
    }

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

        // Add YAML frontmatter as a variable - but don't wrap it in --- separators
        // as that will be handled by the template/prompt
        if (!string.IsNullOrEmpty(_yamlFrontmatter))
        {
            // The _yamlFrontmatter should now contain just the YAML content without separators
            variables["yamlfrontmatter"] = _yamlFrontmatter;
            Logger.LogDebug($"Added yamlfrontmatter variable ({_yamlFrontmatter.Length:N0} chars) for AI summarizer effectivePrompt:{effectivePrompt}:");
        }
        else
        {
            // Build it now if not already built - again without wrapping in --- separators
            string yamlContent = BuildYamlFrontmatter(metadata);
            variables["yamlfrontmatter"] = yamlContent;
            Logger.LogDebug($"Built and added yamlfrontmatter variable ({yamlContent.Length:N0} chars) for AI summarizer effectivePrompt:{effectivePrompt}:");
        }

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

    /// <summary>    /// <summary>
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
    /// <returns>List of line indices where images should be inserted.</returns>
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
    /// <returns>The file extension (without dot) for the image.</returns>
    private static string DetermineImageExtension(IPdfImage image)
    {
        // Try to determine format based on the image properties
        // PdfPig supports extracting as PNG which is more reliable
        return "png";
    }

    /// <summary>
    /// Saves image bytes to the specified file path using PdfPig's proper image extraction methods.
    /// </summary>
    /// <param name="image">The PDF image object.</param>
    /// <param name="imagePath">The file path to save the image to.</param>
    /// <returns>True if the image was successfully saved, false otherwise.</returns>
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
    /// <returns>True if the image is valid and extractable, false otherwise.</returns>
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
