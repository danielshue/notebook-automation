// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Cli.Utilities;

/// <summary>
/// Utility class for detecting command line modes from environment variables and arguments.
/// </summary>
/// <remarks>
/// This class provides centralized logic for determining if debug or verbose modes are enabled,
/// checking environment variables first and falling back to command line arguments.
/// </remarks>
internal static class CommandLineModeDetector
{
    /// <summary>
    /// Determines if debug mode is enabled from environment variable or command line arguments.
    /// Environment variable takes precedence over command line arguments.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>True if debug mode should be enabled.</returns>
    public static bool IsDebugModeEnabled(string[] args)
    {
        // Check environment variable first
        var envDebug = Environment.GetEnvironmentVariable("DEBUG");
        if (!string.IsNullOrEmpty(envDebug))
        {
            return envDebug.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                   envDebug.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                   envDebug.Equals("yes", StringComparison.OrdinalIgnoreCase);
        }

        // Fall back to command line arguments
        return args.Contains("--debug") || args.Contains("-d");
    }


    /// <summary>
    /// Determines if verbose mode is enabled from environment variable or command line arguments.
    /// Environment variable takes precedence over command line arguments.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>True if verbose mode should be enabled.</returns>
    public static bool IsVerboseModeEnabled(string[] args)
    {
        // Check environment variable first
        var envVerbose = Environment.GetEnvironmentVariable("VERBOSE");
        if (!string.IsNullOrEmpty(envVerbose))
        {
            return envVerbose.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                   envVerbose.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                   envVerbose.Equals("yes", StringComparison.OrdinalIgnoreCase);
        }

        // Fall back to command line arguments
        return args.Contains("--verbose") || args.Contains("-v");
    }


    /// <summary>
    /// Data structure representing detected command line flags and their sources.
    /// </summary>
    /// <param name="IsDebugEnabled">Whether debug mode is enabled.</param>
    /// <param name="IsVerboseEnabled">Whether verbose mode is enabled.</param>
    /// <param name="DebugSource">Source of debug mode setting.</param>
    /// <param name="VerboseSource">Source of verbose mode setting.</param>
    public record CommandLineFlags(
        bool IsDebugEnabled,
        bool IsVerboseEnabled,
        string DebugSource,
        string VerboseSource
    );


    /// <summary>
    /// Detects all command line flags and their sources in a single operation.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>CommandLineFlags record with detected settings and sources.</returns>
    public static CommandLineFlags DetectFlags(string[] args)
    {
        // Debug detection
        var debugFromEnv = IsEnvironmentVariableEnabled("DEBUG");
        var debugFromArgs = args.Contains("--debug") || args.Contains("-d");
        var isDebugEnabled = debugFromEnv || debugFromArgs;
        var debugSource = debugFromEnv ? "Environment" : debugFromArgs ? "Command Line" : "Disabled";

        // Verbose detection
        var verboseFromEnv = IsEnvironmentVariableEnabled("VERBOSE");
        var verboseFromArgs = args.Contains("--verbose") || args.Contains("-v");
        var isVerboseEnabled = verboseFromEnv || verboseFromArgs;
        var verboseSource = verboseFromEnv ? "Environment" : verboseFromArgs ? "Command Line" : "Disabled";

        return new CommandLineFlags(
            isDebugEnabled,
            isVerboseEnabled,
            debugSource,
            verboseSource
        );
    }


    /// <summary>
    /// Helper method to check if an environment variable is enabled.
    /// </summary>
    /// <param name="variableName">Name of the environment variable.</param>
    /// <returns>True if the environment variable is set to a truthy value.</returns>
    private static bool IsEnvironmentVariableEnabled(string variableName)
    {
        var envValue = Environment.GetEnvironmentVariable(variableName);
        return !string.IsNullOrEmpty(envValue) &&
               (envValue.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                envValue.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                envValue.Equals("yes", StringComparison.OrdinalIgnoreCase));
    }
}
