// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Core.Services;

/// <summary>
/// Service interface for OneDrive operations exposed to Copilot tools.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="IOneDriveToolService"/> provides a simplified API for OneDrive authentication
/// status and token refresh operations. It wraps <see cref="IOneDriveService"/> to provide
/// a Copilot-friendly interface with structured result types.
/// </para>
/// </remarks>
public interface IOneDriveToolService
{
    /// <summary>
    /// Refreshes the OneDrive authentication token.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An <see cref="OneDriveTokenResult"/> indicating success or failure.</returns>
    /// <remarks>
    /// <para>
    /// This operation clears cached tokens and initiates a new authentication flow.
    /// The user may need to complete device code authentication.
    /// </para>
    /// </remarks>
    /// <example>
    /// Copilot tool calls:
    /// <code>
    /// onedrive_refresh_token()  // Refresh authentication token
    /// </code>
    /// </example>
    Task<OneDriveTokenResult> RefreshTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current OneDrive authentication status.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An <see cref="OneDriveStatusResult"/> with authentication status.</returns>
    /// <example>
    /// Copilot tool calls:
    /// <code>
    /// onedrive_status()  // Check if OneDrive is authenticated
    /// </code>
    /// </example>
    Task<OneDriveStatusResult> GetStatusAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a OneDrive token refresh operation.
/// </summary>
public record OneDriveTokenResult
{
    /// <summary>Whether the token refresh succeeded.</summary>
    public bool Success { get; init; }

    /// <summary>Human-readable message about the operation.</summary>
    public required string Message { get; init; }

    /// <summary>Whether the token is now valid.</summary>
    public bool TokenValid { get; init; }

    /// <summary>Error message if refresh failed.</summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Result of a OneDrive status check.
/// </summary>
public record OneDriveStatusResult
{
    /// <summary>Whether OneDrive service is configured.</summary>
    public bool IsConfigured { get; init; }

    /// <summary>Whether the current token is valid.</summary>
    public bool TokenValid { get; init; }

    /// <summary>Human-readable status message.</summary>
    public required string Message { get; init; }

    /// <summary>OneDrive root path if configured.</summary>
    public string? OneDriveRoot { get; init; }

    /// <summary>Error message if status check failed.</summary>
    public string? ErrorMessage { get; init; }
}
