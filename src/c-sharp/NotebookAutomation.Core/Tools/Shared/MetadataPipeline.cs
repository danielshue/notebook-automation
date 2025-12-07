// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Tools.Shared;

/// <summary>
/// Default implementation of <see cref="IMetadataPipeline"/>.
/// </summary>
/// <remarks>
/// <para>
/// This implementation composes metadata using a deterministic, schema-driven process:
/// </para>
/// <list type="number">
/// <item><description>Parse YAML frontmatter from <paramref name="bodyText"/> (when present) and strip it from the body.</description></item>
/// <item><description>Load template defaults for <paramref name="noteType"/> via <see cref="IMetadataTemplateManager"/>.</description></item>
/// <item><description>Apply hierarchy information when <c>_internal_path</c> is present in any input layer.</description></item>
/// <item><description>Resolve dynamic fields via the template manager and registered resolvers using the provided <paramref name="context"/>.</description></item>
/// <item><description>Merge values with precedence: schema defaults < resolvers < frontmatter < explicit overrides.</description></item>
/// <item><description>Cleanup transient/internal fields and strip date-like keys (<c>date-*</c>, <c>*-date</c>).</description></item>
/// </list>
/// <para>
/// All exceptions are handled internally with Warning-level logs. The pipeline returns best-effort results without throwing.
/// </para>
/// </remarks>
public class MetadataPipeline(
    ILogger<MetadataPipeline> logger,
    IYamlHelper yamlHelper,
    IMetadataTemplateManager templateManager,
    IMetadataHierarchyDetector hierarchyDetector,
    AppConfig appConfig
) : IMetadataPipeline
{
    private readonly ILogger<MetadataPipeline> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IYamlHelper _yaml = yamlHelper ?? throw new ArgumentNullException(nameof(yamlHelper));
    private readonly IMetadataTemplateManager _templates = templateManager ?? throw new ArgumentNullException(nameof(templateManager));
    private readonly IMetadataHierarchyDetector _hierarchy = hierarchyDetector ?? throw new ArgumentNullException(nameof(hierarchyDetector));
    private readonly AppConfig _config = appConfig ?? throw new ArgumentNullException(nameof(appConfig));

    /// <inheritdoc />
    public (string CleanBody, Dictionary<string, object> Metadata) Compose(
        string bodyText,
        Dictionary<string, object>? inputMetadata,
        string noteType,
        Dictionary<string, object>? context = null)
    {
        inputMetadata ??= new();
        context ??= new();

        // 1) Parse existing frontmatter from body
        Dictionary<string, object?> contentMetadataNullable = new();
        try
        {
            var fm = _yaml.ExtractFrontmatter(bodyText);
            if (!string.IsNullOrWhiteSpace(fm))
            {
                var parsed = _yaml.ParseYamlToDictionary(fm);
                if (parsed.Count == 0)
                {
                    // Fallback: naive key:value parsing for simple frontmatter
                    var fallback = new Dictionary<string, object?>();
                    foreach (var line in fm.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
                    {
                        var trimmed = line.Trim();
                        if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("-") || !trimmed.Contains(':')) continue;
                        var idx = trimmed.IndexOf(':');
                        var key = trimmed[..idx].Trim();
                        var val = trimmed[(idx + 1)..].Trim();
                        if (!string.IsNullOrEmpty(key)) fallback[key] = val;
                    }
                    contentMetadataNullable = fallback;
                }
                else
                {
                    contentMetadataNullable = parsed.ToDictionary(k => k.Key, v => (object?)v.Value);
                }
                _logger.LogDebug("Frontmatter detected in body with {Count} fields", contentMetadataNullable.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed parsing frontmatter; proceeding without it");
        }

        // Remove frontmatter from body regardless of parse success
        var cleanBody = _yaml.RemoveFrontmatter(bodyText);

        // 3) Run schema-driven resolution by reusing EnhanceMetadataWithTemplate on the union context
        // Start context with knowns (filePath etc.) and merge content + overrides for richer resolution
        var resolutionContext = new Dictionary<string, object>(context);

        foreach (var kv in contentMetadataNullable)
        {
            if (!resolutionContext.ContainsKey(kv.Key) && kv.Value is not null)
                resolutionContext[kv.Key] = kv.Value!;
        }
        foreach (var kv in inputMetadata)
        {
            resolutionContext[kv.Key] = kv.Value;
        }

        // 2) Determine base template via template manager
        // First check if metadata already contains explicit template-type
        string templateType = DetermineTemplateType(noteType, resolutionContext);
        var schemaDefaults = _templates.GetTemplate(templateType) ?? new();

        // Apply hierarchy first if _internal_path is present in any source
        var working = new Dictionary<string, object>(schemaDefaults);
        // temporarily add context into working to allow resolvers to see it later
        foreach (var kv in resolutionContext)
        {
            if (!working.ContainsKey(kv.Key)) working[kv.Key] = kv.Value;
        }

        try
        {
            string? internalPath = null;
            if (TryGetStringFromDict("_internal_path", inputMetadata)) internalPath = inputMetadata["_internal_path"].ToString();
            else if (TryGetStringFromNullableDict("_internal_path", contentMetadataNullable)) internalPath = contentMetadataNullable["_internal_path"]?.ToString();
            else if (TryGetStringFromDict("_internal_path", working)) internalPath = working["_internal_path"].ToString();

            if (!string.IsNullOrWhiteSpace(internalPath))
            {
                var info = _hierarchy.FindHierarchyInfo(internalPath!);
                var nullable = working.ToDictionary(x => x.Key, x => (object?)x.Value);
                var docType = noteType.Split(' ')[0].ToLowerInvariant();
                var updated = _hierarchy.UpdateMetadataWithHierarchy(nullable, info, docType);
                working = updated.ToDictionary(x => x.Key, x => x.Value ?? new());
                _logger.LogDebug("Applied hierarchy for path: {Path}", internalPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Hierarchy application failed; continuing");
        }

        // 4) Use TemplateManager to resolve fields and merge defaults
        working = _templates.EnhanceMetadataWithTemplate(working, templateType);

        // 5) Merge with precedence: schema defaults < resolver values (already in working via Enhance) < content frontmatter < input overrides
        var merged = new Dictionary<string, object>(schemaDefaults);
        foreach (var kv in working) merged[kv.Key] = kv.Value;
        foreach (var kv in contentMetadataNullable)
        {
            if (kv.Value is not null) merged[kv.Key] = kv.Value!;
        }
        foreach (var kv in inputMetadata) merged[kv.Key] = kv.Value;

        // 6) Cleanup internal and transient fields
        merged.Remove("_internal_path");
        merged.Remove("yaml-frontmatter");
        merged.Remove("yamlfrontmatter");
        merged.Remove("aliases");
        merged.Remove("permalink");

        // Remove processing-only fields that shouldn't appear in final frontmatter
        merged.Remove("filePath");
        merged.Remove("resources_root");
        // Ensure transcript path is never persisted in frontmatter
        merged.Remove("transcript");
        merged.Remove("share-link");
        merged.Remove("share_link");
        merged.Remove("pdftext_file");
        merged.Remove("video_transcript_sources");

        // 7) Remove date-* fields as done historically in base
        var remove = merged.Keys.Where(k => k.StartsWith("date-") || k.EndsWith("-date")).ToList();
        foreach (var key in remove) merged.Remove(key);

        return (cleanBody, merged);
    }

    private static bool TryGetStringFromDict(string key, IDictionary<string, object> src)
        => src.TryGetValue(key, out var obj) && obj != null && obj is string;

    private static bool TryGetStringFromNullableDict(string key, IDictionary<string, object?> src)
        => src.TryGetValue(key, out var obj) && obj != null && obj is string;

    private static string DetermineTemplateType(string noteType, Dictionary<string, object> context)
    {
        // First check if metadata already contains explicit template-type
        if (context.TryGetValue("template-type", out var templateTypeValue) &&
            templateTypeValue is string explicitTemplateType &&
            !string.IsNullOrWhiteSpace(explicitTemplateType))
        {
            return explicitTemplateType;
        }

        // Fall back to noteType-based determination
        return noteType switch
        {
            "Video Note" => "video-reference",
            "PDF Note" => "pdf-reference",
            "Live Session Note" => "live-session-note",
            "Transcript" => "transcript",
            _ => "video-reference"
        };
    }
}
