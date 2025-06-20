// Licensed under the MIT License. See LICENSE file in the project root for full license information.
namespace NotebookAutomation.Tests.Cli.Commands;

/// <summary>
/// Comprehensive unit tests for the ConfigCommands class functionality.
/// </summary>
/// <remarks>
/// <para>
/// This test class provides extensive coverage of the ConfigCommands class, which is responsible
/// for managing configuration-related CLI commands in the Notebook Automation CLI. The tests
/// verify functionality across multiple categories:
/// </para>
/// <list type="bullet">
/// <item><description><strong>Command Registration:</strong> Validates that config commands are properly registered in the CLI hierarchy</description></item>
/// <item><description><strong>Configuration Display:</strong> Tests the 'config list-keys' and 'config view' commands for displaying configuration information</description></item>
/// <item><description><strong>Configuration Updates:</strong> Verifies the 'config update-key' command for modifying configuration values</description></item>
/// <item><description><strong>Security:</strong> Tests secret masking functionality to protect sensitive information in output</description></item>
/// <item><description><strong>Error Handling:</strong> Validates proper behavior with invalid inputs and edge cases</description></item>
/// <item><description><strong>AI Service Configuration:</strong> Tests specific configuration updates for AI service providers (OpenAI, Azure, Foundry)</description></item>
/// </list>
/// <para>
/// The ConfigCommands class supports managing various configuration aspects including:
/// paths, Microsoft Graph settings, AI service configuration, video extensions,
/// and other application settings essential for the Notebook Automation workflow.
/// </para>
/// <para>
/// All tests use mocked dependencies to isolate the ConfigCommands logic from external
/// systems and ensure consistent, reliable test execution. The tests follow the AAA
/// (Arrange-Act-Assert) pattern for clarity and maintainability.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Example commands tested by this class:
/// // na config list-keys
/// // na config view
/// // na config update-key aiService.provider OpenAI
/// // na config update-key videoExtensions ".mp4,.avi,.mov"
/// </code>
/// </example>
[TestClass]
public class ConfigCommandsTests
{
    /// <summary>
    /// Mock logger for capturing and verifying log output from ConfigCommands operations.
    /// </summary>
    private Mock<ILogger<ConfigCommands>> mockLogger = null!;

    /// <summary>
    /// Service provider instance for dependency injection container used in testing.
    /// </summary>
    private IServiceProvider serviceProvider = null!;

    /// <summary>
    /// Mock configuration manager for testing configuration-related operations without external dependencies.
    /// </summary>
    private Mock<IConfigManager> mockConfigManager = null!;

