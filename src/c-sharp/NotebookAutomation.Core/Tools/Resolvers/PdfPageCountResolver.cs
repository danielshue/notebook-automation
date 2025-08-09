// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using UglyToad.PdfPig;

namespace NotebookAutomation.Core.Tools.Resolvers;

/// <summary>
/// Resolves the page count of a PDF file using PdfPig.
/// </summary>
/// <remarks>
/// <para>
/// <b>Context:</b> Requires <c>context["filePath"]</c> (absolute path). Accepts <c>_internal_path</c> as an alias when provided by
/// higher-level processors. Returns an <see cref="int"/> count, <c>0</c> on failure, or <see langword="null"/> when path is missing.
/// </para>
/// <para>
/// <b>Precedence:</b> Resolver values are merged by <see cref="IMetadataPipeline"/> (schema defaults < resolvers < content/frontmatter < overrides).
/// </para>
/// <example>
/// <code>
/// var ctx = new Dictionary<string, object> { ["filePath"] = @"D:\\docs\\paper.pdf" };
/// var pages = (int?)resolver.Resolve("page-count", ctx); // e.g., 12
/// </code>
/// </example>
/// </remarks>
public class PdfPageCountResolver(ILogger<PdfPageCountResolver> logger) : IFieldValueResolver
{
    private readonly ILogger<PdfPageCountResolver> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Resolves the total number of pages for a PDF file.
    /// </summary>
    /// <param name="fieldName">The field name to resolve (e.g., <c>"pdf-page-count"</c>).</param>
    /// <param name="context">Context containing <c>filePath</c> for the PDF file.</param>
    /// <returns>The page count as an <see cref="int"/>; returns <c>0</c> on failure, or <see langword="null"/> if <c>filePath</c> is missing.</returns>
    /// <remarks>
    /// Opens the PDF using PdfPig and logs failures at Warning level.
    /// </remarks>
    public object? Resolve(string fieldName, Dictionary<string, object>? context = null)
    {
        try
        {
            if (context == null)
            {
                _logger.LogDebug("{Resolver}: filePath missing from context; returning null", nameof(PdfPageCountResolver));
                return null;
            }
            string? path = null;
            if (context.TryGetValue("filePath", out var value) && value is string p1 && !string.IsNullOrWhiteSpace(p1))
                path = p1;
            else if (context.TryGetValue("_internal_path", out var value2) && value2 is string p2 && !string.IsNullOrWhiteSpace(p2))
                path = p2;
            if (string.IsNullOrWhiteSpace(path))
            {
                _logger.LogDebug("{Resolver}: path not provided; returning null");
                return null;
            }

            using var document = PdfDocument.Open(path);
            int count = document.NumberOfPages;
            _logger.LogDebug("Resolved page-count for {Path}: {Count}", path, count);
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve PDF page count; returning 0");
            return 0;
        }
    }
}
