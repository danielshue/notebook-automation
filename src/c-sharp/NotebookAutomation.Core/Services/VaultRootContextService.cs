// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Core.Services;

/// <summary>
/// Provides scoped context for vault root path overrides during processing operations.
/// </summary>
/// <remarks>
/// This service allows the dependency injection container to provide different vault root paths
/// to components that need them, without requiring constructor changes throughout the system.
/// It's particularly useful when processing different vaults than the one configured in AppConfig.
/// </remarks>
public class VaultRootContextService
{
    /// <summary>
    /// Gets or sets the vault root path override.
    /// </summary>
    /// <value>
    /// The vault root path to use instead of the configured path, or null to use the configured path.
    /// </value>
    public string? VaultRootOverride { get; set; }

    /// <summary>
    /// Gets a value indicating whether determines if a vault root override is active.
    /// </summary>
    /// <value>
    /// True if a vault root override is set, false otherwise.
    /// </value>
    public bool HasVaultRootOverride => !string.IsNullOrWhiteSpace(VaultRootOverride);
}
