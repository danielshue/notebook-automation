// <copyright file="ConfigCommandsResources.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// <author>Dan Shue</author>
// <summary>
// File: ./src/c-sharp/NotebookAutomation.Cli/Commands/ConfigCommandsResources.cs
// Purpose: String resources for ConfigCommands to ensure compliance with StyleCop string literal rules.
// Created: 2025-06-07
// </summary>

namespace NotebookAutomation.Cli.Commands;

/// <summary>
/// Provides string resources for the ConfigCommands class to avoid StyleCop string literal warnings.
/// </summary>
internal static class ConfigCommandsResources
{
    /// <summary>
    /// Message displayed when configuration is null.
    /// </summary>
    public const string ConfigIsNull = "[Config is null]";

    /// <summary>
    /// Header text for the Paths section.
    /// </summary>
    public const string PathsHeader = "== Paths ==";

    /// <summary>
    /// Header text for the Microsoft Graph API section.
    /// </summary>
    public const string MicrosoftGraphHeader = "== Microsoft Graph API ==";

    /// <summary>
    /// Header text for the AI Service section.
    /// </summary>
    public const string AiServiceHeader = "== AI Service ==";

    /// <summary>
    /// Header text for the Video Extensions section.
    /// </summary>
    public const string VideoExtensionsHeader = "== Video Extensions ==";

    /// <summary>
    /// Tip message for updating configuration.
    /// </summary>
    public const string ConfigUpdateTip = "Tip: Use 'config update <key> <value>' to change a setting.";

    /// <summary>
    /// Command example for setting user secrets.
    /// </summary>
    public const string UserSecretsSetExample = "  dotnet user-secrets set \"UserSecrets:OpenAI:ApiKey\" \"your-api-key\" --project src/c-sharp/NotebookAutomation.Cli";

    /// <summary>
    /// Command example for listing user secrets.
    /// </summary>
    public const string UserSecretsListExample = "  dotnet user-secrets list --project src/c-sharp/NotebookAutomation.Cli";

    /// <summary>
    /// Key description for Foundry API key.
    /// </summary>
    public const string FoundryKeyDescription = "  aiservice.foundry.key              - Foundry API key (if using Foundry)";
}
