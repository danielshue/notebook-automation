// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace NotebookAutomation.Cli.Services.Copilot;

/// <summary>
/// Provides platform-specific installation guidance for GitHub Copilot CLI.
/// Based on: https://docs.github.com/en/copilot/how-tos/set-up/install-copilot-cli
/// </summary>
public static class CopilotInstallationGuide
{
    /// <summary>
    /// Gets the current operating system platform.
    /// </summary>
    public static string CurrentPlatform => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows"
        : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "macOS"
        : RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "Linux"
        : "Unknown";

    /// <summary>
    /// Gets platform-specific installation instructions.
    /// </summary>
    /// <returns>Installation instructions for the current platform.</returns>
    public static InstallationInstructions GetInstallationInstructions()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return GetWindowsInstructions();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return GetMacOSInstructions();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return GetLinuxInstructions();
        }
        else
        {
            return GetGenericInstructions();
        }
    }

    /// <summary>
    /// Gets Windows-specific installation instructions.
    /// </summary>
    private static InstallationInstructions GetWindowsInstructions() => new(
        Platform: "Windows",
        PrerequisiteCommands: [
            "# Check if WinGet is available",
            "winget --version"
        ],
        InstallCommands: [
            "# Install GitHub Copilot CLI via WinGet (recommended)",
            "winget install GitHub.Copilot",
            "",
            "# Alternative: Install via npm",
            "npm install -g @githubnext/github-copilot-cli"
        ],
        AuthenticationCommands: [
            "# Authenticate with GitHub (required)",
            "gh auth login --scopes copilot",
            "",
            "# Verify authentication",
            "gh auth status"
        ],
        VerifyCommands: [
            "# Verify installation",
            "copilot --version"
        ],
        Notes: [
            "WinGet is the recommended installation method for Windows.",
            "The CLI will be installed to your user app data directory.",
            "Restart your terminal after installation to refresh PATH.",
            "GitHub Copilot requires an active Copilot subscription."
        ],
        DocumentationUrl: "https://docs.github.com/en/copilot/how-tos/set-up/install-copilot-cli"
    );

    /// <summary>
    /// Gets macOS-specific installation instructions.
    /// </summary>
    private static InstallationInstructions GetMacOSInstructions() => new(
        Platform: "macOS",
        PrerequisiteCommands: [
            "# Check if Homebrew is available",
            "brew --version",
            "",
            "# Install Homebrew if needed",
            "/bin/bash -c \"$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)\""
        ],
        InstallCommands: [
            "# Install GitHub CLI first (if not installed)",
            "brew install gh",
            "",
            "# Install GitHub Copilot extension",
            "gh extension install github/gh-copilot",
            "",
            "# Alternative: Install standalone CLI via npm",
            "npm install -g @githubnext/github-copilot-cli"
        ],
        AuthenticationCommands: [
            "# Authenticate with GitHub (required)",
            "gh auth login --scopes copilot",
            "",
            "# Verify authentication",
            "gh auth status"
        ],
        VerifyCommands: [
            "# Verify installation (if using gh extension)",
            "gh copilot --version",
            "",
            "# Verify installation (if using standalone)",
            "copilot --version"
        ],
        Notes: [
            "Homebrew is the recommended package manager for macOS.",
            "You can use either the GitHub CLI extension or the standalone CLI.",
            "The CLI uses your existing GitHub authentication.",
            "GitHub Copilot requires an active Copilot subscription."
        ],
        DocumentationUrl: "https://docs.github.com/en/copilot/how-tos/set-up/install-copilot-cli"
    );

    /// <summary>
    /// Gets Linux-specific installation instructions.
    /// </summary>
    private static InstallationInstructions GetLinuxInstructions() => new(
        Platform: "Linux",
        PrerequisiteCommands: [
            "# Check if GitHub CLI is available",
            "gh --version",
            "",
            "# Install GitHub CLI - Debian/Ubuntu",
            "sudo apt update && sudo apt install gh",
            "",
            "# Install GitHub CLI - Fedora",
            "sudo dnf install gh",
            "",
            "# Install GitHub CLI - Arch Linux",
            "sudo pacman -S github-cli"
        ],
        InstallCommands: [
            "# Install GitHub Copilot extension",
            "gh extension install github/gh-copilot",
            "",
            "# Alternative: Install standalone CLI via npm",
            "npm install -g @githubnext/github-copilot-cli"
        ],
        AuthenticationCommands: [
            "# Authenticate with GitHub (required)",
            "gh auth login --scopes copilot",
            "",
            "# Verify authentication",
            "gh auth status"
        ],
        VerifyCommands: [
            "# Verify installation (if using gh extension)",
            "gh copilot --version",
            "",
            "# Verify installation (if using standalone)",
            "copilot --version"
        ],
        Notes: [
            "Use your distribution's package manager to install GitHub CLI.",
            "The npm method works on any Linux distribution with Node.js installed.",
            "GitHub Copilot requires an active Copilot subscription."
        ],
        DocumentationUrl: "https://docs.github.com/en/copilot/how-tos/set-up/install-copilot-cli"
    );

    /// <summary>
    /// Gets generic installation instructions.
    /// </summary>
    private static InstallationInstructions GetGenericInstructions() => new(
        Platform: "Generic",
        PrerequisiteCommands: [
            "# Ensure Node.js and npm are installed",
            "node --version",
            "npm --version"
        ],
        InstallCommands: [
            "# Install GitHub Copilot CLI via npm",
            "npm install -g @githubnext/github-copilot-cli"
        ],
        AuthenticationCommands: [
            "# Authenticate with GitHub (requires GitHub CLI)",
            "gh auth login --scopes copilot",
            "",
            "# Verify authentication",
            "gh auth status"
        ],
        VerifyCommands: [
            "# Verify installation",
            "copilot --version"
        ],
        Notes: [
            "The npm installation method works on most platforms.",
            "GitHub CLI (gh) is recommended for authentication.",
            "GitHub Copilot requires an active Copilot subscription."
        ],
        DocumentationUrl: "https://docs.github.com/en/copilot/how-tos/set-up/install-copilot-cli"
    );

    /// <summary>
    /// Attempts to install the Copilot CLI using the platform's package manager.
    /// </summary>
    /// <param name="logger">Logger for output.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if installation was successful.</returns>
    public static async Task<bool> TryInstallAsync(
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return await TryInstallWindowsAsync(logger, cancellationToken);
        }

        logger.LogWarning("Automatic installation is only supported on Windows. Please follow the manual instructions.");
        return false;
    }

    /// <summary>
    /// Attempts to install the Copilot CLI on Windows using WinGet.
    /// </summary>
    private static async Task<bool> TryInstallWindowsAsync(
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Attempting to install GitHub Copilot CLI via WinGet...");

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "winget",
                    Arguments = "install GitHub.Copilot --accept-source-agreements --accept-package-agreements",
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

            if (process.ExitCode == 0)
            {
                logger.LogInformation("GitHub Copilot CLI installed successfully");
                logger.LogInformation("Please restart your terminal to refresh PATH");
                return true;
            }

            // Exit code -1978335189 means already installed
            if (process.ExitCode == -1978335189 || output.Contains("No available upgrade", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogInformation("GitHub Copilot CLI is already installed");
                return true;
            }

            logger.LogWarning("WinGet installation returned exit code {ExitCode}", process.ExitCode);
            if (!string.IsNullOrWhiteSpace(error))
            {
                logger.LogWarning("WinGet error output: {Error}", error);
            }

            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to install GitHub Copilot CLI via WinGet");
            return false;
        }
    }

    /// <summary>
    /// Displays the installation instructions to the console using Spectre.Console.
    /// </summary>
    public static void DisplayInstructions()
    {
        var instructions = GetInstallationInstructions();

        AnsiConsole.MarkupLine($"[bold cyan]GitHub Copilot CLI Installation Guide - {instructions.Platform}[/]");
        AnsiConsole.WriteLine();

        // Prerequisites
        if (instructions.PrerequisiteCommands.Length > 0)
        {
            AnsiConsole.MarkupLine("[bold yellow]Prerequisites:[/]");
            foreach (var cmd in instructions.PrerequisiteCommands)
            {
                if (cmd.StartsWith("#"))
                {
                    AnsiConsole.MarkupLine($"[dim]{cmd.EscapeMarkup()}[/]");
                }
                else if (!string.IsNullOrWhiteSpace(cmd))
                {
                    AnsiConsole.MarkupLine($"  [cyan]{cmd.EscapeMarkup()}[/]");
                }
                else
                {
                    AnsiConsole.WriteLine();
                }
            }

            AnsiConsole.WriteLine();
        }

        // Install commands
        AnsiConsole.MarkupLine("[bold yellow]Installation:[/]");
        foreach (var cmd in instructions.InstallCommands)
        {
            if (cmd.StartsWith("#"))
            {
                AnsiConsole.MarkupLine($"[dim]{cmd.EscapeMarkup()}[/]");
            }
            else if (!string.IsNullOrWhiteSpace(cmd))
            {
                AnsiConsole.MarkupLine($"  [cyan]{cmd.EscapeMarkup()}[/]");
            }
            else
            {
                AnsiConsole.WriteLine();
            }
        }

        AnsiConsole.WriteLine();

        // Authentication
        AnsiConsole.MarkupLine("[bold yellow]Authentication:[/]");
        foreach (var cmd in instructions.AuthenticationCommands)
        {
            if (cmd.StartsWith("#"))
            {
                AnsiConsole.MarkupLine($"[dim]{cmd.EscapeMarkup()}[/]");
            }
            else if (!string.IsNullOrWhiteSpace(cmd))
            {
                AnsiConsole.MarkupLine($"  [cyan]{cmd.EscapeMarkup()}[/]");
            }
            else
            {
                AnsiConsole.WriteLine();
            }
        }

        AnsiConsole.WriteLine();

        // Verify
        AnsiConsole.MarkupLine("[bold yellow]Verification:[/]");
        foreach (var cmd in instructions.VerifyCommands)
        {
            if (cmd.StartsWith("#"))
            {
                AnsiConsole.MarkupLine($"[dim]{cmd.EscapeMarkup()}[/]");
            }
            else if (!string.IsNullOrWhiteSpace(cmd))
            {
                AnsiConsole.MarkupLine($"  [cyan]{cmd.EscapeMarkup()}[/]");
            }
            else
            {
                AnsiConsole.WriteLine();
            }
        }

        AnsiConsole.WriteLine();

        // Notes
        if (instructions.Notes.Length > 0)
        {
            AnsiConsole.MarkupLine("[bold yellow]Notes:[/]");
            foreach (var note in instructions.Notes)
            {
                AnsiConsole.MarkupLine($"  [dim]• {note.EscapeMarkup()}[/]");
            }

            AnsiConsole.WriteLine();
        }

        // Documentation link
        AnsiConsole.MarkupLine($"[dim]Documentation: [link={instructions.DocumentationUrl}]{instructions.DocumentationUrl}[/][/]");
    }
}

/// <summary>
/// Platform-specific installation instructions.
/// </summary>
/// <param name="Platform">The platform name.</param>
/// <param name="PrerequisiteCommands">Commands to check/install prerequisites.</param>
/// <param name="InstallCommands">Commands to install the CLI.</param>
/// <param name="AuthenticationCommands">Commands to authenticate.</param>
/// <param name="VerifyCommands">Commands to verify the installation.</param>
/// <param name="Notes">Additional notes.</param>
/// <param name="DocumentationUrl">Link to official documentation.</param>
public record InstallationInstructions(
    string Platform,
    string[] PrerequisiteCommands,
    string[] InstallCommands,
    string[] AuthenticationCommands,
    string[] VerifyCommands,
    string[] Notes,
    string DocumentationUrl);
