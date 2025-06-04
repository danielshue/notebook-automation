using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using NotebookAutomation.Core.Tools.Shared;
using NotebookAutomation.Core.Services;
using NotebookAutomation.Core.Utils;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.IO;

namespace NotebookAutomation.Core.Tools.PdfProcessing
{
    /// <summary>
    /// Provides functionality for extracting text from PDF files and generating markdown notes.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="PdfNoteProcessor"/> class with logger and AI summarizer.
    /// </remarks>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <param name="aiSummarizer">The AISummarizer service for generating AI-powered summaries.</param>    
    public class PdfNoteProcessor(ILogger<PdfNoteProcessor> logger, AISummarizer aiSummarizer) : DocumentNoteProcessorBase(logger, aiSummarizer)
    {
        private readonly YamlHelper _yamlHelper = new(logger);
        private string _yamlFrontmatter = string.Empty; // Temporarily store YAML frontmatter

        /// <summary>
        /// Extracts text and metadata from a PDF file.
        /// </summary>
        /// <param name="pdfPath">Path to the PDF file.</param>
        /// <returns>Tuple of extracted text and metadata dictionary.</returns>
        public override async Task<(string Text, Dictionary<string, object> Metadata)> ExtractTextAndMetadataAsync(string pdfPath)
        {
            var metadata = new Dictionary<string, object>();
            if (!File.Exists(pdfPath))
            {
                Logger.LogErrorWithPath("PDF file not found: {FilePath}", pdfPath);
                return (string.Empty, metadata);
            }
            string extractedText = string.Empty;
            try
            {
                Logger.LogInformationWithPath("Starting PDF content extraction: {FilePath}", pdfPath);
                extractedText = await Task.Run(() =>
                {
                    var sb = new System.Text.StringBuilder();
                    using (PdfDocument document = PdfDocument.Open(pdfPath))
                    {
                        Logger.LogDebugWithPath("Opened PDF document with {PageCount} pages", pdfPath, document.NumberOfPages);
                        sb.AppendLine();

                        int pageCount = 0;
                        foreach (Page page in document.GetPages())
                        {
                            pageCount++;
                            if (pageCount % 10 == 0 || pageCount == 1 || pageCount == document.NumberOfPages)
                            {
                                Logger.LogDebugWithPath("Extracting text from page {CurrentPage}/{TotalPages}", pdfPath, pageCount, document.NumberOfPages);
                            }
                            sb.AppendLine(page.Text);
                        }

                        // Collect metadata after reading pages
                        metadata["page_count"] = document.NumberOfPages;
                        // "generated" field removed as requested
                        var info = document.Information;
                        if (!string.IsNullOrWhiteSpace(info?.Title))
                            metadata["title"] = info.Title;
                        if (!string.IsNullOrWhiteSpace(info?.Author))
                            metadata["authors"] = new string[] { info.Author }; // Using authors (string array) as requested
                        if (!string.IsNullOrWhiteSpace(info?.Subject))
                            metadata["subject"] = info.Subject;
                        if (!string.IsNullOrWhiteSpace(info?.Keywords))
                            metadata["keywords"] = info.Keywords;
                    }                    // Extract module and lesson information
                    Logger.LogDebugWithPath("Extracting course structure information", pdfPath);
                    var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
                    var courseLogger = loggerFactory.CreateLogger<CourseStructureExtractor>();
                    var courseStructureExtractor = new CourseStructureExtractor(courseLogger);
                    courseStructureExtractor.ExtractModuleAndLesson(pdfPath, metadata);

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
                    metadata["auto-generated-state"] = "writable";

                    // Add the file path for later use
                    metadata["onedrive_fullpath_file_reference"] = pdfPath;

                    return sb.ToString();
                });

                int extractedCharCount = extractedText.Length;
                Logger.LogInformationWithPath("Extracted {CharCount:N0} characters of text from PDF: {FilePath}", pdfPath, extractedCharCount);

                // Ensure all required fields are in the metadata dictionary
                // These will be used both for frontmatter and for the AI summarizer
                if (!metadata.ContainsKey("template-type"))
                    metadata["template-type"] = "pdf-reference";

                if (!metadata.ContainsKey("auto-generated-state"))
                    metadata["auto-generated-state"] = "writable";

                if (!metadata.ContainsKey("module"))
                    metadata["module"] = string.Empty;

                if (!metadata.ContainsKey("lesson"))
                    metadata["lesson"] = string.Empty;

                if (!metadata.ContainsKey("comprehension"))
                    metadata["comprehension"] = 0;

                if (!metadata.ContainsKey("completion-date"))
                    metadata["completion-date"] = string.Empty;

                if (!metadata.ContainsKey("date-review"))
                    metadata["date-review"] = string.Empty;

                if (!metadata.ContainsKey("onedrive-shared-link"))
                    metadata["onedrive-shared-link"] = string.Empty;

                if (!metadata.ContainsKey("publisher"))
                    metadata["publisher"] = "University of Illinois at Urbana-Champaign";

                // Make sure we have the author field from authors if available
                if (metadata.TryGetValue("authors", out var authors) && authors != null)
                {
                    metadata["author"] = authors; // For consistency in output
                }

                // Build YAML frontmatter without the --- separators
                string yamlContent = BuildYamlFrontmatter(metadata);

                // Store in a temporary field for use by GeneratePdfSummaryAsync
                _yamlFrontmatter = yamlContent;

                // Remove any unwanted fields
                metadata.Remove("aliases");
                metadata.Remove("pdf-link");
                metadata.Remove("permalink");
                metadata.Remove("yaml-frontmatter"); // Prevent duplication

                return (extractedText, metadata);
            }
            catch (Exception ex)
            {
                Logger.LogErrorWithPath(ex, "Failed to extract text from PDF: {FilePath}", pdfPath);
                return (string.Empty, metadata);
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
                    ["type"] = "note/case-study"
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
                    yamlData["publisher"] = "University of Illinois at Urbana-Champaign";
                }

                // Set status as unread by default
                yamlData["status"] = "unread";

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
                Logger.LogInformationWithPath("Generated YAML frontmatter for PDF: {Length} chars, {FieldCount} fields", "yamlFrontmatter", yamlLength, fields);
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
            return base.GenerateMarkdownNote(pdfText, metadata, "PDF Note", includeNoteTypeTitle: true);
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

            Logger.LogInformationWithPath("Preparing variables for AI summarization", effectivePrompt);

            // Track character counts for detailed progress reporting
            int textLength = pdfText?.Length ?? 0;
            int estimatedTokens = textLength / 4; // Rough estimate: 4 chars per token
            Logger.LogInformationWithPath("PDF content to summarize: {CharCount:N0} characters (~{TokenEstimate:N0} estimated tokens)",
                effectivePrompt, textLength, estimatedTokens);

            // Add title if available
            if (metadata.TryGetValue("title", out var titleObj) && titleObj != null)
            {
                variables["title"] = titleObj.ToString() ?? "Untitled PDF";
                Logger.LogInformationWithPath("Added title to variables: {Title}", effectivePrompt, variables["title"]);
            }            // Add YAML frontmatter as a variable - but don't wrap it in --- separators
            // as that will be handled by the template/prompt
            if (!string.IsNullOrEmpty(_yamlFrontmatter))
            {
                // The _yamlFrontmatter should now contain just the YAML content without separators
                variables["yamlfrontmatter"] = _yamlFrontmatter;
                Logger.LogInformationWithPath("Added yamlfrontmatter variable ({Length:N0} chars) for AI summarizer",
                    effectivePrompt, _yamlFrontmatter.Length);
            }
            else
            {
                // Build it now if not already built - again without wrapping in --- separators
                string yamlContent = BuildYamlFrontmatter(metadata);
                variables["yamlfrontmatter"] = yamlContent;
                Logger.LogInformationWithPath("Built and added yamlfrontmatter variable ({Length:N0} chars) for AI summarizer",
                    effectivePrompt, yamlContent.Length);
            }

            // Make a copy to avoid modifying the original metadata
            _ = new
            // Make a copy to avoid modifying the original metadata
            Dictionary<string, object>(metadata);

            Logger.LogInformationWithPath("Starting AI summarization process with prompt template: {FilePath}",
                effectivePrompt);
            Logger.LogInformationWithPath("AI summary generation beginning - this may take some time for large documents",
                effectivePrompt);

            // Use the summarizer directly
            string? result = null;
            try
            {
                if (Summarizer != null)
                {
                    Logger.LogInformationWithPath("Sending content to AI summarizer", effectivePrompt);
                    result = await Summarizer.SummarizeWithVariablesAsync(
                        pdfText ?? string.Empty,
                        variables,
                        effectivePrompt);
                }
                else
                {
                    Logger.LogWarningWithPath("AI summarizer service not available", effectivePrompt);
                    result = "[Simulated AI summary - summarizer service unavailable]";
                }

                // Log the result statistics
                int summaryLength = result?.Length ?? 0;
                int compressionRatio = textLength > 0 ? (int)(100 - ((double)summaryLength / textLength * 100)) : 0;
                Logger.LogInformationWithPath("AI summary generation complete: {Length:N0} characters ({CompressionRatio}% reduction)",
                    effectivePrompt, summaryLength, compressionRatio);
            }
            catch (Exception ex)
            {
                Logger.LogErrorWithPath(ex, "Error generating AI summary for PDF", effectivePrompt);
                result = "[Error generating AI summary]";
            }

            return result ?? string.Empty;
        }
    }
}