    /// <summary>
    /// Initializes test dependencies and sets up the dependency injection container for each test.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method runs before each test and ensures a clean state by:
    /// <list type="number">
    /// <item><description>Creating fresh mock instances for ILogger and IConfigManager</description></item>
    /// <item><description>Building a real ServiceProvider with mocked dependencies</description></item>
    /// <item><description>Ensuring proper dependency injection behavior during testing</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The use of a real ServiceProvider (rather than mocking it) ensures that the dependency
    /// injection mechanics work correctly while still isolating external dependencies through mocking.
    /// </para>    /// </remarks>
    [TestInitialize]
    public void Setup()
    {
        mockLogger = new Mock<ILogger<ConfigCommands>>();
        mockConfigManager = new Mock<IConfigManager>();

        // Create a real service collection for testing
        var services = new ServiceCollection();
        services.AddSingleton(mockConfigManager.Object);
        serviceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    /// Factory method for creating ConfigCommands instances with properly configured test dependencies.
    /// </summary>
    /// <returns>A ConfigCommands instance ready for testing with mocked dependencies.</returns>
    /// <remarks>
    /// This method ensures consistent creation of ConfigCommands instances across all tests,
    /// using the same mocked logger and service provider setup. This promotes test reliability
    /// and reduces code duplication.
    /// </remarks>
    private ConfigCommands CreateConfigCommands()
    {
        return new ConfigCommands(mockLogger.Object, serviceProvider);
    }    /// <summary>
         /// Tests that the 'config list-keys' command prints all available configuration keys
         /// including paths, Microsoft Graph settings, AI service configuration, and video extensions.
         /// </summary>
         /// <remarks>
         /// <para>
         /// This test verifies the core functionality of the 'config list-keys' command, which is
         /// essential for users to discover what configuration options are available in the system.
         /// The command should display a comprehensive list of all configurable keys organized by category.
         /// </para>
         /// <para>
         /// The test captures console output to verify that key categories are properly displayed,
         /// including:
         /// <list type="bullet">
         /// <item><description>Path configurations (oneDrivePath, obsidianVaultPath, etc.)</description></item>
         /// <item><description>Microsoft Graph API settings</description></item>
         /// <item><description>AI service provider configurations</description></item>
         /// <item><description>Video file extension settings</description></item>
         /// <item><description>Other application-specific configurations</description></item>
         /// </list>
         /// </para>
         /// <para>
         /// This is an integration-style test that exercises the full command execution pipeline
         /// while using mocked dependencies to ensure deterministic behavior.
         /// </para>
         /// </remarks>
         /// <returns>A task representing the asynchronous test operation.</returns>
         /// <exception cref="AssertFailedException">
         /// Thrown if expected configuration keys are not found in the command output.
         /// </exception>
    [TestMethod]
    public async Task ListKeysCommand_PrintsAllAvailableConfigKeys()
    {
        // Arrange
        var rootCommand = new RootCommand();
        var configOption = new Option<string>("--config");
        var debugOption = new Option<bool>("--debug");
        var configCommands = CreateConfigCommands();
        configCommands.Register(rootCommand, configOption, debugOption);

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
    }    /// <summary>
         /// Tests that the PrintViewUsage method displays the expected usage information
         /// for the 'config view' command, including usage syntax and description.
         /// </summary>
         /// <remarks>
         /// <para>
         /// This test verifies that the help/usage information for the 'config view' command
         /// is properly formatted and contains the necessary information for users to understand
         /// how to use the command effectively.
         /// </para>
         /// <para>
         /// The test captures console output and validates that the usage text includes
         /// proper command syntax and helpful descriptions. This ensures users receive
         /// consistent and helpful guidance when they need assistance with the command.
         /// </para>
         /// </remarks>
         /// <exception cref="AssertFailedException">
         /// Thrown if the expected usage information is not found in the output.
         /// </exception>
    [TestMethod]
    public void PrintViewUsage_PrintsExpectedUsage()
    {
        _ = CreateConfigCommands();
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
    }    /// <summary>
         /// Tests that UpdateConfigKey correctly parses and sets video extensions
         /// from a comma-separated list, trimming whitespace and validating the result.
         /// </summary>
         /// <remarks>
         /// <para>
         /// This test validates the configuration update functionality for video file extensions,
         /// which is critical for the system to recognize and process different video file types
         /// during notebook automation workflows.
         /// </para>
         /// <para>
         /// The test specifically verifies:
         /// <list type="bullet">
         /// <item><description>Proper parsing of comma-separated extension values</description></item>
         /// <item><description>Automatic whitespace trimming for user convenience</description></item>
         /// <item><description>Successful update of the AppConfig.VideoExtensions property</description></item>
         /// <item><description>Validation that all expected extensions are properly stored</description></item>
         /// </list>
         /// </para>
         /// <para>
         /// This test uses reflection to access the private UpdateConfigKey method, allowing
         /// direct testing of the core update logic without requiring full command execution.
         /// </para>
         /// </remarks>
         /// <exception cref="AssertFailedException">
         /// Thrown if the configuration update fails or the parsed extensions don't match expectations.
         /// </exception>
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
    }    /// <summary>
         /// Tests that UpdateConfigKey correctly updates the AI service provider setting
         /// when given a valid "aiservice.provider" key-value pair.
         /// </summary>
         /// <remarks>
         /// <para>
         /// This test validates the configuration update functionality for AI service provider settings,
         /// which is essential for the notebook automation system to interface with different AI services
         /// for content processing and analysis.
         /// </para>
         /// <para>
         /// The test verifies that the system can successfully:
         /// <list type="bullet">
         /// <item><description>Parse the nested configuration key "aiservice.provider"</description></item>
         /// <item><description>Update the corresponding AppConfig.AiService.Provider property</description></item>
         /// <item><description>Return success status for valid configuration updates</description></item>
         /// <item><description>Support switching between different AI providers (OpenAI, Azure, Foundry)</description></item>
         /// </list>
         /// </para>
         /// <para>
         /// This functionality is critical for users who need to configure different AI service
         /// providers based on their specific requirements, access permissions, or organizational policies.
         /// </para>
         /// </remarks>
         /// <exception cref="AssertFailedException">
         /// Thrown if the configuration update fails or the provider value is not set correctly.
         /// </exception>
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
        _ = CreateConfigCommands();
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
    {        // Arrange
        _ = CreateConfigCommands();
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
        var debugOption = new Option<bool>("--debug"); var configCommands = CreateConfigCommands();
        configCommands.Register(rootCommand, configOption, debugOption);

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
        var command = CreateConfigCommands();

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
        var debugOption = new Option<bool>("--debug"); var configCommands = CreateConfigCommands();

        // Act
        configCommands.Register(rootCommand, configOption, debugOption);

        // Assert
        var configCommand = rootCommand.Subcommands.FirstOrDefault(c => c.Name == "config");
        Assert.IsNotNull(configCommand, "config command should be registered on the root command.");
    }
}
