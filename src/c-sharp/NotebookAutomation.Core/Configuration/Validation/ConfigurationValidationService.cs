// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.IO;

using Microsoft.Extensions.Logging;

namespace NotebookAutomation.Core.Configuration.Validation;

/// <summary>
/// Validates configuration files and related resource paths.
/// </summary>
/// <param name="config">The application configuration.</param>
/// <param name="fileSystem">File system abstraction for path checks.</param>
/// <param name="logger">Logger instance.</param>
public class ConfigurationValidationService(
    AppConfig config,
    IFileSystemWrapper fileSystem,
    ILogger<ConfigurationValidationService> logger) : IConfigurationValidationService
{
    private readonly AppConfig _config = config ?? throw new ArgumentNullException(nameof(config));
    private readonly IFileSystemWrapper _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    private readonly ILogger<ConfigurationValidationService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public Task<ConfigurationValidationResult> ValidateAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var issues = new List<ConfigurationValidationIssue>();

        string? configPath = ResolveAbsolutePath(_config.ConfigFilePath, null);
        if (!string.IsNullOrWhiteSpace(_config.ConfigFilePath))
        {
            if (configPath is null || !_fileSystem.FileExists(configPath))
            {
                issues.Add(new ConfigurationValidationIssue(
                    ConfigurationValidationIssueSeverity.Error,
                    nameof(AppConfig.ConfigFilePath),
                    $"Configuration file path '{_config.ConfigFilePath}' was not found.",
                    "Update ConfigFilePath in config.json or specify --config with a valid location."));
            }
        }

        var configDirectory = TryGetDirectory(configPath);

        // Validate metadata schema file
        ValidateFilePath(
            issues,
            _config.Paths.MetadataSchemaFile,
            configDirectory,
            "paths.metadata_schema_file",
            "Metadata schema file is required for metadata validation.");

        // Validate prompts directory when configured
        if (!string.IsNullOrWhiteSpace(_config.Paths.PromptsPath))
        {
            var promptsDir = ResolveAbsolutePath(_config.Paths.PromptsPath, configDirectory);
            if (promptsDir is null || !_fileSystem.DirectoryExists(promptsDir))
            {
                issues.Add(new ConfigurationValidationIssue(
                    ConfigurationValidationIssueSeverity.Error,
                    "paths.prompts_path",
                    $"Prompts directory '{_config.Paths.PromptsPath}' was not found.",
                    "Confirm prompts_path in config.json points to a valid directory."));
            }
        }

        // Validate base block template filename (can be relative to prompts or config directory)
        if (!string.IsNullOrWhiteSpace(_config.Paths.BaseBlockTemplateFilename))
        {
            var templatePath = ResolveTemplatePath(configDirectory);
            if (templatePath is null || !_fileSystem.FileExists(templatePath))
            {
                issues.Add(new ConfigurationValidationIssue(
                    ConfigurationValidationIssueSeverity.Error,
                    "paths.base_block_template_filename",
                    $"Base block template '{_config.Paths.BaseBlockTemplateFilename}' was not found.",
                    "Ensure the template file exists or update base_block_template_filename."));
            }
        }

        // Validate logging directory (must exist)
        if (!string.IsNullOrWhiteSpace(_config.Paths.LoggingDir))
        {
            var loggingDir = ResolveAbsolutePath(_config.Paths.LoggingDir, configDirectory);
            if (loggingDir is null || !_fileSystem.DirectoryExists(loggingDir))
            {
                issues.Add(new ConfigurationValidationIssue(
                    ConfigurationValidationIssueSeverity.Error,
                    "paths.logging_dir",
                    $"Logging directory '{_config.Paths.LoggingDir}' does not exist.",
                    "Create the directory or update logging_dir in config.json."));
            }
        }

        // Validate vault root
        ValidateDirectory(
            issues,
            _config.Paths.NotebookVaultFullpathRoot,
            configDirectory,
            "paths.notebook_vault_fullpath_root",
            "Vault root directory is required to locate markdown content.");

        // Validate OneDrive root
        ValidateDirectory(
            issues,
            _config.Paths.OnedriveFullpathRoot,
            configDirectory,
            "paths.onedrive_fullpath_root",
            "OneDrive root directory is required for resource synchronization.");

        // Effective paths (vault / OneDrive + base path)
        var effectiveVaultRoot = _config.Paths.GetEffectiveVaultRoot();
        if (!string.IsNullOrWhiteSpace(effectiveVaultRoot) && !_fileSystem.DirectoryExists(effectiveVaultRoot))
        {
            issues.Add(new ConfigurationValidationIssue(
                ConfigurationValidationIssueSeverity.Error,
                "paths.notebook_vault_resources_basepath",
                $"Effective vault resources path '{effectiveVaultRoot}' does not exist.",
                "Verify notebook_vault_fullpath_root and notebook_vault_resources_basepath."));
        }

        var effectiveOneDriveRoot = _config.Paths.GetEffectiveOneDriveRoot();
        if (!string.IsNullOrWhiteSpace(effectiveOneDriveRoot) && !_fileSystem.DirectoryExists(effectiveOneDriveRoot))
        {
            issues.Add(new ConfigurationValidationIssue(
                ConfigurationValidationIssueSeverity.Warning,
                "paths.onedrive_resources_basepath",
                $"Effective OneDrive resources path '{effectiveOneDriveRoot}' does not exist.",
                "Ensure OneDrive resources have been synced locally or update onedrive settings."));
        }

        foreach (var issue in issues)
        {
            if (issue.Severity == ConfigurationValidationIssueSeverity.Error)
            {
                _logger.LogError("Configuration validation: {Key} - {Message}", issue.Key, issue.Message);
            }
            else
            {
                _logger.LogWarning("Configuration validation warning: {Key} - {Message}", issue.Key, issue.Message);
            }
        }

        ConfigurationValidationResult result = issues.Count == 0
            ? ConfigurationValidationResult.Success(configPath)
            : ConfigurationValidationResult.FromIssues(issues, configPath);

        if (result.IsValid)
        {
            _logger.LogInformation("Configuration validation succeeded{Suffix}.",
                result.HasWarnings ? " with warnings" : string.Empty);
        }
        else
        {
            _logger.LogError("Configuration validation failed with {ErrorCount} errors and {WarningCount} warnings.",
                result.Issues.Count(i => i.Severity == ConfigurationValidationIssueSeverity.Error),
                result.Issues.Count(i => i.Severity == ConfigurationValidationIssueSeverity.Warning));
        }

        return Task.FromResult(result);
    }

    /// <summary>
    /// Validates that the provided file path exists and adds an error issue when it cannot be located.
    /// </summary>
    /// <param name="issues">Collection to append validation issues to.</param>
    /// <param name="pathValue">The configured file path value.</param>
    /// <param name="baseDirectory">The base directory used to resolve relative paths.</param>
    /// <param name="key">Configuration key associated with the value.</param>
    /// <param name="missingMessage">Message to emit when the value is missing entirely.</param>
    private void ValidateFilePath(
        ICollection<ConfigurationValidationIssue> issues,
        string? pathValue,
        string? baseDirectory,
        string key,
        string missingMessage)
    {
        if (string.IsNullOrWhiteSpace(pathValue))
        {
            issues.Add(new ConfigurationValidationIssue(
                ConfigurationValidationIssueSeverity.Error,
                key,
                missingMessage,
                "Populate the value in config.json with an absolute or relative path."));
            return;
        }

        var absolutePath = ResolveAbsolutePath(pathValue, baseDirectory);
        if (absolutePath is null || !_fileSystem.FileExists(absolutePath))
        {
            issues.Add(new ConfigurationValidationIssue(
                ConfigurationValidationIssueSeverity.Error,
                key,
                $"File '{pathValue}' was not found.",
                "Confirm the file exists and update the configuration value."));
        }
    }

    /// <summary>
    /// Validates that the provided directory path exists and adds an error issue when it cannot be located.
    /// </summary>
    /// <param name="issues">Collection to append validation issues to.</param>
    /// <param name="pathValue">The configured directory value.</param>
    /// <param name="baseDirectory">The base directory used to resolve relative paths.</param>
    /// <param name="key">Configuration key associated with the value.</param>
    /// <param name="missingMessage">Message to emit when the value is missing entirely.</param>
    private void ValidateDirectory(
        ICollection<ConfigurationValidationIssue> issues,
        string? pathValue,
        string? baseDirectory,
        string key,
        string missingMessage)
    {
        if (string.IsNullOrWhiteSpace(pathValue))
        {
            issues.Add(new ConfigurationValidationIssue(
                ConfigurationValidationIssueSeverity.Error,
                key,
                missingMessage,
                "Provide an absolute path in config.json."));
            return;
        }

        var absolutePath = ResolveAbsolutePath(pathValue, baseDirectory);
        if (absolutePath is null || !_fileSystem.DirectoryExists(absolutePath))
        {
            issues.Add(new ConfigurationValidationIssue(
                ConfigurationValidationIssueSeverity.Error,
                key,
                $"Directory '{pathValue}' was not found.",
                "Ensure the directory exists or update the configuration."));
        }
    }

    /// <summary>
    /// Resolves the full path to the base block template by probing prompts and configuration directories.
    /// </summary>
    /// <param name="configDirectory">The directory containing the primary configuration file.</param>
    /// <returns>The normalized template path, or <c>null</c> when it cannot be resolved.</returns>
    private string? ResolveTemplatePath(string? configDirectory)
    {
        var templateValue = _config.Paths.BaseBlockTemplateFilename;
        if (string.IsNullOrWhiteSpace(templateValue))
        {
            return null;
        }

        if (Path.IsPathRooted(templateValue))
        {
            return NormalizePath(templateValue);
        }

        if (!string.IsNullOrWhiteSpace(_config.Paths.PromptsPath))
        {
            var promptsDir = ResolveAbsolutePath(_config.Paths.PromptsPath, configDirectory);
            if (!string.IsNullOrWhiteSpace(promptsDir))
            {
                return NormalizePath(Path.Combine(promptsDir, templateValue));
            }
        }

        return configDirectory is null
            ? NormalizePath(templateValue)
            : NormalizePath(Path.Combine(configDirectory, templateValue));
    }

    /// <summary>
    /// Resolves the supplied path to an absolute location using the provided base directory as a fallback.
    /// </summary>
    /// <param name="pathValue">The raw configuration value to resolve.</param>
    /// <param name="baseDirectory">The directory used when <paramref name="pathValue"/> is relative.</param>
    /// <returns>A normalized absolute path, or <c>null</c> when unable to resolve.</returns>
    private string? ResolveAbsolutePath(string? pathValue, string? baseDirectory)
    {
        if (string.IsNullOrWhiteSpace(pathValue))
        {
            return null;
        }

        if (Path.IsPathRooted(pathValue))
        {
            return NormalizePath(pathValue);
        }

        var baseDir = baseDirectory;
        if (string.IsNullOrWhiteSpace(baseDir))
        {
            try
            {
                baseDir = Directory.GetCurrentDirectory();
            }
            catch
            {
                baseDir = null;
            }
        }

        if (string.IsNullOrWhiteSpace(baseDir))
        {
            return NormalizePath(pathValue);
        }

        try
        {
            return NormalizePath(Path.Combine(baseDir, pathValue));
        }
        catch
        {
            return NormalizePath(pathValue);
        }
    }

    /// <summary>
    /// Attempts to extract the directory component of the provided file path.
    /// </summary>
    /// <param name="path">The source path.</param>
    /// <returns>The directory portion, or <c>null</c> when unavailable.</returns>
    private static string? TryGetDirectory(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        try
        {
            return Path.GetDirectoryName(path);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Normalizes a path to its full-path representation, falling back to the original value on failure.
    /// </summary>
    /// <param name="value">The input path value.</param>
    /// <returns>A normalized path, or <c>null</c> when the value is empty.</returns>
    private static string? NormalizePath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        try
        {
            return Path.GetFullPath(value);
        }
        catch
        {
            return value;
        }
    }
}
