using NotebookAutomation.Cli.Utilities;
using System.CommandLine;
using System.Diagnostics;
using System.Reflection;

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
        public static void Register(RootCommand rootCommand)
        {
            var versionCommand = new Command("version", "Display version information");
            versionCommand.SetHandler(() =>
            {
                Assembly? entryAssembly = Assembly.GetEntryAssembly();
                if (entryAssembly != null)
                {

                    // In single-file publish, entryAssembly.Location is empty. Use MainModule.FileName.
                    string exePath = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
                    var versionInfo = !string.IsNullOrEmpty(exePath)
                        ? FileVersionInfo.GetVersionInfo(exePath)
                        : null;

                    var companyName = versionInfo?.CompanyName ?? "Unknown Company";
                    var copyrightInfo = versionInfo?.LegalCopyright ?? "Unknown Copyright";

                    AnsiConsoleHelper.WriteInfo($"Notebook Automation v1.0.0 {Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown"}");
                    AnsiConsoleHelper.WriteInfo($"Running on .NET {Environment.Version}");
                    AnsiConsoleHelper.WriteInfo($"{companyName}");
                    AnsiConsoleHelper.WriteInfo($"Copyright {copyrightInfo}");
                }
                else
                {
                    AnsiConsoleHelper.WriteError("Unable to retrieve version information. Entry assembly is null.");
                }

            });
            rootCommand.Add(versionCommand);
        }
    }
}
