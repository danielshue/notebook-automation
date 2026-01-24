// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace NotebookAutomation.Cli.Services.Copilot;

/// <summary>
/// Utility to check if GitHub Copilot CLI is installed and authenticated.
/// </summary>
public class CopilotAvailabilityChecker
{
    private readonly ILogger<CopilotAvailabilityChecker> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CopilotAvailabilityChecker"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public CopilotAvailabilityChecker(ILogger<CopilotAvailabilityChecker> logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Check if Copilot CLI is available and authenticated.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Availability result with details.</returns>
    public async Task<CopilotAvailabilityResult> CheckAvailabilityAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if CLI is installed
            var isInstalled = await IsCliInstalledAsync(cancellationToken);
            if (!isInstalled)
            {
                logger.LogDebug("GitHub Copilot CLI is not installed or not in PATH");
                return new CopilotAvailabilityResult(
                    IsAvailable: false,
                    IsCliInstalled: false,
                    IsAuthenticated: false,
                    CliVersion: null,
                    ErrorMessage: "GitHub Copilot CLI is not installed. Install with: 'gh extension install github/gh-copilot'");
            }

            // Get CLI version
            var version = await GetCliVersionAsync(cancellationToken);

            // Check authentication status
            var isAuthenticated = await IsAuthenticatedAsync(cancellationToken);
            if (!isAuthenticated)
            {
                logger.LogDebug("GitHub Copilot CLI is not authenticated");
                return new CopilotAvailabilityResult(
                    IsAvailable: false,
                    IsCliInstalled: true,
                    IsAuthenticated: false,
                    CliVersion: version,
                    ErrorMessage: "GitHub Copilot CLI is not authenticated. Run 'gh auth login' to authenticate.");
            }

            logger.LogInformation("GitHub Copilot CLI is available (version: {Version})", version);
            return new CopilotAvailabilityResult(
                IsAvailable: true,
                IsCliInstalled: true,
                IsAuthenticated: true,
                CliVersion: version,
                ErrorMessage: null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking Copilot availability");
            return new CopilotAvailabilityResult(
                IsAvailable: false,
                IsCliInstalled: false,
                IsAuthenticated: false,
                CliVersion: null,
                ErrorMessage: $"Error checking Copilot availability: {ex.Message}");
        }
    }

    /// <summary>
    /// Check if the Copilot CLI is installed and accessible.
    /// </summary>
    private async Task<bool> IsCliInstalledAsync(CancellationToken cancellationToken)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = GetCopilotCommand(),
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                return false;
            }

            await process.WaitForExitAsync(cancellationToken);
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to check if Copilot CLI is installed");
            return false;
        }
    }

    /// <summary>
    /// Get the version of the installed Copilot CLI.
    /// </summary>
    private async Task<string?> GetCliVersionAsync(CancellationToken cancellationToken)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = GetCopilotCommand(),
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                return null;
            }

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
            {
                // Extract version from output (format may vary)
                return output.Trim().Split('\n')[0];
            }

            return null;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to get Copilot CLI version");
            return null;
        }
    }

    /// <summary>
    /// Check if the user is authenticated with GitHub Copilot.
    /// </summary>
    private async Task<bool> IsAuthenticatedAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Try running a simple Copilot command that requires authentication
            var psi = new ProcessStartInfo
            {
                FileName = GetCopilotCommand(),
                Arguments = "config list",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                return false;
            }

            await process.WaitForExitAsync(cancellationToken);
            
            // If the command succeeds, the user is authenticated
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to check Copilot authentication status");
            return false;
        }
    }

    /// <summary>
    /// Get the Copilot CLI command based on the operating system.
    /// </summary>
    private static string GetCopilotCommand()
    {
        // The Copilot CLI is typically installed as a GitHub CLI extension
        // Command could be 'gh copilot' or just 'copilot' depending on installation
        return OperatingSystem.IsWindows() ? "gh" : "gh";
    }
}
