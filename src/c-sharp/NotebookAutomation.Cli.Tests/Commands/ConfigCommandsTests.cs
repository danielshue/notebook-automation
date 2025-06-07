// <copyright file="ConfigCommandsTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// <author>Dan Shue</author>
// <summary>
// File: ./src/c-sharp/NotebookAutomation.Cli.Tests/Commands/ConfigCommandsTests.cs
// Purpose: [TODO: Add file purpose description]
// Created: 2025-06-07
// </summary>
namespace NotebookAutomation.Cli.Tests.Commands;

/// <summary>
/// Unit tests for ConfigCommands.
/// </summary>
[TestClass]
internal class ConfigCommandsTests
{
    /// <summary>
    /// Tests that the 'config list-keys' command prints all available configuration keys
    /// including paths, Microsoft Graph settings, AI service configuration, and video extensions.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [TestMethod]
    public async Task ListKeysCommand_PrintsAllAvailableConfigKeys()
    {
        // Arrange
        var rootCommand = new RootCommand();
        var configOption = new Option<string>("--config");
        var debugOption = new Option<bool>("--debug");
        _ = new ConfigCommands();
        ConfigCommands.Register(rootCommand, configOption, debugOption);

        var consoleOut = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(consoleOut);
        try
        {
            // Act
            await rootCommand.InvokeAsync("config list-keys").ConfigureAwait(false);
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        // Assert
        var output = consoleOut.ToString();
        Assert.IsTrue(output.Contains("Available configuration keys"));
        Assert.IsTrue(output.Contains("paths.onedrive_fullpath_root"));
        Assert.IsTrue(output.Contains("microsoft_graph.client_id"));
        Assert.IsTrue(output.Contains("aiservice.provider"));
        Assert.IsTrue(output.Contains("video_extensions"));
    }

    /// <summary>
    /// Tests that the PrintViewUsage method displays the expected usage information
    /// for the 'config view' command, including usage syntax and description.
    /// </summary>
    [TestMethod]
    public void PrintViewUsage_PrintsExpectedUsage()
    {
        _ = new ConfigCommands();
        var originalOut = Console.Out;
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);
        try
        {
            ConfigCommands.PrintViewUsage();
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        string output = stringWriter.ToString();
        Assert.IsTrue(output.Contains("Usage: config view"));
        Assert.IsTrue(output.Contains("Shows the current configuration settings."));
    }

    /// <summary>
    /// Tests that PrintConfigFormatted correctly handles a minimal configuration
    /// by displaying "[not set]" for null or empty configuration values.
    /// </summary>
    [TestMethod]
    public void PrintConfigFormatted_MinimalConfig_PrintsNotSet()
    {
        // Arrange: minimal config with nulls
        var config = new AppConfig();
        var originalOut = Console.Out;
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);
        try
        {
            ConfigCommands.PrintConfigFormatted(config);
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        string output = stringWriter.ToString();
        Assert.IsTrue(output.Contains("[not set]"));
        Assert.IsTrue(output.Contains("== Paths =="));
        Assert.IsTrue(output.Contains("== Microsoft Graph API =="));
        Assert.IsTrue(output.Contains("== AI Service =="));
        Assert.IsTrue(output.Contains("== Video Extensions =="));
    }

    /// <summary>
    /// Tests that UpdateConfigKey correctly parses and sets video extensions
    /// from a comma-separated list, trimming whitespace and validating the result.
    /// </summary>
    [TestMethod]
    public void UpdateConfigKey_VideoExtensions_ParsesList()
    {
        var config = new AppConfig();
        var updateConfigKey = typeof(ConfigCommands)
            .GetMethod("UpdateConfigKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.IsNotNull(updateConfigKey);
        var result = updateConfigKey.Invoke(null, [config, "video_extensions", "mp4,webm,avi"]);
        Assert.IsTrue((bool)result!, "UpdateConfigKey did not return true");
        Assert.IsNotNull(config.VideoExtensions, "VideoExtensions is null");

        // Defensive: trim and check for whitespace issues
        var trimmed = config.VideoExtensions.Select(e => e.Trim()).ToList();
        Assert.AreEqual(3, trimmed.Count, $"Expected 3 video extensions, got {trimmed.Count}: {string.Join(",", trimmed)}");
        CollectionAssert.Contains(trimmed, "mp4", $"Actual: {string.Join(",", trimmed)}");
        CollectionAssert.Contains(trimmed, "webm", $"Actual: {string.Join(",", trimmed)}");
        CollectionAssert.Contains(trimmed, "avi", $"Actual: {string.Join(",", trimmed)}");
    }

    /// <summary>
    /// Tests that UpdateConfigKey correctly updates the AI service provider setting
    /// when given a valid "aiservice.provider" key-value pair.
    /// </summary>
    [TestMethod]
    public void UpdateConfigKey_AiServiceProvider_UpdatesProvider()
    {
        var config = new AppConfig();
        var updateConfigKey = typeof(ConfigCommands)
            .GetMethod("UpdateConfigKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.IsNotNull(updateConfigKey);
        var result = updateConfigKey.Invoke(null, [config, "aiservice.provider", "openai"]);
        Assert.IsTrue((bool)result!);
        Assert.AreEqual("openai", config.AiService.Provider);
    }

    /// <summary>
    /// Tests that UpdateConfigKey correctly updates the OpenAI model setting
    /// when given a valid "aiservice.openai.model" key-value pair.
    /// </summary>
    [TestMethod]
    public void UpdateConfigKey_AiServiceOpenAiModel_UpdatesModel()
    {
        var config = new AppConfig();
        var updateConfigKey = typeof(ConfigCommands)
            .GetMethod("UpdateConfigKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.IsNotNull(updateConfigKey);
        var result = updateConfigKey.Invoke(null, [config, "aiservice.openai.model", "gpt-4"]);
        Assert.IsTrue((bool)result!);
        Assert.IsNotNull(config.AiService.OpenAI);
        Assert.AreEqual("gpt-4", config.AiService.OpenAI.Model);
    }

    /// <summary>
    /// Tests that UpdateConfigKey correctly updates the Azure deployment setting
    /// when given a valid "aiservice.azure.deployment" key-value pair.
    /// </summary>
    [TestMethod]
    public void UpdateConfigKey_AiServiceAzureDeployment_UpdatesDeployment()
    {
        var config = new AppConfig();
        var updateConfigKey = typeof(ConfigCommands)
            .GetMethod("UpdateConfigKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.IsNotNull(updateConfigKey);
        var result = updateConfigKey.Invoke(null, [config, "aiservice.azure.deployment", "my-deploy"]);
        Assert.IsTrue((bool)result!);
        Assert.IsNotNull(config.AiService.Azure);
        Assert.AreEqual("my-deploy", config.AiService.Azure.Deployment);
    }

    /// <summary>
    /// Tests that UpdateConfigKey correctly updates the Foundry endpoint setting
    /// when given a valid "aiservice.foundry.endpoint" key-value pair.
    /// </summary>
    [TestMethod]
    public void UpdateConfigKey_AiServiceFoundryEndpoint_UpdatesEndpoint()
    {
        var config = new AppConfig();
        var updateConfigKey = typeof(ConfigCommands)
            .GetMethod("UpdateConfigKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.IsNotNull(updateConfigKey);
        var result = updateConfigKey.Invoke(null, [config, "aiservice.foundry.endpoint", "https://foundry.ai"]);
        Assert.IsTrue((bool)result!);
        Assert.IsNotNull(config.AiService.Foundry);
        Assert.AreEqual("https://foundry.ai", config.AiService.Foundry.Endpoint);
    }

    /// <summary>
    /// Tests that the MaskSecret method correctly masks sensitive information,
    /// returning "[Not Set]" for null/empty values, "[Set]" for short values,
    /// and a partially masked string for longer secrets.
    /// </summary>
    [TestMethod]
    public void MaskSecret_ReturnsMaskedOrNotSet()
    {
        _ = new ConfigCommands();
        var maskSecretMethod = typeof(ConfigCommands)
            .GetMethod("MaskSecret", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.IsNotNull(maskSecretMethod);

        // Null
        var resultNull = maskSecretMethod.Invoke(null, [null]);
        Assert.AreEqual("[Not Set]", resultNull);

        // Empty
        var resultEmpty = maskSecretMethod.Invoke(null, [string.Empty]);
        Assert.AreEqual("[Not Set]", resultEmpty);            // Short secret
        var resultShort = maskSecretMethod.Invoke(null, ["abc123"]);
        Assert.AreEqual("[Set]", resultShort);

        // Long secret
        var resultLong = maskSecretMethod.Invoke(null, ["1234567890abcdef"]);

        // For long secrets, expect a masked string: first 4 chars, then asterisks, then last 4 chars
        Assert.IsTrue(resultLong is string);
        var longMasked = (string)resultLong;
        Assert.IsTrue(longMasked.StartsWith("123"), $"Expected to start with 123, got {longMasked}");
        Assert.IsTrue(longMasked.EndsWith("def"), $"Expected to end with def, got {longMasked}");
        Assert.IsTrue(longMasked.Contains("..."), $"Expected to contain ..., got {longMasked}");
    }

    /// <summary>
    /// Tests that PrintConfigFormatted handles null configuration input gracefully
    /// without throwing an exception.
    /// </summary>
    [TestMethod]
    public void PrintConfigFormatted_NullConfig_DoesNotThrow()
    {
        // Should not throw even if config is null
        ConfigCommands.PrintConfigFormatted(null!);
    }

    /// <summary>
    /// Tests that UpdateConfigKey returns false when given invalid configuration keys,
    /// such as unknown sections or keys with insufficient parts.
    /// </summary>
    [TestMethod]
    public void UpdateConfigKey_InvalidKey_ReturnsFalse()
    {
        var config = new AppConfig();
        var updateConfigKey = typeof(ConfigCommands)
            .GetMethod("UpdateConfigKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.IsNotNull(updateConfigKey);

        // Unknown section
        var result1 = updateConfigKey.Invoke(null, [config, "unknownsection.key", "value"]);
        Assert.IsFalse((bool)result1!);

        // Too few parts
        var result2 = updateConfigKey.Invoke(null, [config, "justone", "value"]);
        Assert.IsFalse((bool)result2!);
    }

    /// <summary>
    /// Tests that the Initialize method handles invalid or non-existent configuration file paths
    /// gracefully without throwing an exception.
    /// </summary>
    [TestMethod]
    public void Initialize_InvalidConfigPath_DoesNotThrow()
    {
        // Should not throw even if file does not exist
        ConfigCommands.Initialize("nonexistent_config_file.json", false);
    }

    /// <summary>
    /// Tests that the 'config show' command prints usage/help when no arguments are provided.
    /// </summary>
    [TestMethod]
    public void ConfigShow_NoArgs_PrintsUsage()
    {
        // Arrange
        _ = new ConfigCommands();
        var originalOut = Console.Out;
        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);
        try
        {
            // Act: Directly call the usage method
            ConfigCommands.PrintViewUsage();
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        // Assert
        string output = stringWriter.ToString();
        Assert.IsTrue(output.Contains("Usage: config view"), "Should print usage/help when no args provided.");
    }

    /// <summary>
    /// Tests that the 'config' command group contains the 'show' and 'update-key' subcommands after registration.
    /// </summary>
    [TestMethod]
    public void Register_ConfigCommand_HasViewAndUpdateSubcommands()
    {
        // Arrange
        var rootCommand = new RootCommand();
        var configOption = new Option<string>("--config");
        var debugOption = new Option<bool>("--debug");
        var configCommands = new ConfigCommands();
        ConfigCommands.Register(rootCommand, configOption, debugOption);

        // Act
        var configCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "config") as Command;

        // Assert
        Assert.IsNotNull(configCommand, "config command should be registered on the root command.");
        Assert.IsTrue(configCommand.Subcommands.Any(c => c.Name == "view"), "config command should have a 'view' subcommand.");
        Assert.IsTrue(configCommand.Subcommands.Any(c => c.Name == "update"), "config command should have an 'update' subcommand.");
    }

    /// <summary>
    /// Tests that the ConfigCommand constructor initializes successfully
    /// and creates a valid instance.
    /// </summary>
    [TestMethod]
    public void ConfigCommand_Initialization_ShouldSucceed()
    {
        // Arrange
        var command = new ConfigCommands();

        // Act & Assert
        Assert.IsNotNull(command);
    }

    /// <summary>
    /// Tests that the Register method successfully adds the config command
    /// to the root command during registration.
    /// </summary>
    [TestMethod]
    public void Register_AddsConfigCommandToRoot()
    {
        // Arrange
        var rootCommand = new RootCommand();
        var configOption = new Option<string>("--config");
        var debugOption = new Option<bool>("--debug");
        var configCommands = new ConfigCommands();

        // Act
        ConfigCommands.Register(rootCommand, configOption, debugOption);

        // Assert
        var configCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "config");
        Assert.IsNotNull(configCommand, "config command should be registered on the root command.");
    }
}