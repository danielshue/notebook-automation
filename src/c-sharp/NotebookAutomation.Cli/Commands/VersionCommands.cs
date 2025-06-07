// <copyright file="VersionCommands.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// <author>Dan Shue</author>
// <summary>
// File: ./src/c-sharp/NotebookAutomation.Cli/Commands/VersionCommands.cs
// Purpose: [TODO: Add file purpose description]
// Created: 2025-06-07
// </summary>
using System.Diagnostics;
using System.Reflection;

using NotebookAutomation.Cli.Utilities;

namespace NotebookAutomation.Cli.Commands;

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

        // Command for displaying detailed version info
        var detailedCommand = new Command("detailed", "Display detailed version information");
        versionCommand.Add(detailedCommand);

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

                var companyName = versionInfo?.CompanyName ?? "Notebook Automation";
                var copyrightInfo = versionInfo?.LegalCopyright ?? "Copyright © 2025";                    // Get the more detailed file version if available, otherwise fall back to assembly version
                var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;
                string fileVersion = versionInfo?.FileVersion ?? assemblyVersion?.ToString() ?? "1.0.0.0";

                AnsiConsoleHelper.WriteInfo($"Notebook Automation v{fileVersion}");
                AnsiConsoleHelper.WriteInfo($"Running on .NET {Environment.Version}");
                AnsiConsoleHelper.WriteInfo($"{companyName}");
                AnsiConsoleHelper.WriteInfo($"Copyright {copyrightInfo}");
            }
            else
            {
                AnsiConsoleHelper.WriteError("Unable to retrieve version information. Entry assembly is null.");
            }
        });

        // Handler for detailed version command
        detailedCommand.SetHandler(() =>
        {
            var versionInfo = VersionHelper.GetVersionInfo();

            AnsiConsoleHelper.WriteHeading("Detailed Version Information");

            foreach (var info in versionInfo)
            {
                AnsiConsoleHelper.WriteKeyValue(info.Key, info.Value);
            }
        });

        rootCommand.Add(versionCommand);
    }
}
