// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using NotebookAutomation.Core.Tools;
using NotebookAutomation.Core.Tools.Resolvers;

namespace NotebookAutomation.Tests.Core.Helpers;

/// <summary>
/// Helper class for creating MetadataSchemaLoader instances for tests.
/// </summary>
internal static class MetadataSchemaLoaderHelper
{
    /// <summary>
    /// Creates a MetadataSchemaLoader instance for testing using the test metadata-schema.yml file.
    /// </summary>
    /// <param name="logger">Optional logger instance. If null, a NullLogger will be used.</param>
    /// <returns>A MetadataSchemaLoader instance configured for testing.</returns>
    public static MetadataSchemaLoader CreateTestMetadataSchemaLoader(ILogger<MetadataSchemaLoader>? logger = null)
    {
        // Back-compat overload: no AppConfig provided; delegate to new overload with null
        return CreateTestMetadataSchemaLoader(appConfig: null, logger);
    }

    /// <summary>
    /// Creates a MetadataSchemaLoader instance for testing using the schema path from AppConfig when provided.
    /// </summary>
    /// <param name="appConfig">Optional AppConfig containing configured Paths.MetadataSchemaFile. In unit tests, pass a mocked AppConfig with this value set.</param>
    /// <param name="logger">Optional logger instance. If null, a NullLogger will be used.</param>
    /// <returns>A MetadataSchemaLoader instance configured for testing.</returns>
    public static MetadataSchemaLoader CreateTestMetadataSchemaLoader(AppConfig? appConfig, ILogger<MetadataSchemaLoader>? logger = null)
    {
        logger ??= NullLogger<MetadataSchemaLoader>.Instance;

        // Prefer schema path from AppConfig when available (tests can mock this)
        string? configuredPath = appConfig?.Paths?.MetadataSchemaFile;
        string schemaPathToUse;

        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            try
            {
                // Normalize to absolute path if necessary
                schemaPathToUse = Path.IsPathRooted(configuredPath!)
                  ? Path.GetFullPath(configuredPath!)
                  : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredPath!));

