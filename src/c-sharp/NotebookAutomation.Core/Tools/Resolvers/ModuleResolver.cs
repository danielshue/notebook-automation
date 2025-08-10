// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Tools.Resolvers;

/// <summary>
/// Resolves the <c>module</c> field from a file path using <see cref="IMetadataHierarchyDetector"/>.
/// </summary>
/// <remarks>
/// <para>
/// This resolver extracts the <c>module</c> segment from a content path using
/// <see cref="IMetadataHierarchyDetector.FindHierarchyInfo(string)"/> and returns the value associated with the
/// <c>"module"</c> key when available.
/// </para>
/// <para>
/// Context requirements: provide either <c>context["filePath"]</c> (preferred) or <c>context["_internal_path"]</c>.
/// When neither key is present or the module cannot be inferred, the resolver returns <see cref="string.Empty"/>.
/// </para>
/// <para>
/// Precedence: resolver-derived values are merged by <see cref="IMetadataPipeline"/> (schema defaults < resolvers < content/frontmatter < explicit overrides).
/// Returning <see cref="string.Empty"/> allows higher-precedence sources to supply the value.
/// </para>
/// <para>
/// Reliability: exceptions are caught and logged at Debug level; this resolver never throws.
/// </para>
/// </remarks>
/// <example>
/// <code><![CDATA[
/// var ctx = new Dictionary<string, object>
/// {
///     ["filePath"] = @"D:\\content\\ProgramA\\Course1\\Class2\\Module3\\Lesson.md"
/// };
/// var value = (string?)resolver.Resolve("module", ctx); // "Module3"
/// ]]></code>
/// </example>
/// <seealso cref="IMetadataPipeline"/>
/// <seealso cref="IMetadataHierarchyDetector"/>
/// <seealso cref="ProgramResolver"/>
/// <seealso cref="CourseResolver"/>
/// <seealso cref="ClassResolver"/>
public class ModuleResolver(ILogger<ModuleResolver> logger, IMetadataHierarchyDetector hierarchy) : IFieldValueResolver
{
    private readonly ILogger<ModuleResolver> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IMetadataHierarchyDetector _hierarchy = hierarchy ?? throw new ArgumentNullException(nameof(hierarchy));

    /// <summary>
    /// Resolves the module identifier or name based on the provided path context.
    /// </summary>
    /// <param name="fieldName">The field name to resolve (typically <c>"module"</c>); value is not used to compute the result.</param>
    /// <param name="context">Resolver context providing <c>filePath</c> or <c>_internal_path</c>.</param>
    /// <returns>
    /// The resolved module as a string, or <see cref="string.Empty"/> when the path is missing, the hierarchy lacks a module
    /// segment, or an error occurs.
    /// </returns>
    /// <remarks>
    /// Expects <c>context["filePath"]</c> or <c>context["_internal_path"]</c>. Any exceptions are caught and logged at Debug level, and the
    /// method returns <see cref="string.Empty"/> to allow higher-precedence sources to override.
    /// </remarks>
    /// <example>
    /// <code><![CDATA[
    /// var ctx = new Dictionary<string, object> { ["_internal_path"] = "/mnt/share/ProgramB/CourseX/ClassY/ModuleZ/Lesson.md" };
    /// var value = (string?)resolver.Resolve("module", ctx);
    /// ]]></code>
    /// </example>
    public object? Resolve(string fieldName, Dictionary<string, object>? context = null)
    {
        try
        {
            var path = GetPath(context);
            if (string.IsNullOrWhiteSpace(path)) return string.Empty;
            var info = _hierarchy.FindHierarchyInfo(path);
            return info.TryGetValue("module", out var value) ? value : string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "ModuleResolver failed; returning empty");
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
}
