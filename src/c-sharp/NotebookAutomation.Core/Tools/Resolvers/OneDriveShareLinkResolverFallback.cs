// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Tools.Resolvers;

/// <summary>
/// Fallback resolver used when <see cref="IOneDriveService"/> is unavailable.
/// </summary>
/// <remarks>
/// <para>
/// Returns <see cref="string.Empty"/> for share-link fields to satisfy required-field validation without making any network calls.
/// This enables offline or constrained environments to proceed while allowing explicit overrides to provide a link when available.
/// </para>
/// <para>
/// <b>Context:</b> No required context. Any provided context is ignored.
/// </para>
/// <para>
/// <b>Precedence:</b> The <see cref="IMetadataPipeline"/> merges values in the following order (lowest to highest): schema defaults,
/// resolver-derived values, content/frontmatter, explicit overrides. Returning <see cref="string.Empty"/> ensures the field exists
/// while allowing higher-precedence sources to replace it if available.
/// </para>
/// <example>
/// <code>
/// var value = (string?)new OneDriveShareLinkResolverFallback(logger).Resolve("share-link", null);
/// // value == ""
/// </code>
/// </example>
/// </remarks>
public class OneDriveShareLinkResolverFallback(ILogger<OneDriveShareLinkResolverFallback> logger) : IFieldValueResolver
{
    private readonly ILogger<OneDriveShareLinkResolverFallback> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Returns an empty string for OneDrive share-link fields when the service is not available.
    /// </summary>
    /// <param name="fieldName">The field name to resolve (e.g., <c>"share-link"</c>).</param>
    /// <param name="context">Optional context, ignored by this resolver.</param>
    /// <returns><see cref="string.Empty"/> always.</returns>
    public object? Resolve(string fieldName, Dictionary<string, object>? context = null)
    {
        _logger.LogDebug("{Resolver}: OneDrive service not available, returning empty share-link", nameof(OneDriveShareLinkResolverFallback));
        return string.Empty;
    }
}