                if (!File.Exists(schemaPathToUse))
                {
                    // Try to locate repo-level schema before falling back to minimal
                    schemaPathToUse = TryLocateRepositorySchema() ?? CreateMinimalTestSchemaFile();
                }
            }
            catch
            {
                // Any issues resolving the configured path -> try repo schema, then minimal
                schemaPathToUse = TryLocateRepositorySchema() ?? CreateMinimalTestSchemaFile();
            }
        }
        else
        {
            // No configured path provided; attempt to locate repository schema first
            schemaPathToUse = TryLocateRepositorySchema() ?? CreateMinimalTestSchemaFile();
        }

        return new MetadataSchemaLoader(schemaPathToUse, logger);
    }

    /// <summary>
    /// Attempts to locate the repository's metadata-schema.yml by searching common roots.
    /// </summary>
    /// <returns>Absolute file path if found; otherwise null.</returns>
    private static string? TryLocateRepositorySchema()
    {
        // Common candidates relative to test binaries and working dir
        var candidates = new List<string?>
    {
      // Running from test bin/Release/netX.Y - walk up to repo root then config/
      Path.Combine(AppContext.BaseDirectory ?? string.Empty, "..", "..", "..", "..", "..", "config", "metadata-schema.yml"),
      Path.Combine(AppContext.BaseDirectory ?? string.Empty, "..", "..", "..", "config", "metadata-schema.yml"),
      // Current directory (when tests run from solution root)
      Path.Combine(Directory.GetCurrentDirectory(), "config", "metadata-schema.yml"),
    };

        foreach (var path in candidates)
        {
            try
            {
                if (path == null) continue;
                var full = Path.GetFullPath(path);
                if (File.Exists(full))
                {
                    return full;
                }
            }
            catch
            {
                // ignore and continue
            }
        }

        // Last resort: traverse upwards from current dir to find a config/metadata-schema.yml
        try
        {
            var dir = Directory.GetCurrentDirectory();
            for (int i = 0; i < 6 && !string.IsNullOrEmpty(dir); i++)
            {
                var candidate = Path.Combine(dir, "config", "metadata-schema.yml");
                if (File.Exists(candidate)) return Path.GetFullPath(candidate);
                dir = Directory.GetParent(dir)?.FullName ?? string.Empty;
            }
        }
        catch { }

        return null;
    }

    /// <summary>
    /// Creates a MetadataTemplateManager instance for testing using the schema loader.
    /// </summary>
    /// <param name="logger">Optional logger instance. If null, a NullLogger will be used.</param>
    /// <param name="schemaLoader">Optional schema loader instance. If null, a test schema loader will be created.</param>
    /// <returns>A MetadataTemplateManager instance configured for testing.</returns>
    public static MetadataTemplateManager CreateTestMetadataTemplateManager(
        ILogger<MetadataTemplateManager>? logger = null,
    IMetadataSchemaLoader? schemaLoader = null,
    IOneDriveService? oneDriveService = null,
  IMetadataHierarchyDetector? hierarchyDetector = null,
  AppConfig? appConfig = null)
    {
        logger ??= NullLogger<MetadataTemplateManager>.Instance;
        // Use AppConfig-provided schema path when available; tests can mock this value
        var loggingFactory = LoggerFactory.Create(builder => { });
        schemaLoader ??= CreateTestMetadataSchemaLoader(appConfig, loggingFactory.CreateLogger<MetadataSchemaLoader>());
        // Wire up default resolvers similar to production registration so tests get realistic behavior
        var templateManager = new MetadataTemplateManager(logger, schemaLoader);

        try
        {
            var registry = schemaLoader.ResolverRegistry;
            registry.Register("DateCreatedResolver", new DateCreatedResolver(loggingFactory.CreateLogger<DateCreatedResolver>()));
            registry.Register("VideoDurationResolver", new VideoDurationResolver(loggingFactory.CreateLogger<VideoDurationResolver>()));
            registry.Register("PdfPageCountResolver", new PdfPageCountResolver(loggingFactory.CreateLogger<PdfPageCountResolver>()));
            registry.Register("TitleResolver", new TitleResolver(loggingFactory.CreateLogger<TitleResolver>()));

            if (hierarchyDetector != null)
            {
                // Use overloads that accept AppConfig when provided to match production registration
                if (appConfig == null) appConfig = new AppConfig();
                registry.Register("ProgramResolver", new ProgramResolver(loggingFactory.CreateLogger<ProgramResolver>(), hierarchyDetector, appConfig));
                registry.Register("CourseResolver", new CourseResolver(loggingFactory.CreateLogger<CourseResolver>(), hierarchyDetector, appConfig));
                registry.Register("ClassResolver", new ClassResolver(loggingFactory.CreateLogger<ClassResolver>(), hierarchyDetector));
                registry.Register("ModuleResolver", new ModuleResolver(loggingFactory.CreateLogger<ModuleResolver>(), hierarchyDetector));
                registry.Register("LessonResolver", new LessonResolver(loggingFactory.CreateLogger<LessonResolver>(), hierarchyDetector));
            }

            if (oneDriveService != null)
            {
                registry.Register("OneDriveShareLinkResolver", new OneDriveShareLinkResolver(loggingFactory.CreateLogger<OneDriveShareLinkResolver>(), oneDriveService));
            }
            else
            {
                registry.Register("OneDriveShareLinkResolver", new OneDriveShareLinkResolverFallback(loggingFactory.CreateLogger<OneDriveShareLinkResolverFallback>()));
            }

            // Register OneDriveRelativePathResolver for tests
            if (appConfig == null) appConfig = new AppConfig();
            registry.Register("OneDriveRelativePathResolver", new OneDriveRelativePathResolver(loggingFactory.CreateLogger<OneDriveRelativePathResolver>(), appConfig));
        }
        catch
        {
            // Tests should not fail if resolver wiring throws; template manager still functions.
        }

        return templateManager;
    }

    /// <summary>
    /// Creates a MetadataHierarchyDetector instance for testing using the schema loader.
    /// </summary>
    /// <param name="logger">Optional logger instance. If null, a NullLogger will be used.</param>
    /// <param name="appConfig">Optional app config instance. If null, a test config will be created.</param>
    /// <param name="schemaLoader">Optional schema loader instance. If null, a test schema loader will be created.</param>
    /// <param name="vaultRootOverride">Optional vault root override for testing.</param>
    /// <returns>A MetadataHierarchyDetector instance configured for testing.</returns>
    public static MetadataHierarchyDetector CreateTestMetadataHierarchyDetector(
        ILogger<MetadataHierarchyDetector>? logger = null,
        AppConfig? appConfig = null,
        IMetadataSchemaLoader? schemaLoader = null,
        string? vaultRootOverride = null)
    {
        logger ??= NullLogger<MetadataHierarchyDetector>.Instance;
        schemaLoader ??= CreateTestMetadataSchemaLoader();

        // If no appConfig provided, create a default one
        if (appConfig == null)
        {
            appConfig = new AppConfig
            {
                Paths = new PathsConfig
                {
                    NotebookVaultFullpathRoot = vaultRootOverride ?? Path.GetTempPath()
                }
            };
        }
        else
        {
            // Ensure the provided appConfig has proper paths configured
            appConfig.Paths ??= new PathsConfig();

            // If NotebookVaultFullpathRoot is empty and no vaultRootOverride, set a default
            if (string.IsNullOrEmpty(appConfig.Paths.NotebookVaultFullpathRoot) && string.IsNullOrEmpty(vaultRootOverride))
            {
                appConfig.Paths.NotebookVaultFullpathRoot = Path.GetTempPath();
            }
        }

        return new MetadataHierarchyDetector(logger, appConfig, schemaLoader, vaultRootOverride);
    }

    /// <summary>
    /// Creates a minimal test schema file for testing when the main schema file is not available.
    /// </summary>
    /// <returns>Path to the created test schema file.</returns>
    private static string CreateMinimalTestSchemaFile()
    {
        var tempSchemaPath = Path.Combine(Path.GetTempPath(), $"test-metadata-schema-{Guid.NewGuid():N}.yaml");

        var minimalSchema = @"TemplateTypes:
  video-reference:
    BaseTypes:
      - universal-fields
    Type: note/video-note
    RequiredFields:
      - status
      - tags
    Fields:
      publisher:
        Default: University
      status:
        Default: unwatched
      date-created:
        Default: ''
      title:
        Default: Video Note
      tags:
        Default: [video, reference]
  pdf-reference:
    BaseTypes:
      - universal-fields
    Type: note/case-study
    RequiredFields:
      - status
      - tags
    Fields:
      publisher:
        Default: University
      status:
        Default: unread
      date-created:
        Default: ''
      title:
        Default: PDF Note
      tags:
        Default: [pdf, reference]
UniversalFields:
  - date-created
  - publisher
TypeMapping:
  video-reference: note/video-note
  pdf-reference: note/case-study
ReservedTags:
  - video
  - pdf
";

        File.WriteAllText(tempSchemaPath, minimalSchema);
        return tempSchemaPath;
    }
}
