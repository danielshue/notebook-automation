// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Tools.Resolvers;

/// <summary>
/// Resolves a OneDrive anonymous view share-link for a given file path.
/// </summary>
/// <remarks>
/// <para>
/// Uses <see cref="IOneDriveService"/> to create (or retrieve) a shareable link for the specified file and
/// returns a service-normalized, canonical URL. This resolver is intended to be mandatory in the schema for
/// assets that must always carry a share-link. If OneDrive is unavailable or the operation fails, the
/// resolver returns <see cref="string.Empty"/> so pipeline precedence can still merge overrides or existing
/// frontmatter.
/// </para>
/// <para>
/// <b>Required Context Parameters:</b>
/// <list type="bullet">
/// <item><description><c>filePath</c> (string): Absolute path to the file that should be shared</description></item>
/// <item><description><c>_internal_path</c> (string, optional): Alternate key accepted by some callers</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Precedence:</b> The <see cref="IMetadataPipeline"/> merges values in this order (lowest to highest):
/// schema defaults, resolver-derived values, content/frontmatter, explicit overrides. Returning
/// <see langword="null"/> when context is missing allows higher-precedence sources to take effect; returning
/// <see cref="string.Empty"/> indicates "not available" while still satisfying schema requirements.
/// </para>
/// <para>
/// <b>Behavior and Notes:</b>
/// <list type="bullet">
/// <item><description>Returns <see langword="null"/> when <c>filePath</c> is missing, enabling precedence to elevate frontmatter/overrides.</description></item>
/// <item><description>Returns <see cref="string.Empty"/> on service errors; callers should treat empty as "not available".</description></item>
/// <item><description>Blocks to wait for the async OneDrive call; call from non-UI threads to avoid deadlocks.</description></item>
/// <item><description>Link normalization (e.g., removing tracking query params) is handled by <see cref="IOneDriveService"/>.</description></item>
/// </list>
/// </para>
/// <example>
/// <code><![CDATA[
/// var ctx = new Dictionary<string, object> { ["filePath"] = @"D:\\vault\\res\\file.mp4" };
/// var value = (string?)resolver.Resolve("share-link", ctx);
/// // value is a normalized OneDrive URL or "" on failure
/// ]]></code>
/// </example>
/// </remarks>
public class OneDriveShareLinkResolver(ILogger<OneDriveShareLinkResolver> logger, IOneDriveService oneDrive) : IFieldValueResolver
{
    private readonly ILogger<OneDriveShareLinkResolver> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IOneDriveService _oneDrive = oneDrive ?? throw new ArgumentNullException(nameof(oneDrive));

    /// <summary>
    /// Resolves or creates a OneDrive share-link for the provided file.
    /// </summary>
    /// <param name="fieldName">The field name to resolve (typically <c>"share-link"</c> or equivalent schema field).</param>
    /// <param name="context">Resolver context containing <c>filePath</c> as an absolute path.</param>
    /// <returns>
    /// A normalized OneDrive share-link URL as a string; <see langword="null"/> if <c>filePath</c> is missing;
    /// <see cref="string.Empty"/> on service failures.
    /// </returns>
    /// <remarks>
    /// Synchronously waits on <see cref="IOneDriveService.CreateShareLinkAsync(string)"/>. Exceptions are caught and logged at Warning level.
    /// </remarks>
    public object? Resolve(string fieldName, Dictionary<string, object>? context = null)
    {
        try
        {
            if (context == null || !context.TryGetValue("filePath", out var value) || value is not string path || string.IsNullOrWhiteSpace(path))
            {
                _logger.LogDebug("{Resolver}: filePath missing from context; returning null", nameof(OneDriveShareLinkResolver));
                return null;
            }

            // Check if OneDrive share link resolution should be skipped for performance
            if (context.TryGetValue("skip_onedrive_share_link", out var skipValue) && skipValue is true)
            {
                _logger.LogDebug("{Resolver}: skipping OneDrive share link resolution due to context flag", nameof(OneDriveShareLinkResolver));
                return string.Empty;
            }

            // Synchronously wait for async call in resolver context; callers should avoid deadlocks by running off the UI thread.
            var task = _oneDrive.CreateShareLinkAsync(path);
            task.Wait();
            var link = task.Result;
            _logger.LogDebug("Resolved share link for {Path}: {HasLink}", path, !string.IsNullOrEmpty(link));
            // Map schema field name 'share-link' to canonical key used elsewhere 'onedrive-shared-link'
            return link ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve OneDrive share link");
            return string.Empty;
        }
    }
}
