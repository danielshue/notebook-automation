// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using NotebookAutomation.Core.Tools;

namespace NotebookAutomation.Tests.Core.Helpers;

/// <summary>
/// Helper class for creating MetadataSchemaLoader instances for tests.
/// </summary>
internal static class MetadataSchemaLoaderHelper
{
    /// <summary>
    /// Creates a MetadataSchemaLoader instance for testing using the test metadata-schema.yaml file.
    /// </summary>
    /// <param name="logger">Optional logger instance. If null, a NullLogger will be used.</param>
    /// <returns>A MetadataSchemaLoader instance configured for testing.</returns>
    public static MetadataSchemaLoader CreateTestMetadataSchemaLoader(ILogger<MetadataSchemaLoader>? logger = null)
    {
        logger ??= NullLogger<MetadataSchemaLoader>.Instance;
        
        // Use the test metadata-schema.yaml file
        var testSchemaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "metadata-schema.yaml");
        
        // If the test schema file doesn't exist, fall back to a minimal schema
        if (!File.Exists(testSchemaPath))
        {
            testSchemaPath = CreateMinimalTestSchemaFile();
        }
        
        return new MetadataSchemaLoader(testSchemaPath, logger);
    }

    /// <summary>
    /// Creates a MetadataTemplateManager instance for testing using the schema loader.
    /// </summary>
    /// <param name="logger">Optional logger instance. If null, a NullLogger will be used.</param>
    /// <param name="schemaLoader">Optional schema loader instance. If null, a test schema loader will be created.</param>
    /// <returns>A MetadataTemplateManager instance configured for testing.</returns>
    public static MetadataTemplateManager CreateTestMetadataTemplateManager(
        ILogger<MetadataTemplateManager>? logger = null,
        IMetadataSchemaLoader? schemaLoader = null)
    {
        logger ??= NullLogger<MetadataTemplateManager>.Instance;
        schemaLoader ??= CreateTestMetadataSchemaLoader();
        
        return new MetadataTemplateManager(logger, schemaLoader);
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
        
        appConfig ??= new AppConfig
        {
            Paths = new PathsConfig
            {
                NotebookVaultFullpathRoot = vaultRootOverride ?? Path.GetTempPath()
            }
        };
        
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
        Default: ''
      tags:
        Default: []
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
        Default: ''
      tags:
        Default: []
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