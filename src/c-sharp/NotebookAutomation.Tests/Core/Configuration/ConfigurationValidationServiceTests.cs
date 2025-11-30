// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Linq;

using Microsoft.Extensions.Logging.Abstractions;

using NotebookAutomation.Core.Configuration;
using NotebookAutomation.Core.Configuration.Validation;

namespace NotebookAutomation.Tests.Core.Configuration;

/// <summary>
/// Unit tests covering the behaviour of <see cref="ConfigurationValidationService"/>.
/// </summary>
[TestClass]
public class ConfigurationValidationServiceTests
{
    /// <summary>
    /// Ensures validation succeeds when all configured paths exist.
    /// </summary>
    [TestMethod]
    public async Task ValidateAsync_WithAllPathsPresent_ReturnsSuccess()
    {
        // Arrange
        var paths = CreateDefaultPaths();
        var fileSystem = CreateFileSystem(paths, includeOneDriveResources: true, includeMetadataFile: true, includeTemplate: true);
        var config = CreateAppConfig(paths);
        var service = new ConfigurationValidationService(config, fileSystem, NullLogger<ConfigurationValidationService>.Instance);

        // Act
        var result = await service.ValidateAsync();

        // Assert
        Assert.IsTrue(result.IsValid, "Expected configuration validation to succeed when all resources exist.");
        Assert.IsFalse(result.HasWarnings, "Unexpected warnings when all paths are present.");
    }

    /// <summary>
    /// Ensures missing metadata schema is surfaced as an error condition.
    /// </summary>
    [TestMethod]
    public async Task ValidateAsync_MissingMetadataFile_ReportsError()
    {
        // Arrange
        var paths = CreateDefaultPaths();
        var fileSystem = CreateFileSystem(paths, includeOneDriveResources: true, includeMetadataFile: false, includeTemplate: true);
        var config = CreateAppConfig(paths);
        var service = new ConfigurationValidationService(config, fileSystem, NullLogger<ConfigurationValidationService>.Instance);

        // Act
        var result = await service.ValidateAsync();

        // Assert
        Assert.IsFalse(result.IsValid, "Expected validation to fail when metadata schema file is missing.");
        Assert.IsTrue(result.Issues.Any(issue => issue.Key == "paths.metadata_schema_file" && issue.Severity == ConfigurationValidationIssueSeverity.Error));
    }

    /// <summary>
    /// Ensures missing optional OneDrive resources path only yields a warning.
    /// </summary>
    [TestMethod]
    public async Task ValidateAsync_MissingOneDriveResources_ReportsWarning()
    {
        // Arrange
        var paths = CreateDefaultPaths();
        var fileSystem = CreateFileSystem(paths, includeOneDriveResources: false, includeMetadataFile: true, includeTemplate: true);
        var config = CreateAppConfig(paths);
        var service = new ConfigurationValidationService(config, fileSystem, NullLogger<ConfigurationValidationService>.Instance);

        // Act
        var result = await service.ValidateAsync();

        // Assert
        Assert.IsTrue(result.IsValid, "Expected validation to remain valid when only warnings are present.");
        Assert.IsTrue(result.HasWarnings, "Expected warnings when OneDrive resources are missing.");
        Assert.IsTrue(result.Issues.Any(issue => issue.Key == "paths.onedrive_resources_basepath" && issue.Severity == ConfigurationValidationIssueSeverity.Warning));
    }

    /// <summary>
    /// Creates a set of standard, consistent test paths used across scenarios.
    /// </summary>
    /// <returns>A tuple describing all common path locations.</returns>
    private static (string Root, string ConfigFile, string Metadata, string LoggingDir, string PromptsDir, string TemplateFile, string VaultRoot, string VaultResources, string OneDriveRoot, string OneDriveResources) CreateDefaultPaths()
    {
        var root = Path.Combine(Path.GetTempPath(), "NotebookAutomation", "ValidationTests");
        var configFile = Path.Combine(root, "config", "config.json");
        var metadata = Path.Combine(root, "config", "metadata-schema.yml");
        var loggingDir = Path.Combine(root, "logs");
        var promptsDir = Path.Combine(root, "prompts");
        var templateFile = Path.Combine(promptsDir, "BaseBlockTemplate.yml");
        var vaultRoot = Path.Combine(root, "vault");
        var vaultResources = Path.Combine(vaultRoot, "Courses");
        var oneDriveRoot = Path.Combine(root, "onedrive");
        var oneDriveResources = Path.Combine(oneDriveRoot, "Resources");

        return (root, configFile, metadata, loggingDir, promptsDir, templateFile, vaultRoot, vaultResources, oneDriveRoot, oneDriveResources);
    }

