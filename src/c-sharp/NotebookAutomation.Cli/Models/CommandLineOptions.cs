// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Cli.Models;

/// <summary>
/// Data structure representing global command line options.
/// </summary>
/// <param name="ConfigOption">Option for specifying configuration file path.</param>
/// <param name="DebugOption">Option for enabling debug output.</param>
/// <param name="VerboseOption">Option for enabling verbose output.</param>
/// <param name="DryRunOption">Option for simulating actions without making changes.</param>
/// <remarks>
/// This record encapsulates all global command line options that are available
/// across all commands in the CLI application, providing a type-safe way to
/// pass options between services and command handlers.
/// </remarks>
internal record CommandLineOptions(
    Option<string> ConfigOption,
    Option<bool> DebugOption,
    Option<bool> VerboseOption,
    Option<bool> DryRunOption
);
