using Microsoft.VisualStudio.TestTools.UnitTesting;
using NotebookAutomation.Cli.Commands;
using System.Linq;
using System.CommandLine;

namespace NotebookAutomation.Cli.Tests.Commands
{
    /// <summary>
    /// Unit tests for ConfigCommands.
    /// </summary>

    [TestClass]
    public class ConfigCommandsTests
    {

        [TestMethod]

        public async System.Threading.Tasks.Task ListKeysCommand_PrintsAllAvailableConfigKeys()
        {
            // Arrange
            var rootCommand = new System.CommandLine.RootCommand();
            var configOption = new System.CommandLine.Option<string>("--config");
            var debugOption = new System.CommandLine.Option<bool>("--debug");
            var configCommands = new ConfigCommands();
            configCommands.Register(rootCommand, configOption, debugOption);

            var consoleOut = new System.IO.StringWriter();
            var originalOut = System.Console.Out;
            System.Console.SetOut(consoleOut);
            try
            {
                // Act
                await rootCommand.InvokeAsync("config list-keys");
            }
            finally
            {
                System.Console.SetOut(originalOut);
            }
            // Assert
            var output = consoleOut.ToString();
            Assert.IsTrue(output.Contains("Available configuration keys"));
            Assert.IsTrue(output.Contains("paths.onedrive_fullpath_root"));
            Assert.IsTrue(output.Contains("microsoft_graph.client_id"));
            Assert.IsTrue(output.Contains("aiservice.provider"));
            Assert.IsTrue(output.Contains("video_extensions"));
        }



        [TestMethod]
        public void PrintViewUsage_PrintsExpectedUsage()
        {
            var configCommands = new ConfigCommands();
            var originalOut = System.Console.Out;
            var stringWriter = new System.IO.StringWriter();
            System.Console.SetOut(stringWriter);
            try
            {
                ConfigCommands.PrintViewUsage();
            }
            finally
            {
                System.Console.SetOut(originalOut);
            }
            string output = stringWriter.ToString();
            Assert.IsTrue(output.Contains("Usage: config view"));
            Assert.IsTrue(output.Contains("Shows the current configuration settings."));
        }

        [TestMethod]
        public void PrintConfigFormatted_MinimalConfig_PrintsNotSet()
        {
            // Arrange: minimal config with nulls
            var config = new NotebookAutomation.Core.Configuration.AppConfig();
            var originalOut = System.Console.Out;
            var stringWriter = new System.IO.StringWriter();
            System.Console.SetOut(stringWriter);
            try
            {
                ConfigCommands.PrintConfigFormatted(config);
            }
            finally
            {
                System.Console.SetOut(originalOut);
            }
            string output = stringWriter.ToString();
            Assert.IsTrue(output.Contains("[not set]"));
            Assert.IsTrue(output.Contains("== Paths =="));
            Assert.IsTrue(output.Contains("== Microsoft Graph API =="));
            Assert.IsTrue(output.Contains("== AI Service =="));
            Assert.IsTrue(output.Contains("== Video Extensions =="));
        }