    /// <summary>
    /// Creates an <see cref="AppConfig"/> instance pre-populated with the provided paths.
    /// </summary>
    /// <param name="paths">The tuple of test paths.</param>
    /// <returns>A fully configured <see cref="AppConfig"/>.</returns>
    private static AppConfig CreateAppConfig((string Root, string ConfigFile, string Metadata, string LoggingDir, string PromptsDir, string TemplateFile, string VaultRoot, string VaultResources, string OneDriveRoot, string OneDriveResources) paths)
    {
        return new AppConfig
        {
            ConfigFilePath = paths.ConfigFile,
            Paths = new PathsConfig
            {
                MetadataSchemaFile = paths.Metadata,
                LoggingDir = paths.LoggingDir,
                PromptsPath = paths.PromptsDir,
                BaseBlockTemplateFilename = Path.GetFileName(paths.TemplateFile),
                NotebookVaultFullpathRoot = paths.VaultRoot,
                NotebookVaultResourcesBasepath = Path.GetFileName(paths.VaultResources),
                OnedriveFullpathRoot = paths.OneDriveRoot,
                OnedriveResourcesBasepath = Path.GetFileName(paths.OneDriveResources)
            }
        };
    }

    /// <summary>
    /// Constructs a stub file system wrapper containing the requested files and directories.
    /// </summary>
    /// <param name="paths">The tuple of test paths.</param>
    /// <param name="includeOneDriveResources">Indicates whether OneDrive resources should exist.</param>
    /// <param name="includeMetadataFile">Indicates whether the metadata file should exist.</param>
    /// <param name="includeTemplate">Indicates whether the base template should exist.</param>
    /// <returns>A file-system stub reflecting the desired state.</returns>
    private static StubFileSystemWrapper CreateFileSystem(
        (string Root, string ConfigFile, string Metadata, string LoggingDir, string PromptsDir, string TemplateFile, string VaultRoot, string VaultResources, string OneDriveRoot, string OneDriveResources) paths,
        bool includeOneDriveResources,
        bool includeMetadataFile,
        bool includeTemplate)
    {
        var files = new List<string> { paths.ConfigFile };
        if (includeMetadataFile)
        {
            files.Add(paths.Metadata);
        }

        if (includeTemplate)
        {
            files.Add(paths.TemplateFile);
        }

        var directories = new List<string>
        {
            paths.LoggingDir,
            paths.PromptsDir,
            paths.VaultRoot,
            paths.VaultResources,
            paths.OneDriveRoot
        };

        if (includeOneDriveResources)
        {
            directories.Add(paths.OneDriveResources);
        }

        return new StubFileSystemWrapper(files, directories);
    }

    /// <summary>
    /// Simple file system stub that tracks a known set of files and directories.
    /// </summary>
    private sealed class StubFileSystemWrapper : IFileSystemWrapper
    {
        private readonly HashSet<string> _files;
        private readonly HashSet<string> _directories;

        /// <summary>
        /// Initializes a new instance of the <see cref="StubFileSystemWrapper"/> class.
        /// </summary>
        /// <param name="files">Collection of files that should appear to exist.</param>
        /// <param name="directories">Collection of directories that should appear to exist.</param>
        public StubFileSystemWrapper(IEnumerable<string> files, IEnumerable<string> directories)
        {
            _files = new HashSet<string>(files.Select(Normalize), StringComparer.OrdinalIgnoreCase);
            _directories = new HashSet<string>(directories.Select(Normalize), StringComparer.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public bool FileExists(string path) => _files.Contains(Normalize(path));

        /// <inheritdoc />
        public bool DirectoryExists(string path) => _directories.Contains(Normalize(path));

        /// <inheritdoc />
        public Task<string> ReadAllTextAsync(string path) => Task.FromResult(string.Empty);

        /// <inheritdoc />
        public Task WriteAllTextAsync(string path, string content) => Task.CompletedTask;

        /// <inheritdoc />
        public string CombinePath(params string[] paths) => Path.Combine(paths);

        /// <inheritdoc />
        public string? GetDirectoryName(string path) => Path.GetDirectoryName(path);

        /// <inheritdoc />
        public string GetFullPath(string path) => Normalize(path);

        /// <summary>
        /// Normalizes a path to the environment full path using <see cref="Path.GetFullPath(string)"/>.
        /// </summary>
        /// <param name="path">Path to normalize.</param>
        /// <returns>Normalized path string.</returns>
        private static string Normalize(string path) => Path.GetFullPath(path);
    }
}
