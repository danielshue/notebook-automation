// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Tools.Shared;

/// <summary>
/// Orchestrates schema-driven metadata composition by merging template defaults,
/// resolver-derived values (with context), existing frontmatter, and explicit overrides.
/// </summary>
/// <remarks>
/// <para>
/// <b>Merge precedence (lowest to highest)</b>:
/// <list type="number">
/// <item><description>Schema defaults from template</description></item>
/// <item><description>Resolver-derived values (context-aware)</description></item>
/// <item><description>Existing frontmatter (if present in the body)</description></item>
/// <item><description>Explicit overrides provided by the caller</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Cleanup</b>: The pipeline removes internal/transient fields (for example, <c>_internal_path</c>,
/// <c>yaml-frontmatter</c>, <c>yamlfrontmatter</c>, <c>aliases</c>, <c>permalink</c>) and strips all date-like keys
/// that match <c>date-*</c> or <c>*-date</c> to align with historical behavior.
/// </para>
/// <para>
/// <b>Resolver context</b>: Callers should provide a context dictionary that includes <c>filePath</c> and/or
/// <c>_internal_path</c> so resolvers (for example, hierarchy, share-link, file-type) can compute values reliably.
/// </para>
/// <para>
/// <b>Idempotence</b>: Given the same inputs, <see cref="Compose"/> will always produce the same result. It never throws;
/// failures are logged and reasonable defaults are returned.
/// </para>
/// </remarks>
public interface IMetadataPipeline
{
    /// <summary>
    /// Composes final metadata for the given note type and returns body content with any embedded frontmatter removed.
    /// </summary>
    /// <param name="bodyText">Raw body text that may contain YAML frontmatter.</param>
    /// <param name="inputMetadata">Explicit overrides (highest precedence).</param>
    /// <param name="noteType">Human-friendly note type (e.g., "PDF Note", "Video Note").</param>
    /// <param name="context">Optional context for resolvers (e.g., file paths, sizes, additional metadata).</param>
    /// <returns>Tuple with cleaned body text (frontmatter removed) and merged metadata dictionary.</returns>
    /// <example>
    /// <code><![CDATA[
    /// var context = new Dictionary<string, object> { ["filePath"] = "C:/Vault/Docs/Module/lesson_02-roi-analysis.pdf" };
    /// var (body, metadata) = pipeline.Compose(rawBody, overrides, "PDF Note", context);
    /// // body has frontmatter removed; metadata contains merged fields (template + resolvers + frontmatter + overrides)
    /// ]]></code>
    /// </example>
    (string CleanBody, Dictionary<string, object> Metadata) Compose(
        string bodyText,
        Dictionary<string, object>? inputMetadata,
        string noteType,
        Dictionary<string, object>? context = null);
}
