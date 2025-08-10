// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Core.Utils;

namespace NotebookAutomation.Core.Tools.Resolvers;

/// <summary>
/// Resolves the <c>program</c> field from a file path using <see cref="IMetadataHierarchyDetector"/>.
/// </summary>
/// <remarks>
/// <para>
/// This resolver extracts the highest-level "program" segment from a content path. It looks up hierarchy
/// information via <see cref="IMetadataHierarchyDetector.FindHierarchyInfo(string)"/> and returns the value
/// associated with the <c>"program"</c> key when available.
/// </para>
/// <para>
/// Context requirements: provide either <c>context["filePath"]</c> (preferred) or <c>context["_internal_path"]</c>.
/// When neither key is present or the program cannot be inferred, the resolver returns <see cref="string.Empty"/>.
/// </para>
/// <para>
/// Precedence: values produced by resolvers are merged by <see cref="IMetadataPipeline"/> with the following order
/// of precedence (lowest to highest): schema defaults, resolver-derived values, content/frontmatter, and explicit
/// overrides. Returning <see cref="string.Empty"/> from this resolver allows content/frontmatter or overrides to win.
/// </para>
/// <para>
/// Reliability: all errors are trapped and logged at Debug level to avoid noisy logs; the resolver never throws and
/// will return <see cref="string.Empty"/> on failure.
/// </para>
/// <para>
/// Thread-safety: the resolver is stateless aside from its injected dependencies and is safe to use concurrently.
/// </para>
/// </remarks>
/// <example>
/// <code><![CDATA[
/// var ctx = new Dictionary<string, object>
/// {
///     ["filePath"] = @"D:\\content\\ProgramA\\Course1\\Class2\\Module3\\Lesson.md"
/// };
/// var value = (string?)resolver.Resolve("program", ctx); // "ProgramA"
/// ]]></code>
/// </example>
/// <seealso cref="IMetadataPipeline"/>
/// <seealso cref="IMetadataHierarchyDetector"/>
/// <seealso cref="CourseResolver"/>
/// <seealso cref="ClassResolver"/>
/// <seealso cref="ModuleResolver"/>
public class ProgramResolver(ILogger<ProgramResolver> logger, IMetadataHierarchyDetector hierarchy, AppConfig appConfig) : IFieldValueResolver
{
    private readonly ILogger<ProgramResolver> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IMetadataHierarchyDetector _hierarchy = hierarchy ?? throw new ArgumentNullException(nameof(hierarchy));
    private readonly AppConfig _config = appConfig ?? throw new ArgumentNullException(nameof(appConfig));

    // Backward-compatible constructor for tests and existing code that didn't provide AppConfig
    public ProgramResolver(ILogger<ProgramResolver> logger, IMetadataHierarchyDetector hierarchy)
        : this(logger, hierarchy, new AppConfig())
    {
    }

    /// <summary>
    /// Resolves the program identifier or name based on the provided path context.
    /// </summary>
    /// <param name="fieldName">The field name to resolve (typically <c>"program"</c>); value is not used to compute the result.</param>
    /// <param name="context">Resolver context providing <c>filePath</c> or <c>_internal_path</c>.</param>
    /// <returns>
    /// The resolved program as a string, or <see cref="string.Empty"/> when the path is missing, the hierarchy
    /// lacks a program segment, or an error occurs.
    /// </returns>
    /// <remarks>
    /// Expects <c>context["filePath"]</c> or <c>context["_internal_path"]</c>. Any exceptions are caught and logged at Debug level,
    /// and the method returns <see cref="string.Empty"/>.
    /// </remarks>
    /// <example>
    /// <code><![CDATA[
    /// var ctx = new Dictionary<string, object> { ["_internal_path"] = "/mnt/share/ProgramB/CourseX/Lesson.md" };
    /// var value = (string?)resolver.Resolve("program", ctx);
    /// ]]></code>
    /// </example>
    public object? Resolve(string fieldName, Dictionary<string, object>? context = null)
    {
        try
        {
            var path = GetPath(context);
            if (string.IsNullOrWhiteSpace(path)) return string.Empty;

            // Normalize OneDrive paths to vault-relative to ensure consistent hierarchy detection
            var normalized = NormalizeForHierarchy(path);

            var info = _hierarchy.FindHierarchyInfo(normalized);
            return info.TryGetValue("program", out var value) ? value : string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "ProgramResolver failed; returning empty");
            return string.Empty;
        }
    }

    private static string? GetPath(Dictionary<string, object>? ctx)
    {
        if (ctx == null) return null;
        if (ctx.TryGetValue("filePath", out var fp) && fp is string s1) return s1;
        if (ctx.TryGetValue("_internal_path", out var ip) && ip is string s2) return s2;
        return null;
    }

    private string NormalizeForHierarchy(string path)
    {
        try
        {
            var oneDriveRoot = _config.Paths?.GetEffectiveOneDriveRoot();
            var vaultRoot = _config.Paths?.GetEffectiveVaultRoot();

            if (!string.IsNullOrWhiteSpace(oneDriveRoot) && !string.IsNullOrWhiteSpace(vaultRoot))
            {
                var fullPath = Path.GetFullPath(path);
                var odRootFull = Path.GetFullPath(oneDriveRoot!);

                var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
                if (fullPath.StartsWith(odRootFull, comparison))
                {
                    var relative = Path.GetRelativePath(odRootFull, fullPath);
                    var remapped = Path.Combine(vaultRoot!, relative);
                    _logger.LogDebug("ProgramResolver remapped OneDrive path to vault path: {Original} -> {Remapped}", path, remapped);
                    return remapped;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "ProgramResolver path normalization skipped; using original path");
        }

        return path;
    }
}
