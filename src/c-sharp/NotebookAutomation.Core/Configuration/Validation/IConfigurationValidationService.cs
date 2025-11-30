// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Core.Configuration.Validation;

/// <summary>
/// Provides methods for validating configuration files and related resources.
/// </summary>
public interface IConfigurationValidationService
{
    /// <summary>
    /// Validates the current application configuration.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A <see cref="ConfigurationValidationResult"/> describing validation findings.</returns>
    Task<ConfigurationValidationResult> ValidateAsync(CancellationToken cancellationToken = default);
}