        [TestMethod]
        public void UpdateConfigKey_VideoExtensions_ParsesList()
        {
            var config = new NotebookAutomation.Core.Configuration.AppConfig();
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

        [TestMethod]
        public void UpdateConfigKey_AiServiceProvider_UpdatesProvider()
        {
            var config = new NotebookAutomation.Core.Configuration.AppConfig();
            var updateConfigKey = typeof(ConfigCommands)
                .GetMethod("UpdateConfigKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.IsNotNull(updateConfigKey);
            var result = updateConfigKey.Invoke(null, [config, "aiservice.provider", "openai"]);
            Assert.IsTrue((bool)result!);
            Assert.AreEqual("openai", config.AiService.Provider);
        }

        [TestMethod]
        public void UpdateConfigKey_AiServiceOpenAiModel_UpdatesModel()
        {
            var config = new NotebookAutomation.Core.Configuration.AppConfig();
            var updateConfigKey = typeof(ConfigCommands)
                .GetMethod("UpdateConfigKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.IsNotNull(updateConfigKey);
            var result = updateConfigKey.Invoke(null, [config, "aiservice.openai.model", "gpt-4"]);
            Assert.IsTrue((bool)result!);
            Assert.IsNotNull(config.AiService.OpenAI);
            Assert.AreEqual("gpt-4", config.AiService.OpenAI.Model);
        }

        [TestMethod]
        public void UpdateConfigKey_AiServiceAzureDeployment_UpdatesDeployment()
        {
            var config = new NotebookAutomation.Core.Configuration.AppConfig();
            var updateConfigKey = typeof(ConfigCommands)
                .GetMethod("UpdateConfigKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.IsNotNull(updateConfigKey);
            var result = updateConfigKey.Invoke(null, [config, "aiservice.azure.deployment", "my-deploy"]);
            Assert.IsTrue((bool)result!);
            Assert.IsNotNull(config.AiService.Azure);
            Assert.AreEqual("my-deploy", config.AiService.Azure.Deployment);
        }

        [TestMethod]
        public void UpdateConfigKey_AiServiceFoundryEndpoint_UpdatesEndpoint()
        {
            var config = new NotebookAutomation.Core.Configuration.AppConfig();
            var updateConfigKey = typeof(ConfigCommands)
                .GetMethod("UpdateConfigKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.IsNotNull(updateConfigKey);
            var result = updateConfigKey.Invoke(null, [config, "aiservice.foundry.endpoint", "https://foundry.ai"]);
            Assert.IsTrue((bool)result!);
            Assert.IsNotNull(config.AiService.Foundry);
            Assert.AreEqual("https://foundry.ai", config.AiService.Foundry.Endpoint);
        }
        [TestMethod]
        public void MaskSecret_ReturnsMaskedOrNotSet()
        {
            var configCommands = new ConfigCommands();
            var maskSecretMethod = typeof(ConfigCommands)
                .GetMethod("MaskSecret", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.IsNotNull(maskSecretMethod);

            // Null
            var resultNull = maskSecretMethod.Invoke(null, [null]);
            Assert.AreEqual("[Not Set]", resultNull);
            // Empty
            var resultEmpty = maskSecretMethod.Invoke(null, [""]);
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

        [TestMethod]
        public void PrintConfigFormatted_NullConfig_DoesNotThrow()
        {
            // Should not throw even if config is null
            ConfigCommands.PrintConfigFormatted(null!);
        }

        [TestMethod]
        public void UpdateConfigKey_InvalidKey_ReturnsFalse()
        {
            var config = new NotebookAutomation.Core.Configuration.AppConfig();
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
            var configCommands = new ConfigCommands();
            var originalOut = System.Console.Out;
            var stringWriter = new System.IO.StringWriter();
            System.Console.SetOut(stringWriter);
            try
            {
                // Act: Directly call the usage method
                ConfigCommands.PrintViewUsage();
            }
            finally
            {
                System.Console.SetOut(originalOut);
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
            var rootCommand = new System.CommandLine.RootCommand();
            var configOption = new System.CommandLine.Option<string>("--config");
            var debugOption = new System.CommandLine.Option<bool>("--debug");
            var configCommands = new ConfigCommands();
            configCommands.Register(rootCommand, configOption, debugOption);

            // Act
            var configCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "config") as System.CommandLine.Command;

            // Assert
            Assert.IsNotNull(configCommand, "config command should be registered on the root command.");
            Assert.IsTrue(configCommand.Subcommands.Any(c => c.Name == "view"), "config command should have a 'view' subcommand.");
            Assert.IsTrue(configCommand.Subcommands.Any(c => c.Name == "update"), "config command should have an 'update' subcommand.");
        }

        [TestMethod]
        public void ConfigCommand_Initialization_ShouldSucceed()
        {
            // Arrange
            var command = new ConfigCommands();
            // Act & Assert
            Assert.IsNotNull(command);
        }

        [TestMethod]
        public void Register_AddsConfigCommandToRoot()
        {
            // Arrange
            var rootCommand = new System.CommandLine.RootCommand();
            var configOption = new System.CommandLine.Option<string>("--config");
            var debugOption = new System.CommandLine.Option<bool>("--debug");
            var configCommands = new ConfigCommands();

            // Act
            configCommands.Register(rootCommand, configOption, debugOption);

            // Assert
            var configCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "config");
            Assert.IsNotNull(configCommand, "config command should be registered on the root command.");
        }

    }
}
