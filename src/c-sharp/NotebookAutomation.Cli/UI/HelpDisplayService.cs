// Licensed under the MIT License. See LICENSE file in the project root for full license information.

namespace NotebookAutomation.Cli.UI;

/// <summary>
/// Service responsible for displaying help information and version details.
/// </summary>
/// <remarks>
/// This service handles all help-related display logic, including custom help formatting,
/// version information display, and coordinating with environment display services.
/// </remarks>
internal class HelpDisplayService
{
    private readonly EnvironmentDisplayService _environmentDisplayService;

    /// <summary>
    /// Initializes a new instance of the <see cref="HelpDisplayService"/> class.
    /// </summary>
    /// <param name="environmentDisplayService">Service for displaying environment information.</param>
    public HelpDisplayService(EnvironmentDisplayService environmentDisplayService)
    {
        _environmentDisplayService = environmentDisplayService ?? throw new ArgumentNullException(nameof(environmentDisplayService));
    }


    /// <summary>
    /// Displays custom help with current environment information.
    /// </summary>
    /// <param name="rootCommand">The root command to display help for.</param>
    /// <param name="configPath">The configuration file path.</param>
    /// <param name="isDebug">Whether debug mode is enabled.</param>
    /// <param name="args">The original command line arguments.</param>
    /// <returns>Task representing the async operation.</returns>
    public async Task DisplayCustomHelpAsync(RootCommand rootCommand, string? configPath, bool isDebug, string[] args)
    {
        // Display description
        Console.WriteLine($"{AnsiColors.BOLD}Description:{AnsiColors.ENDC}");
        Console.WriteLine($"  {rootCommand.Description}");
        Console.WriteLine();

        // Display usage
        Console.WriteLine($"{AnsiColors.BOLD}Usage:{AnsiColors.ENDC}");
        Console.WriteLine($"  na [command] [options]");
        Console.WriteLine();

        // Display current environment
        Console.WriteLine($"{AnsiColors.HEADER}Current Environment:{AnsiColors.ENDC}");
        await _environmentDisplayService.DisplayEnvironmentSettingsAsync(configPath, isDebug, args);
        Console.WriteLine();

        // Get and display options
        Console.WriteLine($"{AnsiColors.BOLD}Options:{AnsiColors.ENDC}");
        foreach (var option in rootCommand.Options)
        {
            var aliases = string.Join(", ", option.Aliases);
            var description = option.Description ?? "";
            Console.WriteLine($"  {aliases,-25} {description}");
        }

        // Add help and version options that are built-in
        Console.WriteLine($"  {"-?, -h, --help",-25} Show help and usage information");
        Console.WriteLine($"  {"--version",-25} Show version information");
        Console.WriteLine();

        // Get and display commands
        Console.WriteLine($"{AnsiColors.BOLD}Commands:{AnsiColors.ENDC}");
        foreach (var command in rootCommand.Subcommands)
        {
            Console.WriteLine($"  {command.Name,-18} {command.Description}");
        }
    }


    /// <summary>
    /// Displays professional version information for the application.
    /// </summary>
    /// <remarks>
    /// Shows version, runtime information, author, and copyright in a well-formatted style.
    /// Used by both --version option and version command for consistency.
    /// </remarks>
    public void ShowVersionInfo()
    {
        try
        {
            // Get version using the new AppVersion record system
            var version = VersionHelper.GetVersion();
            var versionInfo = VersionHelper.GetVersionInfo();

            Assembly? entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly != null)
            {
                // Get version information from the executable for display purposes
                string exePath = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
                var fileVersionInfo = !string.IsNullOrEmpty(exePath)
                    ? FileVersionInfo.GetVersionInfo(exePath)
                    : null;

                // Extract display details with GitVersion information when available
                string displayVersion = version.ToSemanticVersionString();
                string fullVersion = version.ToDisplayString();
                string productName = fileVersionInfo?.ProductName ?? "Notebook Automation";
                string companyName = fileVersionInfo?.CompanyName ?? "Dan Shue";
                string copyrightInfo = fileVersionInfo?.LegalCopyright ?? "Copyright 2025";
                string buildDate = version.BuildDateUtc.ToString("yyyy-MM-dd");

                // Use fallback version information when GitVersion is disabled
                string branch = "main";
                string shortSha = version.Commit;
                string commitDate = version.BuildDateUtc.ToString("yyyy-MM-dd");
                string semVer = version.ToSemanticVersionString();



                // Display professional version information
                Console.WriteLine();
                Console.WriteLine($"Notebook Automation version {fullVersion} ({shortSha})");
                Console.WriteLine("Copyright (C) Dan Shue. All rights reserved.");
                Console.WriteLine();
            }
            else
            {
                AnsiConsoleHelper.WriteError("Unable to retrieve version information.");
            }
        }
        catch (Exception ex)
        {
            AnsiConsoleHelper.WriteError($"Error retrieving version information: {ex.Message}");
        }
    }
}
