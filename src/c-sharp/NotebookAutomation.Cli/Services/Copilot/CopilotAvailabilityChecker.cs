// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace NotebookAutomation.Cli.Services.Copilot;

/// <summary>
/// Utility to check if GitHub Copilot CLI is installed and available.
/// </summary>
public class CopilotAvailabilityChecker(
    ILogger<CopilotAvailabilityChecker> logger,
    AppConfig appConfig)
{
    private readonly ILogger<CopilotAvailabilityChecker> logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly AppConfig appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));

    /// <summary>
    /// Check if GitHub Copilot CLI is installed and authenticated.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Availability result with details.</returns>
    public async Task<CopilotAvailabilityResult> CheckAvailabilityAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if Copilot is enabled in configuration
            if (!appConfig.Copilot.Enabled)
            {
                logger.LogDebug("Copilot is disabled in configuration");
                return new CopilotAvailabilityResult(
                    IsAvailable: false,
                    IsCliInstalled: false,
                    IsAuthenticated: false,
                    CliVersion: null,
                    ErrorMessage: "Copilot is disabled in configuration. Set 'copilot.enabled' to true in config.json.");
            }

            // Always auto-detect copilot CLI
            const string cliCommand = "copilot";

            // Check if CLI is installed by getting version
            var (isInstalled, version) = await CheckCliInstalledAsync(cliCommand, cancellationToken);
            if (!isInstalled)
            {
                logger.LogDebug("GitHub Copilot CLI not found");
                var instructions = CopilotInstallationGuide.GetInstallationInstructions();
                var installHint = instructions.Platform switch
                {
                    "Windows" => "winget install GitHub.Copilot",
                    "macOS" => "brew install copilot-cli",
                    "Linux" => "curl -fsSL https://gh.io/copilot-install | bash",
                    _ => "npm install -g @github/copilot"
                };

                return new CopilotAvailabilityResult(
                    IsAvailable: false,
                    IsCliInstalled: false,
                    IsAuthenticated: false,
                    CliVersion: null,
                    ErrorMessage: $"GitHub Copilot CLI not found. Install with: {installHint}\nFor detailed instructions, run: na copilot --install-guide");
            }

            // Check if authenticated
            var isAuthenticated = await CheckAuthenticationAsync(cancellationToken);
            if (!isAuthenticated)
            {
                logger.LogDebug("GitHub Copilot not authenticated");
                return new CopilotAvailabilityResult(
                    IsAvailable: false,
                    IsCliInstalled: true,
                    IsAuthenticated: false,
                    CliVersion: version,
                    ErrorMessage: "GitHub Copilot not authenticated. Run: copilot auth login");
            }

            logger.LogInformation("GitHub Copilot CLI is available, version: {Version}", version);
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
    /// Check if the Copilot CLI is installed and get its version.
    /// </summary>
    private async Task<(bool IsInstalled, string? Version)> CheckCliInstalledAsync(
        string cliPath,
        CancellationToken cancellationToken)
    {
        try
        {
            // On Windows, copilot may be installed as a .ps1 script (via VS Code Copilot Chat extension)
            // which is not detectable via 'where.exe'. Use PowerShell's Get-Command instead.
            if (OperatingSystem.IsWindows())
            {
                var (found, path) = await CheckCliViaPowerShellAsync(cliPath, cancellationToken);
                if (found)
                {
                    // Try to get the actual version
                    var version = await GetCopilotVersionAsync(cancellationToken);
                    logger.LogDebug("Copilot CLI found at: {Path}", path);
                    return (true, version ?? "installed");
                }
            }

            // Fall back to 'where' (Windows) or 'which' (Unix)
            var whereCommand = OperatingSystem.IsWindows() ? "where" : "which";
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = whereCommand,
                    Arguments = cliPath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
            {
                logger.LogDebug("Copilot CLI found at: {Path}", output.Trim().Split('\n')[0]);
                var version = await GetCopilotVersionAsync(cancellationToken);
                return (true, version ?? "installed");
            }

            logger.LogDebug("Copilot CLI not found via {WhereCommand}", whereCommand);
            return (false, null);
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or FileNotFoundException)
        {
            // CLI not found in PATH
            logger.LogDebug("Copilot CLI not found: {Message}", ex.Message);
            return (false, null);
        }
    }

    /// <summary>
    /// Check for Copilot CLI using PowerShell's Get-Command (finds .ps1 scripts in PATH).
    /// </summary>
    private async Task<(bool Found, string? Path)> CheckCliViaPowerShellAsync(
        string cliPath,
        CancellationToken cancellationToken)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "pwsh",
                    Arguments = $"-NoProfile -NonInteractive -Command \"(Get-Command {cliPath} -ErrorAction SilentlyContinue).Source\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(output))
            {
                return (true, output.Trim());
            }

            // Fall back to Windows PowerShell if pwsh not available
            using var fallbackProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = $"-NoProfile -NonInteractive -Command \"(Get-Command {cliPath} -ErrorAction SilentlyContinue).Source\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            fallbackProcess.Start();
            output = await fallbackProcess.StandardOutput.ReadToEndAsync(cancellationToken);
            await fallbackProcess.WaitForExitAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(output))
            {
                return (true, output.Trim());
            }

            return (false, null);
        }
        catch (Exception ex)
        {
            logger.LogDebug("PowerShell CLI check failed: {Message}", ex.Message);
            return (false, null);
        }
    }

    /// <summary>
    /// Get the Copilot CLI version by running 'copilot --version'.
    /// </summary>
    private async Task<string?> GetCopilotVersionAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Use PowerShell to run copilot --version to handle .ps1 scripts
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = OperatingSystem.IsWindows() ? "pwsh" : "copilot",
                    Arguments = OperatingSystem.IsWindows()
                        ? "-NoProfile -NonInteractive -Command \"copilot --version\""
                        : "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            // Parse version from output (e.g., "0.0.394\nCommit: 3d79feb")
            if (!string.IsNullOrWhiteSpace(output))
            {
                var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length > 0)
                {
                    var versionLine = lines[0].Trim();
                    // Extract just the version number if it contains dots
                    if (versionLine.Contains('.'))
                    {
                        return versionLine;
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            logger.LogDebug("Failed to get Copilot version: {Message}", ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Check if the user is authenticated with GitHub Copilot.
    /// </summary>
    private async Task<bool> CheckAuthenticationAsync(
        CancellationToken cancellationToken)
    {
        // The Copilot CLI uses GitHub CLI (gh) for authentication.
        // Check gh auth status instead of trying the interactive copilot CLI.
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "gh",
                    Arguments = "auth status",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            // gh auth status returns 0 if authenticated
            if (process.ExitCode == 0)
            {
                logger.LogDebug("GitHub CLI is authenticated");
                return true;
            }

            // Check if output mentions logged in
            var allOutput = output + error;
            if (allOutput.Contains("Logged in", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogDebug("GitHub CLI shows logged in status");
                return true;
            }

            logger.LogDebug("GitHub CLI is not authenticated");
            return false;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error checking GitHub CLI authentication, assuming authenticated");
            // Assume authenticated if we can't check - SDK will fail with clear error if not
            return true;
        }
    }
}
