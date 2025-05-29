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
        public void Register(RootCommand rootCommand)
        {
            var versionCommand = new Command("version", "Display version information");
            versionCommand.SetHandler(() =>
            {
                Assembly? entryAssembly = Assembly.GetEntryAssembly();
                if (entryAssembly != null)
                {
                    var versionInfo = FileVersionInfo.GetVersionInfo(entryAssembly.Location);

                    var companyName = string.Empty;
                    var copyrightInfo = string.Empty;

                    object[] assemblyCompanyAttributes = entryAssembly.GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
                    var companyAttribute = assemblyCompanyAttributes.Length > 0 ? assemblyCompanyAttributes[0] as AssemblyCompanyAttribute : null;
                    companyName = companyAttribute?.Company ?? "Unknown Company";

                    object[] assemblyCopyrightAttributes = entryAssembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
                    var copyrightAttribute = assemblyCopyrightAttributes.Length > 0 ? assemblyCopyrightAttributes[0] as AssemblyCopyrightAttribute : null;
                    copyrightInfo = copyrightAttribute?.Copyright ?? "Unknown Copyright";


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
