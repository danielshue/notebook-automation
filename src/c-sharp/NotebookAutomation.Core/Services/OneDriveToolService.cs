// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

using NotebookAutomation.Core.Configuration;

namespace NotebookAutomation.Core.Services;

/// <summary>
/// Service for OneDrive tool operations.
/// </summary>
/// <remarks>
/// This service wraps <see cref="IOneDriveService"/> to provide a simplified API for Copilot tools.
/// </remarks>
/// <param name="logger">The logger instance.</param>
/// <param name="oneDriveService">The OneDrive service (may be null if not configured).</param>
/// <param name="appConfig">The application configuration.</param>
public class OneDriveToolService(
    ILogger<OneDriveToolService> logger,
    IOneDriveService? oneDriveService,
    AppConfig appConfig) : IOneDriveToolService
{
    private readonly ILogger<OneDriveToolService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IOneDriveService? _oneDriveService = oneDriveService;
    private readonly AppConfig _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));

    /// <inheritdoc />
    public async Task<OneDriveTokenResult> RefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Refreshing OneDrive authentication token");

        if (_oneDriveService == null)
        {
            return new OneDriveTokenResult
            {
                Success = false,
                Message = "OneDrive service is not configured",
                TokenValid = false,
                ErrorMessage = "OneDrive service is not available. Check Microsoft Graph configuration."
            };
        }

        try
        {
            await _oneDriveService.RefreshAuthenticationAsync();

            // Verify the token is valid after refresh
            var isValid = await _oneDriveService.IsTokenValidAsync();

            return new OneDriveTokenResult
            {
                Success = isValid,
                Message = isValid
                    ? "OneDrive token refreshed successfully"
                    : "Token refresh completed but token validation failed",
                TokenValid = isValid,
                ErrorMessage = isValid ? null : "Token validation failed after refresh"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing OneDrive token");
            return new OneDriveTokenResult
            {
                Success = false,
                Message = "Failed to refresh OneDrive token",
                TokenValid = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public async Task<OneDriveStatusResult> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Checking OneDrive status");

        var oneDriveRoot = _appConfig.Paths?.OnedriveFullpathRoot;
        var isConfigured = !string.IsNullOrEmpty(oneDriveRoot) && _oneDriveService != null;

        if (!isConfigured)
        {
            return new OneDriveStatusResult
            {
                IsConfigured = false,
                TokenValid = false,
                Message = "OneDrive is not configured",
                OneDriveRoot = oneDriveRoot,
                ErrorMessage = _oneDriveService == null
                    ? "OneDrive service is not available. Check Microsoft Graph configuration."
                    : "OneDrive root path is not configured."
            };
        }

        try
        {
            var isValid = await _oneDriveService!.IsTokenValidAsync();

            return new OneDriveStatusResult
            {
                IsConfigured = true,
                TokenValid = isValid,
                Message = isValid
                    ? "OneDrive is configured and authenticated"
                    : "OneDrive is configured but authentication token is invalid or expired",
                OneDriveRoot = oneDriveRoot
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking OneDrive status");
            return new OneDriveStatusResult
            {
                IsConfigured = true,
                TokenValid = false,
                Message = "Error checking OneDrive authentication status",
                OneDriveRoot = oneDriveRoot,
                ErrorMessage = ex.Message
            };
        }
    }
}
