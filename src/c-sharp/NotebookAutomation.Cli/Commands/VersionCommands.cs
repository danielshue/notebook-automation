using NotebookAutomation.Cli.Utilities;
using System.CommandLine;

namespace NotebookAutomation.Cli.Commands
{
    /// <summary>
    /// Provides CLI commands for displaying version information about the application.
    /// 
    /// This class registers the 'version' command that shows the application version,
    /// the .NET runtime version, and copyright information.
    /// </summary>
    internal class VersionCommands
    {
        /// <summary>
        /// Registers the 'version' command with the root command.
        /// </summary>
        /// <param name="rootCommand">The root command to add the version command to.</param>
        /// <remarks>
        /// When executed, this command displays:
        /// <list type="bullet">
        /// <item><description>The Notebook Automation version number</description></item>
        /// <item><description>The .NET runtime version</description></item>
        /// <item><description>Copyright information</description></item>
        /// </list>
        /// </remarks>
        public void Register(RootCommand rootCommand)
        {
            var versionCommand = new Command("version", "Display version information");
            versionCommand.SetHandler(() =>
            {
                AnsiConsoleHelper.WriteInfo($"Notebook Automation v1.0.0");
                AnsiConsoleHelper.WriteInfo($"Running on .NET {Environment.Version}");
                AnsiConsoleHelper.WriteInfo($"(c) 2025 Dan Shue");
            });
            rootCommand.Add(versionCommand);
        }
    }
}
