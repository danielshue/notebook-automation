// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Core.Utils;

/// <summary>
/// Provides path resolution functionality for input and output locations based on PathResolutionConfig.
/// </summary>
/// <remarks>
/// <para>
/// The PathResolver class translates logical path root identifiers (e.g., "onedrive", "vault", "cwd", "input")
/// into actual file system paths based on application configuration. This enables flexible path resolution
/// for different processing scenarios and template types.
/// </para>
/// <para>
/// Supported root identifiers:
/// <list type="bullet">
///   <item><description>"onedrive" - OneDrive base path from configuration</description></item>
///   <item><description>"vault" - Notebook vault resources base path from configuration</description></item>
///   <item><description>"cwd" - Current working directory</description></item>
///   <item><description>"input" - Same directory as the input file (OutputRoot only)</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var resolver = new PathResolver(config);
/// var inputRoot = resolver.ResolveInputRoot(pathConfig);
/// var outputRoot = resolver.ResolveOutputRoot(pathConfig, inputFilePath);
/// </code>
/// </example>
public class PathResolver
{
    private readonly Configuration.AppConfig config;

    /// <summary>
    /// Initializes a new instance of the <see cref="PathResolver"/> class.
    /// </summary>
    /// <param name="config">The application configuration containing path settings.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="config"/> is null.</exception>
    public PathResolver(Configuration.AppConfig config)
    {
        this.config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>
    /// Resolves the input root path based on the path resolution configuration.
    /// </summary>
    /// <param name="pathConfig">The path resolution configuration. If null, uses "onedrive" as default.</param>
    /// <returns>The resolved input root path.</returns>
    /// <remarks>
    /// <para>
    /// Resolution mapping:
    /// <list type="bullet">
    ///   <item><description>"onedrive" → OneDriveBasePath from configuration</description></item>
    ///   <item><description>"vault" → NotebookVaultResourcesBasePath from configuration</description></item>
    ///   <item><description>"cwd" → Current working directory</description></item>
    ///   <item><description>Default (null or unknown) → OneDriveBasePath from configuration</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var pathConfig = new PathResolutionConfig { InputRoot = "vault" };
    /// var inputRoot = resolver.ResolveInputRoot(pathConfig);
    /// // Returns the vault path from configuration
    /// </code>
    /// </example>
    public string ResolveInputRoot(Tools.PathResolutionConfig? pathConfig)
    {
        var inputRoot = pathConfig?.InputRoot ?? "onedrive";
        return inputRoot.ToLowerInvariant() switch
        {
            "onedrive" => config.Paths.GetEffectiveOneDriveRoot(),
            "vault" => config.Paths.GetEffectiveVaultRoot(),
            "cwd" => Directory.GetCurrentDirectory(),
            _ => config.Paths.GetEffectiveOneDriveRoot()
        };
    }

    /// <summary>
    /// Resolves the output root path based on the path resolution configuration.
    /// </summary>
    /// <param name="pathConfig">The path resolution configuration. If null, uses "vault" as default.</param>
    /// <param name="inputFilePath">The input file path, used when OutputRoot is "input".</param>
    /// <returns>The resolved output root path.</returns>
    /// <remarks>
    /// <para>
    /// Resolution mapping:
    /// <list type="bullet">
    ///   <item><description>"vault" → NotebookVaultResourcesBasePath from configuration</description></item>
    ///   <item><description>"onedrive" → OneDriveBasePath from configuration</description></item>
    ///   <item><description>"cwd" → Current working directory</description></item>
    ///   <item><description>"input" → Directory containing the input file</description></item>
    ///   <item><description>Default (null or unknown) → NotebookVaultResourcesBasePath from configuration</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var pathConfig = new PathResolutionConfig { OutputRoot = "input" };
    /// var outputRoot = resolver.ResolveOutputRoot(pathConfig, "C:\\Documents\\file.pdf");
    /// // Returns "C:\\Documents"
    /// </code>
    /// </example>
    public string ResolveOutputRoot(Tools.PathResolutionConfig? pathConfig, string inputFilePath)
    {
        var outputRoot = pathConfig?.OutputRoot ?? "vault";
        return outputRoot.ToLowerInvariant() switch
        {
            "vault" => config.Paths.GetEffectiveVaultRoot(),
            "onedrive" => config.Paths.GetEffectiveOneDriveRoot(),
            "cwd" => Directory.GetCurrentDirectory(),
            "input" => GetInputDirectory(inputFilePath),
            _ => config.Paths.GetEffectiveVaultRoot()
        };
    }

    private static string GetInputDirectory(string inputFilePath)
    {
        var directory = Path.GetDirectoryName(inputFilePath);
        // Path.GetDirectoryName returns empty string or null for filenames without directory
        return string.IsNullOrEmpty(directory) ? Directory.GetCurrentDirectory() : directory;
    }
}
