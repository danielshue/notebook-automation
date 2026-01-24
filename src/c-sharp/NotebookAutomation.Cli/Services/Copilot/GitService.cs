// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace NotebookAutomation.Cli.Services.Copilot;

/// <summary>
/// Service for detecting Git repository information.
/// </summary>
public class GitService : IGitService
{
    private readonly ILogger<GitService> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GitService"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public GitService(ILogger<GitService> logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<bool> IsGitRepositoryAsync(string? path = null)
    {
        var gitRoot = await GetGitRootAsync(path);
        return gitRoot != null;
    }

    /// <inheritdoc/>
    public async Task<string?> GetGitRootAsync(string? path = null)
    {
        try
        {
            var workingDir = path ?? Directory.GetCurrentDirectory();
            
            var psi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "rev-parse --show-toplevel",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDir
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                return null;
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
            {
                return output.Trim();
            }

            return null;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to get Git root");
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<string?> GetCurrentBranchAsync(string? path = null)
    {
        try
        {
            var workingDir = path ?? Directory.GetCurrentDirectory();
            
            var psi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "rev-parse --abbrev-ref HEAD",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDir
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                return null;
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
            {
                return output.Trim();
            }

            return null;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to get current Git branch");
            return null;
        }
    }
}
